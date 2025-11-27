

using Mirror;

public abstract class ItemContainer : NetworkBehaviour
{
    
    public readonly SyncList<ItemSlot> slots = new SyncList<ItemSlot>();

    
    public int GetItemIndexByName(string itemName)
    {
        
        for (int i = 0; i < slots.Count; ++i)
        {
            ItemSlot slot = slots[i];
            if (slot.amount > 0 && slot.item.name == itemName)
                return i;
        }
        return -1;
    }

    
    
    public int GetTotalMissingDurability()
    {
        int total = 0;
        foreach (ItemSlot slot in slots)
            if (slot.amount > 0 && slot.item.data.maxDurability > 0)
                total += slot.item.data.maxDurability - slot.item.durability;
        return total;
    }

    
    [Server]
    public void RepairAllItems()
    {
        for (int i = 0; i < slots.Count; ++i)
        {
            if (slots[i].amount > 0)
            {
                ItemSlot slot = slots[i];
                slot.item.durability = slot.item.maxDurability;
                slots[i] = slot;
            }
        }
    }
}
