using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(PlayerIndicator))]
[DisallowMultipleComponent]
public class PlayerTabTargeting : NetworkBehaviour
{
    [Header("Components")]
    public Player player;
    public PlayerIndicator indicator;

    [Header("Targeting")]
    public KeyCode key = KeyCode.Tab;

    void Update()
    {
        
        if (!isLocalPlayer) return;

        
        if (player.state == "IDLE" ||
            player.state == "MOVING" ||
            player.state == "CASTING" ||
            player.state == "STUNNED")
        {
            
            if (Input.GetKeyDown(key))
                TargetNearest();
        }
    }

    [Client]
    void TargetNearest()
    {
        
        
        
        List<Monster> monsters = NetworkClient.spawned.Values
            .Select(ni => ni.GetComponent<Monster>())
            .Where(m => m != null && m.health.current > 0)
            .ToList();
        List<Monster> sorted = monsters.OrderBy(m => Vector3.Distance(transform.position, m.transform.position)).ToList();

        
        if (sorted.Count > 0)
        {
            indicator.SetViaParent(sorted[0].transform);
            player.CmdSetTarget(sorted[0].netIdentity);
        }
    }
}
