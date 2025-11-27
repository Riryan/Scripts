using UnityEngine;
using Mirror;

[CreateAssetMenu(menuName="uMMORPG Item/Pet", order=999)]
public class PetItem : SummonableItem
{
    
    public override bool CanUse(Player player, int inventoryIndex)
    {
        
        
        return base.CanUse(player, inventoryIndex) &&
               player.petControl.activePet == null;
    }

    public override void Use(Player player, int inventoryIndex)
    {
        
        base.Use(player, inventoryIndex);

        
        ItemSlot slot = player.inventory.slots[inventoryIndex];
        GameObject go = Instantiate(summonPrefab.gameObject, player.petControl.petDestination, Quaternion.identity);
        Pet pet = go.GetComponent<Pet>();
        pet.name = summonPrefab.name; 
        pet.owner = player;
        pet.level.current = slot.item.summonedLevel;
        pet.experience.current = slot.item.summonedExperience;
        
        pet.health.current = slot.item.summonedHealth;

        
        NetworkServer.Spawn(go, player.connectionToClient);
        player.petControl.activePet = pet; 

        
        slot.item.summoned = go.GetComponent<NetworkIdentity>();
        player.inventory.slots[inventoryIndex] = slot;
    }
}
