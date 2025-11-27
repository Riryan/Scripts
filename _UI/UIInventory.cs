

using UnityEngine;
using UnityEngine.UI;

public partial class UIInventory : MonoBehaviour
{
    public static UIInventory singleton;
    public KeyCode hotKey = KeyCode.I;
    public GameObject panel;
    public UIInventorySlot slotPrefab;
    public Transform content;
    public Text goldText;
    public UIDragAndDropable trash;
    public Image trashImage;
    public GameObject trashOverlay;
    public Text trashAmountText;

    [Header("Durability Colors")]
    public Color brokenDurabilityColor = Color.red;
    public Color lowDurabilityColor = Color.magenta;
    [Range(0.01f, 0.99f)] public float lowDurabilityThreshold = 0.1f;

    public UIInventory()
    {
        
        
        if (singleton == null) singleton = this;
    }

    void Update()
    {
        Player player = Player.localPlayer;
        if (player != null)
        {
            
            if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                panel.SetActive(!panel.activeSelf);

            
            if (panel.activeSelf)
            {
                
                UIUtils.BalancePrefabs(slotPrefab.gameObject, player.inventory.slots.Count, content);

                
                for (int i = 0; i < player.inventory.slots.Count; ++i)
                {
                    UIInventorySlot slot = content.GetChild(i).GetComponent<UIInventorySlot>();
                    if (slot == null || slot.dragAndDropable == null)
                        continue; // skip broken slot entry
                    slot.dragAndDropable.name = i.ToString(); 
                    ItemSlot itemSlot = player.inventory.slots[i];

                    if (itemSlot.amount > 0)
                    {
                        
                        int icopy = i; 
                        slot.button.onClick.SetListener(() => {
                            if (itemSlot.item.data is UsableItem usable &&
                                usable.CanUse(player, icopy))
                                player.inventory.CmdUseItem(icopy);
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
                        else slot.image.color = Color.white; 
                        slot.image.sprite = itemSlot.item.image;

                        
                        if (itemSlot.item.data is UsableItem usable2)
                        {
                            float cooldown = player.GetItemCooldown(usable2.cooldownCategory);
                            slot.cooldownCircle.fillAmount = usable2.cooldown > 0 ? cooldown / usable2.cooldown : 0;
                        }
                        else slot.cooldownCircle.fillAmount = 0;
                        slot.amountOverlay.SetActive(itemSlot.amount > 1);
                        slot.amountText.text = itemSlot.amount.ToString();
                    }
                    else
                    {
                        
                        slot.button.onClick.RemoveAllListeners();
                        slot.tooltip.enabled = false;
                        slot.dragAndDropable.dragable = false;
                        slot.image.color = Color.clear;
                        slot.image.sprite = null;
                        slot.cooldownCircle.fillAmount = 0;
                        slot.amountOverlay.SetActive(false);
                    }
                }

                
                goldText.text = player.gold.ToString();

                
                trash.dragable = player.inventory.trash.amount > 0;
                if (player.inventory.trash.amount > 0)
                {
                    
                    if (player.inventory.trash.item.maxDurability > 0)
                    {
                        if (player.inventory.trash.item.durability == 0)
                            trashImage.color = brokenDurabilityColor;
                        else if (player.inventory.trash.item.DurabilityPercent() < lowDurabilityThreshold)
                            trashImage.color = lowDurabilityColor;
                        else
                            trashImage.color = Color.white;
                    }
                    else trashImage.color = Color.white; 
                    trashImage.sprite = player.inventory.trash.item.image;

                    trashOverlay.SetActive(player.inventory.trash.amount > 1);
                    trashAmountText.text = player.inventory.trash.amount.ToString();
                }
                else
                {
                    
                    trashImage.color = Color.clear;
                    trashImage.sprite = null;
                    trashOverlay.SetActive(false);
                }
            }
        }
        else panel.SetActive(false);
    }
}
