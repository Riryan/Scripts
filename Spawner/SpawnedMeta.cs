using UnityEngine;
using System.Collections.Generic;

public class SpawnedMeta : MonoBehaviour
{
    [Header("Spawn Source (runtime)")]
    [Tooltip("The spawn node that created this instance (server-owned).")]
    public SmartSpawnNode spawnedFromNode;

    [Tooltip("Original prefab reference used by the pool (optional).")]
    public GameObject spawnedFromPrefab;

    [Tooltip("If true, this instance participates in pooling on Despawn.")]
    public bool usePooling = true;

    [Header("Tags (optional)")]
    public List<string> tags = new List<string>(4);

    [Header("Accounting")]
    [Tooltip("Has this instance already been accounted for despawn (to avoid double decrement)?")]
    public bool accounted = false;

#if UNITY_SERVER || UNITY_EDITOR
    void OnDestroy()
    {
        // Server-side accounting guard:
        // Ensure we only inform the manager once per destroyed/despawned instance.
        var mgr = SmartSpawnManager_Scene.singleton;
        if (mgr != null && spawnedFromNode != null && !accounted)
        {
            accounted = true;
            mgr.NotifyDespawn(spawnedFromNode);
        }
    }
#endif
}
