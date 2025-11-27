
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PortalToInstance : MonoBehaviour
{
    [Tooltip("Instance template in the Scene. Don't use a prefab, Mirror can't handle prefabs that contain NetworkIdentity children.")]
    public Instance instanceTemplate;

    void OnPortal(Player player)
    {
        
        if (player.party.InParty())
        {
            
            if (instanceTemplate.instances.TryGetValue(player.party.party.partyId, out Instance existingInstance))
            {
                
                if (player.isServer) player.movement.Warp(existingInstance.entry.position);
                Debug.Log("Teleporting " + player.name + " to existing instance=" + existingInstance.name + " with partyId=" + player.party.party.partyId);
            }
            
            else
            {
                Instance instance = Instance.CreateInstance(instanceTemplate, player.party.party.partyId);
                if (instance != null)
                {
                    
                    if (player.isServer) player.movement.Warp(instance.entry.position);
                    Debug.Log("Teleporting " + player.name + " to new instance=" + instance.name + " with partyId=" + player.party.party.partyId);
                }
                else if (player.isServer) player.chat.TargetMsgInfo("There are already too many " + instanceTemplate.name + " instances. Please try again later.");
            }
        }
    }

    void OnTriggerEnter(Collider co)
    {
        if (instanceTemplate != null)
        {
            
            
            
            
            Player player = co.GetComponent<Player>();
            if (player != null)
            {
                
                
                
                if (player.isServer || player.isLocalPlayer)
                {
                    
                    if (player.level.current >= instanceTemplate.requiredLevel)
                    {
                        
                        if (player.party.InParty())
                        {
                            
                            OnPortal(player);
                        }
                        
                        else if (player.isClient)
                            player.chat.AddMsgInfo("Can't enter instance without a party.");
                    }
                    
                    else if (player.isClient)
                        player.chat.AddMsgInfo("Portal requires level " + instanceTemplate.requiredLevel);
                }
            }
        }
    }
}
