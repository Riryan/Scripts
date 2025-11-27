using System.Collections.Generic;
using UnityEngine;

public partial class NetworkManagerMMO
{
#if UNITY_SERVER || UNITY_EDITOR
    // server/editor only: tombstone registry
    public readonly Dictionary<int, Tombstone> tombstones = new();

    public void RegisterTombstone(Tombstone t)
    {
        if (tombstones.ContainsKey(t.tombstoneId))
            Debug.LogWarning($"Duplicate TombstoneID {t.tombstoneId} on {t.name}");
        tombstones[t.tombstoneId] = t;
    }

    public void UnregisterTombstone(Tombstone t)
    {
        // only remove if this instance is the one currently registered
        if (tombstones.TryGetValue(t.tombstoneId, out var current) && current == t)
            tombstones.Remove(t.tombstoneId);
    }
#endif

    // this method must exist in all builds because Player calls it
    public Transform GetTombstoneSpawn(int tombstoneId)
    {
#if UNITY_SERVER || UNITY_EDITOR
        if (tombstones != null &&
            tombstones.TryGetValue(tombstoneId, out var t) &&
            t.spawnPoint != null)
        {
            return t.spawnPoint;
        }
#endif
        // no tombstone found for this ID
        return null;
    }
}
