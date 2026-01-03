using System;
using TMPro;
using UnityEngine;
using Mirror;
using uMMORPG;

#if UNITY_EDITOR || UNITY_SERVER
public sealed class ServerMetricsUI : MonoBehaviour
{
    // ================== SINGLETON ==================
    private static ServerMetricsUI _instance;
    public static ServerMetricsUI Instance => _instance;

    // ================== UI REFERENCES ===============
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Server")]
    [SerializeField] private TMP_Text uptimeText;
    [SerializeField] private TMP_Text serverLoadText;
    [SerializeField] private TMP_Text memoryText;
    [SerializeField] private TMP_Text gcText;

    [Header("Network")]
    [SerializeField] private TMP_Text connectionsText;
    [SerializeField] private TMP_Text avgPingText;
    [SerializeField] private TMP_Text bandwidthText;
    [SerializeField] private TMP_Text lastUpdateText;

    // ================== STATE ======================
    private long _lastSnapshotTicks;

    // ================== LIFECYCLE ==================
    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        //DontDestroyOnLoad(gameObject);

        if (root != null)
            root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    // ================== VISIBILITY ==================
    public void SetVisibleFor(Player player)
    {
//#if UNITY_SERVER
        SetVisible(true);
//#else
//        SetVisible(player != null && player.isGameMaster);
//#endif
    }

    private void SetVisible(bool value)
    {
        if (root != null)
            root.SetActive(value);
    }

    // ================== APPLY SNAPSHOT ==============
public void Apply(ServerMetricsSnapshot snap)
{
    if (root == null || !root.activeSelf)
        return;

    _lastSnapshotTicks = snap.timestamp;

    TimeSpan up = TimeSpan.FromSeconds(snap.uptimeSeconds);
    uptimeText.text = $"Uptime: {up:dd\\.hh\\:mm\\:ss}";

    serverLoadText.text =
        $"CPU Time: {(snap.cpuTimeMs / 1000f):F1}s";

    memoryText.text =
        $"Managed: {snap.managedMemoryMB} MB";

    gcText.text =
        $"GC Gen0:{snap.gen0}  Gen1:{snap.gen1}  Gen2:{snap.gen2}";

    connectionsText.text =
        $"Clients: {snap.connectedClients}";

    avgPingText.text =
        $"Avg Ping: {snap.avgPingMs} ms";

    // ---- Bandwidth handling
    if (snap.totalBytesIn < 0 || snap.totalBytesOut < 0)
    {
        bandwidthText.text = "Bandwidth: Unsupported";
    }
    else
    {
        bandwidthText.text =
            $"In: {FormatBytes(snap.totalBytesIn)}  Out: {FormatBytes(snap.totalBytesOut)}";
    }

    lastUpdateText.text =
        $"Last update: {GetAgeSeconds()}s ago";
}

    // ================== HELPERS =====================
    private string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        if (bytes < 1024 * 1024)
            return $"{bytes / 1024f:F1} KB";

        return $"{bytes / (1024f * 1024f):F1} MB";
    }

    private long GetAgeSeconds()
    {
        if (_lastSnapshotTicks == 0)
            return 0;

        long now = DateTime.UtcNow.Ticks;
        return (now - _lastSnapshotTicks) / TimeSpan.TicksPerSecond;
    }
}
#endif
