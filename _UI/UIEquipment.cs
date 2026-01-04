// Note: this script has to be on an always-active UI parent, so that we can
// always react to the hotkey.
using UnityEngine;

namespace uMMORPG
{
    public partial class UIEquipment : MonoBehaviour
    {
        public KeyCode hotKey = KeyCode.U; // 'E' is already used for rotating
        public GameObject panel;
        public UIEquipmentSlot slotPrefab;
        public Transform content;

        [Header("Durability Colors")]
        public Color brokenDurabilityColor = Color.red;
        public Color lowDurabilityColor = Color.magenta;
        [Range(0.01f, 0.99f)] public float lowDurabilityThreshold = 0.1f;

        // ------------------------------------------------------------
        // determine how many equipment slots should be visible in UI
        // (hide customization slots like __Hair, __Eyes, etc.)
        // ------------------------------------------------------------
        int GetVisibleSlotCount(PlayerEquipment equipment)
        {
            for (int i = 0; i < equipment.slotInfo.Length; i++)
            {
                if (equipment.slotInfo[i].requiredCategory.StartsWith("__"))
                    return i;
            }
            return equipment.slots.Count;
        }

        void Update()
        {
            Player player = Player.localPlayer;
            if (player)
            {
                if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                    panel.SetActive(!panel.activeSelf);

                PlayerEquipment equipment = (PlayerEquipment)player.equipment;
                equipment.avatarCamera.enabled = panel.activeSelf;

                if (panel.activeSelf)
                {
                    int visibleSlots = GetVisibleSlotCount(equipment);

                    UIUtils.BalancePrefabs(
                        slotPrefab.gameObject,
                        visibleSlots,
                        content
                    );

                    for (int i = 0; i < visibleSlots; ++i)
                    {
                        UIEquipmentSlot slot = content.GetChild(i).GetComponent<UIEquipmentSlot>();
                        slot.dragAndDropable.name = i.ToString();

                        ItemSlot itemSlot = equipment.slots[i];
                        EquipmentInfo slotInfo = equipment.slotInfo[i];

                        slot.categoryOverlay.SetActive(slotInfo.requiredCategory != "");
                        string overlay = Utils.ParseLastNoun(slotInfo.requiredCategory);
                        slot.categoryText.text = overlay != "" ? overlay : "?";

                        if (itemSlot.amount > 0)
                        {
                            slot.tooltip.enabled = true;
                            if (slot.tooltip.IsVisible())
                                slot.tooltip.text = itemSlot.ToolTip();

                            slot.dragAndDropable.dragable = true;

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

                            if (itemSlot.item.data is UsableItem usable)
                            {
                                float cooldown = player.GetItemCooldown(usable.cooldownCategory);
                                slot.cooldownCircle.fillAmount =
                                    usable.cooldown > 0 ? cooldown / usable.cooldown : 0;
                            }
                            else slot.cooldownCircle.fillAmount = 0;

                            slot.amountOverlay.SetActive(itemSlot.amount > 1);
                            slot.amountText.text = itemSlot.amount.ToString();
                        }
                        else
                        {
                            slot.tooltip.enabled = false;
                            slot.dragAndDropable.dragable = false;
                            slot.image.color = Color.clear;
                            slot.image.sprite = null;
                            slot.cooldownCircle.fillAmount = 0;
                            slot.amountOverlay.SetActive(false);
                        }
                    }
                }
            }
            else panel.SetActive(false);
        }
    }
}
