using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public partial class UISkills : MonoBehaviour
{
    [Header("General")]
    public KeyCode hotKey = KeyCode.R;
    public GameObject panel;
    public Text skillExperienceText;

    [Header("Single List (fallback)")]
    [Tooltip("Used when section containers are not assigned.")]
    public UISkillSlot slotPrefab;
    public Transform content;

    [Header("Sectioned Lists (optional)")]
    public Transform combatContent;
    public Transform craftingContent;
    public Transform gatheringContent;
    public UISkillSlot combatSlotPrefab;
    public UISkillSlot craftingSlotPrefab;
    public UISkillSlot gatheringSlotPrefab;

    enum SkillCategory { Combat, Crafting, Gathering }

    void Update()
    {
        Player player = Player.localPlayer;
        if (!player)
        {
            panel.SetActive(false);
            return;
        }

        if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
            panel.SetActive(!panel.activeSelf);

        if (!panel.activeSelf) return;

        // Render either the sectioned view or the legacy single list (fallback)
        bool hasSectionUI =
            combatContent != null && craftingContent != null && gatheringContent != null &&
            combatSlotPrefab != null && craftingSlotPrefab != null && gatheringSlotPrefab != null;

        if (hasSectionUI)
        {
            RenderSectioned(player);
        }
        else
        {
            RenderSingleList(player);
        }

        // Experience display (if present in your layout)
        if (skillExperienceText != null)
            skillExperienceText.text = ((PlayerSkills)player.skills).skillExperience.ToString();
    }

    void RenderSingleList(Player player)
    {
        if (slotPrefab == null || content == null) return;

        var list = player.skills.skills;
        UIUtils.BalancePrefabs(slotPrefab.gameObject, list.Count, content);
        for (int i = 0; i < list.Count; ++i)
        {
            FillSlot(content.GetChild(i).GetComponent<UISkillSlot>(), player, i);
        }
    }

    void RenderSectioned(Player player)
    {
        // Group indices per category so we can keep original indices for button callbacks
        List<int> combatIdx = new List<int>();
        List<int> craftingIdx = new List<int>();
        List<int> gatheringIdx = new List<int>();

        for (int i = 0; i < player.skills.skills.Count; ++i)
        {
            var s = player.skills.skills[i];
            var cat = DetectCategory(s);
            if (cat == SkillCategory.Crafting) craftingIdx.Add(i);
            else if (cat == SkillCategory.Gathering) gatheringIdx.Add(i);
            else combatIdx.Add(i); // default to Combat
        }

        // Balance and fill each section
        BalanceAndFillSection(combatContent, combatSlotPrefab, player, combatIdx);
        BalanceAndFillSection(craftingContent, craftingSlotPrefab, player, craftingIdx);
        BalanceAndFillSection(gatheringContent, gatheringSlotPrefab, player, gatheringIdx);
    }

    static SkillCategory DetectCategory(Skill s)
    {
        if (s.data == null) return SkillCategory.Combat;
        // Data-driven heuristic based on ScriptableObject type name
        string t = s.data.GetType().Name;
        if (t.IndexOf("Craft", StringComparison.OrdinalIgnoreCase) >= 0)
            return SkillCategory.Crafting;
        if (t.IndexOf("Gather", StringComparison.OrdinalIgnoreCase) >= 0)
            return SkillCategory.Gathering;
        return SkillCategory.Combat;
    }

    void BalanceAndFillSection(Transform parent, UISkillSlot prefab, Player player, List<int> indices)
    {
        if (parent == null || prefab == null) return;
        UIUtils.BalancePrefabs(prefab.gameObject, indices.Count, parent);
        for (int n = 0; n < indices.Count; ++n)
        {
            int skillIndex = indices[n];
            FillSlot(parent.GetChild(n).GetComponent<UISkillSlot>(), player, skillIndex);
        }
    }

    void FillSlot(UISkillSlot slot, Player player, int skillIndex)
    {
        Skill skill = player.skills.skills[skillIndex];
        bool isPassive = skill.data is PassiveSkill;

        if (slot.dragAndDropable != null)
        {
            slot.dragAndDropable.name = skillIndex.ToString();
            slot.dragAndDropable.dragable = skill.level > 0 && !isPassive;
        }

        bool canCast = player.skills.CastCheckSelf(skill);
        if (!player.movement.CanNavigate())
            canCast &= player.skills.CastCheckDistance(skill, out Vector3 _);

        if (slot.button != null)
        {
            slot.button.interactable = skill.level > 0 && !isPassive && canCast;
            int icopy = skillIndex;
            slot.button.onClick.SetListener(() => {
                ((PlayerSkills)player.skills).TryUse(icopy);
            });
        }

        if (slot.image != null)
        {
            if (skill.level > 0)
            {
                slot.image.color = Color.white;
                slot.image.sprite = skill.image;
            }
            else
            {
                // keep whatever placeholder is set in the prefab
            }
        }

        if (slot.descriptionText != null)
            slot.descriptionText.text = skill.ToolTip(showRequirements: skill.level == 0);

        if (slot.upgradeButton != null)
        {
            if (skill.level < skill.maxLevel && ((PlayerSkills)player.skills).CanUpgrade(skill))
            {
                slot.upgradeButton.gameObject.SetActive(true);
                var txt = slot.upgradeButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = skill.level == 0 ? "Learn" : "Upgrade";
                int icopy = skillIndex;
                slot.upgradeButton.onClick.SetListener(() => {
                    ((PlayerSkills)player.skills).CmdUpgrade(icopy);
                });
            }
            else slot.upgradeButton.gameObject.SetActive(false);
        }

        if (slot.cooldownOverlay != null || slot.cooldownText != null || slot.cooldownCircle != null)
        {
            float cooldown = skill.CooldownRemaining();
            if (slot.cooldownOverlay != null) slot.cooldownOverlay.SetActive(skill.level > 0 && cooldown > 0);
            if (slot.cooldownText != null) slot.cooldownText.text = cooldown.ToString("F0");
            if (slot.cooldownCircle != null) slot.cooldownCircle.fillAmount = skill.cooldown > 0 ? cooldown / skill.cooldown : 0;
        }

        if (slot.tooltip != null)
            slot.tooltip.enabled = true;
    }
}
