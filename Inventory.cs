using UnityEngine;


[DisallowMultipleComponent]
public class Inventory : ItemContainer
{
    
    public int SlotsFree()
    {
        
        int free = 0;
        foreach (ItemSlot slot in slots)
            if (slot.amount == 0)
                ++free;
        return free;
    }

    
    public int SlotsOccupied()
    {
        
        int occupied = 0;
        foreach (ItemSlot slot in slots)
            if (slot.amount > 0)
                ++occupied;
        return occupied;
    }

    
    
    public int Count(Item item)
    {
        
        int amount = 0;
        foreach (ItemSlot slot in slots)
            if (slot.amount > 0 && slot.item.Equals(item))
                amount += slot.amount;
        return amount;
    }

    
    public bool Remove(Item item, int amount)
    {
        for (int i = 0; i < slots.Count; ++i)
        {
            ItemSlot slot = slots[i];
            
            if (slot.amount > 0 && slot.item.Equals(item))
            {
                
                amount -= slot.DecreaseAmount(amount);
                slots[i] = slot;

                
                if (amount == 0) return true;
            }
        }

        
        return false;
    }

    
    
    
    
    
    
    
    
    public bool CanAdd(Item item, int amount)
    {
        
        for (int i = 0; i < slots.Count; ++i)
        {
            
            if (slots[i].amount == 0)
                amount -= item.maxStack;
            
            
            else if (slots[i].item.Equals(item))
                amount -= (slots[i].item.maxStack - slots[i].amount);

            
            if (amount <= 0) return true;
        }

        
        return false;
    }

    
    
    
    
    public bool Add(Item item, int amount)
    {
        
        
        if (CanAdd(item, amount))
        {
            
            
            
            for (int i = 0; i < slots.Count; ++i)
            {
                
                
                if (slots[i].amount > 0 && slots[i].item.Equals(item))
                {
                    ItemSlot temp = slots[i];
                    amount -= temp.IncreaseAmount(amount);
                    slots[i] = temp;
                }

                
                if (amount <= 0) return true;
            }

            
            for (int i = 0; i < slots.Count; ++i)
            {
                
                if (slots[i].amount == 0)
                {
                    int add = Mathf.Min(amount, item.maxStack);
                    slots[i] = new ItemSlot(item, add);
                    amount -= add;
                }

                
                if (amount <= 0) return true;
            }
            
            if (amount != 0) Debug.LogError("inventory add failed: " + item.name + " " + amount);
        }
        return false;
    }
}
