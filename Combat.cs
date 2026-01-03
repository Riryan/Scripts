using System;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.Events;

namespace uMMORPG
{
    public enum DamageType { Normal, Block, Crit }

    // inventory, attributes etc. can influence max health
    public interface ICombatBonus
    {
        int GetDamageBonus();
        int GetDefenseBonus();
        float GetCriticalChanceBonus();
        float GetBlockChanceBonus();
    }

    [Serializable] public class UnityEventIntDamageType : UnityEvent<int, DamageType> {}

    [DisallowMultipleComponent]
    public class Combat : NetworkBehaviour
    {
        [Header("Components")]
        public Level level;
        public Entity entity;
#pragma warning disable CS0109
        public new Collider collider;
#pragma warning restore CS0109

        [Header("Stats")]
        [SyncVar] public bool invincible = false; // GMs, Npcs, ...
        public LinearInt baseDamage = new LinearInt { baseValue = 1 };
        public LinearInt baseDefense = new LinearInt { baseValue = 1 };
        public LinearFloat baseBlockChance;
        public LinearFloat baseCriticalChance;

        [Header("Damage Popup")]
        public GameObject damagePopupPrefab;

        // events
        [Header("Events")]
        public UnityEventEntity onDamageDealtTo;
        public UnityEventEntity onKilledEnemy;
        public UnityEventEntityInt onServerReceivedDamage;
        public UnityEventIntDamageType onClientReceivedDamage;

        // cache components that give a bonus (attributes, inventory, etc.)
        ICombatBonus[] _bonusComponents;
        ICombatBonus[] bonusComponents =>
            _bonusComponents ?? (_bonusComponents = GetComponents<ICombatBonus>());

        // ---------------------------------------------------------------------
        // Calculated stats (GC-safe)
        // ---------------------------------------------------------------------

        public int damage
        {
            get
            {
                int bonus = 0;
                foreach (ICombatBonus bonusComponent in bonusComponents)
                    bonus += bonusComponent.GetDamageBonus();
                return baseDamage.Get(level.current) + bonus;
            }
        }

        public int defense
        {
            get
            {
                int bonus = 0;
                foreach (ICombatBonus bonusComponent in bonusComponents)
                    bonus += bonusComponent.GetDefenseBonus();
                return baseDefense.Get(level.current) + bonus;
            }
        }

        public float blockChance
        {
            get
            {
                float bonus = 0;
                foreach (ICombatBonus bonusComponent in bonusComponents)
                    bonus += bonusComponent.GetBlockChanceBonus();
                return baseBlockChance.Get(level.current) + bonus;
            }
        }

        public float criticalChance
        {
            get
            {
                float bonus = 0;
                foreach (ICombatBonus bonusComponent in bonusComponents)
                    bonus += bonusComponent.GetCriticalChanceBonus();
                return baseCriticalChance.Get(level.current) + bonus;
            }
        }

        // ---------------------------------------------------------------------
        // Combat
        // ---------------------------------------------------------------------

        [Server]
        public virtual void DealDamageAt(Entity victim, int amount, float stunChance = 0, float stunTime = 0)
        {
            Combat victimCombat = victim.combat;
            int damageDealt = 0;
            DamageType damageType = DamageType.Normal;

            if (!victimCombat.invincible)
            {
                if (UnityEngine.Random.value < victimCombat.blockChance)
                {
                    damageType = DamageType.Block;
                }
                else
                {
                    damageDealt = Mathf.Max(amount - victimCombat.defense, 1);

                    if (UnityEngine.Random.value < criticalChance)
                    {
                        damageDealt *= 2;
                        damageType = DamageType.Crit;
                    }
                   if (entity is Player player) player.combatSkills.HitEnemy(victim, damageDealt);
                    victim.health.current -= damageDealt;

                    victimCombat.onServerReceivedDamage.Invoke(entity, damageDealt);

                    if (UnityEngine.Random.value < stunChance)
                    {
                        double newStunEndTime = NetworkTime.time + stunTime;
                        victim.stunTimeEnd = Math.Max(newStunEndTime, victim.stunTimeEnd);
                    }
                }
                if (victim is Player targetPlayer) targetPlayer.combatSkills.ReceivedDamage(damageType);
                onDamageDealtTo.Invoke(victim);
                if (victim.health.current == 0)
                    onKilledEnemy.Invoke(victim);
            }

            victim.OnAggro(entity);

            // ---- NETWORK CHANGE (CRITICAL) ----
            // Send damage feedback ONLY to the victim, unreliable.
            if (victim.connectionToClient != null)
            {
                victimCombat.TargetOnReceivedDamaged(
                    victim.connectionToClient,
                    damageDealt,
                    damageType
                );
            }

            entity.lastCombatTime = NetworkTime.time;
            victim.lastCombatTime = NetworkTime.time;
        }

        // ---------------------------------------------------------------------
        // Client-side damage feedback (SELF ONLY)
        // ---------------------------------------------------------------------

        [TargetRpc(channel = Channels.Unreliable)]
        public void TargetOnReceivedDamaged(
            NetworkConnection target,
            int amount,
            DamageType damageType)
        {
            // Visual feedback is local-only
            ShowDamagePopup(amount, damageType);

            // UI / addons hook
            onClientReceivedDamage.Invoke(amount, damageType);
        }

        [Client]
        void ShowDamagePopup(int amount, DamageType damageType)
        {
            if (damagePopupPrefab == null)
                return;

            Bounds bounds = collider.bounds;
            Vector3 position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            GameObject popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);
            TextMeshPro text = popup.GetComponentInChildren<TextMeshPro>();

            if (damageType == DamageType.Normal)
                text.text = amount.ToString();
            else if (damageType == DamageType.Block)
                text.text = "<i>Block!</i>";
            else if (damageType == DamageType.Crit)
                text.text = amount + " Crit!";
        }
    }
}
