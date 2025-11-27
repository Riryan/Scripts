using UnityEngine;
using Mirror;

public partial class Mount : Summonable
{
    [Header("Death")]
    public float deathTime = 2; 
    [HideInInspector] public double deathTimeEnd; 

    [Header("Seat Position")]
    public Transform seat;
    
    void LateUpdate()
    {
        if (isClient) 
        {
            animator.SetBool("MOVING", health.current > 0 && owner != null && owner.movement.IsMoving());
            animator.SetBool("DEAD", state == "DEAD");
        }
    }

    void OnDestroy()
    {
        if (isServer)
            SyncToOwnerItem();
    }

    public override bool IsWorthUpdating() => true;
    
    bool EventOwnerDisappeared()=>
        owner == null;

    bool EventOwnerDied() =>
        owner != null && owner.health.current == 0;

    bool EventDied() =>
        health.current == 0;

    bool EventDeathTimeElapsed() =>
        state == "DEAD" && NetworkTime.time >= deathTimeEnd;
    
    void CopyOwnerPositionAndRotation()
    {
        if (owner != null)
        {
            transform.position = owner.transform.position;
            transform.rotation = owner.transform.rotation;
        }
    }

    [Server]
    string UpdateServer_IDLE()
    {
        CopyOwnerPositionAndRotation();
        
        if (EventOwnerDisappeared())
        {
            NetworkServer.Destroy(gameObject);
            return "IDLE";
        }
        if (EventOwnerDied())
        {
            health.current = 0;
        }
        if (EventDied())
        {
            return "DEAD";
        }
        if (EventDeathTimeElapsed()) {} 

        return "IDLE"; 
    }

    [Server]
    string UpdateServer_DEAD()
    {
        
        if (EventOwnerDisappeared())
        {
            NetworkServer.Destroy(gameObject);
            return "DEAD";
        }
        if (EventDeathTimeElapsed())
        {
            NetworkServer.Destroy(gameObject);
            return "DEAD";
        }
        if (EventOwnerDied()) {} 
        if (EventDied()) {} 

        return "DEAD"; 
    }

    [Server]
    protected override string UpdateServer()
    {
        if (state == "IDLE")    return UpdateServer_IDLE();
        if (state == "DEAD")    return UpdateServer_DEAD();
        Debug.LogError("invalid state:" + state);
        return "IDLE";
    }
    
    [Client]
    protected override void UpdateClient()
    {
        if (state == "IDLE" || state == "MOVING")
        {
            CopyOwnerPositionAndRotation();
        }
    }

    
    [Server]
    public override void OnDeath()
    {
        base.OnDeath();
        deathTimeEnd = NetworkTime.time + deathTime;
        SyncToOwnerItem();
    }

    
    public override bool CanAttack(Entity entity) { return false; }
    
    protected override void OnInteract()
    {
        Player player = Player.localPlayer;
        
        if (player.CanAttack(this) && player.skills.skills.Count > 0)
        {
            ((PlayerSkills)player.skills).TryUse(0);
        }
        else
        {
            Vector3 destination = Utils.ClosestPoint(this, player.transform.position);
            player.movement.Navigate(destination, player.interactionRange);
        }
    }
}
