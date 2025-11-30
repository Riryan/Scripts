using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using TMPro;

[Serializable] public class UnityEventPlayer : UnityEvent<Player> {}

[RequireComponent(typeof(Experience))]
[RequireComponent(typeof(Intelligence))]
[RequireComponent(typeof(Strength))]
[RequireComponent(typeof(PlayerChat))]
[RequireComponent(typeof(PlayerCrafting))]
[RequireComponent(typeof(PlayerGameMasterTool))]
[RequireComponent(typeof(PlayerGuild))]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerItemMall))]
[RequireComponent(typeof(PlayerLooting))]
[RequireComponent(typeof(PlayerMountControl))]
[RequireComponent(typeof(PlayerNpcRevive))]
[RequireComponent(typeof(PlayerNpcTeleport))]
[RequireComponent(typeof(PlayerNpcTrading))]
[RequireComponent(typeof(PlayerParty))]
[RequireComponent(typeof(PlayerPetControl))]
[RequireComponent(typeof(PlayerQuests))]
[RequireComponent(typeof(PlayerSkillbar))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerTrading))]
[RequireComponent(typeof(NetworkName))]
public partial class Player : Entity
{
    [HideInInspector] public bool zoneTransferPending;
    [Header("Components")]
    public Experience experience;
    public Intelligence intelligence;
    public Strength strength;
    public PlayerChat chat;
    public PlayerCrafting crafting;
    public PlayerGameMasterTool gameMasterTool;
    public PlayerGuild guild;
    //public PlayerIndicator indicator;
    public PlayerInventory inventory;
    public PlayerItemMall itemMall;
    public PlayerLooting looting;
    public PlayerMountControl mountControl;
    public PlayerNpcRevive npcRevive;
    public PlayerNpcTeleport npcTeleport;
    public PlayerNpcTrading npcTrading;
    public PlayerParty party;
    public PlayerPetControl petControl;
    public PlayerQuests quests;
    public PlayerSkillbar skillbar;
    public PlayerTrading trading;

    [Header("Text Meshes")]
    public TextMeshPro nameOverlay;
    public Color nameOverlayDefaultColor = Color.white;
    public Color nameOverlayOffenderColor = Color.magenta;
    public Color nameOverlayMurdererColor = Color.red;
    public Color nameOverlayPartyColor = new Color(0.341f, 0.965f, 0.702f);
    public string nameOverlayGameMasterPrefix = "[GM] ";

    [Header("Icons")]
    public Sprite classIcon; 
    public Sprite portraitIcon; 
  
    [HideInInspector] public string account = "";
    [HideInInspector] public string className = "";
   
    [SyncVar] public bool isGameMaster;
    
    public static Player localPlayer;
    
    public override float speed =>
        
        mountControl.activeMount != null && mountControl.activeMount.health.current > 0
            ? mountControl.activeMount.speed
            : base.speed;
    
    internal readonly SyncDictionary<string, double> itemCooldowns =
        new SyncDictionary<string, double>();

    [Header("Interaction")]
    public float interactionRange = 4;
    public bool localPlayerClickThrough = true; 
    public KeyCode cancelActionKey = KeyCode.Escape;
    public KeyCode interactKey = KeyCode.E;
    [Header("World Interaction (Tombstones, doors, etc.)")]
    [Tooltip("Layers considered for world interactions (InteractionTarget). " +
         "Set this to the same layers used by UI_InteractionPrompt.")]
    public LayerMask worldInteractionLayers = ~0;
    public readonly SyncList<ItemSlot> warehouseSlots = new SyncList<ItemSlot>();

    [Header("Action Combat")]
    [Tooltip("If false, Action-mode mouse attack is completely disabled.")]
    public bool actionCombatEnabled = false;
    [Tooltip("Key used for default attack when in Action movement mode.")]
    public KeyCode actionAttackKey = KeyCode.Mouse0;

    [Tooltip("How far the center-screen ray can look for a target.")]
    public float actionAttackRayDistance = 50f;

    [Tooltip("Being stunned interrupts the cast. Enable this option to continue the cast afterwards.")]
    public bool continueCastAfterStunned = true;

    [Header("PvP")]
    public BuffSkill offenderBuff;
    public BuffSkill murdererBuff;
    
    [Header("Movement")]
    [Range(0.1f, 1)] public float attackToMoveRangeRatio = 0.8f;
    [SyncVar, HideInInspector] public double nextRiskyActionTime = 0; 
    [SyncVar, HideInInspector] public Entity nextTarget;
    
    public static Dictionary<string, Player> onlinePlayers = new Dictionary<string, Player>();
    public double allowedLogoutTime => lastCombatTime + ((NetworkManagerMMO)NetworkManager.singleton).combatLogoutDelay;
    public double remainingLogoutTime => NetworkTime.time < allowedLogoutTime ? (allowedLogoutTime - NetworkTime.time) : 0;
    [HideInInspector] public int useSkillWhenCloser = -1;
    public override void OnStartLocalPlayer()
    {
        localPlayer = this;
        GameObject.FindWithTag("MinimapCamera").GetComponent<CopyPosition>().target = transform;
    }

    protected override void Start()
    {
        if (!isServer && !isClient) return;
        base.Start();
        onlinePlayers[name] = this;
    }

    void LateUpdate()
    {
        if (isClient) 
        {
            foreach (Animator anim in GetComponentsInChildren<Animator>())
            {
                anim.SetBool("MOVING", movement.IsMoving() && !mountControl.IsMounted());
                anim.SetBool("CASTING", state == "CASTING");
                anim.SetBool("STUNNED", state == "STUNNED");
                anim.SetBool("MOUNTED", mountControl.IsMounted()); 
                anim.SetBool("DEAD", state == "DEAD");
                foreach (Skill skill in skills.skills)
                    if (skill.level > 0 && !(skill.data is PassiveSkill))
                        anim.SetBool(skill.name, skill.CastTimeRemaining() > 0);
            }
        }
    }

    void OnDestroy()
    {
        if (onlinePlayers.TryGetValue(name, out Player entry) && entry == this)
            onlinePlayers.Remove(name);
        if (!isServer && !isClient) return;
        if (isLocalPlayer)
            localPlayer = null;
    }

    
    
    bool EventDied() =>
        health.current == 0;

    bool EventTargetDisappeared() =>
        target == null;

    bool EventTargetDied() =>
        target != null && target.health.current == 0;

    bool EventSkillRequest() =>
        0 <= skills.currentSkill && skills.currentSkill < skills.skills.Count;

    bool EventSkillFinished() =>
        0 <= skills.currentSkill && skills.currentSkill < skills.skills.Count &&
        skills.skills[skills.currentSkill].CastTimeRemaining() == 0;

    bool EventMoveStart() =>
        state != "MOVING" && movement.IsMoving(); 

    bool EventMoveEnd() =>
        state == "MOVING" && !movement.IsMoving(); 

    bool EventTradeStarted()
    {
        
        Player player = trading.FindPlayerFromInvitation();
        return player != null && player.trading.requestFrom == name;
    }

    bool EventTradeDone() =>
        
        state == "TRADING" && trading.requestFrom == "";

    bool EventCraftingStarted()
    {
        bool result = crafting.requestPending;
        crafting.requestPending = false;
        return result;
    }

    bool EventCraftingDone() =>
        state == "CRAFTING" && NetworkTime.time > crafting.endTime;

    bool EventStunned() =>
        NetworkTime.time <= stunTimeEnd;

    HashSet<string> cmdEvents = new HashSet<string>();

    [Command]
    public void CmdRespawn() { cmdEvents.Add("Respawn"); }
    bool EventRespawn() { return cmdEvents.Remove("Respawn"); }

    [Command]
    public void CmdCancelAction() { cmdEvents.Add("CancelAction"); }
    bool EventCancelAction() { return cmdEvents.Remove("CancelAction"); }
    [Command]
    void CmdInteractWorld(NetworkIdentity targetIdentity)
    {
        if (targetIdentity == null) return;

        InteractionTarget target = targetIdentity.GetComponent<InteractionTarget>();
        if (target == null) return;

        // Validate range on server
        if (!target.IsInRange(this))
            return;

        if (!target.TryGetInteractable(out var interactable))
            return;

        interactable.OnInteractServer(this);
    }
    // --- CENTRALIZED CLIENT TARGETING HELPER ------------------------------
    // Any system (click, action combat, E-interact, etc.) should use this
    // so the Target UI always sees a consistent 'target' value.
    [Client]
    public void ClientSetEntityTarget(Entity e)
    {
        if (!isLocalPlayer) return;
        if (e == null || e == this) return;
        if (e.netIdentity == null) return;

        CmdSetTarget(e.netIdentity);
    }
    [Server]
    string UpdateServer_IDLE()
    {
        
        if (EventDied())
        {
            return "DEAD";
        }
        if (EventStunned())
        {
            movement.Reset();
            return "STUNNED";
        }
        if (EventCancelAction())
        {
            
            target = null;
            return "IDLE";
        }
        if (EventTradeStarted())
        {
            
            skills.CancelCast(); 
            target = trading.FindPlayerFromInvitation();
            return "TRADING";
        }
        if (EventCraftingStarted())
        {
            
            skills.CancelCast(); 
            return "CRAFTING";
        }
        if (EventMoveStart())
        {
            
            skills.CancelCast();
            return "MOVING";
        }
        if (EventSkillRequest())
        {
            if (!mountControl.IsMounted())
            {
                Skill skill = skills.skills[skills.currentSkill];
                nextTarget = target; 
                if (skills.CastCheckSelf(skill) &&
                    skills.CastCheckTarget(skill) &&
                    skills.CastCheckDistance(skill, out Vector3 destination))
                {
                    movement.Reset();
                    skills.StartCast(skill);
                    return "CASTING";
                }
                else
                {
                    
                    skills.currentSkill = -1;
                    nextTarget = null; 
                    return "IDLE";
                }
            }
        }
        if (EventSkillFinished()) {} 
        if (EventMoveEnd()) {} 
        if (EventTradeDone()) {} 
        if (EventCraftingDone()) {} 
        if (EventRespawn()) {} 
        if (EventTargetDied()) {} 
        if (EventTargetDisappeared()) {} 

        return "IDLE"; 
    }

    [Server]
    string UpdateServer_MOVING()
    {
        
        if (EventDied())
        {
            
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
        if (EventCancelAction())
        {
            
            skills.CancelCast();
            
            return "IDLE";
        }
        if (EventTradeStarted())
        {
            
            skills.CancelCast();
            movement.Reset();
            target = trading.FindPlayerFromInvitation();
            return "TRADING";
        }
        if (EventCraftingStarted())
        {
            
            skills.CancelCast();
            movement.Reset();
            return "CRAFTING";
        }
        
        
        
        
        
        
        
        
        
        
        
        
        if (EventSkillRequest())
        {
            
            
            if (!mountControl.IsMounted())
            {
                Skill skill = skills.skills[skills.currentSkill];
                if (skills.CastCheckSelf(skill) &&
                    skills.CastCheckTarget(skill) &&
                    skills.CastCheckDistance(skill, out Vector3 destination))
                {
                    
                    
                    skills.StartCast(skill);
                    return "CASTING";
                }
            }
        }
        if (EventMoveStart()) {} 
        if (EventSkillFinished()) {} 
        if (EventTradeDone()) {} 
        if (EventCraftingDone()) {} 
        if (EventRespawn()) {} 
        if (EventTargetDied()) {} 
        if (EventTargetDisappeared()) {} 

        return "MOVING"; 
    }

    void UseNextTargetIfAny()
    {
        
        
        
        if (nextTarget != null)
        {
            target = nextTarget;
            nextTarget = null;
        }
    }

    [Server]
    string UpdateServer_CASTING()
    {
        
        if (target && movement.DoCombatLookAt())
            movement.LookAtY(target.transform.position);

        
        
        
        
        
        
        
        
        if (EventDied())
        {
            
            UseNextTargetIfAny(); 
            return "DEAD";
        }
        if (EventStunned())
        {
            
            
            skills.CancelCast(!continueCastAfterStunned);
            movement.Reset();
            return "STUNNED";
        }
        if (EventMoveStart())
        {
            
            
            
            
            
            
            
            
            
            
            

            
            
            

            
            
            
            
            
        }
        if (EventCancelAction())
        {
            
            skills.CancelCast();
            UseNextTargetIfAny(); 
            return "IDLE";
        }
        if (EventTradeStarted())
        {
            
            skills.CancelCast();
            movement.Reset();

            
            target = trading.FindPlayerFromInvitation();
            nextTarget = null;
            return "TRADING";
        }
        if (EventTargetDisappeared())
        {
            
            if (skills.skills[skills.currentSkill].cancelCastIfTargetDied)
            {
                skills.CancelCast();
                UseNextTargetIfAny(); 
                return "IDLE";
            }
        }
        if (EventTargetDied())
        {
            
            if (skills.skills[skills.currentSkill].cancelCastIfTargetDied)
            {
                skills.CancelCast();
                UseNextTargetIfAny(); 
                return "IDLE";
            }
        }
        if (EventSkillFinished())
        {
            
            
            
            Skill skill = skills.skills[skills.currentSkill];

            
            skills.FinishCast(skill);

            
            skills.currentSkill = -1;

            
            UseNextTargetIfAny();

            
            return "IDLE";
        }
        if (EventMoveEnd()) {} 
        if (EventTradeDone()) {} 
        if (EventCraftingStarted()) {} 
        if (EventCraftingDone()) {} 
        if (EventRespawn()) {} 
        if (EventSkillRequest()) {} 

        return "CASTING"; 
    }

    [Server]
    string UpdateServer_STUNNED()
    {
        
        if (EventDied())
        {
            
            return "DEAD";
        }
        if (EventStunned())
        {
            return "STUNNED";
        }

        
        
        return "IDLE";
    }

    [Server]
    string UpdateServer_TRADING()
    {
        
        if (EventDied())
        {
            
            trading.Cleanup();
            return "DEAD";
        }
        if (EventStunned())
        {
            
            skills.CancelCast();
            movement.Reset();
            trading.Cleanup();
            return "STUNNED";
        }
        if (EventMoveStart())
        {
            
            movement.Reset();
            return "TRADING";
        }
        if (EventCancelAction())
        {
            
            trading.Cleanup();
            return "IDLE";
        }
        if (EventTargetDisappeared())
        {
            
            trading.Cleanup();
            return "IDLE";
        }
        if (EventTargetDied())
        {
            
            trading.Cleanup();
            return "IDLE";
        }
        if (EventTradeDone())
        {
            
            trading.Cleanup();
            return "IDLE";
        }
        if (EventMoveEnd()) {} 
        if (EventSkillFinished()) {} 
        if (EventCraftingStarted()) {} 
        if (EventCraftingDone()) {} 
        if (EventRespawn()) {} 
        if (EventTradeStarted()) {} 
        if (EventSkillRequest()) {} 

        return "TRADING"; 
    }

    [Server]
    string UpdateServer_CRAFTING()
    {
        if (EventDied())
        {
            return "DEAD";
        }
        if (EventStunned())
        {
            movement.Reset();
            return "STUNNED";
        }
        if (EventMoveStart())
        {
            movement.Reset();
            return "CRAFTING";
        }
        if (EventCraftingDone())
        {
            crafting.Craft();
            return "IDLE";
        }
        if (EventCancelAction()) {} 
        if (EventTargetDisappeared()) {} 
        if (EventTargetDied()) {} 
        if (EventMoveEnd()) {} 
        if (EventSkillFinished()) {} 
        if (EventRespawn()) {} 
        if (EventTradeStarted()) {} 
        if (EventTradeDone()) {} 
        if (EventCraftingStarted()) {} 
        if (EventSkillRequest()) {} 

        return "CRAFTING"; 
    }

    [Server]
    string UpdateServer_DEAD()
    {
        
        if (EventRespawn())
        {
            //Transform start = NetworkManagerMMO.GetNearestStartPosition(transform.position);
            //movement.Warp(start.position);
            //Revive(0.5f);
            HandleGraveyardRespawn();
            return "IDLE";
        }
        if (EventMoveStart())
        {
            //Debug.LogWarning("Player " + name + " moved while dead. This should not happen.");
            return "DEAD";
        }
        if (EventMoveEnd()) {} 
        if (EventSkillFinished()) {} 
        if (EventDied()) {} 
        if (EventCancelAction()) {} 
        if (EventTradeStarted()) {} 
        if (EventTradeDone()) {} 
        if (EventCraftingStarted()) {} 
        if (EventCraftingDone()) {} 
        if (EventTargetDisappeared()) {} 
        if (EventTargetDied()) {} 
        if (EventSkillRequest()) {} 

        return "DEAD"; 
    }

    [Server]
    protected override string UpdateServer()
    {
        if (state == "IDLE")     return UpdateServer_IDLE();
        if (state == "MOVING")   return UpdateServer_MOVING();
        if (state == "CASTING")  return UpdateServer_CASTING();
        if (state == "STUNNED")  return UpdateServer_STUNNED();
        if (state == "TRADING")  return UpdateServer_TRADING();
        if (state == "CRAFTING") return UpdateServer_CRAFTING();
        if (state == "DEAD")     return UpdateServer_DEAD();
        Debug.LogError("invalid state:" + state);
        return "IDLE";
    }
    // Trigger the same interaction pipeline as double-clicking a target.
    // Trigger the same interaction pipeline as double-clicking a target.
    [Client]
    void TryInteractWithTarget()
    {
        if (!isLocalPlayer) return;

        // Only allow in the same states as before
        if (!(state == "IDLE" ||
              state == "MOVING" ||
              state == "CASTING" ||
              state == "STUNNED"))
            return;

        // 1) If we already have an Entity target, use the original logic.
        if (target != null && target != this)
        {
            // Cancel any pending "use skill when closer" just like before
            useSkillWhenCloser = -1;

            // This calls into Monster/Npc/Player/Mount.OnInteract etc.
            target.Interact();
            return;
        }

        // 2) No Entity target: try a world interaction (Tombstones, doors, mobs with InteractionTarget, etc.)
        float range = interactionRange;

        // Origin: a bit above the player's position (chest height)
        Vector3 origin = transform.position + Vector3.up * 1.2f;

        // Layer mask: configured in inspector, and ignore our own layer
        int mask = worldInteractionLayers;
        mask &= ~(1 << gameObject.layer);

        // Find all colliders in range on the allowed layers
        Collider[] hits = Physics.OverlapSphere(origin, range, mask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
            return;

        // Direction we consider "forward": prefer camera forward, fall back to player forward
        Vector3 fwd;
        Camera cam = Camera.main;
        if (cam != null)
            fwd = cam.transform.forward;
        else
            fwd = transform.forward;

        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f)
            fwd = Vector3.forward;
        fwd.Normalize();

        // Choose the "best" target by angle+distance (same idea as UI_InteractionPrompt)
        InteractionTarget best = null;
        float bestScore = 0f;

        foreach (var col in hits)
        {
            if (col == null) continue;

            InteractionTarget t = col.GetComponentInParent<InteractionTarget>();
            if (t == null) continue;
            if (!t.IsInRange(this)) continue;

            Vector3 to = t.transform.position - transform.position;
            float dist = new Vector2(to.x, to.z).magnitude;
            if (dist < 0.01f) dist = 0.01f;

            Vector3 toFlat = new Vector3(to.x, 0f, to.z).normalized;
            float dot = Mathf.Clamp01(Vector3.Dot(fwd, toFlat));

            // score = angle alignment / (1 + distance)
            float score = dot / (1f + dist);

            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        if (best == null)
            return;

        // if this InteractionTarget belongs to an Entity (NPC / monster / player),
        // also set it as our target so the Target UI shows up.
        Entity bestEntity = best.GetComponentInParent<Entity>();
        if (bestEntity != null && bestEntity != this)
        {
            ClientSetEntityTarget(bestEntity);
        }

        if (best.serverAuthoritative)
        {
            if (best.Identity != null)
            {
                CmdInteractWorld(best.Identity);
            }
            else
            {
                Debug.LogWarning($"[InteractionDebug] {best.name} is serverAuthoritative but has no NetworkIdentity.");
            }
        }
        else
        {
            if (best.TryGetInteractable(out var interactable))
            {
                interactable.OnInteractClient(this);
            }
        }
    }

    
    [Client]
    protected override void UpdateClient()
    {
        if (state == "IDLE" || state == "MOVING")
        {
            if (isLocalPlayer)
            {
                // cancel (Escape)
                if (Input.GetKeyDown(cancelActionKey))
                {
                    movement.Reset();
                    CmdCancelAction();
                }

                // primary interact (E) on current target
                if (Input.GetKeyDown(interactKey))
                {
                    TryInteractWithTarget();
                }
                HandleActionModeDefaultAttack();
                if (useSkillWhenCloser != -1)
                {
                    if (CanAttack(target))
                    {
                        float range = skills.skills[useSkillWhenCloser].castRange * attackToMoveRangeRatio;
                        if (Utils.ClosestDistance(this, target) <= range)
                        {
                            ((PlayerSkills)skills).CmdUse(useSkillWhenCloser);
                            useSkillWhenCloser = -1;
                        }
                        else
                        {
                            Vector3 destination = Utils.ClosestPoint(target, transform.position);
                            movement.Navigate(destination, range);
                        }
                    }
                    else useSkillWhenCloser = -1;
                }
            }
        }
        else if (state == "CASTING")
        {
            if (target && movement.DoCombatLookAt())
                movement.LookAtY(target.transform.position);

            if (isLocalPlayer)
            {
                movement.Reset();

                // cancel (Escape)
                if (Input.GetKeyDown(cancelActionKey))
                    CmdCancelAction();

                // allow E to interact while casting (matches click behaviour)
                if (Input.GetKeyDown(interactKey))
                    TryInteractWithTarget();
            }
        }
        else if (state == "STUNNED")
        {
            if (isLocalPlayer)
            {
                movement.Reset();

                // cancel (Escape)
                if (Input.GetKeyDown(cancelActionKey))
                    CmdCancelAction();

                // optional: E allowed while stunned (matches click behaviour)
                if (Input.GetKeyDown(interactKey))
                    TryInteractWithTarget();
            }
        }
        else if (state == "TRADING") {}
        else if (state == "CRAFTING") {}
        else if (state == "DEAD") {}
        else Debug.LogError("invalid state:" + state);
    }
    [Client]
    void HandleActionModeDefaultAttack()
    {
        if (!actionCombatEnabled) return;
        if (!isLocalPlayer) return;

        // we only do this if we are using PlayerNavMeshMovement in Action mode
        var navMove = movement as PlayerNavMeshMovement;
        if (navMove == null) return;
        if (navMove.movementMode != PlayerNavMeshMovement.MovementMode.Action) return;

        // don't fire through UI
        if (Utils.IsCursorOverUserInterface()) return;

        // only on button down
        if (!Input.GetKeyDown(actionAttackKey)) return;

        // 1) Try to pick a target in front of the camera (center of screen)
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
            Ray ray = cam.ScreenPointToRay(center);

            if (Physics.Raycast(ray, out RaycastHit hit, actionAttackRayDistance))
            {
                Entity e = hit.collider.GetComponentInParent<Entity>();
                if (e != null && e != this)
                {
                    // 🔹 NEW: route all targeting through the helper so Target UI always updates
                    ClientSetEntityTarget(e);
                }
            }
        }

        // 2) Use the SAME default-attack selection logic as OnSkillCastFinished

        var ps = (PlayerSkills)skills;
        if (ps == null || ps.skills.Count == 0)
            return;

        int idx = 0; // safe fallback: unarmed default attack (skills[0])

        // if weapon / system filled skillbar slot 1 (index 0), prefer that
        if (skillbar != null &&
            skillbar.slots != null &&
            skillbar.slots.Length > 0)
        {
            string basicName = skillbar.slots[0].reference;
            if (!string.IsNullOrWhiteSpace(basicName))
            {
                for (int i = 0; i < ps.skills.Count; ++i)
                {
                    if (ps.skills[i].name == basicName)
                    {
                        idx = i;
                        break;
                    }
                }
            }
        }

        // this calls into your existing default combat system
        ps.TryUse(idx);
    }

    protected override void UpdateOverlays()
    {
        base.UpdateOverlays();

        if (nameOverlay != null)
        {
            nameOverlay.text = name;
            if (localPlayer != null)
            {
                if (IsMurderer())
                    nameOverlay.color = nameOverlayMurdererColor;
                else if (IsOffender())
                    nameOverlay.color = nameOverlayOffenderColor;
                
                else if (localPlayer.party.InParty() && localPlayer.party.party.Contains(name))
                    nameOverlay.color = nameOverlayPartyColor;
                
                else
                    nameOverlay.color = nameOverlayDefaultColor;
            }
        }
    }

    [HideInInspector] public int pendingSkill = -1;
    [HideInInspector] public Vector3 pendingDestination;
    [HideInInspector] public bool pendingDestinationValid;
    
    [Client]
    public void OnSkillCastFinished(Skill skill)
    {
        if (!isLocalPlayer) return;
        if (pendingDestinationValid)
        {
            movement.Navigate(pendingDestination, 0);
        }

        else if (pendingSkill != -1)
        {
            ((PlayerSkills)skills).TryUse(pendingSkill, true);
        }

        else if (skill.followupDefaultAttack)
        {
            var ps = (PlayerSkills)skills;
            int idx = 0; // safe fallback

            // use whatever the weapon put into skillbar slot 1 (index 0)
            if (skillbar != null && skillbar.slots != null && skillbar.slots.Length > 0)
            {
                string basicName = skillbar.slots[0].reference;
                if (!string.IsNullOrWhiteSpace(basicName))
                {
                    for (int i = 0; i < ps.skills.Count; ++i)
                        if (ps.skills[i].name == basicName) { idx = i; break; }
                }
            }

            ps.TryUse(idx, true);
        }
        
        pendingSkill = -1;
        pendingDestinationValid = false;
    }

    
    [Server]
    public void OnDamageDealtTo(Entity victim)
    {
        
        if (victim is Player && ((Player)victim).IsInnocent())
        {
            
            if (!IsMurderer()) StartOffender();
        }
        
        else if (victim is Pet && ((Pet)victim).owner.IsInnocent())
        {
            
            if (!IsMurderer()) StartOffender();
        }
    }

    [Server]
    public void OnKilledEnemy(Entity victim)
    {
        
        if (victim is Player && ((Player)victim).IsInnocent())
        {
            StartMurderer();
        }
        
        else if (victim is Pet && ((Pet)victim).owner.IsInnocent())
        {
            StartMurderer();
        }
    }

    
    
    [ServerCallback]
    public override void OnAggro(Entity entity)
    {
        
        base.OnAggro(entity);

        
        if (petControl.activePet != null && petControl.activePet.defendOwner)
            petControl.activePet.OnAggro(entity);
    }

    
    
    
    
    
    public bool IsMovementAllowed()
    {
        
        bool castingAndAllowed = state == "CASTING" &&
                                 skills.currentSkill != -1 &&
                                 skills.skills[skills.currentSkill].allowMovement;

        
        
        
        
        bool isLocalPlayerTyping = isLocalPlayer && UIUtils.AnyInputActive();
        return (state == "IDLE" || state == "MOVING" || castingAndAllowed) &&
               !isLocalPlayerTyping;
    }

    
    [Server]
    public override void OnDeath()
    {
        
        base.OnDeath();

        
        movement.Reset();
    }

    
    
    public float GetItemCooldown(string cooldownCategory)
    {
        
        if (itemCooldowns.TryGetValue(cooldownCategory, out double cooldownEnd))
        {
            return NetworkTime.time >= cooldownEnd ? 0 : (float)(cooldownEnd - NetworkTime.time);
        }

        
        return 0;
    }

    
    public void SetItemCooldown(string cooldownCategory, float cooldown)
    {
        
        itemCooldowns[cooldownCategory] = NetworkTime.time + cooldown;
    }

    
    
    
    public override bool CanAttack(Entity entity)
    {
        return base.CanAttack(entity) &&
               (entity is Monster ||
                entity is Player ||
                (entity is Pet && entity != petControl.activePet) ||
                (entity is Mount && entity != mountControl.activeMount));
    }

    
    
    
    
    
    
    
    
    
    
    
    public bool IsOffender()
    {
        return offenderBuff != null && skills.GetBuffIndexByName(offenderBuff.name) != -1;
    }

    public bool IsMurderer()
    {
        return murdererBuff != null && skills.GetBuffIndexByName(murdererBuff.name) != -1;
    }

    public bool IsInnocent()
    {
        return !IsOffender() && !IsMurderer();
    }

    public void StartOffender()
    {
        if (offenderBuff != null) skills.AddOrRefreshBuff(new Buff(offenderBuff, 1));
    }

    public void StartMurderer()
    {
        if (murdererBuff != null) skills.AddOrRefreshBuff(new Buff(murdererBuff, 1));
    }

    [Command]
    public void CmdSetTarget(NetworkIdentity ni)
    {
        if (ni != null)
        {
            if (state == "IDLE" || state == "MOVING" || state == "STUNNED")
                target = ni.GetComponent<Entity>();
            else if (state == "CASTING")
                nextTarget = ni.GetComponent<Entity>();
        }
    }
    
    protected override void OnInteract()
    {
        if (this != localPlayer)
        {
            if (localPlayer.CanAttack(this) && localPlayer.skills.skills.Count > 0)
            {
                var ps = (PlayerSkills)localPlayer.skills;
                int idx = 0; // safe fallback

                if (localPlayer.skillbar != null && localPlayer.skillbar.slots != null && localPlayer.skillbar.slots.Length > 0)
                {
                    string basicName = localPlayer.skillbar.slots[0].reference;
                    if (!string.IsNullOrWhiteSpace(basicName))
                        for (int i = 0; i < ps.skills.Count; ++i)
                            if (ps.skills[i].name == basicName) { idx = i; break; }
                }
                ps.TryUse(idx);
            }
            else
            {
                Vector3 destination = Utils.ClosestPoint(this, localPlayer.transform.position);
                localPlayer.movement.Navigate(destination, localPlayer.interactionRange);
            }
        }
    }
}
