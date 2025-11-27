using UnityEngine;
using Mirror;

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class PlayerNpcTrading : NetworkBehaviour
{
    [Header("Components")]
    public Player player;
    public PlayerInventory inventory;

    
    [Command]
    public void CmdBuyItem(int index, int amount)
    {
        
        
        if (player.state == "IDLE" &&
            player.target != null &&
            player.target.health.current > 0 &&
            player.target is Npc npc &&
            npc.trading != null && 
            Utils.ClosestDistance(player, npc) <= player.interactionRange &&
            0 <= index && index < npc.trading.saleItems.Length)
        {
            
            Item npcItem = new Item(npc.trading.saleItems[index]);
            if (1 <= amount && amount <= npcItem.maxStack)
            {
                long price = npcItem.buyPrice * amount;

                
                if (player.gold >= price && inventory.CanAdd(npcItem, amount))
                {
                    
                    player.gold -= price;
                    inventory.Add(npcItem, amount);
                }
            }
        }
    }

    [Command]
    public void CmdSellItem(int index, int amount)
    {
        
        
        if (player.state == "IDLE" &&
            player.target != null &&
            player.target.health.current > 0 &&
            player.target is Npc npc &&
            npc.trading != null && 
            Utils.ClosestDistance(player, player.target) <= player.interactionRange &&
            0 <= index && index < inventory.slots.Count)
        {
            
            ItemSlot slot = inventory.slots[index];
            if (slot.amount > 0 && slot.item.sellable && !slot.item.summoned)
            {
                
                if (1 <= amount && amount <= slot.amount)
                {
                    
                    long price = slot.item.sellPrice * amount;
                    player.gold += price;
                    slot.DecreaseAmount(amount);
                    inventory.slots[index] = slot;
                }
            }
        }
    }

    [Command]
    public void CmdRepairAllItems()
    {
        
        
        if (player.state == "IDLE" &&
            player.target != null &&
            player.target.health.current > 0 &&
            player.target is Npc npc &&
            npc.trading != null && 
            npc.trading.offersRepair && 
            Utils.ClosestDistance(player, player.target) <= player.interactionRange)
        {
            
            int missing = player.inventory.GetTotalMissingDurability() +
                          player.equipment.GetTotalMissingDurability();

            
            int price = missing * npc.trading.repairCostPerDurabilityPoint;

            
            
            if (price > 0)
            {
                
                if (player.gold >= price)
                {
                    
                    player.inventory.RepairAllItems();
                    player.equipment.RepairAllItems();

                    
                    player.gold -= price;
                }
            }
        }
    }

    
    void OnDragAndDrop_InventorySlot_NpcSellSlot(int[] slotIndices)
    {
        
        ItemSlot slot = inventory.slots[slotIndices[0]];
        if (slot.item.sellable && !slot.item.summoned)
        {
            UINpcTrading.singleton.sellIndex = slotIndices[0];
            UINpcTrading.singleton.sellAmountInput.text = slot.amount.ToString();
        }
    }

    void OnDragAndClear_NpcSellSlot(int slotIndex)
    {
        UINpcTrading.singleton.sellIndex = -1;
    }
}
