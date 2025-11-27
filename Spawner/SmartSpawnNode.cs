using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class SmartSpawnNode : MonoBehaviour
{
    [Header("Spawn Table (Randomized)")]
    public List<SpawnEntry> spawnTable = new();

    [Header("Spawn Settings")]
    [Tooltip("Radius to slightly offset the spawn location randomly")] public float spawnRadius = 2f;
    [Tooltip("Cooldown in seconds between spawns")] public float cooldown = 60f;
    [Tooltip("Maximum entities allowed concurrently spawned from this node")] public int maxConcurrent = 1;
    [Tooltip("If true, first spawn attempt isn’t blocked by the cooldown")] public bool spawnImmediatelyOnStart = true;

    [Header("Activation & Safety (AOI)")]
    [Tooltip("Priority lane: 0=Low,1=Normal,2=High,3=Critical")] [Range(0,3)] public int priority = 1;
    [Tooltip("Optional global class tag for population caps (e.g., 'Undead','Herb')")] public string classTag = "";
    [Tooltip("If >0, a global cap for all spawns with this classTag across the scene")] public int globalClassMax = 0;
    [Tooltip("At least one player must be within this radius to allow a spawn attempt")] public float activationRadius = 30f;
    [Tooltip("No player may be closer than this to the node when spawning occurs")] public float spawnSafetyBuffer = 5f;

    [Header("Time Window (UTC)")]
    [Tooltip("Restrict spawns to a UTC hour window (optional)")] public bool useTimeWindow = false;
    [Range(0, 23)] public int allowedStartHour = 0;   // inclusive
    [Range(0, 23)] public int allowedEndHour = 23;    // exclusive

    [Header("Despawn Policy")]
    [Tooltip("If true, active mobs from this node will despawn if no players are nearby")] public bool despawnIfNoPlayers = true;
    [Tooltip("Distance within which a player must remain to prevent despawn")] public float despawnDistance = 50f;
    [Tooltip("Minimum idle time (in seconds) before mob can be considered for despawn")] public float despawnIdleTime = 60f;
    [Tooltip("If true, this node will never despawn mobs automatically")] public bool alwaysKeepAlive = false;

    [Header("Physics Safety (Optional)")]
    [Tooltip("Check ground slope & overlaps before spawning")] public bool usePhysicsSafety = false;
    [Tooltip("Maximum allowed ground slope angle in degrees")] [Range(0, 85)] public float slopeLimit = 45f;
    [Tooltip("Capsule half-height for overlap check")] public float capsuleHalfHeight = 0.9f;
    [Tooltip("Capsule radius for overlap check")] public float overlapRadius = 0.4f;
    [Tooltip("Height above candidate point to raycast down from")] public float safetyRayHeight = 3f;
    [Tooltip("Max random probes per spawn attempt")] [Range(1, 8)] public int safetyProbeCount = 4;
    [Tooltip("Ground layers for raycast")] public LayerMask groundMask = ~0;
    [Tooltip("Obstacles to avoid (capsule overlap)")] public LayerMask obstacleMask = ~0;

    // runtime state
    [HideInInspector] public float lastSpawnTime = 0f;
    [HideInInspector] public float nextCheckTime = 0f;
    [HideInInspector] public int activeCount = 0;
    [HideInInspector] public readonly List<GameObject> spawned = new List<GameObject>();

    // --- New: robust cleanup & notifications -------------------------------

    /// <summary>
    /// Call this when you successfully spawn an entity from this node.
    /// Safe to call multiple times for different instances.
    /// </summary>
    public void RegisterSpawned(GameObject go)
    {
        if (go == null) return;
        spawned.Add(go);
        activeCount = spawned.Count;
        lastSpawnTime = Time.time; // start cooldown from the actual spawn
    }

    /// <summary>
    /// Call this from the entity's despawn/OnDestroy to keep counts accurate.
    /// </summary>
    public void RegisterDespawn(GameObject go)
    {
        int removed = spawned.RemoveAll(s => s == null || s == go);
        if (removed > 0)
        {
            activeCount = spawned.Count;
            // If nothing remains, clear cooldown so we can spawn again immediately
            // when a player returns (honors spawnImmediatelyOnStart behavior).
            if (activeCount == 0)
                lastSpawnTime = 0f;
        }
    }

    /// <summary>
    /// Server-side periodic prune of destroyed references. Keeps 'activeCount' accurate
    /// even if an entity was removed by other systems without calling RegisterDespawn.
    /// </summary>
    public void PruneSpawned()
    {
        int removed = spawned.RemoveAll(s => s == null);
        if (removed > 0)
        {
            activeCount = spawned.Count;
            if (activeCount == 0)
                lastSpawnTime = 0f;
        }
    }

    void LateUpdate()
    {
        // Housekeeping at most twice per second
        if (!Application.isPlaying) return;
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + 0.5f;
            PruneSpawned();
        }
    }

#if UNITY_EDITOR
    [Header("Gizmos (Editor Only)")]
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.25f);
    public Color gizmoWireColor = new Color(0f, 0f, 0f, 0.5f);

    void OnValidate()
    {
        if (spawnRadius < 0f) spawnRadius = 0f;
        if (cooldown < 0f) cooldown = 0f;
        if (maxConcurrent < 1) maxConcurrent = 1;
        if (activationRadius < 0.1f) activationRadius = 0.1f;
        float maxSafe = Mathf.Max(0f, activationRadius - 0.1f);
        if (spawnSafetyBuffer > maxSafe) spawnSafetyBuffer = maxSafe;
        if (spawnSafetyBuffer < 0f) spawnSafetyBuffer = 0f;
        if (despawnDistance < 0f) despawnDistance = 0f;
        allowedStartHour = Mathf.Clamp(allowedStartHour, 0, 23);
        allowedEndHour = Mathf.Clamp(allowedEndHour, 0, 23);
        if (capsuleHalfHeight < 0.25f) capsuleHalfHeight = 0.25f;
        if (overlapRadius < 0.1f) overlapRadius = 0.1f;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        float fill = Mathf.Clamp01(maxConcurrent > 0 ? (activeCount / (float)maxConcurrent) : 0f);
        Color dynamic = Color.Lerp(gizmoColor, Color.red, fill);
        Gizmos.color = dynamic; Gizmos.DrawSphere(position, Mathf.Max(0.1f, spawnRadius));
        Gizmos.color = gizmoWireColor; Gizmos.DrawWireSphere(position, Mathf.Max(activationRadius, 0.1f));
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f); Gizmos.DrawWireSphere(position, Mathf.Clamp(spawnSafetyBuffer, 0f, activationRadius));
    }
#endif
}