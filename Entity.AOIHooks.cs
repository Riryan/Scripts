using UnityEngine;
using Mirror;

public partial class Entity : NetworkBehaviour
{
#if UNITY_SERVER || UNITY_EDITOR
    // Track the last AOI grid cell we reported
    Vector2Int _aoiLastCell;
    bool _aoiInitialized;

    // -------- AOI lifecycle hooks (called from Entity.cs) --------

    // Called by Entity.OnStartServer()
    public void AOI_OnServerSpawned()
    {
        var aoi = CustomAOIInterestManagement.Instance;
        if (aoi == null) return;

        // Register subject + force initial targeted rebuild
        aoi.NotifySubjectStateChanged(netIdentity, CustomAOIInterestManagement.AOIChangeReason.Spawn);

        // Initialize our cached cell
        Vector3 pos = transform.position;
        _aoiLastCell = WorldToCell(pos);
        _aoiInitialized = true;

        // If this entity has a client connection (player), register as observer too
        if (connectionToClient != null)
            aoi.NotifyObserverMoved(connectionToClient, pos);
    }

    // Called by Entity.OnStopServer()
    public void AOI_OnServerDespawned()
    {
        var aoi = CustomAOIInterestManagement.Instance;
        if (aoi == null) return;

        // Subject cleanup
        aoi.NotifySubjectStateChanged(netIdentity, CustomAOIInterestManagement.AOIChangeReason.Despawn);

        // Observer cleanup for players
        if (connectionToClient != null)
            aoi.NotifyObserverDisconnected(connectionToClient);
    }

    // Called once per server frame after movement is committed in Entity.Update()
    public void AOI_TryNotifyMove()
    {
        if (!_aoiInitialized) return;
        var aoi = CustomAOIInterestManagement.Instance;
        if (aoi == null) return;

        Vector3 pos = transform.position;
        var cell = WorldToCell(pos);
        if (cell != _aoiLastCell)
        {
            _aoiLastCell = cell;

            // This entity can be observed…
            aoi.NotifySubjectMoved(netIdentity, pos);

            // …and if it’s a player, it also observes
            if (connectionToClient != null)
                aoi.NotifyObserverMoved(connectionToClient, pos);
        }
    }

    // Optional helper if you need to notify AOI on teleports/stealth toggles from other scripts
    public void AOI_NotifySubjectStateChanged(CustomAOIInterestManagement.AOIChangeReason reason)
        => CustomAOIInterestManagement.Instance?.NotifySubjectStateChanged(netIdentity, reason);

    // -------- helpers --------
    static Vector2Int WorldToCell(Vector3 p)
    {
        var inst = CustomAOIInterestManagement.Instance;
        float size = (inst && inst.settings) ? inst.settings.cellSizeMeters : 64f;
        return new Vector2Int(Mathf.FloorToInt(p.x / size), Mathf.FloorToInt(p.z / size));
    }
#endif
}
