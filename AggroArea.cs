using System.Collections.Generic;
using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public sealed class AggroArea : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider trigger;    // your aggro trigger (Sphere/Capsule/etc.)
    [SerializeField] private Entity owner;        // the monster that owns this area

    // pooled set to avoid per-instance GC
    private static readonly Stack<HashSet<Entity>> Pool = new Stack<HashSet<Entity>>(32);
    private static readonly Entity[] Empty = System.Array.Empty<Entity>();
    private HashSet<Entity> tracked;

    public int Count => tracked?.Count ?? 0;
    public IReadOnlyCollection<Entity> Tracked => tracked ?? (IReadOnlyCollection<Entity>)Empty;

    // --- Lifecycle ----------------------------------------------------------

    private void Reset()
    {
        // Editor convenience
        if (owner == null) owner = GetComponentInParent<Entity>();
        if (trigger == null) trigger = GetComponent<Collider>() ?? GetComponentInChildren<Collider>(true);

#if UNITY_EDITOR
        if (trigger != null) trigger.isTrigger = true;
#endif
    }

    private void OnEnable()
    {
        tracked = Pool.Count > 0 ? Pool.Pop() : new HashSet<Entity>();
        tracked.Clear();

#if UNITY_SERVER || UNITY_EDITOR
        EnsureServerPhysicsReady();
#endif
    }

    private void OnDisable()
    {
        if (tracked != null)
        {
            tracked.Clear();
            Pool.Push(tracked);
            tracked = null;
        }
    }

    // --- Server-only physics prep ------------------------------------------

#if UNITY_SERVER || UNITY_EDITOR
    private void EnsureServerPhysicsReady()
    {
        // Make sure we have a trigger
        if (trigger == null)
        {
            trigger = GetComponent<Collider>() ?? GetComponentInChildren<Collider>(true);
        }
        if (trigger != null) trigger.isTrigger = true;

        // IMPORTANT: at least one side needs a Rigidbody for trigger events
        // Keep it kinematic & frozen to avoid any physics overhead or movement
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }
#endif

    // --- Trigger events (server) -------------------------------------------

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (owner == null || tracked == null) return;

        // find the entity on the other collider’s hierarchy
        var e = other.GetComponentInParent<Entity>();
        if (e == null || e == owner) return;

        // only act on first sighting; HashSet guards dupes from multi-collider rigs
        if (tracked.Add(e))
        {
            // hand off to monster; Monster.OnAggro(Entity) already validates CanAttack, etc.
            owner.OnAggro(e);
        }
    }

    [ServerCallback]
    private void OnTriggerExit(Collider other)
    {
        if (tracked == null) return;

        var e = other.GetComponentInParent<Entity>();
        if (e == null) return;

        tracked.Remove(e);
    }
}
