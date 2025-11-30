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

    [Tooltip("Maximum range for tab targeting.")]
    public float maxRange = 40f;

    void Awake()
    {
        if (player == null)
            player = GetComponent<Player>();

        if (indicator == null)
            indicator = GetComponent<PlayerIndicator>();
    }

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
        if (!isLocalPlayer) return;
        if (player == null) return;

        List<Monster> monsters = NetworkClient.spawned.Values
            .Select(ni => ni.GetComponent<Monster>())
            .Where(m =>
                m != null &&
                m.health.current > 0 &&
                Vector3.Distance(transform.position, m.transform.position) <= maxRange)
            .ToList();

        if (monsters.Count == 0)
            return;

        Monster nearest = monsters
            .OrderBy(m => Vector3.Distance(transform.position, m.transform.position))
            .First();

        if (indicator != null)
            indicator.SetViaParent(nearest.transform);

        player.CmdSetTarget(nearest.netIdentity);
    }
}
