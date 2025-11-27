#if UNITY_SERVER || UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Mirror;
using Debug = UnityEngine.Debug;

public interface IServerTick30 { void ServerTick30(float dt); }

public enum WorkCategory { Critical, Important, Ambient }

[DisallowMultipleComponent]
[DefaultExecutionOrder(-10000)]
public sealed class ServerRuntime30 : MonoBehaviour
{
#if UNITY_SERVER || UNITY_EDITOR
    public const float DT = 1f / 30f;
    [Tooltip("Optional: spread work by limiting how many tickables are processed per frame (0 = all).")]
    public int maxTickablesPerFrame = 0;
    [Header("CPU Budget (ms)")]
    [Tooltip("Target max average main-thread time per frame on server/headless.")]
    public float frameBudgetMs = 28f;
    [Header("Network Budget (bytes/sec)")]
    [Tooltip("Approximate total server bandwidth cap (both directions). 1 Gbps ~= 125,000,000 B/s.")]
    public long bandwidthCapBytesPerSec = 125_000_000;
    [Header("Category Ratios (permit share per frame)")]
    [Range(0,1)] public float criticalShare  = 0.60f;
    [Range(0,1)] public float importantShare = 0.30f;
    [Range(0,1)] public float ambientShare   = 0.10f;
    [Header("Admission Smoothing")]
    [Tooltip("Lerp factor for avg frame ms (0..1). Higher = faster response.")]
    [Range(0.01f, 0.5f)] public float smoothing = 0.12f;
    [Header("Snapshot Governor (Hz)")]
    [Tooltip("Normal snapshot send rate when under budget.")]
    public int highHz = 30;
    [Tooltip("Relief mode snapshot rate when over budget.")]
    public int lowHz = 20;
    [Tooltip("Seconds to wait between rate changes (down).")]
    public float downCooldownSec = 5f;
    [Tooltip("Seconds to wait between rate changes (up).")]
    public float upCooldownSec = 10f;
    [Header("Host Testing (Editor)")]
    [Tooltip("When running Host in Editor, force 30 Hz sim while keeping 60 FPS render.")]
    public bool enableOnHostInEditor = true;
    public float AvgFrameTimeMs { get; private set; }
    public float AvgFixedTimeMs { get; private set; }
    public float CpuUtilization  => frameBudgetMs <= 0 ? 0 : (AvgFrameTimeMs / frameBudgetMs);
    public long  TotalBandwidthPerSec => bytesSentPerSec + bytesRecvPerSec;
    public bool  IsCpuOverBudget => AvgFrameTimeMs > frameBudgetMs;
    public bool  IsNetOverBudget => bandwidthCapBytesPerSec > 0 && TotalBandwidthPerSec > bandwidthCapBytesPerSec;
    public int   SnapshotHz { get; private set; }
    public static ServerRuntime30 Instance { get; private set; }
    readonly Dictionary<IServerTick30, Entry> registry = new();
    readonly List<IServerTick30> scratch = new();
    struct Entry { public WorkCategory cat; public int baseCost; }
    int permitsCritical, permitsImportant, permitsAmbient;
    const int permitsTotal = 10_000;
    long bytesSentThisSec, bytesRecvThisSec;
    long bytesSentPerSec,  bytesRecvPerSec;
    float netWindow;
    float accumulator;
    readonly Stopwatch sw = new();
    float lastRateSwitchTime;
    float avgFrameMsGui, avgFixedMsGui;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate ServerRuntime30 detected. Destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SnapshotHz = highHz;
        ApplySnapshotHz(SnapshotHz);
    }

    void Update()
    {
#if !UNITY_SERVER || UNITY_EDITOR
        if (IsHostProcess() && enableOnHostInEditor)
            Time.fixedDeltaTime = DT;
#endif
        if (!NetworkServer.active) return;
        float dt = Time.unscaledDeltaTime;
        accumulator += dt;
        while (accumulator >= DT)
        {
            accumulator -= DT;
            ServerTick(DT);
        }
        AvgFrameTimeMs = Mathf.Lerp(AvgFrameTimeMs, Time.unscaledDeltaTime * 1000f, smoothing);

        netWindow += Time.unscaledDeltaTime;
        if (netWindow >= 1f)
        {
            bytesSentPerSec = bytesSentThisSec;
            bytesRecvPerSec = bytesRecvThisSec;
            bytesSentThisSec = bytesRecvThisSec = 0;
            netWindow = 0f;
        }
    }

    void FixedUpdate()
    {
        AvgFixedTimeMs = Mathf.Lerp(AvgFixedTimeMs, Time.fixedUnscaledDeltaTime * 1000f, smoothing);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    void ServerTick(float dt)
    {
        BeginFrame();
        scratch.Clear();
        foreach (var kv in registry) scratch.Add(kv.Key);

        int limit = maxTickablesPerFrame > 0 ? Mathf.Min(maxTickablesPerFrame, scratch.Count) : scratch.Count;

        for (int i = 0; i < limit; ++i)
        {
            var t = scratch[i];
            var e = registry[t];
            if (!RequestWork(e.cat, e.baseCost))
                continue;

            try { t.ServerTick30(dt); }
            catch (System.Exception ex) { Debug.LogException(ex); }
        }

        //ProjectileHitBuffer.DrainAndResolve(dt);

        MaybeAdjustSnapshotRate();

        EndFrame();
    }

    public void Register(IServerTick30 tickable, WorkCategory category, int baseCost = 1)
    {
        if (tickable == null) return;
        registry[tickable] = new Entry { cat = category, baseCost = Mathf.Max(1, baseCost) };
    }

    public void Unregister(IServerTick30 tickable)
    {
        if (tickable == null) return;
        registry.Remove(tickable);
    }

    public static void RegisterTickable(IServerTick30 t, WorkCategory c, int cost = 1)
    {
        EnsureInstance();
        Instance.Register(t, c, cost);
    }
    public static void UnregisterTickable(IServerTick30 t)
    {
        if (Instance == null) return;
        Instance.Unregister(t);
    }
    public void BeginFrame()
    {
        var load = CpuUtilization;

        float ambientMul   = Mathf.Clamp01(1.25f - load);
        float importantMul = Mathf.Clamp01(1.10f - (load - 0.2f));
        float criticalMul  = Mathf.Clamp01(1.05f - (load - 0.4f));
        permitsCritical  = Mathf.RoundToInt(permitsTotal * Mathf.Clamp01(criticalShare)  * Mathf.Max(0.1f, criticalMul));
        permitsImportant = Mathf.RoundToInt(permitsTotal * Mathf.Clamp01(importantShare) * Mathf.Max(0.1f, importantMul));
        permitsAmbient   = Mathf.RoundToInt(permitsTotal * Mathf.Clamp01(ambientShare)   * Mathf.Max(0.0f, ambientMul));

        sw.Restart();
    }

    public bool RequestWork(WorkCategory category, int baseCost)
    {
        baseCost = Mathf.Max(1, baseCost);
        switch (category)
        {
            case WorkCategory.Critical:
                if (permitsCritical >= baseCost) { permitsCritical -= baseCost; return true; }
                if (permitsImportant >= baseCost) { permitsImportant -= baseCost; return true; }
                return true;
            case WorkCategory.Important:
                if (permitsImportant >= baseCost) { permitsImportant -= baseCost; return true; }
                return false;
            default:
                if (permitsAmbient >= baseCost) { permitsAmbient -= baseCost; return true; }
                return false;
        }
    }

    public void EndFrame()
    {
        sw.Stop();
        float ms = (float)sw.Elapsed.TotalMilliseconds;
        AvgFrameTimeMs = Mathf.Lerp(AvgFrameTimeMs, ms, smoothing);
    }

    public void AddBytesSent(int count) { if (count > 0) bytesSentThisSec += count; }
    public void AddBytesRecv(int count) { if (count > 0) bytesRecvThisSec += count; }

    void MaybeAdjustSnapshotRate()
    {
        float now = Time.unscaledTime;
        if (AvgFrameTimeMs > frameBudgetMs)
        {
            if (SnapshotHz != lowHz && now - lastRateSwitchTime > downCooldownSec)
            {
                SnapshotHz = Mathf.Clamp(lowHz, 1, 120);
                ApplySnapshotHz(SnapshotHz);
                lastRateSwitchTime = now;
            }
        }
        else if (AvgFrameTimeMs <= frameBudgetMs * 0.8f)
        {
            if (SnapshotHz != highHz && now - lastRateSwitchTime > upCooldownSec)
            {
                SnapshotHz = Mathf.Clamp(highHz, 1, 120);
                ApplySnapshotHz(SnapshotHz);
                lastRateSwitchTime = now;
            }
        }
    }

// Replace the whole method in ServerRuntime30.cs
static void ApplySnapshotHz(int hz)
{
    // Optional hook: if you later add a compile symbol and a real API, you can short-circuit here.
    // #if HAS_MOVEMENT_SNAPSHOT_API
    //     Movement.SetSnapshotHz(hz);
    //     return;
    // #endif

    try
    {
        // Try to find a type named "Movement" and a static public SetSnapshotHz(int) method at runtime.
        var asms = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in asms)
        {
            System.Type t = null;
            try { t = asm.GetType("Movement"); } catch { /* ignore */ }
            if (t == null) continue;

            var mi = t.GetMethod("SetSnapshotHz",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static);

            if (mi != null)
            {
                mi.Invoke(null, new object[] { hz });
                break; // done
            }
        }
    }
    catch
    {
        // No-op: either Movement doesn't exist or it has no SetSnapshotHz API.
        // Step 1 doesn't require it; we're only monitoring perf.
    }
}
    static void EnsureInstance()
    {
        if (Instance != null) return;
        Instance = FindFirstObjectByType<ServerRuntime30>();
        if (Instance != null) return;
        var go = new GameObject("ServerRuntime30 (Auto)");
        Instance = go.AddComponent<ServerRuntime30>();
    }

    static bool IsHostProcess()
    {
        return NetworkServer.active && NetworkClient.isConnected;
    }

#endif 

#if !UNITY_SERVER || UNITY_EDITOR
    [Header("Debug Overlay (Host/Client only)")]
    public bool showStats = false;

    void OnGUI()
    {
        if (!showStats) return;
        const int pad = 6;
        const int w = 220;
        GUILayout.BeginArea(new Rect(pad, pad, w, 110), GUI.skin.box);
        GUILayout.Label("ServerRuntime30");
#if UNITY_SERVER
        GUILayout.Label("Mode: Server");
#else
        GUILayout.Label(IsHostProcess() && enableOnHostInEditor ? "Mode: Host (30 Hz sim)" : "Mode: Client");
#endif
        GUILayout.Label($"fixedDeltaTime: {Time.fixedDeltaTime:0.000} s");
#if UNITY_SERVER || UNITY_EDITOR
        GUILayout.Label($"Frame (avg): {AvgFrameTimeMs:0.0} ms");
        GUILayout.Label($"Fixed (avg): {AvgFixedTimeMs:0.0} ms");
        GUILayout.Label($"CPU Util: {(CpuUtilization*100f):0}%");
#endif
        GUILayout.EndArea();
    }
#endif
}

public sealed class ServerPerfManager
{
#if UNITY_SERVER || UNITY_EDITOR
    private static readonly ServerPerfManager _instance = new();
    public static ServerPerfManager Instance => _instance;

    private ServerPerfManager() {}

    public void BeginFrame()                      => ServerRuntime30.Instance?.BeginFrame();
    public void EndFrame()                        => ServerRuntime30.Instance?.EndFrame();
    public void AddBytesSent(int count)           => ServerRuntime30.Instance?.AddBytesSent(count);
    public void AddBytesRecv(int count)           => ServerRuntime30.Instance?.AddBytesRecv(count);
    public bool RequestWork(WorkCategory c, int baseCost) => ServerRuntime30.Instance?.RequestWork(c, baseCost) ?? true;

    public void LogStatsIfNeeded() {}
    public void CheckAlerts() {}
    public void RegisterSystem(string name) {}
    public void ReportSystemUsage(string name, int cost) {}
#else
    private static readonly ServerPerfManager _instance = new();
    public static ServerPerfManager Instance => _instance;
    private ServerPerfManager() {}

    public void BeginFrame() {}
    public void EndFrame() {}
    public void AddBytesSent(int count) {}
    public void AddBytesRecv(int count) {}
    public bool RequestWork(WorkCategory c, int baseCost) => true;

    public void LogStatsIfNeeded() {}
    public void CheckAlerts() {}
    public void RegisterSystem(string name) {}
    public void ReportSystemUsage(string name, int cost) {}
#endif
}
#endif