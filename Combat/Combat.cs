using System;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.Events;

public enum DamageType { Normal, Block, Crit }

public interface ICombatBonus
{
    int GetDamageBonus();
    int GetDefenseBonus();
    float GetCriticalChanceBonus();
    float GetBlockChanceBonus();
}

[Serializable] public partial class UnityEventIntDamageType : UnityEvent<int, DamageType> {}

[DisallowMultipleComponent]
public partial class Combat : NetworkBehaviour
{
    [Header("Components")]
    public Level level;
    public Entity entity;
#pragma warning disable CS0109 
    public new Collider collider;
#pragma warning restore CS0109 
    [Header("Stats")]
    [SyncVar] public bool invincible = false;
    public LinearInt baseDamage = new LinearInt { baseValue = 1 };
    public LinearInt baseDefense = new LinearInt { baseValue = 1 };
    public LinearFloat baseBlockChance;
    public LinearFloat baseCriticalChance;
    [Header("Damage Popup")]
    public GameObject damagePopupPrefab;
    [Header("Events")]
    public UnityEventEntity onDamageDealtTo;
    public UnityEventEntity onKilledEnemy;
    public UnityEventEntityInt onServerReceivedDamage;
    public UnityEventIntDamageType onClientReceivedDamage;
    ICombatBonus[] _bonusComponents;
    ICombatBonus[] bonusComponents =>
        _bonusComponents ?? (_bonusComponents = GetComponents<ICombatBonus>());

    public int damage
    {
        get
        {
            int bonus = 0;
            foreach (ICombatBonus bonusComponent in bonusComponents)
                bonus += bonusComponent.GetDamageBonus();
            if (entity is Player player) return baseDamage.Get(level.current) + bonus + player.combatSkills.GetDamageBonus(baseDamage.Get(level.current) + bonus);
                        else return baseDamage.Get(level.current) + bonus;
        }
    }

    public int defense
    {
        get
        {
            int bonus = 0;
            foreach (ICombatBonus bonusComponent in bonusComponents)
                bonus += bonusComponent.GetDefenseBonus();
            if (entity is Player player) return baseDefense.Get(level.current) + bonus + player.combatSkills.GetDefenseBonus();
                        else return baseDefense.Get(level.current) + bonus;
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
                    victim.stunTimeEnd = Math.Max(newStunEndTime, entity.stunTimeEnd);
                }
            }
            if (victim is Player targetPlayer) targetPlayer.combatSkills.ReceivedDamage(damageType);
            onDamageDealtTo.Invoke(victim);
            if (victim.health.current == 0)
            {
                if (victim is Monster m)
                    m.inventory.OnDeath();

                victim.OnDeath();
                onKilledEnemy.Invoke(victim);
                return;
            }
        }

        victim.OnAggro(entity);
        victimCombat.RpcOnReceivedDamaged(damageDealt, damageType);
        entity.lastCombatTime = NetworkTime.time;
        victim.lastCombatTime = NetworkTime.time;
    }

    [Client]
    void ShowDamagePopup(int amount, DamageType damageType)
    {
        if (damagePopupPrefab != null)
        {
            Bounds bounds = collider.bounds;
            Vector3 position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            GameObject popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);
            if (damageType == DamageType.Normal)
                popup.GetComponentInChildren<TextMeshPro>().text = amount.ToString();
            else if (damageType == DamageType.Block)
                popup.GetComponentInChildren<TextMeshPro>().text = "<i>Block!</i>";
            else if (damageType == DamageType.Crit)
                popup.GetComponentInChildren<TextMeshPro>().text = amount + " Crit!";
        }
    }

    [ClientRpc]
    public void RpcOnReceivedDamaged(int amount, DamageType damageType)
    {
        ShowDamagePopup(amount, damageType);
        onClientReceivedDamage.Invoke(amount, damageType);
    }
    // ===== Partial hooks (DECLARATIONS — no bodies here) =====
    partial void OnAwake_Server();
    partial void OnStartServer_Combat();
    partial void OnStopServer_Combat();
    partial void OnUpdate_Server(float dt);
    partial void OnUpdate_Client(float dt);

    // ===== MonoBehaviour lifecycle that calls the hooks =====
    void Awake()
    {
#if UNITY_SERVER || UNITY_EDITOR
    OnAwake_Server();
#endif
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        OnStartServer_Combat();
    }

    public override void OnStopServer()
    {
        OnStopServer_Combat();
        base.OnStopServer();
    }

    void Update()
    {
        float dt = Time.deltaTime;

#if UNITY_SERVER || UNITY_EDITOR
    OnUpdate_Server(dt);
#endif

#if !UNITY_SERVER || UNITY_EDITOR
        OnUpdate_Client(dt);
#endif
    }


}
