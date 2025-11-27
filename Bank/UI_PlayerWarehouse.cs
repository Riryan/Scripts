using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class UI_PlayerWarehouse : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public UIInventorySlot slotPrefab;
    public Transform warehouseContent;
    public Transform inventoryContent;
    public Button closeButton;
    public TMP_Text headerText;

    [Header("Labels")]
    public string headerLabel = "Account Warehouse";

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.SetListener(Hide);
    }

    void Update()
    {
        Player player = Player.localPlayer;
        if (!player) return;
        if (!panel.activeSelf) return;

        if (headerText != null)
            headerText.text = headerLabel + " (" + player.account + ")";

        // ----------------- WAREHOUSE -----------------
        if (slotPrefab && warehouseContent)
        {
            UIUtils.BalancePrefabs(slotPrefab.gameObject, player.warehouseSlots.Count, warehouseContent);

            for (int i = 0; i < player.warehouseSlots.Count; ++i)
            {
                Transform child = warehouseContent.GetChild(i);
                if (!child) continue;

                UIInventorySlot slot = child.GetComponent<UIInventorySlot>();
                if (!slot || !slot.button) continue;

                ItemSlot itemSlot = player.warehouseSlots[i];
                bool hasItem = itemSlot.amount > 0;

                // visuals
                if (slot.tooltip) slot.tooltip.enabled = hasItem;
                if (slot.image)
                {
                    slot.image.color = hasItem ? Color.white : Color.clear;
                    slot.image.sprite = hasItem ? itemSlot.item.image : null;
                }
                if (slot.amountOverlay) slot.amountOverlay.SetActive(hasItem && itemSlot.amount > 1);
                if (slot.amountText) slot.amountText.text = hasItem ? itemSlot.amount.ToString() : "";

                // warehouse never uses cooldown circles
                if (slot.cooldownCircle)
                {
                    slot.cooldownCircle.fillAmount = 0;
                    slot.cooldownCircle.gameObject.SetActive(false);
                }

                // no tint highlight on warehouse slots
                if (slot.button != null)
                {
                    slot.button.interactable = hasItem;
                    slot.button.transition = Selectable.Transition.None;
                }

                // drag & drop setup
                if (slot.dragAndDropable)
                {
                    slot.dragAndDropable.dragable = hasItem;
                    slot.dragAndDropable.dropable = true;
                    slot.dragAndDropable.tag = "WarehouseSlot";
                    slot.dragAndDropable.name = i.ToString();
                }

                // click withdraw fallback
                int index = i;
                slot.button.onClick.RemoveAllListeners();
                slot.button.onClick.AddListener(() => player.CmdWarehouseWithdraw(index));
            }
        }

        // ----------------- INVENTORY -----------------
        if (slotPrefab && inventoryContent)
        {
            UIUtils.BalancePrefabs(slotPrefab.gameObject, player.inventory.slots.Count, inventoryContent);

            for (int i = 0; i < player.inventory.slots.Count; ++i)
            {
                Transform child = inventoryContent.GetChild(i);
                if (!child) continue;

                UIInventorySlot slot = child.GetComponent<UIInventorySlot>();
                if (!slot || !slot.button) continue;

                ItemSlot itemSlot = player.inventory.slots[i];
                bool hasItem = itemSlot.amount > 0;

                // visuals
                if (slot.tooltip) slot.tooltip.enabled = hasItem;
                if (slot.image)
                {
                    slot.image.color = hasItem ? Color.white : Color.clear;
                    slot.image.sprite = hasItem ? itemSlot.item.image : null;
                }
                if (slot.amountOverlay) slot.amountOverlay.SetActive(hasItem && itemSlot.amount > 1);
                if (slot.amountText) slot.amountText.text = hasItem ? itemSlot.amount.ToString() : "";

                // inventory keeps cooldown + tint behavior as normal
                // (no changes to slot.cooldownCircle here)
                slot.button.interactable = true;

                if (slot.dragAndDropable)
                {
                    slot.dragAndDropable.dragable = hasItem;
                    slot.dragAndDropable.dropable = true;
                    slot.dragAndDropable.tag = "InventorySlot";
                    slot.dragAndDropable.name = i.ToString();
                }

                int index = i;
                slot.button.onClick.RemoveAllListeners();
                slot.button.onClick.AddListener(() => player.CmdWarehouseDeposit(index));
            }
        }
    }

    // --------------------------------------------------------------------
    // DRAG & DROP HANDLERS (called via UIDragAndDropable.SendMessage on Player)
    // --------------------------------------------------------------------

    // inventory → warehouse
    public void OnDragAndDrop_InventorySlot_WarehouseSlot(int[] indices)
    {
        Player player = Player.localPlayer;
        if (!player) return;
        player.CmdWarehouseDepositStack(indices[0], indices[1]);
    }

    // warehouse → inventory
    public void OnDragAndDrop_WarehouseSlot_InventorySlot(int[] indices)
    {
        Player player = Player.localPlayer;
        if (!player) return;
        player.CmdWarehouseWithdrawStack(indices[0], indices[1]);
    }

    // --------------------------------------------------------------------
    public void Show()  => panel?.SetActive(true);
    public void Hide()  => panel?.SetActive(false);
}
