using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace uMMORPG
{
    public partial class NetworkManagerMMO
    {
#if UNITY_SERVER || UNITY_EDITOR
        [Header("Connection Watchdog")]
        public bool enableWatchdog = true;
        public float watchdogInterval = 30f;
        public float idleTimeoutSeconds = 180f;
        public float combatGraceSeconds = 30f;

        readonly Dictionary<int, float> lastPacketTime = new();

        // called from existing OnStartServer()
        void StartConnectionWatchdog()
        {
            if (!enableWatchdog)
                return;

            NetworkServer.RegisterHandler<NetworkPingMessage>(
                OnPingReceived,
                false
            );

            InvokeRepeating(
                nameof(RunConnectionWatchdog),
                watchdogInterval,
                watchdogInterval
            );
        }

        // called from existing OnStopServer()
        void StopConnectionWatchdog()
        {
            CancelInvoke(nameof(RunConnectionWatchdog));
            lastPacketTime.Clear();
        }

        void OnPingReceived(NetworkConnectionToClient conn, NetworkPingMessage msg)
        {
            lastPacketTime[conn.connectionId] = Time.time;
        }

        void RunConnectionWatchdog()
        {
            if (!NetworkServer.active)
                return;

            float now = Time.time;
            List<(NetworkConnectionToClient conn, string reason)> toKick = new();

            foreach (var kvp in NetworkServer.connections)
            {
                NetworkConnectionToClient conn = kvp.Value;

                if (conn == null)
                {
                    toKick.Add((conn, "NullConnection"));
                    continue;
                }

                if (!conn.isReady || conn.identity == null)
                {
                    toKick.Add((conn, "UnreadyOrNoIdentity"));
                    continue;
                }

                float lastSeen = lastPacketTime.TryGetValue(
                    conn.connectionId,
                    out float t
                ) ? t : now;

                float timeout = idleTimeoutSeconds;

                Player player = conn.identity.GetComponent<Player>();
                if (player != null && player.remainingLogoutTime > 0)
                    timeout += combatGraceSeconds;

                if (now - lastSeen > timeout)
                    toKick.Add((conn, "IdleTimeout"));
            }

            foreach (var entry in toKick)
            {
                if (entry.conn == null)
                    continue;

                Debug.Log(
                    $"[Watchdog] Disconnecting {entry.conn.connectionId} Reason={entry.reason}"
                );

                entry.conn.Disconnect();
                lastPacketTime.Remove(entry.conn.connectionId);
            }
        }
#endif
    }
}
