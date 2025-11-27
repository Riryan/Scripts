







using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum TradingState { Free, Locked, Accepted }

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class PlayerTrading : NetworkBehaviour
{
    [Header("Components")]
    public Player player;
    public PlayerInventory inventory;

    [Header("Trading")]
    [SyncVar, HideInInspector] public string requestFrom = "";
    [SyncVar, HideInInspector] public TradingState state = TradingState.Free;
    [SyncVar, HideInInspector] public long offerGold = 0;
    public readonly SyncList<int> offerItems = new SyncList<int>(); 

    public override void OnStartServer()
    {
        
        for (int i = 0; i < 6; ++i)
            offerItems.Add(-1);
    }

    
    public bool CanStartTrade()
    {
        
        return player.health.current > 0 && player.state != "TRADING";
    }

    public bool CanStartTradeWith(Entity entity)
    {
        
        return entity != null &&
               entity is Player other &&
               other != player &&
               CanStartTrade() &&
               other.trading.CanStartTrade() &&
               Utils.ClosestDistance(player, entity) <= player.interactionRange;
    }

    
    [Command]
    public void CmdSendRequest()
    {
        
        if (CanStartTradeWith(player.target))
        {
            
            ((Player)player.target).trading.requestFrom = name;
            Debug.Log(name + " invited " + player.target.name + " to trade");
        }
    }

    
    [Server]
    public Player FindPlayerFromInvitation()
    {
        if (requestFrom != "" &&
            Player.onlinePlayers.TryGetValue(requestFrom, out Player sender))
        {
            return sender;
        }
        return null;
    }

    
    
    [Command]
    public void CmdAcceptRequest()
    {
        Player sender = FindPlayerFromInvitation();
        if (sender != null)
        {
            if (CanStartTradeWith(sender))
            {
                
                sender.trading.requestFrom = name;
                Debug.Log(name + " accepted " + sender.name + "'s trade request");
            }
        }
    }

    
    [Command]
    public void CmdDeclineRequest()
    {
        requestFrom = "";
    }

    [Server]
    public void Cleanup()
    {
        
        offerGold = 0;
        for (int i = 0; i < offerItems.Count; ++i)
            offerItems[i] = -1;
        state = TradingState.Free;
        requestFrom = "";
    }

    [Command]
    public void CmdCancel()
    {
        
        if (player.state == "TRADING")
        {
            
            Player other = FindPlayerFromInvitation();
            if (other != null)
                other.trading.requestFrom = "";
            requestFrom = "";
        }
    }

    [Command]
    public void CmdLockOffer()
    {
        
        if (player.state == "TRADING")
            state = TradingState.Locked;
    }

    [Command]
    public void CmdOfferGold(long amount)
    {
        
        if (player.state == "TRADING" && state == TradingState.Free &&
            0 <= amount && amount <= player.gold)
            offerGold = amount;
    }

    [Command]
    public void CmdOfferItem(int inventoryIndex, int offerIndex)
    {
        
        if (player.state == "TRADING" && state == TradingState.Free &&
            0 <= offerIndex && offerIndex < offerItems.Count &&
            !offerItems.Contains(inventoryIndex) && 
            0 <= inventoryIndex && inventoryIndex < inventory.slots.Count)
        {
            ItemSlot slot = inventory.slots[inventoryIndex];
            if (slot.amount > 0 && slot.item.tradable && !slot.item.summoned)
                offerItems[offerIndex] = inventoryIndex;
        }
    }

    [Command]
    public void CmdClearOfferItem(int offerIndex)
    {
        
        if (player.state == "TRADING" && state == TradingState.Free &&
            0 <= offerIndex && offerIndex < offerItems.Count)
            offerItems[offerIndex] = -1;
    }

    bool IsInventorySlotTradable(int index)
    {
        return 0 <= index && index < inventory.slots.Count &&
               inventory.slots[index].amount > 0 &&
               inventory.slots[index].item.tradable;
    }

    [Server]
    bool IsOfferStillValid()
    {
        
        if (player.gold < offerGold)
            return false;

        
        
        foreach (int index in offerItems)
        {
            if (index == -1 || IsInventorySlotTradable(index))
            {
                
            }
            else
            {
                
                return false;
            }
        }
        return true;
    }

    [Server]
    int OfferItemSlotAmount()
    {
        
        int count = 0;
        foreach (int index in offerItems)
            if (index != -1)
                ++count;
        return count;
    }

    [Server]
    int InventorySlotsNeededForTrade()
    {
        
        
        if (player.target != null &&
            player.target is Player other)
        {
            int otherAmount = other.trading.OfferItemSlotAmount();
            int myAmount = OfferItemSlotAmount();
            return Mathf.Max(otherAmount - myAmount, 0);
        }
        return 0;
    }

    [Command]
    public void CmdAcceptOffer()
    {
        
        
        if (player.state == "TRADING" && state == TradingState.Locked &&
            player.target != null &&
            player.target is Player other)
        {
            
            if (other.trading.state == TradingState.Locked)
            {
                
                state = TradingState.Accepted;
                Debug.Log("first accept by " + name);
            }
            
            else if (other.trading.state == TradingState.Accepted)
            {
                
                state = TradingState.Accepted;
                Debug.Log("second accept by " + name);

                
                if (IsOfferStillValid() && other.trading.IsOfferStillValid())
                {
                    
                    
                    
                    
                    
                    
                    if (inventory.SlotsFree() >= InventorySlotsNeededForTrade() &&
                        other.inventory.SlotsFree() >= other.trading.InventorySlotsNeededForTrade())
                    {
                        
                        
                        
                        

                        
                        Queue<ItemSlot> tempMy = new Queue<ItemSlot>();
                        foreach (int index in offerItems)
                        {
                            if (index != -1)
                            {
                                ItemSlot slot = inventory.slots[index];
                                tempMy.Enqueue(slot);
                                slot.amount = 0;
                                inventory.slots[index] = slot;
                            }
                        }

                        Queue<ItemSlot> tempOther = new Queue<ItemSlot>();
                        foreach (int index in other.trading.offerItems)
                        {
                            if (index != -1)
                            {
                                ItemSlot slot = other.inventory.slots[index];
                                tempOther.Enqueue(slot);
                                slot.amount = 0;
                                other.inventory.slots[index] = slot;
                            }
                        }

                        
                        for (int i = 0; i < inventory.slots.Count; ++i)
                            if (inventory.slots[i].amount == 0 && tempOther.Count > 0)
                                inventory.slots[i] = tempOther.Dequeue();

                        for (int i = 0; i < other.inventory.slots.Count; ++i)
                            if (other.inventory.slots[i].amount == 0 && tempMy.Count > 0)
                                other.inventory.slots[i] = tempMy.Dequeue();

                        
                        if (tempMy.Count > 0 || tempOther.Count > 0)
                            Debug.LogWarning("item trade problem");

                        
                        player.gold -= offerGold;
                        other.gold -= other.trading.offerGold;

                        player.gold += other.trading.offerGold;
                        other.gold += offerGold;
                    }
                }
                else Debug.Log("trade canceled (invalid offer)");

                
                
                requestFrom = "";
                other.trading.requestFrom = "";
            }
        }
    }

    
    void OnDragAndDrop_InventorySlot_TradingSlot(int[] slotIndices)
    {
        
        if (inventory.slots[slotIndices[0]].item.tradable)
            CmdOfferItem(slotIndices[0], slotIndices[1]);
    }

    void OnDragAndClear_TradingSlot(int slotIndex)
    {
        CmdClearOfferItem(slotIndex);
    }
}
