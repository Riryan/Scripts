using UnityEngine;
using UnityEngine.UI;
using Mirror;

public partial class UIItemMall : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.X;
    public GameObject panel;
    public Button categorySlotPrefab;
    public Transform categoryContent;
    public ScrollRect scrollRect;
    public UIItemMallSlot itemSlotPrefab;
    public Transform itemContent;
    public string buyUrl = "http://unity3d.com/";
    int currentCategory = 0;
    public Text nameText;
    public Text levelText;
    public Text currencyAmountText;
    public Button buyButton;
    public InputField couponInput;
    public Button couponButton;
    public GameObject inventoryPanel;

    void ScrollToBeginning()
    {
        
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1;
    }

    void Update()
    {/*
        Player player = Player.localPlayer;
        if (player != null)
        {
            
            if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                panel.SetActive(!panel.activeSelf);

            
            if (panel.activeSelf)
            {
                
                ScriptableItemMall config = player.itemMall.config;
                UIUtils.BalancePrefabs(categorySlotPrefab.gameObject, config.categories.Length, categoryContent);

                
                for (int i = 0; i < config.categories.Length; ++i)
                {
                    Button button = categoryContent.GetChild(i).GetComponent<Button>();
                    button.interactable = i != currentCategory;
                    button.GetComponentInChildren<Text>().text = player.itemMall.config.categories[i].category;
                    int icopy = i; 
                    button.onClick.SetListener(() => {
                        
                        currentCategory = icopy;
                        ScrollToBeginning();
                    });
                }

                if (config.categories.Length > 0)
                {
                    
                    ScriptableItem[] items = config.categories[currentCategory].items;
                    UIUtils.BalancePrefabs(itemSlotPrefab.gameObject, items.Length, itemContent);

                    
                    for (int i = 0; i < items.Length; ++i)
                    {
                        UIItemMallSlot slot = itemContent.GetChild(i).GetComponent<UIItemMallSlot>();
                        ScriptableItem itemData = items[i];

                        

                        
                        
                        if (slot.tooltip.IsVisible())
                            slot.tooltip.text = new Item(itemData).ToolTip();
                        slot.image.color = Color.white;
                        slot.image.sprite = itemData.image;
                        slot.nameText.text = itemData.name;
                        slot.priceText.text = itemData.itemMallPrice.ToString();
                        slot.unlockButton.interactable = player.health.current > 0 && player.itemMall.coins >= itemData.itemMallPrice;
                        int icopy = i; 
                        slot.unlockButton.onClick.SetListener(() => {
                            player.itemMall.CmdUnlockItem(currentCategory, icopy);
                            inventoryPanel.SetActive(true); 
                        });
                    }
                }

                
                nameText.text = player.name;
                levelText.text = "Lv. " + player.level.current;
                currencyAmountText.text = player.itemMall.coins.ToString();
                buyButton.onClick.SetListener(() => { Application.OpenURL(buyUrl); });
                couponInput.interactable = NetworkTime.time >= player.nextRiskyActionTime;
                couponButton.interactable = NetworkTime.time >= player.nextRiskyActionTime;
                couponButton.onClick.SetListener(() => {
                    if (!string.IsNullOrWhiteSpace(couponInput.text))
                        player.itemMall.CmdEnterCoupon(couponInput.text);
                    couponInput.text = "";
                });
            }
        }
        else panel.SetActive(false);
        */
    }
}
