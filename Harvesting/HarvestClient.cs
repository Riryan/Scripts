using System.Collections.Generic;
using Mirror;
using UnityEngine;
#if !UNITY_SERVER
using UnityEngine.AI; // NavMesh on client/editor only
#endif
using static HarvestShared;

public class HarvestClient : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] Transform player;              // assign local player actor/camera root
    [SerializeField] HarvestRule[] rules;           // prototype rules (prefabs, gen params)
    [SerializeField] Transform visualsRoot;         // optional parent for spawned visuals

    [Header("World/Zone")]
    [Tooltip("Must match the server's zoneId for deterministic node generation")]
    [SerializeField] int zoneId = 1;

    [Header("Subscription")]
    [Tooltip("Cells radius around player to subscribe (2 => 5×5 cells)")]
    [SerializeField] int subscribeRadiusCells = 2;
    [Tooltip("How often (seconds) to re-check the player's cell and re-subscribe")]
    [SerializeField] float subscribeInterval = 0.25f;

    [Header("Grounding / Snapping")]
#if !UNITY_SERVER
    [Tooltip("Max distance when sampling the NavMesh around the seed position")]
    [SerializeField] float navMeshSnapRadius = 6f;
    [Tooltip("If NavMesh sampling fails, raycast from above as a fallback")]
    [SerializeField] float raycastUp = 100f;
    [SerializeField] float raycastDown = 200f;
    [SerializeField] LayerMask groundMask = ~0;
    [Tooltip("Rotate to surface normal (trees often look better upright; leave OFF to keep yaw only)")]
    [SerializeField] bool alignToSurfaceNormal = false;
#endif
    [Tooltip("Small height offset after snapping, in meters")]
    [SerializeField] float yOffset = 0f;

    // runtime
    readonly Dictionary<int, HarvestRule> ruleByProto = new();
    readonly Dictionary<(int cx,int cy,int pid,int li), HarvestNodeVisual> visuals = new();
    Vector2Int lastCenter;
    float nextUpdateTime;
    readonly HashSet<(int cx,int cy)> subscribedCells = new();

    void Awake()
    {
        ruleByProto.Clear();
        for (int i = 0; i < rules.Length; i++)
        {
            if (rules[i] == null) continue;
            ruleByProto[rules[i].PrototypeId] = rules[i];
        }
        lastCenter = new Vector2Int(int.MinValue, int.MinValue);
        if (visualsRoot == null) visualsRoot = transform;
    }

    void OnEnable()
    {
        if (NetworkClient.active)
        {
            NetworkClient.RegisterHandler<HarvestSnapshotMessage>(OnSnapshot, false);
            NetworkClient.RegisterHandler<HarvestDeltaMessage>(OnDelta, false);
        }
    }

    void OnDisable()
    {
        if (NetworkClient.active)
        {
            NetworkClient.UnregisterHandler<HarvestSnapshotMessage>();
            NetworkClient.UnregisterHandler<HarvestDeltaMessage>();
        }
        // cleanup spawned visuals when disabled
        foreach (var kv in visuals)
            if (kv.Value) Destroy(kv.Value.gameObject);
        visuals.Clear();
        subscribedCells.Clear();
    }

    void Update()
    {
        if (!NetworkClient.active || player == null)
            return;

        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + subscribeInterval;
            var center = WorldToCell(player.position);
            if (center != lastCenter)
            {
                lastCenter = center;
                SendSubscription(center);
                CullUnsubscribedVisuals();
            }
        }
    }

    // ----- Networking -----

    void SendSubscription(Vector2Int center)
    {
        // Build set of desired cells in a (2r+1)x(2r+1) square
        var want = new HashSet<(int,int)>();
        var packed = new List<int>((2 * subscribeRadiusCells + 1) * (2 * subscribeRadiusCells + 1) * 2);
        for (int dy = -subscribeRadiusCells; dy <= subscribeRadiusCells; dy++)
        {
            for (int dx = -subscribeRadiusCells; dx <= subscribeRadiusCells; dx++)
            {
                int cx = center.x + dx;
                int cy = center.y + dy;
                want.Add((cx, cy));
                packed.Add(cx);
                packed.Add(cy);
            }
        }

        // Send subscription list to server
        NetworkClient.Send(new HarvestSubscribeMessage { cells = packed });

        // Track for culling of visuals outside subscription
        subscribedCells.Clear();
        foreach (var t in want) subscribedCells.Add(t);
    }

    void CullUnsubscribedVisuals()
    {
        // Destroy visuals whose (cx,cy) are no longer subscribed
        var toRemove = ListPool<(int,int,int,int)>.Get();
        foreach (var kv in visuals)
        {
            var key = kv.Key; // (cx,cy,pid,li)
            if (!subscribedCells.Contains((key.cx, key.cy)))
                toRemove.Add(key);
        }
        for (int i = 0; i < toRemove.Count; i++)
        {
            var k = toRemove[i];
            if (visuals.TryGetValue(k, out var vis) && vis)
                Destroy(vis.gameObject);
            visuals.Remove(k);
        }
        ListPool<(int,int,int,int)>.Release(toRemove);
    }

    void OnSnapshot(HarvestSnapshotMessage msg)
    {
        if (!ruleByProto.TryGetValue(msg.prototypeId, out var rule) || rule == null)
            return;

        // Build fast lookup for harvested indices reported by server
        var harvested = new HashSet<int>(msg.harvestedLocalIndices ?? new List<int>(0));

        // Generate deterministic nodes for this cell/prototype
        var nodes = GenerateNodes(zoneId, msg.cellX, msg.cellY, rule);
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            bool isHarvested = harvested.Contains(node.localIndex);
            EnsureVisual(msg.cellX, msg.cellY, msg.prototypeId, rule, node, isHarvested);
        }
    }

    void OnDelta(HarvestDeltaMessage msg)
    {
        // Flip/toggle a subset of indices the server tells us about
        for (int i = 0; i < msg.flippedLocalIndices.Count; i++)
        {
            int li = msg.flippedLocalIndices[i];
            bool harvestedNow = msg.newState != null && i < msg.newState.Count && msg.newState[i] == 1;
            var key = (msg.cellX, msg.cellY, msg.prototypeId, li);
            if (visuals.TryGetValue(key, out var vis) && vis)
            {
                vis.SetState(harvestedNow);
            }
        }
    }

    // ----- Visuals -----

    void EnsureVisual(int cx, int cy, int prototypeId, HarvestRule rule, Node node, bool harvested)
    {
        var key = (cx, cy, prototypeId, node.localIndex);
        if (visuals.ContainsKey(key) && visuals[key] != null)
        {
            visuals[key].SetState(harvested);
            return;
        }

        // Create a parent holder with HarvestNodeVisual and spawn the two models as children
        var holder = new GameObject($"Harvest[{prototypeId}] c({cx},{cy}) li({node.localIndex})");
        holder.transform.SetParent(visualsRoot, false);

        Vector3 snappedPos;
        Quaternion snappedRot;
        SnapToGround(node.pos, node.rot, out snappedPos, out snappedRot);
        holder.transform.SetPositionAndRotation(snappedPos, snappedRot);

        var hv = holder.AddComponent<HarvestNodeVisual>();
        hv.cellX = cx;
        hv.cellY = cy;
        hv.prototypeId = prototypeId;
        hv.localIndex = node.localIndex;

        if (rule.prefabWhole != null)
        {
            var w = Instantiate(rule.prefabWhole, holder.transform);
            hv.whole = w;
        }
        if (rule.prefabStump != null)
        {
            var s = Instantiate(rule.prefabStump, holder.transform);
            hv.stump = s;
        }

        hv.SetState(harvested);
        visuals[key] = hv;
    }

    void SnapToGround(in Vector3 seedPos, in Quaternion yawOnly, out Vector3 outPos, out Quaternion outRot)
    {
        // Start with seed (y assumed 0 from deterministic generation)
        Vector3 p = seedPos;
        Quaternion r = yawOnly;

#if !UNITY_SERVER
        bool snapped = false;

        // 1) Try NavMesh first
        if (NavMesh.SamplePosition(p, out var hit, navMeshSnapRadius, NavMesh.AllAreas))
        {
            p = hit.position;
            snapped = true;
            if (alignToSurfaceNormal)
                r = Quaternion.FromToRotation(Vector3.up, hit.normal) * yawOnly;
        }

        // 2) If no NavMesh nearby (e.g., decorative only areas), raycast down from above
        if (!snapped)
        {
            var origin = p + Vector3.up * raycastUp;
            if (Physics.Raycast(origin, Vector3.down, out var rh, raycastUp + raycastDown, groundMask, QueryTriggerInteraction.Ignore))
            {
                p = rh.point;
                if (alignToSurfaceNormal)
                    r = Quaternion.FromToRotation(Vector3.up, rh.normal) * yawOnly;
            }
        }
#endif
        p.y += yOffset;
        outPos = p;
        outRot = r;
    }
}

