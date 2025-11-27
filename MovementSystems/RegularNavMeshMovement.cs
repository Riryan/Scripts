using UnityEngine;

[RequireComponent(typeof(NetworkNavMeshAgent))]
[DisallowMultipleComponent]
public class RegularNavMeshMovement : NavMeshMovement
{
    [Header("Components")]
    public NetworkNavMeshAgent networkNavMeshAgent;

    public override void Reset()
    {
        agent.ResetMovement();
    }

    // NOTE:
    // RegularNavMeshMovement is used for NPCs / non-player entities.
    // NPCs are fully server-authoritative. We don't need (or want)
    // to call RpcWarp here. The server warps the NavMeshAgent locally,
    // and clients get corrected via NetworkNavMeshAgent.OnSerialize/OnDeserialize.
    public override void Warp(Vector3 destination)
    {
        // Server: warp the NavMeshAgent. Clients will receive the new position
        // in the next movement snapshot and rubberband if necessary.
        agent.Warp(destination);
    }
}
