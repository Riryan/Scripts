using UnityEngine;
using UnityEngine.AI;
using Mirror;

[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent]
public class NetworkNavMeshAgent : NetworkBehaviour
{
    [Header("Components")]
    public NavMeshAgent agent;

    [Header("Network Throttling")]
    [Tooltip("Minimum time between movement snapshots (seconds). 0.1 = 10 Hz.")]
    [SerializeField] float sendInterval = 0.1f;

    [Tooltip("Minimum position change before a new snapshot is sent.")]
    [SerializeField] float minPositionDelta = 0.1f;

    [Tooltip("Minimum destination change before a new snapshot is sent.")]
    [SerializeField] float minDestinationDelta = 0.1f;

    // last state that was actually sent to clients
    Vector3 lastSentPosition;
    Vector3 lastSentDestination;
    bool lastSentHasPath;
    double lastSentTime;

    void Awake()
    {
        if (!agent)
            agent = GetComponent<NavMeshAgent>();
    }

    // ------------------------------------------------------------------------
    // SERVER: decide when to send movement snapshots
    // ------------------------------------------------------------------------
    [ServerCallback]
    void Update()
    {
        if (!isServer)
            return;

        double now = NetworkTime.time;
        double elapsed = now - lastSentTime;

        // Respect both our own sendInterval and Mirror's syncInterval if set.
        float effectiveInterval = sendInterval;
        if (syncInterval > 0f)
            effectiveInterval = Mathf.Max(sendInterval, syncInterval);

        if (elapsed < effectiveInterval)
            return;

        Vector3 position = transform.position;

        // Determine if the agent currently has a path we care about
        bool hasPath = agent.hasPath && agent.remainingDistance > 0.01f;
        Vector3 destination = hasPath ? agent.destination : lastSentDestination;

        float posDelta = Vector3.Distance(position, lastSentPosition);
        float destDelta = (hasPath || lastSentHasPath)
            ? Vector3.Distance(destination, lastSentDestination)
            : 0f;

        bool pathStateChanged = hasPath != lastSentHasPath;

        // Only send if something meaningfully changed
        bool shouldSend =
            pathStateChanged ||
            posDelta >= minPositionDelta ||
            destDelta >= minDestinationDelta;

        if (!shouldSend)
            return;

        // Mark this NetworkBehaviour as dirty so Mirror will serialize it
        SetSyncVarDirtyBit(1UL);

        lastSentPosition = position;
        lastSentDestination = destination;
        lastSentHasPath = hasPath;
        lastSentTime = now;
    }

    // ------------------------------------------------------------------------
    // SERVER -> CLIENTS: serialization
    // ------------------------------------------------------------------------
    public override void OnSerialize(NetworkWriter writer, bool initialState)
    {
        // Keep payload small but informative:
        //  - current position
        //  - speed
        //  - hasPath flag
        //  - destination (only if it actually has a path)
        Vector3 position = transform.position;
        bool hasPath = agent.hasPath && agent.remainingDistance > 0.01f;
        Vector3 destination = hasPath ? agent.destination : position;
        float speed = agent.speed;

        writer.WriteVector3(position);
        writer.WriteFloat(speed);
        writer.WriteBool(hasPath);
        if (hasPath)
            writer.WriteVector3(destination);
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        Vector3 position = reader.ReadVector3();
        float speed = reader.ReadFloat();
        bool hasPath = reader.ReadBool();
        Vector3 destination = position;

        if (hasPath)
            destination = reader.ReadVector3();

        agent.speed = speed;

        if (initialState)
        {
            // Initial spawn: warp to the authoritative position.
            if (agent.isOnNavMesh)
                agent.Warp(position);
            else
                transform.position = position; // fallback for weird edge cases
        }

        if (!agent.isOnNavMesh)
            return;

        if (hasPath)
        {
            agent.stoppingDistance = 0;
            agent.destination = destination;
        }

        // Rubberband if we drift too far from server authority
        if (Vector3.Distance(transform.position, position) > agent.speed * 2f)
        {
            agent.Warp(position);
        }
    }
}
