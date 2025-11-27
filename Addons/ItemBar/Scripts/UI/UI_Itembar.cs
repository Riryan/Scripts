using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class UI_Itembar : MonoBehaviour
{
    public GameObject panel;
    public UIItembarSlot slotPrefab;
    public Transform content;
#if !_iMMO2D
    [Header("Durability Colors")]
    public Color brokenDurabilityColor = Color.red;
    public Color lowDurabilityColor = Color.magenta;

    [Range(0.01f, 0.99f)] public float lowDurabilityThreshold = 0.1f;
#endif
	private void Update()
    {
        Player player = Player.localPlayer;
        if (player != null)
        {
            panel.SetActive(true);
            if (player.playerItembar != null)
            {
                // instantiate/destroy enough slots
                UIUtils.BalancePrefabs(slotPrefab.gameObject, player.playerItembar.slots.Length, content);

                // refresh all
                for (int i = 0; i < player.playerItembar.slots.Length; ++i)
                {
                    ItembarEntry entry = player.playerItembar.slots[i];

                    UIItembarSlot slot = content.GetChild(i).GetComponent<UIItembarSlot>();
                    slot.dragAndDropable.name = i.ToString(); // drag and drop index

                    // hotkey overlay (without 'Alpha' etc.)
                    string pretty = entry.hotKey.ToString().Replace("Alpha", "");
                    slot.hotkeyText.text = pretty;

                    // skill, inventory item or equipment item?

                    int inventoryIndex = player.inventory.GetItemIndexByName(entry.reference);
                    int equipmentIndex = player.equipment.GetItemIndexByName(entry.reference);

                    if (inventoryIndex != -1)
                    {
                        ItemSlot itemSlot = player.inventory.slots[inventoryIndex];

                        // hotkey pressed and not typing in any input right now?
                        if (Input.GetKeyDown(entry.hotKey) && !UIUtils.AnyInputActive())
                            player.inventory.CmdUseItem(inventoryIndex);

                        // refresh inventory slot
                        slot.button.onClick.SetListener(() =>
                        {
                            player.inventory.CmdUseItem(inventoryIndex);
                        });

                        // only build tooltip while it's actually shown. this
                        // avoids MASSIVE amounts of StringBuilder allocations.
                        slot.tooltip.enabled = true;
                        if (slot.tooltip.IsVisible())
                            slot.tooltip.text = itemSlot.ToolTip();
                        slot.dragAndDropable.dragable = true;

                        // use durability colors?
#if !_iMMO2D
                        if (itemSlot.item.maxDurability > 0)
                        {
                            if (itemSlot.item.durability == 0)
                                slot.image.color = brokenDurabilityColor;
                            else if (itemSlot.item.DurabilityPercent() < lowDurabilityThreshold)
                                slot.image.color = lowDurabilityColor;
                            else
                                slot.image.color = Color.white;
                        }

                        else slot.image.color = Color.white; // reset for no-durability items
#else
                        slot.image.color = Color.white;

#endif
                        slot.image.sprite = itemSlot.item.image;

                        slot.cooldownOverlay.SetActive(false);
                        // cooldown if usable item
                        if (itemSlot.item.data is UsableItem usable)
                        {
                            float cooldown = player.GetItemCooldown(usable.cooldownCategory);
                            slot.cooldownCircle.fillAmount = usable.cooldown > 0 ? cooldown / usable.cooldown : 0;
                        }
                        else slot.cooldownCircle.fillAmount = 0;
                        slot.amountOverlay.SetActive(itemSlot.amount > 1);
                        slot.amountText.text = itemSlot.amount.ToString();
                    }
                    else if (equipmentIndex != -1)
                    {
                        ItemSlot itemSlot = player.equipment.slots[equipmentIndex];

                        // refresh equipment slot
                        slot.button.onClick.RemoveAllListeners();
                        // only build tooltip while it's actually shown. this
                        // avoids MASSIVE amounts of StringBuilder allocations.
                        slot.tooltip.enabled = true;
                        if (slot.tooltip.IsVisible())
                            slot.tooltip.text = itemSlot.ToolTip();
                        slot.dragAndDropable.dragable = true;

#if !_iMMO2D
                        // use durability colors?
                        if (itemSlot.item.maxDurability > 0)
                        {
                            if (itemSlot.item.durability == 0)
                                slot.image.color = brokenDurabilityColor;
                            else if (itemSlot.item.DurabilityPercent() < lowDurabilityThreshold)
                                slot.image.color = lowDurabilityColor;
                            else
                                slot.image.color = Color.white;
                        }
                        else slot.image.color = Color.white; // reset for no-durability items
#else
                        slot.image.color = Color.white;

#endif
                        slot.image.sprite = itemSlot.item.image;

                        slot.cooldownOverlay.SetActive(false);
                        // cooldown if usable item
                        if (itemSlot.item.data is UsableItem usable)
                        {
                            float cooldown = player.GetItemCooldown(usable.cooldownCategory);
                            slot.cooldownCircle.fillAmount = usable.cooldown > 0 ? cooldown / usable.cooldown : 0;
                        }
                        else slot.cooldownCircle.fillAmount = 0;
                        slot.amountOverlay.SetActive(itemSlot.amount > 1);
                        slot.amountText.text = itemSlot.amount.ToString();
                    }
                    else
                    {
                        // clear the outdated reference
                        // (need to assign directly because it's a struct)
                        player.playerItembar.slots[i].reference = "";

                        // refresh empty slot
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
            else
            {
                Debug.LogWarning("in " + player.className + " prefab, the component PlayerItembar is not configured !");            }
        }
        else panel.SetActive(false);
    }
}

