using UnityEngine;
using Mirror;

public partial class Player
{
    [Header("Graveyard System")]
    [SyncVar] public int graveyardTombstoneId = -1; // -1 = none

    public bool HasGraveyard => graveyardTombstoneId >= 0;

    // called by the server when binding to a tombstone
    [Server]
    public void BindToTombstone(int tombstoneId)
    {
        if (tombstoneId < 0) return;
        if (graveyardTombstoneId == tombstoneId) return;

        graveyardTombstoneId = tombstoneId;
        Debug.Log($"{name} bound to tombstone {tombstoneId}");
        // --- DB DRY-RUN LOG START ---
        // this simulates what we would later pass into Database.SaveCharacter(...)
        var nm = NetworkManager.singleton as NetworkManagerMMO;
        Vector3 spawnPos = Vector3.zero;
        string spawnInfo = "unknown";

        if (nm != null)
        {
            var spawn = nm.GetTombstoneSpawn(graveyardTombstoneId);
            if (spawn != null)
            {
                spawnPos = spawn.position;
                spawnInfo = $"{spawnPos.x:F1}, {spawnPos.y:F1}, {spawnPos.z:F1}";
            }
        }

        string sceneName = gameObject.scene.name;
        Debug.Log($"[DB TEST][Graveyard] Would save graveyardTombstoneId={graveyardTombstoneId} " +
                  $"for character={name} in scene={sceneName} (spawn={spawnInfo})");
        // --- DB DRY-RUN LOG END ---
    }

    // main respawn entry: called by the DEAD state when the player hits Respawn
    [Server]
    public void HandleGraveyardRespawn()
    {
        Transform spawn = null;

        // 1) try bound graveyard via the active NetworkManager, if it's our MMO manager
        if (HasGraveyard &&
            NetworkManager.singleton is NetworkManagerMMO manager &&
            manager != null)
        {
            spawn = manager.GetTombstoneSpawn(graveyardTombstoneId);
        }

        // 2) fallback: original behavior (nearest start position)
        if (spawn == null)
        {
            spawn = NetworkManagerMMO.GetNearestStartPosition(transform.position);
        }

        if (spawn != null)
        {
            // match original respawn behavior in Player.UpdateServer_DEAD
            movement.Warp(spawn.position);
            Revive(0.5f);
        }
        else
        {
            Debug.LogWarning($"No spawn found for {name}, staying dead.");
        }
    }
}
