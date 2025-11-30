using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class PlayerNpcRevive : NetworkBehaviour
{
    [Header("Components")]
    public Player player;

    [Command]
    public void CmdRevive(int index)
    {
        
        
        if (player.state == "IDLE" &&
            player.target != null &&
            player.target.health.current > 0 &&
            player.target is Npc npc &&
            npc.revive != null && 
            Utils.ClosestDistance(player, npc) <= player.interactionRange &&
            0 <= index && index < player.inventory.slots.Count)
        {
            ItemSlot slot = player.inventory.slots[index];
            if (slot.amount > 0 && slot.item.data is SummonableItem summonable)
            {
                
                if (slot.item.summonedHealth == 0 && summonable.summonPrefab != null)
                {
                    
                    if (player.gold >= summonable.revivePrice)
                    {
                        
                        player.gold -= summonable.revivePrice;
                        slot.item.summonedHealth = summonable.summonPrefab.health.max;
                        player.inventory.slots[index] = slot;
                    }
                }
            }
        }
    }
}
