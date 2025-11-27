using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using static HarvestShared;

public class HarvestServer : NetworkBehaviour
{
    [SerializeField] private int zoneId = 1;
    [SerializeField] private HarvestRule[] rules;

    // NodeId -> respawnAtUtc (seconds)
    readonly Dictionary<ulong, double> harvestedUntil = new();
    // Cell key -> (prototype -> set of local indices currently harvested)
    readonly Dictionary<(int cx,int cy), Dictionary<int, HashSet<int>>> cellHarvest = new();

    // Per-connection subscriptions
    readonly Dictionary<NetworkConnectionToClient, HashSet<(int cx,int cy)>> subs = new();

    double Now => NetworkTime.time; // seconds

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<HarvestSubscribeMessage>(OnSubscribe);
        NetworkServer.RegisterHandler<HarvestInteractRequest>(OnInteract);
        InvokeRepeating(nameof(ServerTickRespawns), 1f, 1f);
    }

    void OnSubscribe(NetworkConnectionToClient conn, HarvestSubscribeMessage msg)
    {
        if (!subs.TryGetValue(conn, out var set)) { set = new(); subs[conn] = set; }
        set.Clear();
        for (int i = 0; i+1 < msg.cells.Count; i += 2)
            set.Add((msg.cells[i+0], msg.cells[i+1]));

        // Send snapshots for any cell that has harvested entries.
        foreach (var cell in set)
        {
            if (!cellHarvest.TryGetValue(cell, out var byProto)) continue;
            foreach (var kv in byProto)
            {
                var protoId = kv.Key;
                var indices = kv.Value;
                if (indices.Count == 0) continue;

                var list = new List<int>(indices);
                var secs = new List<ushort>(list.Count);
                foreach (int li in list)
                {
                    var nodeId = NodeId(zoneId, cell.cx, cell.cy, protoId, li);
                    double left = 0;
                    if (harvestedUntil.TryGetValue(nodeId, out var until))
                        left = Math.Max(0, until - Now);
                    secs.Add((ushort)Mathf.Clamp((int)Math.Round(left), 0, 65535));
                }

                conn.Send(new HarvestSnapshotMessage {
                    cellX = cell.cx, cellY = cell.cy, prototypeId = protoId,
                    harvestedLocalIndices = list, secsRemaining = secs
                });
            }
        }
    }

    void OnInteract(NetworkConnectionToClient conn, HarvestInteractRequest req)
    {
        // Validate prototype
        var rule = FindRule(req.prototypeId);
        if (rule == null) return;

        // Deterministically find node
        var nodes = GenerateNodes(zoneId, req.cellX, req.cellY, rule);
        if ((uint)req.localIndex >= (uint)nodes.Count) return;
        var node = nodes[req.localIndex];

        // Basic range check (3m). You can tighten to capsule distance.
        var player = conn.identity ? conn.identity.transform.position : Vector3.zero;
        if ((player - node.pos).sqrMagnitude > 9f) return;

        var nodeId = NodeId(zoneId, req.cellX, req.cellY, req.prototypeId, req.localIndex);

        // If already harvested and not expired, ignore
        if (harvestedUntil.TryGetValue(nodeId, out var until) && until > Now) return;

        // Mark harvested
        double respawn = Now + UnityEngine.Random.Range(rule.respawnSecondsRange.x, rule.respawnSecondsRange.y);
        harvestedUntil[nodeId] = respawn;

        var cellKey = (req.cellX, req.cellY);
        if (!cellHarvest.TryGetValue(cellKey, out var byProto))
            cellHarvest[cellKey] = byProto = new();
        if (!byProto.TryGetValue(req.prototypeId, out var set))
            byProto[req.prototypeId] = set = new();
        set.Add(req.localIndex);

        // Broadcast delta to subscribers of this cell
        BroadcastDelta(cellKey, req.prototypeId, req.localIndex, harvested:true);
        // TODO: grant loot/XP here (server-authoritative)
    }

    void ServerTickRespawns()
    {
        // Simple sweep (fine for tests). For production, use a timing wheel/heap.
        var expired = new List<ulong>();
        foreach (var kv in harvestedUntil)
            if (kv.Value <= Now) expired.Add(kv.Key);

        foreach (var id in expired)
        {
            harvestedUntil.Remove(id);

            // decode id back to cell/proto/localIndex is annoying; we also track cellHarvest, so scan minimal path:
            // We’ll lazily remove from cellHarvest during broadcast; do a small reverse walk:
            foreach (var byCell in cellHarvest)
            {
                var cell = byCell.Key;
                foreach (var kvp in byCell.Value)
                {
                    int proto = kvp.Key;
                    var set = kvp.Value;
                    // Try flip if present
                    // Recompute localIndex via check: NodeId == id for li in set (set is small typically)
                    var toRemove = -1;
                    foreach (var li in set)
                    {
                        if (NodeId(zoneId, cell.cx, cell.cy, proto, li) == id) { toRemove = li; break; }
                    }
                    if (toRemove >= 0)
                    {
                        set.Remove(toRemove);
                        BroadcastDelta(cell, proto, toRemove, harvested:false);
                        goto NextId;
                    }
                }
            }
            NextId: ;
        }
    }

    HarvestRule FindRule(int protoId)
    {
        for (int i = 0; i < rules.Length; i++)
            if (rules[i] && rules[i].PrototypeId == protoId) return rules[i];
        return null;
    }

    void BroadcastDelta((int cx,int cy) cell, int protoId, int localIndex, bool harvested)
    {
        var recv = ListPool<NetworkConnectionToClient>.Get();
        foreach (var kv in subs)
            if (kv.Value.Contains(cell))
                recv.Add(kv.Key);

        if (recv.Count == 0) { ListPool<NetworkConnectionToClient>.Release(recv); return; }

        var msg = new HarvestDeltaMessage {
            cellX = cell.cx, cellY = cell.cy, prototypeId = protoId,
            flippedLocalIndices = new List<int> { localIndex },
            newState = new List<byte> { (byte)(harvested ? 1 : 0) },
            secsRemaining = new List<ushort> { (ushort)(harvested ? 5 : 0) } // placeholder seconds; client treats 0 as ignore
        };

        for (int i = 0; i < recv.Count; i++)
            recv[i].Send(msg);

        ListPool<NetworkConnectionToClient>.Release(recv);
    }
}

// Tiny non-alloc list pool for broadcast fanout (optional)
static class ListPool<T>
{
    static readonly Stack<List<T>> pool = new();
    public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>(8);
    public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
}
