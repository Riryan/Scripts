using UnityEngine;
using Mirror;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class ZonePortal : MonoBehaviour
{
    [Header("Portal Identity")]
    [Tooltip("Unique ID for this portal endpoint (used later for spawn/fallback logic).")]
    public string portalId = "Main_To_DungeonA_Entrance";

    [Tooltip("Logical target zone name (e.g. 'DungeonA'). Not used by debug flow yet.")]
    public string targetZoneId = "DungeonA";

    [Tooltip("Portal ID on the TARGET zone where players should arrive (e.g. 'DungeonA_From_Main_Entrance').")]
    public string targetPortalId = "DungeonA_From_Main_Entrance";

    [Header("Spawn Point")]
    [Tooltip("Where players will appear when arriving at THIS portal. " +
             "Usually a child Transform placed slightly in front of the portal, facing into the world.")]
    public Transform spawnPoint;

    [Header("Debug")]
    [Tooltip("If true, this portal will pretend the destination zone is offline and show an error instead of transferring.")]
    public bool debugSimulateOffline = false;

    NetworkManagerMMO manager;

    // Lazy manager lookup so we don't care about Awake order
    NetworkManagerMMO Manager
    {
        get
        {
            if (manager == null)
            {
                manager = NetworkManager.singleton as NetworkManagerMMO;
                if (manager == null)
                {
                    manager = FindObjectOfType<NetworkManagerMMO>();
                }
            }
            return manager;
        }
    }

// ─── Registry for spawn lookup ────────────────────────────────
static readonly Dictionary<string, ZonePortal> registry = new Dictionary<string, ZonePortal>();

void Awake()
{
    // existing trigger setup here …

    // register this portal
    if (!string.IsNullOrWhiteSpace(portalId))
    {
        registry[portalId] = this;
    }
}

void OnDestroy()
{
    if (!string.IsNullOrWhiteSpace(portalId))
        registry.Remove(portalId);
}

// Find portal by id
public static bool TryGetSpawn(string id, out Vector3 pos, out Quaternion rot)
{
    if (registry.TryGetValue(id, out ZonePortal portal) && portal.spawnPoint != null)
    {
        pos = portal.spawnPoint.position;
        rot = portal.spawnPoint.rotation;
        return true;
    }
    pos = Vector3.zero;
    rot = Quaternion.identity;
    return false;
}


   [ServerCallback]
void OnTriggerEnter(Collider other)
{
    if (!NetworkServer.active)
        return;

    // find player
    Player player = other.GetComponent<Player>() ?? other.GetComponentInParent<Player>();
    if (player == null)
        return;

    // must have a real server connection
    NetworkConnectionToClient conn = player.connectionToClient as NetworkConnectionToClient;
    if (conn == null)
    {
        // happens briefly right after spawn etc.; just ignore
        return;
    }

    // don’t fire twice for the same player while a transfer is in progress
    if (player.zoneTransferPending)
        return;

    NetworkManagerMMO mgr = NetworkManager.singleton as NetworkManagerMMO
                            ?? FindObjectOfType<NetworkManagerMMO>();
    if (mgr == null)
        return;

    if (debugSimulateOffline)
    {
        mgr.ServerSendError(conn, "Destination zone is offline (debug).", false);
        return;
    }

    Debug.Log($"{name}: ZonePortal used by {player.name}, initiating debug zone transfer.");

    player.zoneTransferPending = true;    // block further uses for this connection
    mgr.ServerDebugSendZoneTransfer(conn, targetPortalId);
}


    // Helper you can call from other scripts if you don't want to rely on triggers
    [Server]
    public void TryUsePortal(Player player)
    {
        if (player == null) return;

        NetworkConnectionToClient conn = player.connectionToClient as NetworkConnectionToClient;
        if (conn == null)
        {
            Debug.LogWarning($"{name}: TryUsePortal called with player that has no connectionToClient.");
            return;
        }

        NetworkManagerMMO mgr = Manager;
        if (mgr == null)
        {
            Debug.LogWarning($"{name}: ZonePortal could not find NetworkManagerMMO in the scene.");
            return;
        }

        if (debugSimulateOffline)
        {
            mgr.ServerSendError(conn, "Destination zone is offline (debug).", false);
            return;
        }

        Debug.Log($"{name}: TryUsePortal used by {player.name}, initiating debug zone transfer.");
        mgr.ServerDebugSendZoneTransfer(conn);
    }

#if UNITY_EDITOR
    // simple gizmo to visualize the spawn point in the Scene view
    void OnDrawGizmosSelected()
    {
        if (spawnPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnPoint.position, 0.3f);
        Gizmos.DrawLine(transform.position, spawnPoint.position);

        // draw a little forward arrow from spawn
        Vector3 forward = spawnPoint.forward * 0.7f;
        Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + forward);
    }
#endif
}
