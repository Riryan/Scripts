using Mirror;
using UnityEngine;

public partial class Player
{
    [SyncVar] public bool warehouseOpen;

    // --------------------------------------------------------------------
    // SIMPLE STACK MOVE / MERGE HELPER
    // --------------------------------------------------------------------
    [Server]
    public void MoveOrMergeSlotTo(SyncList<ItemSlot> targetList, int fromIndex, int targetIndex)
    {
        // determine if we are moving from inventory or warehouse
        bool fromInventory = fromIndex < inventory.slots.Count;
        var fromList = fromInventory ? inventory.slots : warehouseSlots;
        if (fromIndex < 0 || fromIndex >= fromList.Count) return;
        if (targetIndex < 0 || targetIndex >= targetList.Count) return;

        ItemSlot fromSlot = fromList[fromIndex];
        ItemSlot targetSlot = targetList[targetIndex];
        if (fromSlot.amount <= 0) return;

        // same item → merge
        if (targetSlot.amount > 0 && targetSlot.item.Equals(fromSlot.item))
        {
            int space = targetSlot.item.maxStack - targetSlot.amount;
            int moveAmount = Mathf.Min(space, fromSlot.amount);
            targetSlot.amount += moveAmount;
            fromSlot.amount -= moveAmount;
        }
        // empty slot → move
        else if (targetSlot.amount == 0)
        {
            targetSlot = fromSlot;
            fromSlot = new ItemSlot();
        }

        // assign back
        targetList[targetIndex] = targetSlot;
        fromList[fromIndex] = fromSlot;
    }

    // --------------------------------------------------------------------
    // COMMANDS
    // --------------------------------------------------------------------
    [Command]
    public void CmdWarehouseDeposit(int inventoryIndex)
    {
        if (!warehouseOpen) return;
        if (inventoryIndex < 0 || inventoryIndex >= inventory.slots.Count) return;

        ItemSlot invSlot = inventory.slots[inventoryIndex];
        if (invSlot.amount <= 0) return;

        int emptyIndex = warehouseSlots.FindIndex(s => s.amount == 0);
        if (emptyIndex == -1) return;

        warehouseSlots[emptyIndex] = invSlot;
        inventory.slots[inventoryIndex] = new ItemSlot();
    }

    [Command]
    public void CmdWarehouseWithdraw(int warehouseIndex)
    {
        if (!warehouseOpen) return;
        if (warehouseIndex < 0 || warehouseIndex >= warehouseSlots.Count) return;

        ItemSlot wSlot = warehouseSlots[warehouseIndex];
        if (wSlot.amount <= 0) return;

        int emptyIndex = inventory.slots.FindIndex(s => s.amount == 0);
        if (emptyIndex == -1) return;

        inventory.slots[emptyIndex] = wSlot;
        warehouseSlots[warehouseIndex] = new ItemSlot();
    }

    // full drag-drop stack versions
    [Command]
    public void CmdWarehouseDepositStack(int invIndex, int targetWareIndex)
    {
        if (!warehouseOpen) return;
        MoveOrMergeSlotTo(warehouseSlots, invIndex, targetWareIndex);
    }

    [Command]
    public void CmdWarehouseWithdrawStack(int wareIndex, int targetInvIndex)
    {
        if (!warehouseOpen) return;
        MoveOrMergeSlotTo(inventory.slots, wareIndex, targetInvIndex);
    }

    // --------------------------------------------------------------------
    // OPEN / CLOSE STATE
    // --------------------------------------------------------------------
    [Server]
    public void ServerOpenWarehouse()
    {
        warehouseOpen = true;
        TargetShowWarehouseUI();
    }

    [Command]
    public void CmdWarehouseClose()
    {
        warehouseOpen = false;
    }
}
