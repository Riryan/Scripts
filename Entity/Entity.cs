// The Entity class is rather simple. It contains a few basic entity properties
// like health, mana and level that all inheriting classes like Players and
// Monsters can use.
//
// Entities also have a _target_ Entity that can't be synchronized with a
// SyncVar. Instead we created a EntityTargetSync component that takes care of
// that for us.
//
// Entities use a deterministic finite state machine to handle IDLE/MOVING/DEAD/
// CASTING etc. states and events. Using a deterministic FSM means that we react
// to every single event that can happen in every state (as opposed to just
// taking care of the ones that we care about right now). This means a bit more
// code, but it also means that we avoid all kinds of weird situations like 'the
// monster doesn't react to a dead target when casting' etc.
// The next state is always set with the return value of the UpdateServer
// function. It can never be set outside of it, to make sure that all events are
// truly handled in the state machine and not outside of it. Otherwise we may be
// tempted to set a state in CmdBeingTrading etc., but would likely forget of
// special things to do depending on the current state.
//
// Entities also need a kinematic Rigidbody so that OnTrigger functions can be
// called. Note that there is currently a Unity bug that slows down the agent
// when having lots of FPS(300+) if the Rigidbody's Interpolate option is
// enabled. So for now it's important to disable Interpolation - which is a good
// idea in general to increase performance.
//
// Note: in a component based architecture we don't necessarily need Entity.cs,
//       but it does help us to avoid lots of GetComponent calls. Passing an
//       Entity to combat and accessing entity.health is faster than passing a
//       GameObject and calling gameObject.GetComponent<Health>() each time!
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Mirror;
using TMPro;

namespace uMMORPG
{
    [Serializable] public class UnityEventEntity : UnityEvent<Entity> {}
    [Serializable] public class UnityEventEntityInt : UnityEvent<Entity, int> {}
    [RequireComponent(typeof(Level))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Mana))]
    [RequireComponent(typeof(Combat))]
    [RequireComponent(typeof(Skills))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody))] // kinematic, only needed for OnTrigger
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
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
    #pragma warning disable CS0109 // member does not hide accessible member
        public new Collider collider;
    #pragma warning restore CS0109 // member does not hide accessible member
        public AudioSource audioSource;

        [Header("State")]
        [SyncVar, SerializeField] string _state = "IDLE";
        public string state => _state;

        [SyncVar] public double lastCombatTime;

        [Header("Target")]
        [SyncVar, HideInInspector] public Entity target;

        [Header("Speed")]
        [SerializeField] protected LinearFloat _speed = new LinearFloat{baseValue=5};
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
        public long gold { get { return _gold; } set { _gold = Math.Max(value, 0); } }

        [Header("Text Meshes")]
        public TextMeshPro stunnedOverlay;

        [Header("Events")]
        public UnityEventEntity onAggro;
        public UnityEvent onSelect; // called when clicking it the first time
        public UnityEvent onInteract; // called when clicking it the second time

        [HideInInspector] public double stunTimeEnd;

        [HideInInspector] public bool inSafeZone;

        public override void OnStartServer()
        {
            if (health.current == 0)
                _state = "DEAD";
        }

        protected virtual void Start()
        {
            if (!isClient) animator.enabled = false;
        }

        [Server]
        public virtual bool IsWorthUpdating() =>
            netIdentity.observers.Count > 0 ||
            IsHidden();

        void Update()
        {
            if (movement != null)
                movement.SetSpeed(speed);

            if (isClient)
            {
                UpdateClient();
            }

            if (isServer && IsWorthUpdating())
            {
                if (target != null && target.IsHidden()) target = null;
                _state = UpdateServer();
            }

            if (!isServerOnly) UpdateOverlays();
        }

        protected abstract string UpdateServer();

        protected abstract void UpdateClient();

        protected virtual void UpdateOverlays()
        {
           // if (stunnedOverlay != null)
           //     stunnedOverlay.gameObject.SetActive(state == "STUNNED");
        }

        [Server]
        public void Hide() => netIdentity.visibility = Visibility.ForceHidden;

        [Server]
        public void Show() => netIdentity.visibility = Visibility.Default;

        public bool IsHidden() => netIdentity.visibility == Visibility.ForceHidden;

        public float VisRange() => ((SpatialHashingInterestManagement)NetworkServer.aoi).visRange;

        [Server]
        public void Revive(float healthPercentage = 1)
        {
            health.current = Mathf.RoundToInt(health.max * healthPercentage);
        }

        public virtual void OnAggro(Entity entity)
        {
            onAggro.Invoke(entity);
        }

        public virtual bool CanAttack(Entity entity)
        {
            return health.current > 0 &&
                   entity.health.current > 0 &&
                   entity != this &&
                   !inSafeZone && !entity.inSafeZone &&
                   !NavMesh.Raycast(transform.position, entity.transform.position, out NavMeshHit hit, NavMesh.AllAreas);
        }

        [Server]
        public virtual void OnDeath()
        {
            target = null;
        }

void OnMouseDown()
{
    Debug.Log("[TRACE] OnMouseDown reached");
    if (Player.localPlayer != null &&
        !Utils.IsCursorOverUserInterface() &&
        (Player.localPlayer.state == "IDLE" ||
         Player.localPlayer.state == "MOVING" ||
         Player.localPlayer.state == "CASTING" ||
         Player.localPlayer.state == "STUNNED"))
    {
        // CRITICAL: always clear queued skill (matches clean 2.44)
        Player.localPlayer.useSkillWhenCloser = -1;

        // always update indicator
        Player.localPlayer.indicator.SetViaParent(transform);

        // first click: select
        if (Player.localPlayer.target != this)
        {
            Player.localPlayer.CmdSetTarget(netIdentity);
            OnSelect();
            onSelect.Invoke();
        }
        // second click: interact (attack)
        else
        {
            OnInteract();
            onInteract.Invoke();
        }
    }
}




        protected virtual void OnSelect() {}
        protected abstract void OnInteract();

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
}