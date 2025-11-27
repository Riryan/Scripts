using UnityEngine;
using Mirror;
using UnityEditor;

[RequireComponent(typeof(PlayerTrading))]
public class PlayerInventory : Inventory
{
    [Header("Components")]
    public Player player;

    [Header("Inventory")]
    public int size = 30;
    public ScriptableItemAndAmount[] defaultItems;
    public KeyCode[] splitKeys = { KeyCode.LeftShift, KeyCode.RightShift };

    [Header("Trash")]
    [SyncVar] public ItemSlot trash;

    public bool InventoryOperationsAllowed()
    {
        return player.state == "IDLE" ||
               player.state == "MOVING" ||
               player.state == "CASTING";
    }

    [Command]
    public void CmdSwapInventoryTrash(int inventoryIndex)
    {
        if (InventoryOperationsAllowed() &&
            0 <= inventoryIndex && inventoryIndex < slots.Count)
        {
            ItemSlot slot = slots[inventoryIndex];
            if (slot.amount > 0 && slot.item.destroyable && !slot.item.summoned)
            {
                trash = slot;
                slot.amount = 0;
                slots[inventoryIndex] = slot;
            }
        }
    }

    [Command]
    public void CmdSwapTrashInventory(int inventoryIndex)
    {
        if (InventoryOperationsAllowed() &&
            0 <= inventoryIndex && inventoryIndex < slots.Count)
        {
            ItemSlot slot = slots[inventoryIndex];
            if (slot.amount == 0 || slot.item.destroyable)
            {
                slots[inventoryIndex] = trash;
                trash = slot;
            }
        }
    }

    [Command]
    public void CmdSwapInventoryInventory(int fromIndex, int toIndex)
    {
        if (InventoryOperationsAllowed() &&
            0 <= fromIndex && fromIndex < slots.Count &&
            0 <= toIndex && toIndex < slots.Count &&
            fromIndex != toIndex)
        {
            ItemSlot temp = slots[fromIndex];
            slots[fromIndex] = slots[toIndex];
            slots[toIndex] = temp;
        }
    }

    [Command]
    public void CmdInventorySplit(int fromIndex, int toIndex)
    {
        if (InventoryOperationsAllowed() &&
            0 <= fromIndex && fromIndex < slots.Count &&
            0 <= toIndex && toIndex < slots.Count &&
            fromIndex != toIndex)
        {
            ItemSlot slotFrom = slots[fromIndex];
            ItemSlot slotTo = slots[toIndex];
            if (slotFrom.amount >= 2 && slotTo.amount == 0)
            {
                slotTo = slotFrom; 
                slotTo.amount = slotFrom.amount / 2;
                slotFrom.amount -= slotTo.amount; 
                slots[fromIndex] = slotFrom;
                slots[toIndex] = slotTo;
            }
        }
    }

    [Command]
    public void CmdInventoryMerge(int fromIndex, int toIndex)
    {
        if (InventoryOperationsAllowed() &&
            0 <= fromIndex && fromIndex < slots.Count &&
            0 <= toIndex && toIndex < slots.Count &&
            fromIndex != toIndex)
        {
            ItemSlot slotFrom = slots[fromIndex];
            ItemSlot slotTo = slots[toIndex];
            if (slotFrom.amount > 0 && slotTo.amount > 0)
            {
                if (slotFrom.item.Equals(slotTo.item))
                {
                    int put = slotTo.IncreaseAmount(slotFrom.amount);
                    slotFrom.DecreaseAmount(put);
                    slots[fromIndex] = slotFrom;
                    slots[toIndex] = slotTo;
                }
            }
        }
    }

    [ClientRpc]
    public void RpcUsedItem(Item item)
    {
        if (item.data is UsableItem usable)
        {
            usable.OnUsed(player);
        }
    }

    [Command]
    public void CmdUseItem(int index)
    {
        if (InventoryOperationsAllowed() &&
            0 <= index && index < slots.Count && slots[index].amount > 0 &&
            slots[index].item.data is UsableItem usable)
        {
            if (usable.CanUse(player, index))
            {
                Item item = slots[index].item;
                usable.Use(player, index);
                RpcUsedItem(item);
            }
        }
    }

    
    void OnDragAndDrop_InventorySlot_InventorySlot(int[] slotIndices)
    {
        if (slots[slotIndices[0]].amount > 0 && slots[slotIndices[1]].amount > 0 &&
            slots[slotIndices[0]].item.Equals(slots[slotIndices[1]].item))
        {
            CmdInventoryMerge(slotIndices[0], slotIndices[1]);
        }
        else if (Utils.AnyKeyPressed(splitKeys))
        {
            CmdInventorySplit(slotIndices[0], slotIndices[1]);
        }
        else
        {
            CmdSwapInventoryInventory(slotIndices[0], slotIndices[1]);
        }
    }

    void OnDragAndDrop_InventorySlot_TrashSlot(int[] slotIndices)
    {
        CmdSwapInventoryTrash(slotIndices[0]);
    }

    void OnDragAndDrop_TrashSlot_InventorySlot(int[] slotIndices)
    {
        CmdSwapTrashInventory(slotIndices[1]);
    }
// Inventory -> Warehouse (drag from inventory slot onto warehouse slot)
void OnDragAndDrop_InventorySlot_WarehouseSlot(int[] slotIndices)
{
    // slotIndices[0] = inventory index, slotIndices[1] = warehouse index
    if (!InventoryOperationsAllowed()) return;
    if (player != null)
        player.CmdWarehouseDepositStack(slotIndices[0], slotIndices[1]);
}

// Warehouse -> Inventory (drag from warehouse slot onto inventory slot)
void OnDragAndDrop_WarehouseSlot_InventorySlot(int[] slotIndices)
{
    // slotIndices[0] = warehouse index, slotIndices[1] = inventory index
    if (!InventoryOperationsAllowed()) return;
    if (player != null)
        player.CmdWarehouseWithdrawStack(slotIndices[0], slotIndices[1]);
}

    
    protected override void OnValidate()
    {
        base.OnValidate();
        if (defaultItems != null)
        {
            for (int i = 0; i < defaultItems.Length; ++i)
                if (defaultItems[i].item != null && defaultItems[i].amount == 0)
                    defaultItems[i].amount = 1;
        }
        
        if (syncMode != SyncMode.Owner) {
            syncMode = SyncMode.Owner;
#if UNITY_EDITOR
            Undo.RecordObject(this, name + " " + GetType() + " component syncMode changed to Owner.");
#endif
        }
    }
}
