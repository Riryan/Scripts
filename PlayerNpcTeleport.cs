using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class PlayerNpcTeleport : NetworkBehaviour
{
    [Header("Components")]
    public Player player;

    [Command]
    public void CmdNpcTeleport()
    {
        
        if (player.state == "IDLE" &&
            player.target != null &&
            player.target.health.current > 0 &&
            player.target is Npc npc &&
            Utils.ClosestDistance(player, npc) <= player.interactionRange &&
            npc.teleport.destination != null)
        {
            
            
            player.movement.Warp(npc.teleport.destination.position);

            
            
            player.target = null;
        }
    }
}
