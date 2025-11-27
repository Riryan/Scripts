


using UnityEngine;
using Mirror;

public abstract class SummonableItem : UsableItem
{
    [Header("Summonable")]
    public Summonable summonPrefab;
    public long revivePrice = 10;
    public bool removeItemIfDied;

    
    public override bool CanUse(Player player, int inventoryIndex)
    {
        
        
        
        
        
        
        
        Item item = player.inventory.slots[inventoryIndex].item;
        return base.CanUse(player, inventoryIndex) &&
               (player.state == "IDLE" || player.state == "MOVING") &&
               NetworkTime.time >= player.nextRiskyActionTime &&
               summonPrefab != null &&
               item.summonedHealth > 0 &&
               item.summonedLevel <= player.level.current;
    }

    public override void Use(Player player, int inventoryIndex)
    {
        
        base.Use(player, inventoryIndex);

        
        player.nextRiskyActionTime = NetworkTime.time + 1;
    }
}
