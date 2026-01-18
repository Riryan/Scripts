using UnityEngine;

namespace uMMORPG
{
    public partial class UISkillbar : MonoBehaviour
    {
        public GameObject panel;
        public UISkillbarSlot slotPrefab;
        public Transform content;

        public Color brokenDurabilityColor = Color.red;
        public Color lowDurabilityColor = Color.magenta;
        [Range(0.01f, 0.99f)] public float lowDurabilityThreshold = 0.1f;

        void Update()
        {
            Player player = Player.localPlayer;
            if (player == null)
            {
                panel.SetActive(false);
                return;
            }

            panel.SetActive(true);
            UIUtils.BalancePrefabs(slotPrefab.gameObject, player.skillbar.slots.Length, content);

            for (int i = 0; i < player.skillbar.slots.Length; ++i)
            {
                SkillbarEntry entry = player.skillbar.slots[i];
                UISkillbarSlot slot = content.GetChild(i).GetComponent<UISkillbarSlot>();
                slot.dragAndDropable.name = i.ToString();

                string pretty = entry.hotKey.ToString().Replace("Alpha", "");
                slot.hotkeyText.text = pretty;

                // =========================
                // SLOT 0 — WEAPON / UNARMED
                // =========================
                if (i == 0)
                {
                    WeaponItem weapon = null;

                    int weaponIndex = player.equipment.GetEquippedWeaponIndex();
                    if (weaponIndex != -1 &&
                        player.equipment.slots[weaponIndex].amount > 0)
                    {
                        weapon = player.equipment.slots[weaponIndex].item.data as WeaponItem;
                    }
                    else
                    {
                        weapon = player.unarmedWeapon; // virtual fallback
                    }

                    ScriptableSkill weaponSkill = weapon != null ? weapon.attackSkill : null;

                    if (weaponSkill != null)
                    {
                        int weaponSkillIndex = player.skills.GetSkillIndexByName(weaponSkill.name);
                        if (weaponSkillIndex != -1)
                        {
                            Skill skill = player.skills.skills[weaponSkillIndex];
                            bool canCast = player.skills.CastCheckSelf(skill);

                            if (!player.movement.CanNavigate())
                                canCast &= player.skills.CastCheckDistance(skill, out Vector3 _);

                            if (Input.GetKeyDown(entry.hotKey) &&
                                !UIUtils.AnyInputActive() &&
                                canCast)
                            {
                                ((PlayerSkills)player.skills).TryUse(weaponSkillIndex);
                            }

                            slot.button.interactable = canCast;
                            slot.button.onClick.SetListener(() =>
                            {
                                ((PlayerSkills)player.skills).TryUse(weaponSkillIndex);
                            });

                            slot.dragAndDropable.dragable = false;
                            slot.tooltip.enabled = true;
                            if (slot.tooltip.IsVisible())
                                slot.tooltip.text = skill.ToolTip();

                            slot.image.color = Color.white;
                            slot.image.sprite = skill.image;

                            float cooldown = skill.CooldownRemaining();
                            slot.cooldownOverlay.SetActive(cooldown > 0);
                            slot.cooldownText.text = cooldown.ToString("F0");
                            slot.cooldownCircle.fillAmount =
                                skill.cooldown > 0 ? cooldown / skill.cooldown : 0;

                            slot.amountOverlay.SetActive(false);
                        }
                    }
                    else
                    {
                        slot.button.onClick.RemoveAllListeners();
                        slot.dragAndDropable.dragable = false;
                        slot.tooltip.enabled = false;
                        slot.image.sprite = null;
                        slot.image.color = Color.clear;
                        slot.cooldownOverlay.SetActive(false);
                        slot.cooldownCircle.fillAmount = 0;
                        slot.amountOverlay.SetActive(false);
                    }

                    continue;
                }

                // =========================
                // ALL OTHER SLOTS (UNCHANGED)
                // =========================

                int skillIndex = player.skills.GetSkillIndexByName(entry.reference);
                int inventoryIndex = player.inventory.GetItemIndexByName(entry.reference);
                int equipmentIndex = player.equipment.GetItemIndexByName(entry.reference);

                if (skillIndex != -1)
                {
                    Skill skill = player.skills.skills[skillIndex];
                    bool canCast = player.skills.CastCheckSelf(skill);

                    if (!player.movement.CanNavigate())
                        canCast &= player.skills.CastCheckDistance(skill, out Vector3 _);

                    if (Input.GetKeyDown(entry.hotKey) &&
                        !UIUtils.AnyInputActive() &&
                        canCast)
                    {
                        ((PlayerSkills)player.skills).TryUse(skillIndex);
                    }

                    slot.button.interactable = canCast;
                    slot.button.onClick.SetListener(() =>
                    {
                        ((PlayerSkills)player.skills).TryUse(skillIndex);
                    });

                    slot.tooltip.enabled = true;
                    if (slot.tooltip.IsVisible())
                        slot.tooltip.text = skill.ToolTip();

                    slot.dragAndDropable.dragable = true;
                    slot.image.color = Color.white;
                    slot.image.sprite = skill.image;

                    float cooldown = skill.CooldownRemaining();
                    slot.cooldownOverlay.SetActive(cooldown > 0);
                    slot.cooldownText.text = cooldown.ToString("F0");
                    slot.cooldownCircle.fillAmount =
                        skill.cooldown > 0 ? cooldown / skill.cooldown : 0;

                    slot.amountOverlay.SetActive(false);
                }
                else if (inventoryIndex != -1)
                {
                    ItemSlot itemSlot = player.inventory.slots[inventoryIndex];

                    if (Input.GetKeyDown(entry.hotKey) && !UIUtils.AnyInputActive())
                        player.inventory.CmdUseItem(inventoryIndex);

                    slot.button.onClick.SetListener(() =>
                    {
                        player.inventory.CmdUseItem(inventoryIndex);
                    });

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
                    else
                        slot.image.color = Color.white;

                    slot.image.sprite = itemSlot.item.image;

                    slot.cooldownOverlay.SetActive(false);
                    slot.amountOverlay.SetActive(itemSlot.amount > 1);
                    slot.amountText.text = itemSlot.amount.ToString();
                }
                else
                {
                    player.skillbar.slots[i].reference = "";
                    slot.button.onClick.RemoveAllListeners();
                    slot.tooltip.enabled = false;
                    slot.dragAndDropable.dragable = false;
                    slot.image.color = Color.clear;
                    slot.image.sprite = null;
                    slot.cooldownOverlay.SetActive(false);
                    slot.cooldownCircle.fillAmount = 0;
                    slot.amountOverlay.SetActive(false);
                }
            }
        }
    }
}
