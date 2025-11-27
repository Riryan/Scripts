using UnityEngine;
using Mirror;

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class PlayerPetControl : NetworkBehaviour
{
    [Header("Components")]
    public Player player;
    public PlayerInventory inventory;

    [Header("Pet")]
    [SyncVar, HideInInspector] public Pet activePet;

    
    
    
    
    public Vector3 petDestination
    {
        get
        {
            Bounds bounds = player.collider.bounds;
            return transform.position - transform.right * bounds.size.x;
        }
    }

    
    
    public bool CanUnsummon()
    {
        
        return activePet != null &&
               (   player.state == "IDLE" ||    player.state == "MOVING") &&
               (activePet.state == "IDLE" || activePet.state == "MOVING");
    }

    [Command]
    public void CmdUnsummon()
    {
        
        if (CanUnsummon())
        {
            
            NetworkServer.Destroy(activePet.gameObject);
        }
    }

    
    [Server]
    public void OnDamageDealtTo(Entity victim)
    {
        
        if (activePet != null && activePet.autoAttack)
            activePet.OnAggro(victim);
    }

    [Server]
    public void OnKilledEnemy(Entity victim)
    {
        
        if (victim is Monster monster)
        {
            
            
            
            
            
            if (activePet != null)
            {
                activePet.experience.current += Experience.BalanceExperienceReward(monster.rewardExperience, activePet.level.current, victim.level.current);
                
                
                
                activePet.SyncToOwnerItem();
            }
        }
    }

    
    void OnDragAndDrop_InventorySlot_NpcReviveSlot(int[] slotIndices)
    {
        
        if (inventory.slots[slotIndices[0]].item.data is SummonableItem)
            UINpcRevive.singleton.itemIndex = slotIndices[0];
    }

    void OnDragAndClear_NpcReviveSlot(int slotIndex)
    {
        UINpcRevive.singleton.itemIndex = -1;
    }
}
