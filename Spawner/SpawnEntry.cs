using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnEntry
{
    [Tooltip("Prefab to spawn")] public GameObject prefab;

    [Tooltip("Relative chance weight for this entry (<=0 disables)")]
    public float chance = 1f;

    [Tooltip("If true, use SmartSpawnPool for this prefab")] public bool usePooling = true;

    [Tooltip("Optional override for runtime instance name (useful for profiling/logs)")]
    public string overrideName = "";

    [Tooltip("Optional semantic tags applied to SpawnedMeta.tags on spawn")] public List<string> tags = new();
}
