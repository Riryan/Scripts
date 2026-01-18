using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using TMPro;

namespace uMMORPG
{
    [Serializable] public class UnityEventPlayer : UnityEvent<Player> {}

    [RequireComponent(typeof(Experience))]
    [RequireComponent(typeof(Intelligence))]
    [RequireComponent(typeof(Strength))]
    [RequireComponent(typeof(PlayerChat))]
    [RequireComponent(typeof(PlayerCrafting))]
    [RequireComponent(typeof(PlayerGameMasterTool))]
    [RequireComponent(typeof(PlayerGuild))]
    [RequireComponent(typeof(PlayerIndicator))]
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
        [Header("Components")]
        public Experience experience;
        public Intelligence intelligence;
        public Strength strength;
        public PlayerChat chat;
        public PlayerCrafting crafting;
        public PlayerGameMasterTool gameMasterTool;
        public PlayerGuild guild;
        public PlayerIndicator indicator;
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
        [SyncVar] public PlayerCustomizationData customization;

        [Header("Text Meshes")]
        public TextMeshPro nameOverlay;
        public Color nameOverlayDefaultColor = Color.white;
        public Color nameOverlayOffenderColor = Color.magenta;
        public Color nameOverlayMurdererColor = Color.red;
        public Color nameOverlayPartyColor = new Color(0.341f, 0.965f, 0.702f);
        public string nameOverlayGameMasterPrefix = "[GM] ";
        [HideInInspector] public bool isPreview;
        [Header("Icons")]
        public Sprite classIcon; // for character selection
        public Sprite portraitIcon; // for top left portrait
        [Header("Combat")]
        public WeaponItem unarmedWeapon;

        [Header("Combat Movement")]
        public float autoCloseDistance = 0.25f;
        // some meta info
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
        public bool localPlayerClickThrough = true; // click selection goes through localplayer. feels best.
        public KeyCode cancelActionKey = KeyCode.Escape;

        [Tooltip("Being stunned interrupts the cast. Enable this option to continue the cast afterwards.")]
        public bool continueCastAfterStunned = true;

        [Header("PvP")]
        public BuffSkill offenderBuff;
        public BuffSkill murdererBuff;

        [Header("Movement")]
        [Range(0.1f, 1)] public float attackToMoveRangeRatio = 0.8f;

        [SyncVar, HideInInspector] public double nextRiskyActionTime = 0; // double for long term precision

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
            if (isClient) // no need for animations on the server
            {
                foreach (Animator anim in GetComponentsInChildren<Animator>())
                {
                    anim.SetBool("MOVING", movement.IsMoving() && !mountControl.IsMounted());
                    anim.SetBool("CASTING", state == "CASTING");
                    anim.SetBool("STUNNED", state == "STUNNED");
                    anim.SetBool("MOUNTED", mountControl.IsMounted()); // for seated animation
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

            // do nothing if not spawned (=for character selection previews)
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
            state != "MOVING" && movement.IsMoving(); // only fire when started moving

        bool EventMoveEnd() =>
            state == "MOVING" && !movement.IsMoving(); // only fire when stopped moving

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


        [Client]
        protected override void UpdateClient()
        {
            if (state == "IDLE" || state == "MOVING")
            {
                if (isLocalPlayer)
                {
                    if (Input.GetKeyDown(cancelActionKey))
                    {
                        movement.Reset();
                        CmdCancelAction();
                    }

if (useSkillWhenCloser != -1)
{
    if (target != null)
    {
        // OLD: hard-coded skill index already stored
        // float range = skills.skills[useSkillWhenCloser].castRange * attackToMoveRangeRatio;

        // NEW: resolve current attack dynamically
        int attackIndex = GetCurrentAttack();
        float range = skills.skills[attackIndex].castRange * attackToMoveRangeRatio;

        if (Utils.ClosestDistance(this, target) <= range)
        {
            // OLD: use queued skill index
            // ((PlayerSkills)skills).CmdUse(useSkillWhenCloser);

            // NEW: use resolved attack
            ((PlayerSkills)skills).CmdUse(attackIndex);

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
                    if (Input.GetKeyDown(cancelActionKey)) CmdCancelAction();
                }
            }
            else if (state == "STUNNED")
            {
                if (isLocalPlayer)
                {
                    movement.Reset();
                    if (Input.GetKeyDown(cancelActionKey)) CmdCancelAction();
                }
            }
            else if (state == "TRADING") {}
            else if (state == "CRAFTING") {}
            else if (state == "DEAD") {}
            else Debug.LogError("invalid state:" + state);
            UpdateFootsteps();
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
            bool castingAndAllowed = state == "CASTING" &&  skills.currentSkill != -1 &&  skills.skills[skills.currentSkill].allowMovement;
            bool isLocalPlayerTyping = isLocalPlayer && UIUtils.AnyInputActive();
            return (state == "IDLE" || state == "MOVING" || castingAndAllowed) && !isLocalPlayerTyping;
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
                   (entity is Monster ||  entity is Player || (entity is Pet && entity != petControl.activePet) || (entity is Mount && entity != mountControl.activeMount));
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
    if (this == localPlayer)
        return;
    if (localPlayer.target != this)
        return;
    if (!localPlayer.CanAttack(this))
        return;
    int skillIndex = localPlayer.GetCurrentAttack();
    if (localPlayer.skills.skills.Count == 0)
        return;
    localPlayer.useSkillWhenCloser = skillIndex;
    ((PlayerSkills)localPlayer.skills).TryUse(skillIndex, true);
}

        public WeaponItem GetLocalEquippedOrUnarmedWeapon()
        {
            int weaponIndex = equipment.GetEquippedWeaponIndex();
            if (weaponIndex != -1 && equipment.slots[weaponIndex].amount > 0)
                return equipment.slots[weaponIndex].item.data as WeaponItem;
                return unarmedWeapon;
        }
    }
}