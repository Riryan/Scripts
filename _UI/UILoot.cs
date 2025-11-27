

using UnityEngine;
using UnityEngine.UI;

public partial class UILoot : MonoBehaviour
{
    public static UILoot singleton;
    public GameObject panel;
    public Button goldButton;
    public Text goldText;
    public Color hasGoldColor = Color.yellow;
    public Color emptyGoldColor = Color.gray;
    public UILootSlot itemSlotPrefab;
    public Transform content;

    public UILoot()
    {
        
        
        if (singleton == null) singleton = this;
    }

    void Update()
    {
        Player player = Player.localPlayer;

        
        if (player != null &&
            panel.activeSelf &&
            player.target != null &&
            player.target.health.current == 0 &&
            Utils.ClosestDistance(player, player.target) <= player.interactionRange &&
            player.target is Monster monster &&
            monster.inventory.HasLoot())
        {
            
            goldButton.interactable = monster.gold > 0;
            goldText.text = monster.gold.ToString();
            goldText.color = monster.gold > 0 ? hasGoldColor : emptyGoldColor;
            goldButton.onClick.SetListener(() => {
                player.looting.CmdTakeGold();
            });

            
            
            
            
            
            

            
            
            UIUtils.BalancePrefabs(itemSlotPrefab.gameObject, monster.inventory.slots.Count, content);

            
            for (int i = 0; i < monster.inventory.slots.Count; ++i)
            {
                ItemSlot itemSlot = monster.inventory.slots[i];

                UILootSlot slot = content.GetChild(i).GetComponent<UILootSlot>();
                slot.dragAndDropable.name = i.ToString(); 

                if (itemSlot.amount > 0)
                {
                    
                    slot.button.interactable = player.inventory.CanAdd(itemSlot.item, itemSlot.amount);
                    int icopy = i;
                    slot.button.onClick.SetListener(() => {
                        player.looting.CmdTakeItem(icopy);
                    });
                    
                    
                    slot.tooltip.enabled = true;
                    if (slot.tooltip.IsVisible())
                        slot.tooltip.text = itemSlot.ToolTip();
                    slot.image.color = Color.white;
                    slot.image.sprite = itemSlot.item.image;
                    slot.nameText.text = itemSlot.item.name;
                    slot.amountOverlay.SetActive(itemSlot.amount > 1);
                    slot.amountText.text = itemSlot.amount.ToString();
                }
                else
                {
                    
                    slot.button.interactable = false;
                    slot.button.onClick.RemoveAllListeners();
                    slot.tooltip.enabled = false;
                    slot.tooltip.text = "";
                    slot.image.color = Color.clear;
                    slot.image.sprite = null;
                    slot.nameText.text = "";
                    slot.amountOverlay.SetActive(false);
                }
            }
        }
        else panel.SetActive(false);
    }

    public void Show() { panel.SetActive(true); }
}
