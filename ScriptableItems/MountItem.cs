using UnityEngine;
using Mirror;

[CreateAssetMenu(menuName="uMMORPG Item/Mount", order=999)]
public class MountItem : SummonableItem
{
    
    public override bool CanUse(Player player, int inventoryIndex)
    {
        
        
        
        Item item = player.inventory.slots[inventoryIndex].item;
        return base.CanUse(player, inventoryIndex) &&
               (player.mountControl.activeMount == null ||
                player.mountControl.activeMount.netIdentity == item.summoned);
    }

    public override void Use(Player player, int inventoryIndex)
    {
        
        base.Use(player, inventoryIndex);

        
        if (player.mountControl.activeMount == null)
        {
            
            ItemSlot slot = player.inventory.slots[inventoryIndex];
            GameObject go = Instantiate(summonPrefab.gameObject, player.transform.position, player.transform.rotation);
            Mount mount = go.GetComponent<Mount>();
            mount.name = summonPrefab.name; 
            mount.owner = player;
            mount.health.current = slot.item.summonedHealth;

            
            NetworkServer.Spawn(go, player.connectionToClient);
            player.mountControl.activeMount = mount; 

            
            slot.item.summoned = go.GetComponent<NetworkIdentity>();
            player.inventory.slots[inventoryIndex] = slot;
        }
        
        else
        {
            
            NetworkServer.Destroy(player.mountControl.activeMount.gameObject);
        }
    }
}
