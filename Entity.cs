using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Mirror;
using TMPro;

[Serializable] public class UnityEventEntity : UnityEvent<Entity> {}
[Serializable] public class UnityEventEntityInt : UnityEvent<Entity, int> {}

[RequireComponent(typeof(Level))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Mana))]
[RequireComponent(typeof(Combat))]
[RequireComponent(typeof(Skills))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public abstract partial class Entity : NetworkBehaviour
{
    [Header("Components")]
    public Level level;
    public Health health;
    public Mana mana;
    public Combat combat;
    public Equipment equipment;
    public Movement movement;
    public Skills skills;
    public Animator animator;
#pragma warning disable CS0109
    public new Collider collider;
#pragma warning restore CS0109
    public AudioSource audioSource;

    [Header("State")]
    [SyncVar, SerializeField] protected string _state = "IDLE";
    public string state => _state;

    // Server-only: used for AI/awake checks. No need to sync to clients.
    [NonSerialized] public double lastCombatTime;

    [Header("Target")]
    [SyncVar, HideInInspector] public Entity target;

    [Header("Speed")]
    [SerializeField] protected LinearFloat _speed = new LinearFloat { baseValue = 5 };
    public virtual float speed
    {
        get
        {
            float passiveBonus = 0;
            foreach (Skill skill in skills.skills)
                if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                    passiveBonus += passiveSkill.speedBonus.Get(skill.level);

            float buffBonus = 0;
            foreach (Buff buff in skills.buffs)
                buffBonus += buff.speedBonus;

            return _speed.Get(level.current) + passiveBonus + buffBonus;
        }
    }

[Header("Gold")]
[SyncVar, SerializeField] long _gold = 0;
public long gold
{
    get => _gold;
    set
    {
        long clamped = Math.Max(value, 0);
        if (clamped == _gold)
            return;
        _gold = clamped;
    }
}


    [Header("Text Meshes")]
    public TextMeshPro stunnedOverlay;

    [Header("Events")]
    public UnityEventEntity onAggro;
    public UnityEvent onSelect;
    public UnityEvent onInteract;

    [HideInInspector] public double stunTimeEnd;
    [HideInInspector] public bool inSafeZone;

    [Header("Server Simulation")]
    [Tooltip("How long after combat to keep an entity 'awake' for server updates.")]
    public float combatAwakeGraceSeconds = 5f;

    // --------- Mirror lifecycle ---------

    public override void OnStartServer()
    {
        if (health.current == 0)
            _state = "DEAD";

        #if UNITY_SERVER || UNITY_EDITOR
        AOI_OnServerSpawned();
        #endif
    }

    public override void OnStopServer()
    {
        #if UNITY_SERVER || UNITY_EDITOR
        AOI_OnServerDespawned();
        #endif
    }

    protected virtual void Start()
    {
        if (!isClient && animator != null)
            animator.enabled = false;
    }
#if UNITY_SERVER
    // ------------------- SERVER STRIPPING -------------------
    // Automatically remove visual-only components from Entities
    // so we can use the same prefabs for client and headless server.
    protected virtual void Awake()
    {
        StripServerVisuals();
    }

    protected void StripServerVisuals()
    {
        var go = gameObject;
        StripType<Animator>(go, true);
        StripType<SkinnedMeshRenderer>(go, true);
        StripType<MeshRenderer>(go, true);
        StripType<ParticleSystem>(go, true);
        StripType<AudioSource>(go, true);
        StripType<Light>(go, true);
        StripType<Camera>(go, true);

        // Optional: Strip post-processing or text if used.
        // StripType<UnityEngine.Rendering.Volume>(go, true);
        // StripType<TMPro.TextMeshPro>(go, true);
    }

    private static void StripType<T>(GameObject root, bool includeChildren) where T : Component
    {
        if (includeChildren)
        {
            var comps = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < comps.Length; ++i)
                if (comps[i] != null)
                    Destroy(comps[i]);
        }
        else
        {
            var comps = root.GetComponents<T>();
            for (int i = 0; i < comps.Length; ++i)
                if (comps[i] != null)
                    Destroy(comps[i]);
        }
    }
#endif

void Update()
{
    if (isClient)
        UpdateClient();

    if (isServer && IsWorthUpdating())
    {
        if (movement != null)
            movement.SetSpeed(speed);

        if (target != null && target.IsHidden())
            target = null;

        // change-only sync for _state
        string newState = UpdateServer();
        if (newState != _state)
            _state = newState;

        #if UNITY_SERVER || UNITY_EDITOR
        AOI_TryNotifyMove();
        #endif
    }

    if (!isServerOnly)
        UpdateOverlays();
}

    // --------- Abstract update hooks ---------

    protected abstract string UpdateServer();
    protected abstract void UpdateClient();

    protected virtual void UpdateOverlays()
    {
        if (stunnedOverlay == null)
            return;
        bool shouldBeActive = (state == "STUNNED");
        if (stunnedOverlay.gameObject.activeSelf != shouldBeActive)
            stunnedOverlay.gameObject.SetActive(shouldBeActive);

    }

    // --------- Visibility helpers ---------

    [Server]
    public void Hide() => netIdentity.visibility = Visibility.ForceHidden;

    [Server]
    public void Show() => netIdentity.visibility = Visibility.Default;

    public bool IsHidden() => netIdentity.visibility == Visibility.ForceHidden;

    // Works with either AOI implementation
    public float VisRange()
    {
        var aoi = NetworkServer.aoi;
        if (aoi is SpatialHashingInterestManagement sh)
            return sh.visRange;

        if (aoi is CustomAOIInterestManagement caoi && caoi.settings != null)
            return Mathf.Max(caoi.settings.cellSizeMeters * 1.5f, caoi.settings.cellSizeMeters);

        return 64f;
    }

    // --------- Lifecycle / combat ---------

    [Server]
    public void Revive(float healthPercentage = 1f)
    {
        health.current = Mathf.RoundToInt(health.max * Mathf.Clamp01(healthPercentage));
    }

    [Server]
    public virtual void OnDeath()
    {
        // Base no-op; derived classes (Player/Monster/etc.) override as needed.
    }

    public virtual void OnAggro(Entity entity)
    {
        onAggro.Invoke(entity);
    }

    public virtual bool CanAttack(Entity entity)
    {
        return
            entity != null &&
            entity != this &&
            entity.health.current > 0;
    }

    // Now virtual so Mount/Pet can override
    public virtual bool IsWorthUpdating()
    {
        bool observed = netIdentity != null && netIdentity.observers != null && netIdentity.observers.Count > 0;
        bool hasTarget = target != null;
        bool recentlyActive = NetworkTime.time <= lastCombatTime + combatAwakeGraceSeconds;
        return observed || hasTarget || recentlyActive;
    }

    // --------- Interaction (client) ---------

    protected virtual void OnMouseDown()
    {
        if (Player.localPlayer != null &&
            !Utils.IsCursorOverUserInterface() &&
            (Player.localPlayer.state == "IDLE" ||
             Player.localPlayer.state == "MOVING" ||
             Player.localPlayer.state == "CASTING" ||
             Player.localPlayer.state == "STUNNED"))
        {
            Player.localPlayer.useSkillWhenCloser = -1;

            if (Player.localPlayer.target != this)
            {
                Player.localPlayer.CmdSetTarget(netIdentity);
                OnSelect();
                onSelect.Invoke();
            }
            else
            {
                OnInteract();
                onInteract.Invoke();
            }
        }
    }
    //Added for non mouse interaction
    //Such as E for interact
    public void Interact()
    {
        OnInteract();
        onInteract.Invoke();
    }

    protected virtual void OnSelect() {}
    protected abstract void OnInteract();

    // --------- Triggers ---------

    protected virtual void OnTriggerEnter(Collider col)
    {
        if (col.isTrigger && col.GetComponent<SafeZone>())
            inSafeZone = true;
    }

    protected virtual void OnTriggerExit(Collider col)
    {
        if (col.isTrigger && col.GetComponent<SafeZone>())
            inSafeZone = false;
    }
}
