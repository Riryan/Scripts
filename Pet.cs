using UnityEngine;
using Mirror;
using TMPro;

[RequireComponent(typeof(Experience))]
[RequireComponent(typeof(PetSkills))]
[RequireComponent(typeof(NavMeshMovement))]
[RequireComponent(typeof(NetworkNavMeshAgent))]
public partial class Pet : Summonable
{
    [Header("Components")]
    public Experience experience;

    [Header("Icons")]
    public Sprite portraitIcon; 

    [Header("Text Meshes")]
    public TextMeshPro ownerNameOverlay;

    [Header("Movement")]
    public float returnDistance = 25; 
    public float followDistance = 20;
    public float teleportDistance = 30;
    [Range(0.1f, 1)] public float attackToMoveRangeRatio = 0.8f; 
    
    public override float speed => owner != null ? owner.speed : base.speed;

    [Header("Death")]
    public float deathTime = 2; 
    [HideInInspector] public double deathTimeEnd; 

    [Header("Behaviour")]
    [SyncVar] public bool defendOwner = true; 
    [SyncVar] public bool autoAttack = true; 
    
    protected override ItemSlot SyncStateToItemSlot(ItemSlot slot)
    {
        slot = base.SyncStateToItemSlot(slot);
        slot.item.summonedExperience = experience.current;
        return slot;
    }

    
    void LateUpdate()
    {
        if (isClient) 
        {
            animator.SetBool("MOVING", state == "MOVING" && movement.GetVelocity() != Vector3.zero);
            animator.SetBool("CASTING", state == "CASTING");
            animator.SetBool("STUNNED", state == "STUNNED");
            animator.SetBool("DEAD", state == "DEAD");
            foreach (Skill skill in skills.skills)
                animator.SetBool(skill.name, skill.CastTimeRemaining() > 0);
        }
        
        if (!isServerOnly)
        {
            if (ownerNameOverlay != null)
            {
                if (owner != null)
                {
                    ownerNameOverlay.text = owner.name;
                    
                    if (Player.localPlayer != null)
                    {
                        if (owner.IsMurderer())
                            ownerNameOverlay.color = owner.nameOverlayMurdererColor;
                        else if (owner.IsOffender())
                            ownerNameOverlay.color = owner.nameOverlayOffenderColor;
                        
                        else if (Player.localPlayer.party.InParty() &&
                                 Player.localPlayer.party.party.Contains(owner.name))
                            ownerNameOverlay.color = owner.nameOverlayPartyColor;
                        
                        else
                            ownerNameOverlay.color = owner.nameOverlayDefaultColor;
                    }
                }
                else ownerNameOverlay.text = "?";
            }
        }
    }

    
    void OnDrawGizmos()
    {
        Vector3 startHelp = Application.isPlaying ? owner.petControl.petDestination : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startHelp, returnDistance);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(startHelp, followDistance);
    }

    void OnDestroy()
    {
        
        if (isServer)
            SyncToOwnerItem();
    }
    
    public override bool IsWorthUpdating() { return true; }
    
    bool EventOwnerDisappeared() =>
        owner == null;

    bool EventDied() =>
        health.current == 0;

    bool EventDeathTimeElapsed() =>
        state == "DEAD" && NetworkTime.time >= deathTimeEnd;

    bool EventTargetDisappeared() =>
        target == null;

    bool EventTargetDied() =>
        target != null && target.health.current == 0;

    bool EventTargetTooFarToAttack() =>
        target != null &&
        0 <= skills.currentSkill && skills.currentSkill < skills.skills.Count &&
        !skills.CastCheckDistance(skills.skills[skills.currentSkill], out Vector3 destination);

    bool EventTargetTooFarToFollow() =>
        target != null &&
        Vector3.Distance(owner.petControl.petDestination, target.collider.ClosestPointOnBounds(transform.position)) > followDistance;

    bool EventNeedReturnToOwner() =>
        Vector3.Distance(owner.petControl.petDestination, transform.position) > returnDistance;

    bool EventNeedTeleportToOwner() =>
        Vector3.Distance(owner.petControl.petDestination, transform.position) > teleportDistance;

    bool EventAggro() =>
        target != null && target.health.current > 0;

    bool EventSkillRequest() =>
        0 <= skills.currentSkill && skills.currentSkill < skills.skills.Count;

    bool EventSkillFinished() =>
        0 <= skills.currentSkill && skills.currentSkill < skills.skills.Count &&
               skills.skills[skills.currentSkill].CastTimeRemaining() == 0;

    bool EventMoveEnd() =>
        state == "MOVING" && !movement.IsMoving();

    bool EventStunned() =>
        NetworkTime.time <= stunTimeEnd;

    
    [Server]
    string UpdateServer_IDLE()
    {
        if (EventOwnerDisappeared())
        {
            NetworkServer.Destroy(gameObject);
            return "IDLE";
        }
        if (EventDied())
        {
            return "DEAD";
        }
        if (EventStunned())
        {
            movement.Reset();
            return "STUNNED";
        }
        if (EventTargetDied())
        {
            target = null;
            skills.CancelCast();
            return "IDLE";
        }
        if (EventNeedTeleportToOwner())
        {
            movement.Warp(owner.petControl.petDestination);
            return "IDLE";
        }
        if (EventNeedReturnToOwner())
        {
            target = null;
            skills.CancelCast();
            movement.Navigate(owner.petControl.petDestination, 0);
            return "MOVING";
        }
        if (EventTargetTooFarToFollow())
        {
            target = null;
            skills.CancelCast();
            movement.Navigate(owner.petControl.petDestination, 0);
            return "MOVING";
        }
        if (EventTargetTooFarToAttack())
        {
            float stoppingDistance = ((PetSkills)skills).CurrentCastRange() * attackToMoveRangeRatio;
            Vector3 destination = Utils.ClosestPoint(target, transform.position);
            movement.Navigate(destination, stoppingDistance);
            return "MOVING";
        }
        if (EventSkillRequest())
        {
            Skill skill = skills.skills[skills.currentSkill];
            if (skills.CastCheckSelf(skill) && skills.CastCheckTarget(skill))
            {
                skills.StartCast(skill);
                return "CASTING";
            }
            else
            {
                target = null;
                skills.currentSkill = -1;
                return "IDLE";
            }
        }
        if (EventAggro())
        {
            if (skills.skills.Count > 0)
                skills.currentSkill = ((PetSkills)skills).NextSkill();
            else
                Debug.LogError(name + " has no skills to attack with.");
            return "IDLE";
        }
        if (EventMoveEnd()) {} 
        if (EventDeathTimeElapsed()) {} 
        if (EventSkillFinished()) {} 
        if (EventTargetDisappeared()) {} 

        return "IDLE"; 
    }

    [Server]
    string UpdateServer_MOVING()
    {
        
        if (EventOwnerDisappeared())
        {
            NetworkServer.Destroy(gameObject);
            return "IDLE";
        }
        if (EventDied())
        {
            
            movement.Reset();
            return "DEAD";
        }
        if (EventStunned())
        {
            movement.Reset();
            return "STUNNED";
        }
        if (EventMoveEnd())
        {
            
            return "IDLE";
        }
        if (EventTargetDied())
        {
            
            target = null;
            skills.CancelCast();
            movement.Reset();
            return "IDLE";
        }
        if (EventNeedTeleportToOwner())
        {
            movement.Warp(owner.petControl.petDestination);
            return "IDLE";
        }
        if (EventTargetTooFarToFollow())
        {
            
            
            target = null;
            skills.CancelCast();
            movement.Navigate(owner.petControl.petDestination, 0);
            return "MOVING";
        }
        if (EventTargetTooFarToAttack())
        {
            
            
            float stoppingDistance = ((PetSkills)skills).CurrentCastRange() * attackToMoveRangeRatio;
            Vector3 destination = Utils.ClosestPoint(target, transform.position);
            movement.Navigate(destination, stoppingDistance);
            return "MOVING";
        }
        if (EventAggro())
        {
            
            
            if (skills.skills.Count > 0)
                skills.currentSkill = ((PetSkills)skills).NextSkill();
            else
                Debug.LogError(name + " has no skills to attack with.");
            movement.Reset();
            return "IDLE";
        }
        if (EventNeedReturnToOwner()) {} 
        if (EventDeathTimeElapsed()) {} 
        if (EventSkillFinished()) {} 
        if (EventTargetDisappeared()) {} 
        if (EventSkillRequest()) {} 

        return "MOVING"; 
    }

    [Server]
    string UpdateServer_CASTING()
    {
        
        if (target)
            movement.LookAtY(target.transform.position);

        
        if (EventOwnerDisappeared())
        {
            
            NetworkServer.Destroy(gameObject);
            return "IDLE";
        }
        if (EventDied())
        {
            
            return "DEAD";
        }
        if (EventStunned())
        {
            skills.CancelCast();
            movement.Reset();
            return "STUNNED";
        }
        if (EventTargetDisappeared())
        {
            
            if (skills.skills[skills.currentSkill].cancelCastIfTargetDied)
            {
                skills.CancelCast();
                target = null;
                return "IDLE";
            }
        }
        if (EventTargetDied())
        {
            
            if (skills.skills[skills.currentSkill].cancelCastIfTargetDied)
            {
                skills.CancelCast();
                target = null;
                return "IDLE";
            }
        }
        if (EventSkillFinished())
        {
            
            skills.FinishCast(skills.skills[skills.currentSkill]);

            
            
            if (target.health.current == 0) target = null;

            
            ((PetSkills)skills).lastSkill = skills.currentSkill;
            skills.currentSkill = -1;
            return "IDLE";
        }
        if (EventMoveEnd()) {} 
        if (EventDeathTimeElapsed()) {} 
        if (EventNeedTeleportToOwner()) {} 
        if (EventNeedReturnToOwner()) {} 
        if (EventTargetTooFarToAttack()) {} 
        if (EventTargetTooFarToFollow()) {} 
        if (EventAggro()) {} 
        if (EventSkillRequest()) {} 

        return "CASTING"; 
    }

    [Server]
    string UpdateServer_STUNNED()
    {
        
        if (EventOwnerDisappeared())
        {
            
            NetworkServer.Destroy(gameObject);
            return "IDLE";
        }
        if (EventDied())
        {
            
            skills.CancelCast(); 
            return "DEAD";
        }
        if (EventStunned())
        {
            return "STUNNED";
        }

        
        
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
        if (EventSkillRequest()) {} 
        if (EventSkillFinished()) {} 
        if (EventMoveEnd()) {} 
        if (EventNeedTeleportToOwner()) {} 
        if (EventNeedReturnToOwner()) {} 
        if (EventTargetDisappeared()) {} 
        if (EventTargetDied()) {} 
        if (EventTargetTooFarToFollow()) {} 
        if (EventTargetTooFarToAttack()) {} 
        if (EventAggro()) {} 
        if (EventDied()) {} 

        return "DEAD"; 
    }

    [Server]
    protected override string UpdateServer()
    {
        if (state == "IDLE")    return UpdateServer_IDLE();
        if (state == "MOVING")  return UpdateServer_MOVING();
        if (state == "CASTING") return UpdateServer_CASTING();
        if (state == "STUNNED") return UpdateServer_STUNNED();
        if (state == "DEAD")    return UpdateServer_DEAD();
        Debug.LogError("invalid state:" + state);
        return "IDLE";
    }

    
    [Client]
    protected override void UpdateClient() {}

    
    
    [ServerCallback]
    public override void OnAggro(Entity entity)
    {
        
        base.OnAggro(entity);

        
        if (CanAttack(entity))
        {
            if (target == null)
            {
                target = entity;
            }
            else
            {
                float oldDistance = Vector3.Distance(transform.position, target.transform.position);
                float newDistance = Vector3.Distance(transform.position, entity.transform.position);
                if (newDistance < oldDistance * 0.8) target = entity;
            }
        }
    }

    
    [Server]
    public override void OnDeath()
    {
        base.OnDeath();
        deathTimeEnd = NetworkTime.time + deathTime;
        SyncToOwnerItem();
    }

    
    
    
    public override bool CanAttack(Entity entity)
    {
        return base.CanAttack(entity) &&
               (entity is Monster ||
                (entity is Player && entity != owner) ||
                (entity is Pet pet && pet.owner != owner) ||
                (entity is Mount mount && mount.owner != owner));
    }

    
    
    
    [Command]
    public void CmdSetAutoAttack(bool value)
    {
        autoAttack = value;
    }

    [Command]
    public void CmdSetDefendOwner(bool value)
    {
        defendOwner = value;
    }

    
    protected override void OnInteract()
    {
        Player player = Player.localPlayer;

        
        if (this != player.petControl.activePet)
        {
            
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
}
