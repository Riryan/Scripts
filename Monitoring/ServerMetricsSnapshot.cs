using System;

[Serializable]
public struct ServerMetricsSnapshot
{
    // ===== META =====
    public long timestamp;          // DateTime.UtcNow.Ticks
    public long uptimeSeconds;      // Server uptime

    // ===== CONNECTIONS =====
    public int connectedClients;
    public int avgPingMs;

    // ===== NETWORK =====
    public long totalBytesIn;
    public long totalBytesOut;

    // ===== SERVER LOAD =====
    public long cpuTimeMs;          // TotalProcessorTime
    public long managedMemoryMB;

    // ===== GC =====
    public int gen0;
    public int gen1;
    public int gen2;
}
