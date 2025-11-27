using UnityEngine;
using UnityEngine.AI;
using Mirror;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Player))]
[DisallowMultipleComponent]
public class NetworkNavMeshAgentRubberbanding : NetworkBehaviour
{
    [Header("Components")]
    public NavMeshAgent agent;
    public Player player;

    [Header("Network Throttling")]
    [Tooltip("Minimum time between movement updates (seconds). 0.1 = 10 Hz.")]
    [SerializeField] float sendInterval = 0.1f;

    [Tooltip("Minimum distance the object must move before sending a new update.")]
    [SerializeField] float minMoveDistance = 0.15f;

    // Internal state for throttling
    Vector3 lastSentPosition;
    double lastSentTime;

    // ------------------------------------------------------------------------
    // Validation / helper
    // ------------------------------------------------------------------------
    bool IsValidDestination(Vector3 position)
    {
        // Original behaviour: defer to Player movement rules.
        return player.IsMovementAllowed();
    }

    // ------------------------------------------------------------------------
    // Client -> Server: movement intent
    // ------------------------------------------------------------------------
    [Command]
    void CmdMoved(Vector3 position)
    {
        if (IsValidDestination(position))
        {
            agent.stoppingDistance = 0;
            agent.destination = position;
            SetSyncVarDirtyBit(1);
        }
        else
        {
            // Still force a sync so the client gets corrected back to server pos
            SetSyncVarDirtyBit(1);
        }
    }

    // ------------------------------------------------------------------------
    // Local update (owner)
    // ------------------------------------------------------------------------
    void Update()
    {
        if (!isLocalPlayer)
            return;

        // Use NetworkTime for consistency with Mirror, but add our own throttle
        double now = NetworkTime.time;
        double elapsed = now - lastSentTime;

        // Effective interval: respect both our custom sendInterval and Mirror's syncInterval if set.
        float effectiveInterval = sendInterval;
        if (syncInterval > 0f)
            effectiveInterval = Mathf.Max(sendInterval, syncInterval);

        // Only consider sending if enough time has passed
        if (elapsed < effectiveInterval)
            return;

        // Only send if we've actually moved a meaningful amount
        float distanceMoved = Vector3.Distance(transform.position, lastSentPosition);
        if (distanceMoved < minMoveDistance)
            return;

        // At this point, we want to sync a new movement sample
        if (isServer)
        {
            // Host mode: we are both client and server, so just mark dirty
            SetSyncVarDirtyBit(1);
        }
        else
        {
            // Pure client: send intent to server
            CmdMoved(transform.position);
        }

        lastSentTime = now;
        lastSentPosition = transform.position;
    }

    // ------------------------------------------------------------------------
    // Server-side helpers for hard resets / warps
    // ------------------------------------------------------------------------
    [ClientRpc]
    public void RpcWarp(Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, NavMeshMovement.SampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else
        {
            Debug.Log(
                $"RpcWarp for {name} ignored because position {position} " +
                "is not on the NavMesh. This can happen if a NavMesh player " +
                "next to us walked into an instance."
            );
        }
    }

    [Server]
    public void ResetMovement()
    {
        TargetResetMovement(transform.position);
        SetSyncVarDirtyBit(1);
    }

    [TargetRpc]
    void TargetResetMovement(Vector3 resetPosition)
    {
        agent.ResetMovement();
        agent.Warp(resetPosition);
    }

    // ------------------------------------------------------------------------
    // Serialization: server -> observers
    // ------------------------------------------------------------------------
    public override void OnSerialize(NetworkWriter writer, bool initialState)
    {
        // Keep payload identical to original for compatibility:
        //  - position
        //  - agent speed
        writer.WriteVector3(transform.position);
        writer.WriteFloat(agent.speed);
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        Vector3 position = reader.ReadVector3();
        float speed = reader.ReadFloat();

        if (initialState)
        {
            // Initial spawn: hard warp to the authoritative position.
            agent.Warp(position);
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning(
                $"NetworkNavMeshAgent.OnDeserialize: {name} agent is not on NavMesh. " +
                $"Current position={transform.position} new position={position}"
            );
            return;
        }

        // Only accept positions that are actually on the NavMesh
        if (NavMesh.SamplePosition(position, out NavMeshHit _, 0.1f, NavMesh.AllAreas))
        {
            // For remote objects, drive movement with NavMeshAgent towards the new destination.
            if (!isLocalPlayer)
            {
                agent.stoppingDistance = 0;
                agent.speed = speed;
                agent.destination = position;
            }

            // Rubberband if we are too far off from the authoritative position
            if (Vector3.Distance(transform.position, position) > agent.speed * 2f && agent.isOnNavMesh)
            {
                agent.Warp(position);
            }
        }
        else
        {
            Debug.Log(
                $"NetworkNavMeshAgent.OnDeserialize: {name} ignored position {position} " +
                "because it's not on the NavMesh. This can happen if a nearby player " +
                "warped to a dungeon instance that isn't on the local player."
            );
        }
    }
}

/*using UnityEngine;
using UnityEngine.AI;
using Mirror;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Player))]
[DisallowMultipleComponent]
public class NetworkNavMeshAgentRubberbanding : NetworkBehaviour
{
    public NavMeshAgent agent; 
    public Player player;
    Vector3 lastSentPosition;
    double lastSentTime; 
    
    bool IsValidDestination(Vector3 position)
    {
        return player.IsMovementAllowed();
    }

    [Command]
    void CmdMoved(Vector3 position)
    {
        if (IsValidDestination(position))
        {
            agent.stoppingDistance = 0;
            agent.destination = position;
            SetSyncVarDirtyBit(1);
        }
        else
        {
            SetSyncVarDirtyBit(1);
        }
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            if (NetworkTime.time >= lastSentTime + syncInterval &&
                transform.position != lastSentPosition)
            {
                if (isServer)
                    SetSyncVarDirtyBit(1);
                else
                    CmdMoved(transform.position);
                lastSentTime = NetworkTime.time;
                lastSentPosition = transform.position;
            }
        }
    }
    
    [ClientRpc]
    public void RpcWarp(Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, NavMeshMovement.SampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else Debug.Log($"RpcWarp for {name} ignored because destination is not on NavMesh: {position}. This can happen if a NavMesh player next to us walked into an instance.");
    }
    
    [Server]
    public void ResetMovement()
    {
        TargetResetMovement(transform.position);
        SetSyncVarDirtyBit(1);
    }
   
    [TargetRpc]
    void TargetResetMovement(Vector3 resetPosition)
    {
        agent.ResetMovement();
        agent.Warp(resetPosition);
    }
    
    public override void OnSerialize(NetworkWriter writer, bool initialState)
    {
        writer.WriteVector3(transform.position);
        writer.WriteFloat(agent.speed);
    }

    
    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        Vector3 position = reader.ReadVector3();
        float speed = reader.ReadFloat();
        if (initialState)
        {
            agent.Warp(position);
        }
        if (agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit _, 0.1f, NavMesh.AllAreas))
            {
                if (!isLocalPlayer)
                {
                    agent.stoppingDistance = 0;
                    agent.speed = speed;
                    agent.destination = position;
                }
                if (Vector3.Distance(transform.position, position) > agent.speed * 2 && agent.isOnNavMesh)
                {
                    agent.Warp(position);
                }
            }
            else Debug.Log("NetworkNavMeshAgent.OnDeserialize: new position not on NavMesh, name=" + name + " new position=" + position + ". This could happen if the agent was warped to a dungeon instance that isn't on the local player.");
        }
        else Debug.LogWarning("NetworkNavMeshAgent.OnDeserialize: agent not on NavMesh, name=" + name + " position=" + transform.position + " new position=" + position);
    }
}
*/