using UnityEngine;
using Mirror;

[RequireComponent(typeof(Inventory))]
[RequireComponent(typeof(MonsterSkills))]
[RequireComponent(typeof(NavMeshMovement))]
[RequireComponent(typeof(NetworkNavMeshAgent))]
public partial class Monster : Entity
{
    [Header("Components")]
    public MonsterInventory inventory;

    [Header("Movement")]
    [Range(0, 1)] public float moveProbability = 0.1f;
    public float moveDistance = 10;

    public float followDistance = 20;
    [Range(0.1f, 1)] public float attackToMoveRangeRatio = 0.8f;

    // ===== Behavior Types (server-side) =====
    public enum BehaviorType { Aggressive, Passive, Assist, Flee }

    [Header("Behavior")]
    public BehaviorType behavior = BehaviorType.Aggressive;

    // Aggressive (proactive scan)
    [Range(0, 50)] public float aggroRadius = 15f;

    // Assist (pack helpers)
    [Range(0, 50)] public float assistRadius = 14f;
    [Range(0, 10)] public int maxHelpersTotal = 3;
    [Range(0, 5)] public int maxHelpersPerSecond = 1;

    // Flee (run on sight / on hit)
    [Range(0, 50)] public float fleeDetectRadius = 10f;
    [Range(1, 3)] public float fleeSpeedMultiplier = 1.2f; // kept for designers; not used if movement can't scale
    [Range(0, 100)] public float fleeSafeDistance = 20f;


    [Header("Flee Advanced")]
    [Range(0, 180)] public float fleeJitterMaxAngle = 25f;
    [Range(0, 1)] public float fleeHomeBias = 0.5f;
    [Range(1, 10)] public int fleeDirectionSamples = 5;
    [Range(1, 30)] public float fleeStep = 10f;

    [Header("Flee Accept Combat")]
    [Range(0, 10)] public float acceptCombatRadius = 3f;
    [Range(0, 5)] public float acceptCombatStickSeconds = 0.75f;
    [Range(1, 15)] public float maxFleeSeconds = 5f;

    // Runtime flee state
    double fleeStartTime;
    double acceptWindowStart; // 0 = inactive
    int fleeRepathCounter;

    // Flee perception cache (throttled scan reuse)
    bool fleeCachedAnyThreat;
    Entity fleeCachedNearest;
    float fleeCachedNearestSqr = float.MaxValue;

    // Common guards
    [Range(50, 1000)] public int repathCooldownMs = 250;     // min time between path recalcs
    [Range(0.1f, 2f)] public float perceptionIntervalActive = 0.2f;
    [Range(0.2f, 2f)] public float perceptionIntervalIdle = 0.8f;
    [Range(0.25f, 5f)] public float minDestinationDelta = 1f;
    [Header("Combat Tuning")]
    [Range(0.05f, 0.5f)] public float castDistanceCheckInterval = 0.1f; // ~10 Hz for cast distance checks
    double nextCastDistanceCheckAt;
    bool cachedTargetTooFarToAttack;


    // ===== Runtime (server-only) =====
    [HideInInspector] public bool isFleeing;
    double nextPerceptionAt;      // when we may scan/decide next
    double nextRepathAt;          // when we may issue next Navigate
    double lastAssistBroadcastAt; // rate-limit assist pings
    int helpersJoined;            // total helpers joined this fight

    // Track last requested navigation destination (since Movement doesn't expose one)
    Vector3 lastNavDest;

    // Assist alert (lightweight, local to Monster.cs)
    bool assistAlerted;
    double assistAlertUntil;
    Entity assistAlertTarget;

    // Small shared buffer to avoid allocs on perception
    static readonly Collider[] perceptionHits = new Collider[32];

    [Header("Experience Reward")]
    public long rewardExperience = 10;
    public long rewardSkillExperience = 2;

    [Header("Respawn")]
    public float deathTime = 30f;
    [HideInInspector] public double deathTimeEnd;
    public bool respawn = true;
    public float respawnTime = 10f;
    [HideInInspector] public double respawnTimeEnd;
    [HideInInspector] public Vector3 startPosition;
    // Client-side animator caching (reduce per-frame string lookups)
    static readonly int AnimHashMoving  = Animator.StringToHash("MOVING");
    static readonly int AnimHashCasting = Animator.StringToHash("CASTING");
    static readonly int AnimHashStunned = Animator.StringToHash("STUNNED");
    static readonly int AnimHashDead    = Animator.StringToHash("DEAD");

    int[] skillParamHashes;
    bool[] lastSkillCasting;

    bool lastMoving;
    bool lastCasting;
    bool lastStunned;
    bool lastDead;


    protected override void Start()
    {
        base.Start();
        startPosition = transform.position;
        lastNavDest = startPosition;
        ClampNavGuardSettings();
    }

    void ClampNavGuardSettings()
    {
        if (repathCooldownMs < 250)
            repathCooldownMs = 250;

        if (minDestinationDelta < 1f)
            minDestinationDelta = 1f;
    }

    void LateUpdate()
    {
        if (!isClient || animator == null)
            return;

        // Ensure we have animator hashes for skills (rebuild if skill list size changed)
        if (skills != null)
        {
            if (skillParamHashes == null || skillParamHashes.Length != skills.skills.Count)
            {
                int count = skills.skills.Count;
                skillParamHashes = new int[count];
                lastSkillCasting = new bool[count];

                for (int i = 0; i < count; ++i)
                {
                    skillParamHashes[i] = Animator.StringToHash(skills.skills[i].name);
                    lastSkillCasting[i] = false;
                }
            }
        }

        // High-level state flags
        bool movingNow  = state == "MOVING" && movement.GetVelocity() != Vector3.zero;
        bool castingNow = state == "CASTING";
        bool stunnedNow = state == "STUNNED";
        bool deadNow    = state == "DEAD";

        if (movingNow != lastMoving)
        {
            animator.SetBool(AnimHashMoving, movingNow);
            lastMoving = movingNow;
        }

        if (castingNow != lastCasting)
        {
            animator.SetBool(AnimHashCasting, castingNow);
            lastCasting = castingNow;
        }

        if (stunnedNow != lastStunned)
        {
            animator.SetBool(AnimHashStunned, stunnedNow);
            lastStunned = stunnedNow;
        }

        if (deadNow != lastDead)
        {
            animator.SetBool(AnimHashDead, deadNow);
            lastDead = deadNow;
        }

        // Per-skill flags: still evaluated per frame, but only push changes when a skill's cast state flips.
        if (skills != null && skillParamHashes != null && lastSkillCasting != null)
        {
            int count = Mathf.Min(skills.skills.Count, skillParamHashes.Length, lastSkillCasting.Length);
            for (int i = 0; i < count; ++i)
            {
                bool activeNow = skills.skills[i].CastTimeRemaining() > 0;
                if (activeNow != lastSkillCasting[i])
                {
                    animator.SetBool(skillParamHashes[i], activeNow);
                    lastSkillCasting[i] = activeNow;
                }
            }
        }
    }

    bool EventDied() =>
    health.current == 0;

    bool EventDeathTimeElapsed() =>
    state == "DEAD" && deathTimeEnd > 0 && NetworkTime.time >= deathTimeEnd;

    bool EventRespawnTimeElapsed() =>
    state == "DEAD" && respawn && respawnTimeEnd > 0 && NetworkTime.time >= respawnTimeEnd;

    bool EventTargetDisappeared() =>
    target == null;

    bool EventTargetDied() =>
    target != null && target.health.current == 0;

    bool EventTargetTooFarToAttack()
    {
        if (target == null)
            return false;

        if (skills.currentSkill < 0 || skills.currentSkill >= skills.skills.Count)
            return false;

        double now = NetworkTime.time;

        // Reuse last result until the next scheduled cast distance check
        if (now < nextCastDistanceCheckAt)
            return cachedTargetTooFarToAttack;

        nextCastDistanceCheckAt = now + castDistanceCheckInterval;

        // CastCheckDistance returns true if we are in range; false if we need to move.
        bool inRange = skills.CastCheckDistance(skills.skills[skills.currentSkill], out Vector3 destination);
        cachedTargetTooFarToAttack = !inRange;
        return cachedTargetTooFarToAttack;
    }

    bool EventTargetTooFarToFollow() =>
    target != null &&
    Vector3.Distance(startPosition, target.collider.ClosestPointOnBounds(transform.position)) > followDistance;

    bool EventTargetEnteredSafeZone() =>
    target != null && target.inSafeZone;

    bool EventAggro() =>
    target != null && target.health.current > 0;

    bool EventSkillRequest() =>
    0 <= skills.currentSkill && skills.currentSkill < skills.skills.Count;

    bool EventSkillFinished() =>
    0 <= skills.currentSkill && skills.currentSkill < skills.skills.Count &&
    skills.skills[skills.currentSkill].CastTimeRemaining() == 0;

    bool EventMoveEnd() =>
    state == "MOVING" && !movement.IsMoving();

    bool EventMoveRandomly() =>
    Random.value <= moveProbability * Time.deltaTime;

    bool EventStunned() =>
    NetworkTime.time <= stunTimeEnd;

    // ===== Server State Updates =====

    [Server]
    string UpdateServer_IDLE()
    {
        if (EventDied()) return "DEAD";

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

        if (EventTargetTooFarToFollow())
        {
            target = null;
            skills.CancelCast();
            ImmediateNavigate(startPosition, 0);
            return "MOVING";
        }

        if (EventTargetTooFarToAttack())
        {
            // navigate toward target (guarded by repath cooldown & delta)
            float stoppingDistance = ((MonsterSkills)skills).CurrentCastRange() * attackToMoveRangeRatio;
            Vector3 destination = Utils.ClosestPoint(target, transform.position);
            TryNavigate(destination, stoppingDistance);
            return "MOVING";
        }

        if (EventTargetEnteredSafeZone())
        {
            EvadeHome(); // walk home instead of dying
            return "MOVING";
        }

        // --- Behavior-driven perception hooks (cheap, bounded) ---
        TryAssistEngage();         // Assist engages if alerted
        TryFleePerception();       // Flee starts if threats nearby
        TryAggressivePerception(); // Aggressive proactively picks a target

        if (EventSkillRequest())
        {
            Skill skill = skills.skills[skills.currentSkill];
            if (skills.CastCheckSelf(skill))
            {
                if (skills.CastCheckTarget(skill))
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
            else
            {
                skills.currentSkill = -1;
                return "IDLE";
            }
        }

        if (EventAggro())
        {
            if (skills.skills.Count > 0)
            skills.currentSkill = ((MonsterSkills)skills).NextSkill();
            else
            Debug.LogError(name + " has no skills to attack with.");

            return "IDLE";
        }

        if (EventMoveRandomly())
        {
            Vector3 circle2D = Random.insideUnitCircle * moveDistance;
            ImmediateNavigate(startPosition + new Vector3(circle2D.x, 0, circle2D.y), 0);
            return "MOVING";
        }

        // fallthrough bookkeeping
        if (EventDeathTimeElapsed()) { }
        if (EventRespawnTimeElapsed()) { }
        if (EventMoveEnd()) { }
        if (EventSkillFinished()) { }
        if (EventTargetDisappeared()) { }

        return "IDLE";
    }

    [Server]
    string UpdateServer_MOVING()
    {
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
        if (EventTargetTooFarToFollow())
        {
            target = null;
            skills.CancelCast();
            ImmediateNavigate(startPosition, 0);
            return "MOVING";
        }

        // Maintain flee behavior while moving (takes priority over chase)
        if (isFleeing)
        {
            UpdateFleeWhileMoving();
            return "MOVING";
        }

        if (EventTargetTooFarToAttack())
        {
            float stoppingDistance = ((MonsterSkills)skills).CurrentCastRange() * attackToMoveRangeRatio;
            Vector3 destination = Utils.ClosestPoint(target, transform.position);
            TryNavigate(destination, stoppingDistance);
            return "MOVING";
        }

        if (EventTargetEnteredSafeZone())
        {
            EvadeHome(); // walk home instead of dying
            return "MOVING";
        }

        if (EventAggro())
        {
            if (skills.skills.Count > 0)
            skills.currentSkill = ((MonsterSkills)skills).NextSkill();
            else
            Debug.LogError(name + " has no skills to attack with.");
            movement.Reset();
            return "IDLE";
        }

        // fallthrough bookkeeping
        if (EventDeathTimeElapsed()) { }
        if (EventRespawnTimeElapsed()) { }
        if (EventSkillFinished()) { }
        if (EventTargetDisappeared()) { }
        if (EventSkillRequest()) { }
        if (EventMoveRandomly()) { }

        return "MOVING";
    }

    [Server]
    string UpdateServer_CASTING()
    {
        if (target)
        movement.LookAtY(target.transform.position);

        if (EventDied()) return "DEAD";

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

        if (EventTargetEnteredSafeZone())
        {
            // No more "safe-zone kills mob": cancel & evade home instead.
            skills.CancelCast();
            EvadeHome();
            return "MOVING";
        }

        if (EventSkillFinished())
        {
            skills.FinishCast(skills.skills[skills.currentSkill]);
            if (target != null && target.health.current == 0)
            target = null;
            ((MonsterSkills)skills).lastSkill = skills.currentSkill;
            skills.currentSkill = -1;
            return "IDLE";
        }

        // fallthrough bookkeeping
        if (EventDeathTimeElapsed()) { }
        if (EventRespawnTimeElapsed()) { }
        if (EventMoveEnd()) { }
        if (EventTargetTooFarToAttack()) { }
        if (EventTargetTooFarToFollow()) { }
        if (EventAggro()) { }
        if (EventSkillRequest()) { }
        if (EventMoveRandomly()) { }

        return "CASTING";
    }

    [Server]
    string UpdateServer_STUNNED()
    {
        if (EventDied()) return "DEAD";
        if (EventStunned()) return "STUNNED";
        return "IDLE";
    }

    [Server]
    string UpdateServer_DEAD()
    {
        if (EventRespawnTimeElapsed())
        {
            gold = 0;
            inventory.slots.Clear();
            Show();
            movement.Warp(startPosition);
            Revive();
            deathTimeEnd = 0;
            respawnTimeEnd = 0;
            // Avoid immediate re-aggro edge cases
            target = null;
            return "IDLE";
        }

        if (EventDeathTimeElapsed())
        {
            if (respawn) Hide();
            else NetworkServer.Destroy(gameObject);
            return "DEAD";
        }

        // fallthrough bookkeeping
        if (EventSkillRequest()) { }
        if (EventSkillFinished()) { }
        if (EventMoveEnd()) { }
        if (EventTargetDisappeared()) { }
        if (EventTargetDied()) { }
        if (EventTargetTooFarToFollow()) { }
        if (EventTargetTooFarToAttack()) { }
        if (EventTargetEnteredSafeZone()) { }
        if (EventAggro()) { }
        if (EventMoveRandomly()) { }
        if (EventStunned()) { }
        if (EventDied()) { }

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
    protected override void UpdateClient()
    {
        if (state == "CASTING")
        {
            if (target)
            movement.LookAtY(target.transform.position);
        }
    }

    public void OnDrawGizmos()
    {
        Vector3 startHelp = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startHelp, moveDistance);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(startHelp, followDistance);
    }

    // ===== Behavior Helpers =====

    [Server]
    void TryAggressivePerception()
    {
        if (behavior != BehaviorType.Aggressive) return;
        if (target != null) return;
        if (NetworkTime.time < nextPerceptionAt) return;

        nextPerceptionAt = NetworkTime.time + (state == "IDLE" ? perceptionIntervalIdle : perceptionIntervalActive);

        int count = Physics.OverlapSphereNonAlloc(transform.position, aggroRadius, perceptionHits);
        float bestSqr = float.MaxValue;
        Entity best = null;

        for (int i = 0; i < count; ++i)
        {
            var e = perceptionHits[i] ? perceptionHits[i].GetComponentInParent<Entity>() : null;
            if (e != null && e != this && e.health.current > 0 && CanAttack(e))
            {
                float d = (e.transform.position - transform.position).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = e; }
            }
        }

        if (best != null) target = best;
    }

    [Server]
    void TryAssistEngage()
    {
        if (behavior != BehaviorType.Assist) return;

        // Only attempt to engage if alerted within window and not already busy.
        if (!assistAlerted) return;
        if (NetworkTime.time > assistAlertUntil) { assistAlerted = false; assistAlertTarget = null; return; }

        if (target == null && assistAlertTarget != null && assistAlertTarget.health.current > 0 && CanAttack(assistAlertTarget))
        {
            target = assistAlertTarget;
            assistAlerted = false; // consume alert
        }
    }

    [Server]
    void MaybeBroadcastAssist(Entity aggressor)
    {
        // Called when THIS monster enters combat; ping nearby Assist monsters
        if (assistRadius <= 0 || maxHelpersTotal <= 0) return;
        if (behavior == BehaviorType.Assist) return; // callers are typically Aggressive/Passive
        if (helpersJoined >= maxHelpersTotal) return;

        // limit pings to once per second
        if (NetworkTime.time < lastAssistBroadcastAt + 1) return;
        lastAssistBroadcastAt = NetworkTime.time;

        int count = Physics.OverlapSphereNonAlloc(transform.position, assistRadius, perceptionHits);
        int joinedThisTick = 0;

        for (int i = 0; i < count && helpersJoined < maxHelpersTotal && joinedThisTick < maxHelpersPerSecond; ++i)
        {
            var m = perceptionHits[i] ? perceptionHits[i].GetComponentInParent<Monster>() : null;
            if (m != null && m != this && m.health.current > 0 &&
            m.behavior == BehaviorType.Assist && m.target == null && !m.isFleeing)
            {
                m.assistAlerted = true;
                m.assistAlertTarget = aggressor;
                m.assistAlertUntil = NetworkTime.time + 2.0; // 2s window
                ++helpersJoined;
                ++joinedThisTick;
            }
        }
    }

    [Server]
    void TryFleePerception()
    {
        if (behavior != BehaviorType.Flee) return;
        if (isFleeing) return;
        if (NetworkTime.time < nextPerceptionAt) return;

        nextPerceptionAt = NetworkTime.time + (state == "IDLE" ? perceptionIntervalIdle : perceptionIntervalActive);

        int count = Physics.OverlapSphereNonAlloc(transform.position, fleeDetectRadius, perceptionHits);
        Entity nearest = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < count; ++i)
        {
            var e = perceptionHits[i] ? perceptionHits[i].GetComponentInParent<Entity>() : null;
            if (e != null && e != this && e.health.current > 0 && e is Player) // Players (and pets, if applicable)
            {
                float d = (e.transform.position - transform.position).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; nearest = e; }
            }
        }

        if (nearest != null) BeginFlee(nearest);
    }

    [Server]
    void BeginFlee(Entity threat)
    {
        // init flee timers & counters
        fleeStartTime = NetworkTime.time;
        acceptWindowStart = 0;
        fleeRepathCounter = 0;
        isFleeing = true;
        helpersJoined = 0; // reset pack state

        // Allow immediate perception on first flee tick
        nextPerceptionAt = NetworkTime.time;

        // Prime flee perception cache with the initial threat if we have one
        if (threat != null)
        {
            fleeCachedAnyThreat = true;
            fleeCachedNearest = threat;
            fleeCachedNearestSqr = (threat.transform.position - transform.position).sqrMagnitude;
        }
        else
        {
            fleeCachedAnyThreat = false;
            fleeCachedNearest = null;
            fleeCachedNearestSqr = float.MaxValue;
        }

        skills.CancelCast();
        target = null;

        Vector3 away = threat != null
            ? (transform.position - threat.transform.position)
            : (transform.position - startPosition);

        away.y = 0;
        if (away.sqrMagnitude < 0.0001f)
            away = (transform.position - startPosition).normalized;

        Vector3 dest = ComputeFleeDestination(away);
        ImmediateNavigate(dest, 0);
    }

    [Server]
    Vector3 ComputeFleeDestination(Vector3 away)
    {
        away.y = 0;
        if (away.sqrMagnitude < 0.0001f) away = (transform.position - startPosition).normalized;
        away = away.normalized * Mathf.Max(2f, fleeSafeDistance);
        Vector3 desired = transform.position + away;

        // Keep within leash (reuse followDistance as leash radius)
        Vector3 toDesired = desired - startPosition;
        float leash = Mathf.Max(2f, followDistance);
        if (toDesired.magnitude > leash) desired = startPosition + toDesired.normalized * (leash - 1f);

        return desired;
    }

    [Server]
    void UpdateFleeWhileMoving()
    {
        double now = NetworkTime.time;

        // --- Perception: nearest threat within detect radius, throttled by nextPerceptionAt ---

        bool anyThreat = fleeCachedAnyThreat;
        Entity nearest = fleeCachedNearest;
        float bestSqr = fleeCachedNearestSqr;

        if (now >= nextPerceptionAt)
        {
            // fleeing is always "active" behavior -> use active perception interval
            nextPerceptionAt = now + perceptionIntervalActive;

            int count = Physics.OverlapSphereNonAlloc(transform.position, fleeDetectRadius, perceptionHits);
            anyThreat = false;
            bestSqr = float.MaxValue;
            nearest = null;

            for (int i = 0; i < count; ++i)
            {
                var e = perceptionHits[i] ? perceptionHits[i].GetComponentInParent<Entity>() : null;
                if (e != null && e != this && e.health.current > 0 && e is Player)
                {
                    anyThreat = true;
                    float ds = (e.transform.position - transform.position).sqrMagnitude;
                    if (ds < bestSqr)
                    {
                        bestSqr = ds;
                        nearest = e;
                    }
                }
            }

            // update cache so we can reuse between scans
            fleeCachedAnyThreat = anyThreat;
            fleeCachedNearest = nearest;
            fleeCachedNearestSqr = bestSqr;
        }

        // --- Accept-combat sticky check (only if a threat exists) ---
        if (anyThreat && nearest != null)
        {
            float nearestDist = Mathf.Sqrt(bestSqr);
            if (nearestDist <= acceptCombatRadius)
            {
                if (acceptWindowStart == 0)
                    acceptWindowStart = now;

                if (now - acceptWindowStart >= acceptCombatStickSeconds)
                {
                    // Accept combat: stop fleeing and set target
                    isFleeing = false;
                    target = nearest;
                    movement.Reset();
                    return;
                }
            }
            else
            {
                // moved back out of accept radius -> reset window
                acceptWindowStart = 0;
            }
        }
        else
        {
            // no threat -> reset window
            acceptWindowStart = 0;
        }

        // --- Calm stop conditions ---
        float homeDist = Vector3.Distance(transform.position, startPosition);
        if (!anyThreat && homeDist <= Mathf.Min(3f, 0.25f * fleeSafeDistance))
        {
            isFleeing = false;
            movement.Reset();
            return;
        }

        if (anyThreat)
        {
            float nearestDist = Mathf.Sqrt(bestSqr);
            // Far enough from threats? stop fleeing
            if (nearestDist >= 1.2f * fleeSafeDistance)
            {
                isFleeing = false;
                movement.Reset();
                return;
            }
        }

        // --- Max flee duration safeguard ---
        if (now - fleeStartTime >= maxFleeSeconds)
        {
            if (nearest != null)
            {
                // Give up and fight the nearest threat
                isFleeing = false;
                target = nearest;
            }
            movement.Reset();
            return;
        }

        // --- Repath away from threats with home bias and deterministic jitter ---
        if (now >= nextRepathAt)
        {
            // Base direction: away from nearest if any, else toward home
            Vector3 baseDir = nearest != null
                ? (transform.position - nearest.transform.position).normalized
                : (transform.position - startPosition).normalized;
            Vector3 homeDir = (startPosition - transform.position).normalized;
            Vector3 dir = (baseDir * (1f - fleeHomeBias) + homeDir * fleeHomeBias);
            dir.y = 0;
            if (dir.sqrMagnitude < 0.0001f)
                dir = homeDir;
            dir = dir.normalized;

            // sample candidates within a cone
            int K = Mathf.Max(1, fleeDirectionSamples);
            float bestScore = -9999f;
            Vector3 bestAway = dir;
            float nearestDist = nearest != null ? Mathf.Sqrt(bestSqr) : 9999f;

            for (int k = 0; k < K; ++k)
            {
                float angle = JitterAngleDeg(fleeRepathCounter, k, fleeJitterMaxAngle, nearestDist, fleeSafeDistance);
                Quaternion rot = Quaternion.Euler(0, angle, 0);
                Vector3 cand = (rot * dir).normalized;

                // simple score: prefer away from threat and toward home
                float s = 0.6f * Vector3.Dot(cand, baseDir) + 0.4f * Vector3.Dot(cand, homeDir);
                if (s > bestScore)
                {
                    bestScore = s;
                    bestAway = cand;
                }
            }

            Vector3 dest = ComputeFleeDestination(bestAway);
            if (TryNavigate(dest, 0))
                fleeRepathCounter++;
        }
    }

[Server]
void EvadeHome()
{
    // Clear combat and walk home (safe-zone/leash handling)
    target = null;
    skills.CancelCast();
    ImmediateNavigate(startPosition, 0);
    isFleeing = false;
}

// ===== Navigation helpers (cooldown + delta) =====
[Server]
bool TryNavigate(Vector3 dest, float stoppingDistance)
{
    // respect cooldown and minimum delta from lastNavDest
    if (NetworkTime.time < nextRepathAt)
    return false;

    if ((lastNavDest - dest).sqrMagnitude < (minDestinationDelta * minDestinationDelta))
    return false;

    movement.Navigate(dest, stoppingDistance);
    lastNavDest = dest;
    nextRepathAt = NetworkTime.time + repathCooldownMs / 1000.0;
    return true;
}

[Server]
void ImmediateNavigate(Vector3 dest, float stoppingDistance)
{
    movement.Navigate(dest, stoppingDistance);
    lastNavDest = dest;
    nextRepathAt = NetworkTime.time + repathCooldownMs / 1000.0;
}

// ===== Event Overrides =====

[ServerCallback]
public override void OnAggro(Entity entity)
{
    base.OnAggro(entity);
    if (!CanAttack(entity)) return;

    // Flee behavior: on aggro, run instead of fighting
    if (behavior == BehaviorType.Flee)
    {
        BeginFlee(entity);
        return;
    }

    // Passive/Assist: engage only on hit or alert
    if (target == null)
    {
        target = entity;
    }
    else if (entity != target)
    {
        // simple proximity retarget preference
        float oldDistance = Vector3.Distance(transform.position, target.transform.position);
        float newDistance = Vector3.Distance(transform.position, entity.transform.position);
        if (newDistance < oldDistance * 0.8f) target = entity;
    }

    // Broadcast assist from non-Assist behaviors (capped)
    MaybeBroadcastAssist(entity);
}

[Server]
public override void OnDeath()
{
    if (deathTimeEnd != 0 || respawnTimeEnd != 0)
    return;

    base.OnDeath();                 // keep existing base behavior
    deathTimeEnd = NetworkTime.time + deathTime;
    if (respawn) respawnTimeEnd = deathTimeEnd + respawnTime;
}

public override bool CanAttack(Entity entity)
{
    return base.CanAttack(entity) &&
    (entity is Player ||
    entity is Pet ||
    entity is Mount);
}

protected override void OnInteract()
{
    Player player = Player.localPlayer;
    float dist = Utils.ClosestDistance(player, this);

    if (health.current == 0) // && inventory.HasLoot())
    {
        if (dist <= player.interactionRange)
        {
            player.target = this;
            UILoot.singleton.Show();
        }
        else
        {
            player.target = this;
            Vector3 destination = Utils.ClosestPoint(this, player.transform.position);
            player.movement.Navigate(destination, player.interactionRange);
        }
        return;
    }

    if (health.current > 0 && player.CanAttack(this) && player.skills.skills.Count > 0)
    {
        var ps = (PlayerSkills)player.skills;
        int idx = 0; // safe fallback


        if (player.skillbar != null && player.skillbar.slots != null && player.skillbar.slots.Length > 0)
        {
            string basicName = player.skillbar.slots[0].reference;
            if (!string.IsNullOrWhiteSpace(basicName))
            {
                for (int i = 0; i < ps.skills.Count; ++i)
                {
                    if (ps.skills[i].name == basicName) { idx = i; break; }
                }
            }
        }

        // ensure target is this monster
        player.target = this;
        ps.TryUse(idx);
        return;
    }

    {
        Vector3 destination = Utils.ClosestPoint(this, player.transform.position);
        player.movement.Navigate(destination, player.interactionRange);
    }
}


// Deterministic jitter: returns angle in degrees within [-maxAngle, +maxAngle],
// tighter cone when threat is very close (reduces side-stepping under pressure).
float JitterAngleDeg(int repathCounter, int k, float maxAngle, float nearestDist, float safeDist)
{
    float tight = Mathf.Clamp01( (safeDist * 0.5f) / Mathf.Max(0.001f, nearestDist) );
    float cone = Mathf.Lerp(maxAngle, maxAngle * 0.4f, tight); // 100% far -> 40% when very close
    // make a stable pseudo-random in [-1,1] using instance id + counters
    int seed = GetInstanceID() ^ (repathCounter * 73856093) ^ (k * 19349663);
    float r = Frac(Mathf.Abs(Mathf.Sin(seed * 12.9898f) * 43758.5453f)); // [0,1)
    return (r * 2f - 1f) * cone;
}

float Frac(float x) => x - Mathf.Floor(x);
}
