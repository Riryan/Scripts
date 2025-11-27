#if UNITY_SERVER || UNITY_EDITOR
using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using System.Reflection;

[DisallowMultipleComponent]
public class SmartSpawnManager_Scene : NetworkBehaviour
{
    public static SmartSpawnManager_Scene singleton;

    [Header("Discovery")]
    [Tooltip("If true, nodes are auto-discovered on server start")] public bool autoDiscoverNodes = true;

    [Header("Tick & Budget (Server)")]
    [Tooltip("How often we snapshot player positions (seconds)")] public float playerSnapshotInterval = 0.25f;
    [Tooltip("How often we run lightweight maintenance (seconds)")] public float maintenanceInterval = 0.5f;
    [Tooltip("How often each node is reconsidered for a spawn attempt (seconds)")] public float aoiCheckInterval = 1.0f;
    [Tooltip("Base max spawn attempts processed per server tick (before budget)")] public int maxSpawnAttemptsPerTick = 8;

    [Header("AOI Grid (Players → Nodes)")]
    [Tooltip("Use a fixed-grid for player proximity checks (recommended for 1000+ players)")]
    public bool useAOIGrid = true;
    [Tooltip("Grid cell size in meters (choose near typical activationRadius/2)")]
    public float gridCellSize = 12f;

    [Header("Budget Monitor")]
    [Tooltip("Adapt spawn attempts to keep spawner under budget per frame")] public bool useBudgetMonitor = true;
    [Tooltip("Spawner time budget in milliseconds per frame on server")] public float spawnerBudgetMs = 0.6f;
    [Tooltip("Hard cap for spawn attempts even when under budget")] public int hardCapAttempts = 24;

    [Header("Metrics (Server)")]
    public int lastTickNodesEvaluated;      // nodes looked at this tick
    public int lastTickGatesPassed;         // nodes that passed AOI+cooldown+window
    public int lastTickSpawnAttempts;       // attempts this tick (reset every tick)
    public int lastTickSpawnsSucceeded;     // successes this tick (reset every tick)
    public int totalSpawnAttempts;          // running total (never resets)
    public int totalSpawns;                 // running total (never resets)
    public float attemptsPerSecond;         // 1s rolling
    public float lastTickDurationMs;        // spawner work only
    public float avgTickDurationMs;         // EMA of spawner work
    [Range(0.01f, 1f)] public float emaSmoothing = 0.18f;
    public int currentAttemptCap;

    [Header("Rejections (this tick)")]
    public int lastTickRejectActiveCap;
    public int lastTickRejectTimeWindow;
    public int lastTickRejectAOIActivation;
    public int lastTickRejectAOISafety;
    public int lastTickRejectCooldown;
    public int lastTickRejectClassCap;
    public int lastTickRejectSafetyProbe;
    public int lastTickRejectNetCap;

    // === POWER MODE MASTER SWITCH ===
    [SerializeField, Tooltip("Enables adaptive spawn orchestration features (server-first).")]
    private bool powerMode = false;
    public bool PowerModeEnabled { get; private set; }

    [Header("Network Budget (Server)")]
    [Tooltip("Max networked spawns allowed per frame (server-side)")] public int maxSpawnsPerFrame = 32;
    [Tooltip("Max networked spawns allowed per second (server-side)")] public int maxSpawnsPerSecond = 400;
    public int spawnsThisFrame;
    public int spawnsThisSecond;

    [Header("Requeue Backoff (safety/player-close)")]
    [Tooltip("Min seconds to delay node after safety/player-close failure at dequeue.")]
    public float requeueBackoffMin = 1.5f;
    [Tooltip("Max seconds to delay node after safety/player-close failure at dequeue.")]
    public float requeueBackoffMax = 4.0f;

    [Header("Diagnostics")]
    public bool logSpawns = false;

    // runtime
    readonly List<SmartSpawnNode> nodes = new List<SmartSpawnNode>(512);
    static readonly List<Vector3> _playerPositions = new List<Vector3>(2048);

    // AOI grid
    struct Cell { public int x, z; public Cell(int x, int z) { this.x = x; this.z = z; } public override int GetHashCode() { unchecked { return (x * 73856093) ^ (z * 19349663); } } public override bool Equals(object o) { if (!(o is Cell)) return false; var c = (Cell)o; return c.x == x && c.z == z; } }
    readonly Dictionary<Cell, List<Vector3>> _grid = new Dictionary<Cell, List<Vector3>>(1024);

    float _nextSnapshotTime;
    float _nextMaintenanceTime;

    readonly Queue<SmartSpawnNode>[] _spawnQueues = new Queue<SmartSpawnNode>[4] {
        new Queue<SmartSpawnNode>(128),
        new Queue<SmartSpawnNode>(256),
        new Queue<SmartSpawnNode>(256),
        new Queue<SmartSpawnNode>(128)
    };

    // global per-class counts (compatible via reflection on SmartSpawnNode.classTag)
    readonly Dictionary<string, int> _classCounts = new Dictionary<string, int>(16);

    // metrics helpers
    float _apsWindowNext;
    int _apsWindowCount;
    float _spsWindowNext;

    void Awake() => singleton = this;

    public override void OnStartServer()
    {
        base.OnStartServer();
        PowerModeEnabled = powerMode; // actually enable deterministic jitter etc.
        if (autoDiscoverNodes) DiscoverNodes();
        _nextSnapshotTime = Time.time;
        _nextMaintenanceTime = Time.time + maintenanceInterval;
        _apsWindowNext = Time.time + 1f;
        _spsWindowNext = Time.time + 1f;
    }

    void OnDestroy()
    {
        if (singleton == this) singleton = null;
        SmartSpawnPool.CleanupColdPools();
    }

    void Update()
    {
#if UNITY_SERVER || UNITY_EDITOR
        if (!NetworkServer.active) return; // host/headless only
        ServerTick();
#else
        // client-only build: do nothing
#endif
    }

#if UNITY_SERVER || UNITY_EDITOR
    void ServerTick()
    {
        spawnsThisFrame = 0;
        if (Time.time >= _spsWindowNext) { spawnsThisSecond = 0; _spsWindowNext = Time.time + 1f; }

        lastTickNodesEvaluated = lastTickGatesPassed = 0;
        lastTickSpawnAttempts = lastTickSpawnsSucceeded = 0;
        lastTickRejectActiveCap = lastTickRejectTimeWindow = 0;
        lastTickRejectAOIActivation = lastTickRejectAOISafety = 0;
        lastTickRejectCooldown = lastTickRejectClassCap = 0;
        lastTickRejectSafetyProbe = lastTickRejectNetCap = 0;

        float now = Time.time;

        if (now >= _nextSnapshotTime)
        {
            SnapshotPlayerPositions();
            if (useAOIGrid) BuildPlayerGrid(); else _grid.Clear();
            _nextSnapshotTime = now + playerSnapshotInterval;
        }

        if (now >= _nextMaintenanceTime)
        {
            Maintenance(now);
            _nextMaintenanceTime = now + maintenanceInterval;
        }

       EnqueueReadyNodes(now);
        float t0 = Time.realtimeSinceStartup;
        ProcessSpawnQueue(now);
        lastTickDurationMs = (Time.realtimeSinceStartup - t0) * 1000f;
        avgTickDurationMs = Mathf.Lerp(avgTickDurationMs, lastTickDurationMs, emaSmoothing);

        // attempts per second window
        _apsWindowCount += lastTickSpawnAttempts;
        if (now >= _apsWindowNext)
        {
            attemptsPerSecond = _apsWindowCount;
            _apsWindowCount = 0;
            _apsWindowNext = now + 1f;
        }
    }

    void DiscoverNodes()
    {
        nodes.Clear();
        var found = FindObjectsOfType<SmartSpawnNode>(true);
        foreach (var n in found)
        {
            nodes.Add(n);
            SeedNode(n);
        }
    }

    public void RegisterNode(SmartSpawnNode node)
    {
        if (!NetworkServer.active || node == null) return;
        if (!nodes.Contains(node)) { nodes.Add(node); SeedNode(node); }
    }
    public void UnregisterNode(SmartSpawnNode node) { if (node == null) return; nodes.Remove(node); }

    void SeedNode(SmartSpawnNode node)
    {
        if (node.spawnImmediatelyOnStart)
        {
            node.lastSpawnTime = Time.time - (node.cooldown + 0.001f);
            node.nextCheckTime = Time.time;
        }
        else
        {
            float jitter01 = PowerModeEnabled ? DeterministicJitter01(node.transform.position) : UnityEngine.Random.value;
            node.nextCheckTime = Time.time + jitter01 * aoiCheckInterval;
        }
        node.activeCount = Mathf.Max(0, node.spawned?.Count ?? 0);
    }

    static float DeterministicJitter01(Vector3 p)
    {
        unchecked
        {
            int hx = Mathf.FloorToInt(p.x * 10f);
            int hz = Mathf.FloorToInt(p.z * 10f);
            int h = hx * 73856093 ^ hz * 19349663;
            uint x = (uint)h;
            x ^= (x << 13);
            x ^= (x >> 17);
            x ^= (x << 5);
            return (x & 0x00FFFFFFu) / 16777216f; // 2^24
        }
    }

    float ComputeBackoffSeconds(SmartSpawnNode node)
    {
        float jitter01 = PowerModeEnabled ? DeterministicJitter01(node.transform.position) : UnityEngine.Random.value;
        return Mathf.Lerp(Mathf.Min(requeueBackoffMin, requeueBackoffMax), Mathf.Max(requeueBackoffMin, requeueBackoffMax), jitter01);
    }

    // ===== AOI =====
    void SnapshotPlayerPositions()
    {
        _playerPositions.Clear();
        var seen = new HashSet<uint>();
        foreach (var kvp in NetworkServer.connections)
        {
            var id = kvp.Value?.identity;
            if (id != null && seen.Add(id.netId)) _playerPositions.Add(id.transform.position);
        }
        var local = NetworkServer.localConnection;
        if (local != null && local.identity != null)
        {
            var lid = local.identity;
            if (seen.Add(lid.netId)) _playerPositions.Add(lid.transform.position);
        }
    }

    void BuildPlayerGrid()
    {
        _grid.Clear();
        float inv = 1f / Mathf.Max(0.01f, gridCellSize);
        for (int i = 0; i < _playerPositions.Count; ++i)
        {
            var p = _playerPositions[i];
            int cx = Mathf.FloorToInt(p.x * inv);
            int cz = Mathf.FloorToInt(p.z * inv);
            var cell = new Cell(cx, cz);
            if (!_grid.TryGetValue(cell, out var list)) { list = new List<Vector3>(4); _grid[cell] = list; }
            list.Add(p);
        }
    }

    bool AnyPlayersWithinAOI(Vector3 pos, float radius)
    {
        if (_playerPositions.Count == 0) return false;
        if (!useAOIGrid || _grid.Count == 0)
        {
            float r2 = radius * radius;
            for (int i = 0; i < _playerPositions.Count; ++i)
                if ((pos - _playerPositions[i]).sqrMagnitude <= r2)
                    return true;
            return false;
        }
        // grid path
        float inv = 1f / Mathf.Max(0.01f, gridCellSize);
        int cx = Mathf.FloorToInt(pos.x * inv);
        int cz = Mathf.FloorToInt(pos.z * inv);
        int r = Mathf.CeilToInt(radius * inv);
        float r2g = radius * radius;
        for (int dz = -r; dz <= r; ++dz)
        {
            for (int dx = -r; dx <= r; ++dx)
            {
                var cell = new Cell(cx + dx, cz + dz);
                if (!_grid.TryGetValue(cell, out var list)) continue;
                for (int i = 0; i < list.Count; ++i)
                    if ((pos - list[i]).sqrMagnitude <= r2g)
                        return true;
            }
        }
        return false;
    }

    void Maintenance(float now)
    {
        for (int n = 0; n < nodes.Count; ++n)
        {
            var node = nodes[n];
            if (node == null) continue;

            if (node.spawnSafetyBuffer > node.activationRadius - 0.1f)
                node.spawnSafetyBuffer = Mathf.Max(0f, node.activationRadius - 0.1f);

            for (int i = node.spawned.Count - 1; i >= 0; --i)
            {
                var go = node.spawned[i];
                if (go == null) { node.spawned.RemoveAt(i); continue; }

                if (!node.alwaysKeepAlive && node.despawnIfNoPlayers)
                {
                    bool playersNear = AnyPlayersWithinAOI(go.transform.position, node.despawnDistance);
                    if (!playersNear)
                    {
                        var meta = go.GetComponent<SpawnedMeta>();
                        if (meta != null) meta.accounted = true; // avoid OnDestroy double-decrement on hard destroy path
                        SmartSpawnPool.Despawn(go, meta != null ? meta.spawnedFromPrefab : null, meta != null && meta.usePooling);
                        node.spawned.RemoveAt(i);
                        NotifyDespawn(node);
                    }
                }
            }
            node.activeCount = node.spawned.Count;
        }
        SmartSpawnPool.CleanupColdPools();
    }

    void EnqueueReadyNodes(float now)
    {
        for (int n = 0; n < nodes.Count; ++n)
        {
            var node = nodes[n];
            if (node == null) continue;
            if (now < node.nextCheckTime) continue;
            lastTickNodesEvaluated++;

            node.nextCheckTime = now + aoiCheckInterval;

            // Gates
            var pos = node.transform.position;
            if (node.activeCount >= node.maxConcurrent) { lastTickRejectActiveCap++; continue; }
            if (node.useTimeWindow && !IsWithinUtcWindow(node.allowedStartHour, node.allowedEndHour)) { lastTickRejectTimeWindow++; continue; }
            if (!AnyPlayersWithinAOI(pos, node.activationRadius)) { lastTickRejectAOIActivation++; continue; }
            if (AnyPlayersWithinAOI(pos, node.spawnSafetyBuffer)) { lastTickRejectAOISafety++; continue; }
            if (now < node.lastSpawnTime + node.cooldown) { lastTickRejectCooldown++; continue; }

            lastTickGatesPassed++;
            int lane = Mathf.Clamp(GetNodePriority(node), 0, 3);
            _spawnQueues[lane].Enqueue(node);
        }
    }

    int ComputeAttemptCap()
    {
        int baseCap = Mathf.Max(1, maxSpawnAttemptsPerTick);
        if (!useBudgetMonitor)
        {
            currentAttemptCap = baseCap;
            return currentAttemptCap;
        }

        // backlog across all lanes
        int backlog = 0;
        for (int i = 0; i < _spawnQueues.Length; ++i) backlog += _spawnQueues[i].Count;

        float budget = Mathf.Max(0.0001f, spawnerBudgetMs);
        float ratio = avgTickDurationMs / budget;

        int cap = baseCap;

        // If under-utilizing budget and backlog exists, gently ramp up towards hard cap
        if (backlog > 0 && ratio < 0.6f)
        {
            float headroom = Mathf.Clamp01(0.6f - ratio) / 0.6f; // 0..1
            cap = baseCap + Mathf.CeilToInt(headroom * (hardCapAttempts - baseCap));
        }
        // If over budget, ramp down towards 1
        else if (ratio > 1.0f)
        {
            float pressure = Mathf.Clamp01((ratio - 1f) / 1f);
            cap = Mathf.RoundToInt(Mathf.Lerp(baseCap, 1, pressure));
        }
        cap = Mathf.Clamp(cap, 1, hardCapAttempts);
        currentAttemptCap = cap;
        return currentAttemptCap;
    }

    void ProcessSpawnQueue(float now)
    {
        int attemptsLeft = ComputeAttemptCap();
        if (attemptsLeft <= 0) return;

        int[] weights = new int[] { 1, 1, 2, 3 };
        int weightSum = 0; for (int w = 0; w < weights.Length; ++w) weightSum += weights[w];
        int[] quota = new int[4];
        int rem = attemptsLeft;
        for (int l = 3; l >= 0; --l) { quota[l] = Mathf.Max(0, Mathf.FloorToInt(attemptsLeft * (weights[l] / (float)weightSum))); rem -= quota[l]; }
        for (int l = 3; l >= 0 && rem > 0; --l) { quota[l]++; rem--; }

        int safetyLoopGuard = 8192;
        while (attemptsLeft > 0 && safetyLoopGuard-- > 0)
        {
            bool didWork = false;
            for (int lane = 3; lane >= 0 && attemptsLeft > 0; --lane)
            {
                if (quota[lane] <= 0) continue;
                if (_spawnQueues[lane].Count == 0) continue;

                var node = _spawnQueues[lane].Dequeue();
                if (node == null) { quota[lane]--; continue; }

                var pos = node.transform.position;
                if (node.activeCount >= node.maxConcurrent) { lastTickRejectActiveCap++; quota[lane]--; continue; }
                if (node.useTimeWindow && !IsWithinUtcWindow(node.allowedStartHour, node.allowedEndHour)) { lastTickRejectTimeWindow++; quota[lane]--; continue; }
                int globalMax = GetNodeGlobalClassMax(node);
                string classTag = GetNodeClassTag(node);
                if (globalMax > 0 && !string.IsNullOrEmpty(classTag) && GetClassCount(classTag) >= globalMax) { lastTickRejectClassCap++; quota[lane]--; continue; }
                if (!AnyPlayersWithinAOI(pos, node.activationRadius)) { lastTickRejectAOIActivation++; quota[lane]--; continue; }
                if (AnyPlayersWithinAOI(pos, node.spawnSafetyBuffer)) { lastTickRejectAOISafety++; quota[lane]--; node.nextCheckTime = now + ComputeBackoffSeconds(node); continue; }
                if (now < node.lastSpawnTime + node.cooldown) { lastTickRejectCooldown++; quota[lane]--; continue; }

                // Network caps: if reached, stop processing this frame
                if (spawnsThisFrame >= maxSpawnsPerFrame || spawnsThisSecond >= maxSpawnsPerSecond)
                {
                    lastTickRejectNetCap++;
                    return;
                }

                // pick an entry
                var entry = PickWeighted(node.spawnTable);
                lastTickSpawnAttempts++;
                totalSpawnAttempts++;
                attemptsLeft--;
                quota[lane]--;

                if (entry == null || entry.prefab == null) { if (logSpawns) Debug.Log($"[SmartSpawn] Node {node.name} roll returned null or missing prefab."); continue; }

                Vector3 spawnPos = RandomInsideXZ(node.transform.position, node.spawnRadius);
                if (node.usePhysicsSafety)
                {
                    if (!TryFindSafeSpawnPosition(node, node.transform.position, node.spawnRadius, ref spawnPos))
                    {
                        lastTickRejectSafetyProbe++;
                        if (logSpawns) Debug.Log($"[SmartSpawn] Safety failed at node {node.name}, skipping this attempt.");
                        node.nextCheckTime = now + ComputeBackoffSeconds(node);
                        continue;
                    }
                }
                if (AnyPlayersWithinAOI(spawnPos, node.spawnSafetyBuffer)) { lastTickRejectAOISafety++; node.nextCheckTime = now + ComputeBackoffSeconds(node); continue; }

                Quaternion rot = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);

                // spawn
                var instance = SmartSpawnPool.Spawn(entry.prefab, spawnPos, rot, entry.usePooling);
                if (instance == null) continue;
                if (!string.IsNullOrWhiteSpace(entry.overrideName)) instance.name = entry.overrideName;

                var meta = instance.GetComponent<SpawnedMeta>();
                if (meta != null)
                {
                    meta.spawnedFromNode = node;
                    meta.tags.Clear();
                    if (entry.tags != null && entry.tags.Count > 0) meta.tags.AddRange(entry.tags);
                }

                if (NetworkServer.active) NetworkServer.Spawn(instance);
                spawnsThisFrame++; spawnsThisSecond++;

                node.spawned.Add(instance);
                node.activeCount = node.spawned.Count;
                node.lastSpawnTime = now;
                lastTickSpawnsSucceeded++;
                totalSpawns++;
                if (globalMax > 0 && !string.IsNullOrEmpty(classTag)) IncClass(classTag);

                if (logSpawns) Debug.Log($"[SmartSpawn] Spawned '{entry.prefab.name}' from {node.name} at {spawnPos}");
                didWork = true;
            }
            if (!didWork) break;
        }
    }
#endif 

    // ===== Helpers (common) =====
    static Vector3 RandomInsideXZ(Vector3 center, float radius)
    {
        if (radius <= 0f) return center;
        Vector2 circle = UnityEngine.Random.insideUnitCircle * radius;
        return new Vector3(center.x + circle.x, center.y, center.z + circle.y);
    }

    static bool IsWithinUtcWindow(int startHour, int endHour)
    {
        if (startHour == endHour) return false; // disallowed
        int h = System.DateTime.UtcNow.Hour;
        if (startHour < endHour) return h >= startHour && h < endHour; // same-day
        return h >= startHour || h < endHour; // wrap-around across midnight
    }

#if UNITY_SERVER || UNITY_EDITOR
    static SpawnEntry PickWeighted(List<SpawnEntry> table)
    {
        if (table == null || table.Count == 0) return null;
        float sum = 0f;
        for (int i = 0; i < table.Count; ++i)
        {
            var e = table[i];
            if (e == null || e.prefab == null || e.chance <= 0f) continue;
            sum += e.chance;
        }
        if (sum <= 0f) return null;
        float r = UnityEngine.Random.value * sum;
        float acc = 0f;
        for (int i = 0; i < table.Count; ++i)
        {
            var e = table[i];
            if (e == null || e.prefab == null || e.chance <= 0f) continue;
            acc += e.chance;
            if (r <= acc) return e;
        }
        return null;
    }

    bool TryFindSafeSpawnPosition(SmartSpawnNode node, Vector3 center, float radius, ref Vector3 result)
    {
        // Fast early exit
        if (!node.usePhysicsSafety) return true;
        int tries = Mathf.Max(1, node.safetyProbeCount);
        float rayHeight = Mathf.Max(2f, node.safetyRayHeight);
        float maxRay = rayHeight + 6f;
        for (int i = 0; i < tries; ++i)
        {
            Vector3 cand = RandomInsideXZ(center, radius);
            Vector3 from = cand + Vector3.up * rayHeight;
            if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, maxRay, node.groundMask, QueryTriggerInteraction.Ignore))
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                if (angle > node.slopeLimit) continue;
                Vector3 p = hit.point;
                // Capsule from knees to head
                float hh = Mathf.Max(0.5f, node.capsuleHalfHeight);
                float rr = Mathf.Max(0.1f, node.overlapRadius);
                Vector3 p1 = p + Vector3.up * (hh);
                Vector3 p2 = p + Vector3.up * (hh * 2f);
                if (Physics.CheckCapsule(p1, p2, rr, node.obstacleMask, QueryTriggerInteraction.Ignore)) continue;
                result = p;
                return true;
            }
        }
        return false;
    }
    public void NotifyDespawn(SmartSpawnNode node)
    {
        if (node == null) return;
        node.activeCount = Mathf.Max(0, node.activeCount - 1);
        string classTag = GetNodeClassTag(node);
        int globalMax = GetNodeGlobalClassMax(node);
        if (globalMax > 0 && !string.IsNullOrEmpty(classTag)) DecClass(classTag);
    }
#endif 


    // These allow Manager to be patched first, before SmartSpawnNode adds new fields.
    int GetNodePriority(SmartSpawnNode node)
    {
        try
        {
            var f = typeof(SmartSpawnNode).GetField("priority", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(int))
            {
                int v = (int)f.GetValue(node);
                return Mathf.Clamp(v, 0, 3);
            }
        }
        catch { }
        return 1; // Normal lane by default
    }

    string GetNodeClassTag(SmartSpawnNode node)
    {
        try
        {
            var f = typeof(SmartSpawnNode).GetField("classTag", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(string))
            {
                return (string)f.GetValue(node) ?? string.Empty;
            }
        }
        catch { }
        return string.Empty;
    }

    int GetNodeGlobalClassMax(SmartSpawnNode node)
    {
        try
        {
            var f = typeof(SmartSpawnNode).GetField("globalClassMax", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(int))
            {
                return (int)f.GetValue(node);
            }
        }
        catch { }
        return 0;
    }

    int GetClassCount(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return 0;
        return _classCounts.TryGetValue(tag, out var c) ? c : 0;
    }
    void IncClass(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;
        _classCounts[tag] = GetClassCount(tag) + 1;
    }
    void DecClass(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;
        int c = GetClassCount(tag) - 1;
        _classCounts[tag] = Mathf.Max(0, c);
    }
}
#endif