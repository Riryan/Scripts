using UnityEngine;
using UnityEngine.AI;
using Mirror;

[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent]
public abstract class NavMeshMovement : Movement
{
    [Header("Components")]
    public NavMeshAgent agent;
    public const float SampleRadius = 1;
    public override Vector3 GetVelocity() =>
        agent.velocity;
    public override bool IsMoving() =>
        agent.pathPending ||
        agent.remainingDistance > agent.stoppingDistance ||
        agent.velocity != Vector3.zero;

    public override void SetSpeed(float speed)
    {
        agent.speed = speed;
    }
    
    public override void LookAtY(Vector3 position)
    {
        transform.LookAt(new Vector3(position.x, transform.position.y, position.z));
    }

    public override bool CanNavigate()
    {
        return true;
    }

    public override void Navigate(Vector3 destination, float stoppingDistance)
    {
        agent.stoppingDistance = stoppingDistance;
        agent.destination = destination;
    }
    
    public override bool IsValidSpawnPoint(Vector3 position)
    {
        return NavMesh.SamplePosition(position, out NavMeshHit _, SampleRadius, NavMesh.AllAreas);
    }

    public override Vector3 NearestValidDestination(Vector3 destination)
    {
        return agent.NearestValidDestination(destination);
    }

    public override bool DoCombatLookAt()
    {
        return true;
    }

    [Server]
    public void OnDeath()
    {
        Reset();
    }
}
