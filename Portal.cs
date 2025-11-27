
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
    public int requiredLevel = 1;
    public Transform destination;

    void OnPortal(Player player)
    {
        if (destination != null)
            player.movement.Warp(destination.position);
    }

    void OnTriggerEnter(Collider co)
    {
        
        Player player = co.GetComponentInParent<Player>();
        if (player != null)
        {
            
            if (player.level.current >= requiredLevel)
            {
                
                if (player.isServer)
                    OnPortal(player);
            }
            else
            {
                
                
                if (player.isClient)
                    player.chat.AddMsgInfo("Portal requires level " + requiredLevel);
            }
        }
    }
}
