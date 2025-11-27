using UnityEngine;
using Mirror;
using System.Collections.Generic;

public static class SmartSpawnPool
{
    static readonly Dictionary<GameObject, Queue<GameObject>> pool = new();
    static readonly Dictionary<GameObject, float> lastUsedTime = new();

    // Max inactive pooled objects per prefab
    public static int maxPoolSizePerPrefab = 10;

    // How long a prefab’s pool can stay unused before being purged
    public static float coldPoolTimeout = 300f; // seconds

    /// <summary>
    /// Get an instance of the prefab. If pooling is enabled and an instance exists, reuse it; otherwise instantiate.
    /// The caller is responsible for NetworkServer.Spawn after this returns (if networking is desired).
    /// </summary>
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, bool usePooling)
    {
        if (prefab == null) return null;

        GameObject instance = null;
        if (usePooling && pool.TryGetValue(prefab, out var q) && q.Count > 0)
        {
            // reuse
            instance = q.Dequeue();
            if (instance != null)
            {
                instance.transform.SetPositionAndRotation(position, rotation);
                instance.SetActive(true);
            }
        }

        if (instance == null)
        {
            // instantiate new
            instance = Object.Instantiate(prefab, position, rotation);
        }

        // Ensure metadata exists and is refreshed (runtime-added; no prefab bloat)
        var meta = instance.GetComponent<SpawnedMeta>();
        if (meta == null) meta = instance.AddComponent<SpawnedMeta>();
        meta.spawnedFromPrefab = prefab;
        meta.usePooling = usePooling;
        // tags will be assigned/cleared by the spawner based on SpawnEntry

        // Mark pool as recently used
        lastUsedTime[prefab] = Time.unscaledTime;

        return instance;
    }

    /// <summary>
    /// Return or destroy an instance.
    /// If pooling is enabled, UnSpawn it from Mirror and enqueue inactive. Otherwise destroy.
    /// </summary>
    public static void Despawn(GameObject instance, GameObject prefabHint, bool usePooling)
    {
        if (instance == null)
            return;

        // Try to resolve prefab from SpawnedMeta when available
        GameObject prefab = prefabHint;
        var meta = instance.GetComponent<SpawnedMeta>();
        if (meta != null && meta.spawnedFromPrefab != null)
            prefab = meta.spawnedFromPrefab;

        if (!usePooling || prefab == null)
        {
            // Hard destroy (network- and scene-safe)
            if (NetworkServer.active)
                NetworkServer.Destroy(instance);
            else
                Object.Destroy(instance);
            return;
        }

        // Pooling path: stop networking and queue the object for reuse
        if (NetworkServer.active)
        {
            // Unspawn keeps the object alive but removes it from replication
            NetworkServer.UnSpawn(instance);
        }

        instance.SetActive(false);

        if (!pool.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>(maxPoolSizePerPrefab);
            pool[prefab] = q;
        }

        if (q.Count < maxPoolSizePerPrefab)
        {
            q.Enqueue(instance);
            lastUsedTime[prefab] = Time.unscaledTime;
        }
        else
        {
            // Pool is full
            Object.Destroy(instance);
        }
    }

    /// <summary>
    /// Periodically purge pools that have been idle longer than coldPoolTimeout.
    /// Call this from a light maintenance tick (e.g., every 1s or few seconds).
    /// </summary>
    public static void CleanupColdPools()
    {
        float now = Time.unscaledTime;
        var toRemove = new List<GameObject>();

        foreach (var kvp in lastUsedTime)
        {
            var prefab = kvp.Key;
            float last = kvp.Value;
            if (now - last > coldPoolTimeout)
            {
                if (pool.TryGetValue(prefab, out var q))
                {
                    while (q.Count > 0)
                    {
                        var obj = q.Dequeue();
                        if (obj != null)
                            Object.Destroy(obj);
                    }
                    pool.Remove(prefab);
                }
                toRemove.Add(prefab);
            }
        }

        foreach (var prefab in toRemove)
            lastUsedTime.Remove(prefab);
    }

    /// <summary>
    /// Immediately destroys all pooled instances and clears all bookkeeping.
    /// Useful on world unloads if you want a hard wipe instead of waiting for timeout.
    /// </summary>
    public static void ClearAll()
    {
        foreach (var kvp in pool)
        {
            var q = kvp.Value;
            while (q.Count > 0)
            {
                var obj = q.Dequeue();
                if (obj != null)
                    Object.Destroy(obj);
            }
        }
        pool.Clear();
        lastUsedTime.Clear();
    }
}
