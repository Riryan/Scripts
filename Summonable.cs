using Mirror;
using UnityEngine;

public abstract class Summonable : Entity
{
    [SyncVar, HideInInspector] public Player owner;

    protected virtual ItemSlot SyncStateToItemSlot(ItemSlot slot)
    {
        slot.item.summonedHealth = health.current;
        slot.item.summonedLevel = level.current;
        if (((SummonableItem)slot.item.data).removeItemIfDied && health.current == 0)
            --slot.amount;

        return slot;
    }
    
    public int GetOwnerItemIndex()
    {
        if (owner != null)
        {
            for (int i = 0; i < owner.inventory.slots.Count; ++i)
            {
                ItemSlot slot = owner.inventory.slots[i];
                if (slot.amount > 0 && slot.item.summoned == netIdentity)
                    return i;
            }
        }
        return -1;
    } 
    
    [Server]
    public void SyncToOwnerItem()
    {
        if (owner != null)
        {
            
            int index = GetOwnerItemIndex();
            if (index != -1)
                owner.inventory.slots[index] = SyncStateToItemSlot(owner.inventory.slots[index]);
        }
    }
}
