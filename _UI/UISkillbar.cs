#if !UNITY_SERVER || UNITY_EDITOR
using UnityEngine;

public partial class UISkillbar : MonoBehaviour
{
    public GameObject panel;
    public UISkillbarSlot slotPrefab;
    public Transform content;

    [Header("Durability Colors")]
    public Color brokenDurabilityColor = Color.red;
    public Color lowDurabilityColor = Color.magenta;
    [Range(0.01f, 0.99f)] public float lowDurabilityThreshold = 0.1f;

    [Header("Update Budget")]
    [Tooltip("How many slots to fully refresh per visual tick.")]
    public int slotsPerFrame = 2;

    [Tooltip("Visual refresh interval in seconds (UI only, input is still every frame).")]
    public float visualRefreshInterval = 0.05f;

    // visual update cursor + schedule
    int visualCursor;
    float nextVisualRefreshTime;

    // track which slots have click handlers wired so we don't rebind every frame
    bool[] clickBound;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player == null)
        {
            if (panel != null)
                panel.SetActive(false);
            return;
        }

        panel.SetActive(true);

        EnsureSlots(player);

        // 1) cheap: input/hotkeys every frame
        HandleHotkeys(player);

        // 2) heavier: visuals spread over time
        if (Time.unscaledTime >= nextVisualRefreshTime)
        {
            nextVisualRefreshTime = Time.unscaledTime +
                                    Mathf.Max(0f, visualRefreshInterval);

            int slotCount = player.skillbar.slots.Length;
            if (slotCount == 0)
                return;

            if (slotsPerFrame <= 0)
                slotsPerFrame = 1;

            int count = Mathf.Min(slotsPerFrame, slotCount);
            for (int n = 0; n < count; ++n)
            {
                if (visualCursor >= slotCount)
                    visualCursor = 0;

                RefreshSlotVisual(player, visualCursor);
                visualCursor++;
            }
        }
    }

    // ------------------------------------------------------------------------
    // Setup & wiring
    // ------------------------------------------------------------------------
    void EnsureSlots(Player player)
    {
        int slotCount = player.skillbar.slots.Length;

        UIUtils.BalancePrefabs(slotPrefab.gameObject, slotCount, content);

        if (clickBound == null || clickBound.Length != slotCount)
            clickBound = new bool[slotCount];

        for (int i = 0; i < slotCount; ++i)
        {
            UISkillbarSlot slot = content.GetChild(i).GetComponent<UISkillbarSlot>();
            if (slot == null)
                continue;

            if (slot.dragAndDropable != null)
                slot.dragAndDropable.name = i.ToString();

            if (!clickBound[i] && slot.button != null)
            {
                int indexCopy = i;
                slot.button.onClick.SetListener(() => OnClickSlot(indexCopy));
                clickBound[i] = true;
            }
        }
    }

    // ------------------------------------------------------------------------
    // Input path – always per-frame, but very cheap.
    // ------------------------------------------------------------------------
    void HandleHotkeys(Player player)
    {
        var bar = player.skillbar.slots;
        for (int i = 0; i < bar.Length; ++i)
        {
            SkillbarEntry entry = bar[i];
            if (entry.hotKey == KeyCode.None)
                continue;

            if (!Input.GetKeyDown(entry.hotKey))
                continue;

            if (UIUtils.AnyInputActive())
                continue;

            // try skill first
            int skillIndex = player.skills.GetSkillIndexByName(entry.reference);
            if (skillIndex != -1)
            {
                Skill skill = player.skills.skills[skillIndex];
                bool canCast = player.skills.CastCheckSelf(skill);
                if (!player.movement.CanNavigate())
                    canCast &= player.skills.CastCheckDistance(skill, out Vector3 _);

                if (canCast)
                    ((PlayerSkills)player.skills).TryUse(skillIndex);

                continue;
            }

            // try inventory item
            int inventoryIndex = player.inventory.GetItemIndexByName(entry.reference);
            if (inventoryIndex != -1)
            {
                player.inventory.CmdUseItem(inventoryIndex);
                continue;
            }

            // equipment entries are not usable via hotkey (same as original)
        }
    }

    void OnClickSlot(int index)
    {
        Player player = Player.localPlayer;
        if (player == null)
            return;

        if (index < 0 || index >= player.skillbar.slots.Length)
            return;

        SkillbarEntry entry = player.skillbar.slots[index];

        // skill?
        int skillIndex = player.skills.GetSkillIndexByName(entry.reference);
        if (skillIndex != -1)
        {
            Skill skill = player.skills.skills[skillIndex];
            bool canCast = player.skills.CastCheckSelf(skill);
            if (!player.movement.CanNavigate())
                canCast &= player.skills.CastCheckDistance(skill, out Vector3 _);

            if (canCast)
                ((PlayerSkills)player.skills).TryUse(skillIndex);
            return;
        }

        // inventory item?
        int inventoryIndex = player.inventory.GetItemIndexByName(entry.reference);
        if (inventoryIndex != -1)
        {
            player.inventory.CmdUseItem(inventoryIndex);
            return;
        }

        // equipment entries: click does nothing (drag only), like before
    }

    // ------------------------------------------------------------------------
    // Visual refresh – can be spread over time.
    // ------------------------------------------------------------------------
    void RefreshSlotVisual(Player player, int index)
    {
        SkillbarEntry entry = player.skillbar.slots[index];
        UISkillbarSlot slot = content.GetChild(index).GetComponent<UISkillbarSlot>();
        if (slot == null)
            return;

        // hotkey label
        if (slot.hotkeyText != null)
        {
            string pretty = entry.hotKey.ToString().Replace("Alpha", "");
            if (entry.hotKey == KeyCode.None || pretty.Equals("None"))
                pretty = string.Empty;
            slot.hotkeyText.text = pretty;
        }

        // resolve references once
        int skillIndex      = player.skills.GetSkillIndexByName(entry.reference);
        int inventoryIndex  = player.inventory.GetItemIndexByName(entry.reference);
        int equipmentIndex  = player.equipment.GetItemIndexByName(entry.reference);

        if (skillIndex != -1)
        {
            RefreshSkillSlot(player, slot, skillIndex);
        }
        else if (inventoryIndex != -1)
        {
            RefreshInventorySlot(player, slot, inventoryIndex);
        }
        else if (equipmentIndex != -1)
        {
            RefreshEquipmentSlot(player, slot, equipmentIndex);
        }
        else
        {
            RefreshEmptySlot(player, slot, index);
        }
    }

    void RefreshSkillSlot(Player player, UISkillbarSlot slot, int skillIndex)
    {
        Skill skill = player.skills.skills[skillIndex];

        bool canCast = player.skills.CastCheckSelf(skill);
        if (!player.movement.CanNavigate())
            canCast &= player.skills.CastCheckDistance(skill, out Vector3 _);

        if (slot.button != null)
            slot.button.interactable = canCast;

        if (slot.tooltip != null)
        {
            slot.tooltip.enabled = true;
            if (slot.tooltip.IsVisible())
                slot.tooltip.text = skill.ToolTip();
        }

        if (slot.dragAndDropable != null)
            slot.dragAndDropable.dragable = true;

        if (slot.image != null)
        {
            slot.image.color = Color.white;
            slot.image.sprite = skill.image;
        }

        float cooldown = skill.CooldownRemaining();

        if (slot.cooldownOverlay != null)
            slot.cooldownOverlay.SetActive(cooldown > 0);

        if (slot.cooldownText != null)
            slot.cooldownText.text = cooldown.ToString("F0");

        if (slot.cooldownCircle != null)
            slot.cooldownCircle.fillAmount =
                skill.cooldown > 0 ? cooldown / skill.cooldown : 0f;

        if (slot.amountOverlay != null)
            slot.amountOverlay.SetActive(false);
    }

    void RefreshInventorySlot(Player player, UISkillbarSlot slot, int inventoryIndex)
    {
        ItemSlot itemSlot = player.inventory.slots[inventoryIndex];

        if (slot.button != null)
            slot.button.interactable = true;

        if (slot.tooltip != null)
        {
            slot.tooltip.enabled = true;
            if (slot.tooltip.IsVisible())
                slot.tooltip.text = itemSlot.ToolTip();
        }

        if (slot.dragAndDropable != null)
            slot.dragAndDropable.dragable = true;

        if (slot.image != null)
        {
            if (itemSlot.item.maxDurability > 0)
            {
                if (itemSlot.item.durability == 0)
                    slot.image.color = brokenDurabilityColor;
                else if (itemSlot.item.DurabilityPercent() < lowDurabilityThreshold)
                    slot.image.color = lowDurabilityColor;
                else
                    slot.image.color = Color.white;
            }
            else slot.image.color = Color.white;

            slot.image.sprite = itemSlot.item.image;
        }

        if (slot.cooldownOverlay != null)
            slot.cooldownOverlay.SetActive(false);

        if (slot.cooldownCircle != null)
        {
            if (itemSlot.item.data is UsableItem usable)
            {
                float cooldown = player.GetItemCooldown(usable.cooldownCategory);
                slot.cooldownCircle.fillAmount =
                    usable.cooldown > 0 ? cooldown / usable.cooldown : 0f;
            }
            else slot.cooldownCircle.fillAmount = 0f;
        }

        if (slot.amountOverlay != null)
            slot.amountOverlay.SetActive(itemSlot.amount > 1);

        if (slot.amountText != null)
            slot.amountText.text = itemSlot.amount.ToString();
    }

    void RefreshEquipmentSlot(Player player, UISkillbarSlot slot, int equipmentIndex)
    {
        ItemSlot itemSlot = player.equipment.slots[equipmentIndex];

        // click does nothing, so keep button disabled to avoid fake highlight
        if (slot.button != null)
            slot.button.interactable = false;

        if (slot.tooltip != null)
        {
            slot.tooltip.enabled = true;
            if (slot.tooltip.IsVisible())
                slot.tooltip.text = itemSlot.ToolTip();
        }

        if (slot.dragAndDropable != null)
            slot.dragAndDropable.dragable = true;

        if (slot.image != null)
        {
            if (itemSlot.item.maxDurability > 0)
            {
                if (itemSlot.item.durability == 0)
                    slot.image.color = brokenDurabilityColor;
                else if (itemSlot.item.DurabilityPercent() < lowDurabilityThreshold)
                    slot.image.color = lowDurabilityColor;
                else
                    slot.image.color = Color.white;
            }
            else slot.image.color = Color.white;

            slot.image.sprite = itemSlot.item.image;
        }

        if (slot.cooldownOverlay != null)
            slot.cooldownOverlay.SetActive(false);

        if (slot.cooldownCircle != null)
        {
            if (itemSlot.item.data is UsableItem usable)
            {
                float cooldown = player.GetItemCooldown(usable.cooldownCategory);
                slot.cooldownCircle.fillAmount =
                    usable.cooldown > 0 ? cooldown / usable.cooldown : 0f;
            }
            else slot.cooldownCircle.fillAmount = 0f;
        }

        if (slot.amountOverlay != null)
            slot.amountOverlay.SetActive(itemSlot.amount > 1);

        if (slot.amountText != null)
            slot.amountText.text = itemSlot.amount.ToString();
    }

    void RefreshEmptySlot(Player player, UISkillbarSlot slot, int index)
    {
        // clear reference like original
        player.skillbar.slots[index].reference = "";

        if (slot.button != null)
            slot.button.interactable = false;

        if (slot.tooltip != null)
            slot.tooltip.enabled = false;

        if (slot.dragAndDropable != null)
            slot.dragAndDropable.dragable = false;

        if (slot.image != null)
        {
            slot.image.color = Color.clear;
            slot.image.sprite = null;
        }

        if (slot.cooldownOverlay != null)
            slot.cooldownOverlay.SetActive(false);

        if (slot.cooldownCircle != null)
            slot.cooldownCircle.fillAmount = 0f;

        if (slot.amountOverlay != null)
            slot.amountOverlay.SetActive(false);
    }
}
#endif
