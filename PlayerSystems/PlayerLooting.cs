using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerParty))]
[DisallowMultipleComponent]
public class PlayerLooting : NetworkBehaviour
{
    [Header("Components")]
    public Player player;
    public PlayerInventory inventory;
    public PlayerParty party;

    
    [Command]
    public void CmdTakeGold()
    {
        
        
        if ((player.state == "IDLE" || player.state == "MOVING" || player.state == "CASTING") &&
            player.target != null &&
            player.target is Monster &&
            player.target.health.current == 0 &&
            Utils.ClosestDistance(player, player.target) <= player.interactionRange)
        {
            
            if (party.InParty() && party.party.shareGold)
            {
                
                
                
                
                List<Player> closeMembers = party.GetMembersInProximity();

                
                
                
                long share = (long)Mathf.Ceil((float)player.target.gold / (float)closeMembers.Count);

                
                foreach (Player member in closeMembers)
                    member.gold += share;
            }
            else
            {
                player.gold += player.target.gold;
            }

            
            player.target.gold = 0;
        }
    }

    [Command]
    public void CmdTakeItem(int index)
    {
        
        
        if ((player.state == "IDLE" || player.state == "MOVING" || player.state == "CASTING") &&
            player.target != null &&
            player.target is Monster monster &&
            player.target.health.current == 0 &&
            Utils.ClosestDistance(player, player.target) <= player.interactionRange)
        {
            if (0 <= index && index < monster.inventory.slots.Count &&
                monster.inventory.slots[index].amount > 0)
            {
                ItemSlot slot = monster.inventory.slots[index];

                
                if (inventory.Add(slot.item, slot.amount))
                {
                    slot.amount = 0;
                    monster.inventory.slots[index] = slot;
                }
            }
        }
    }
}
