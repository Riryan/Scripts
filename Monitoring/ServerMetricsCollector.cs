using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using UnityEngine;
using uMMORPG;

#if UNITY_EDITOR || UNITY_SERVER
public sealed class ServerMetricsCollector : MonoBehaviour
{
    public const float SYNC_INTERVAL = 5f;

    private static ServerMetricsCollector _instance;
    public static ServerMetricsCollector Instance => _instance;

    private readonly Dictionary<int, ConnStats> _connStats =
        new Dictionary<int, ConnStats>(256);

    private static readonly List<int> _cleanupList =
        new List<int>(256);

    private double _serverStartTime;
    private float _nextSyncTime;

    // ---------------- Lifecycle ----------------
    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        _serverStartTime = Time.realtimeSinceStartupAsDouble;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Start()
    {
        InvokeRepeating(nameof(SampleConnections), 1f, 5f);
        InvokeRepeating(nameof(CleanupConnections), 10f, 10f);
    }

    // ---------------- Sampling ----------------
    private void SampleConnections()
    {
        if (!NetworkServer.active)
            return;

        foreach (var kvp in NetworkServer.connections)
        {
            NetworkConnectionToClient conn = kvp.Value;
            if (conn == null)
                continue;

            int id = conn.connectionId;

            if (!_connStats.TryGetValue(id, out ConnStats stats))
            {
                stats = new ConnStats();
                _connStats[id] = stats;
            }

            stats.lastSeenTime = Time.unscaledTime;
            stats.rttMs = (int)conn.rtt;

            if (stats.playerName == null && conn.identity != null)
            {
                Player p = conn.identity.GetComponent<Player>();
                if (p != null)
                    stats.playerName = p.name;
            }
        }
    }

    private void CleanupConnections()
    {
        float now = Time.unscaledTime;
        _cleanupList.Clear();

        foreach (var kvp in _connStats)
        {
            if (now - kvp.Value.lastSeenTime > 30f)
                _cleanupList.Add(kvp.Key);
        }

        for (int i = 0; i < _cleanupList.Count; ++i)
            _connStats.Remove(_cleanupList[i]);
    }

    // ---------------- Snapshot ----------------
    private ServerMetricsSnapshot BuildSnapshot()
    {
        ServerMetricsSnapshot snap = new ServerMetricsSnapshot();

        snap.timestamp = DateTime.UtcNow.Ticks;
        snap.uptimeSeconds =
            (long)(Time.realtimeSinceStartupAsDouble - _serverStartTime);

        snap.connectedClients = NetworkServer.connections.Count;

        if (_connStats.Count > 0)
        {
            int sum = 0;
            foreach (var s in _connStats.Values)
                sum += s.rttMs;

            snap.avgPingMs = sum / _connStats.Count;
        }
        else
        {
            snap.avgPingMs = 0;
        }

        // Transport does not expose bandwidth
        snap.totalBytesIn = -1;
        snap.totalBytesOut = -1;

        snap.managedMemoryMB =
            GC.GetTotalMemory(false) / (1024 * 1024);

        snap.gen0 = GC.CollectionCount(0);
        snap.gen1 = GC.CollectionCount(1);
        snap.gen2 = GC.CollectionCount(2);

        using (Process proc = Process.GetCurrentProcess())
        {
            snap.cpuTimeMs =
                (long)proc.TotalProcessorTime.TotalMilliseconds;
        }

        return snap;
    }

    // ---------------- Update ----------------
    private void Update()
    {
        if (!NetworkServer.active)
            return;

        if (Time.unscaledTime < _nextSyncTime)
            return;

        _nextSyncTime = Time.unscaledTime + SYNC_INTERVAL;

        ServerMetricsSnapshot snap = BuildSnapshot();

        // Host mode: server + local client
        if (NetworkClient.active)
        {
            ServerMetricsUI.Instance?.Apply(snap);
        }
    }

    // ---------------- Internal ----------------
    private struct ConnStats
    {
        public int rttMs;
        public float lastSeenTime;
        public string playerName;
    }
}
#endif
