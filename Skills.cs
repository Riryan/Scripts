
using System;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

[Serializable] public class UnityEventSkill : UnityEvent<Skill> {}

[DisallowMultipleComponent]
public abstract class Skills : NetworkBehaviour, IHealthBonus, IManaBonus, ICombatBonus
{
    [Header("Components")]
    public Entity entity;
    public Health health;
    public Mana mana;
    [Header("Skills & Buffs")]
    public ScriptableSkill[] skillTemplates;
    public readonly SyncList<Skill> skills = new SyncList<Skill>();
    public readonly SyncList<Buff> buffs = new SyncList<Buff>(); 
#pragma warning disable CS0649 
    [SerializeField] Transform _effectMount;
#pragma warning restore CS0649 
    public virtual Transform effectMount
    {
        get { return _effectMount; }
        set { _effectMount = value; }
    }

    [Header("Events")]
    public UnityEventSkill onSkillCastStarted;
    public UnityEventSkill onSkillCastFinished;
    [SyncVar, HideInInspector] public int currentSkill = -1;
    public int GetHealthBonus(int baseHealth)
    {
        int passiveBonus = 0;
        foreach (Skill skill in skills)
            if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                passiveBonus += passiveSkill.healthMaxBonus.Get(skill.level);

        int buffBonus = 0;
        foreach (Buff buff in buffs)
            buffBonus += buff.healthMaxBonus;

        return passiveBonus + buffBonus;
    }
    public int GetHealthRecoveryBonus()
    {
        float passivePercent = 0;
        foreach (Skill skill in skills)
            if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                passivePercent += passiveSkill.healthPercentPerSecondBonus.Get(skill.level);
        float buffPercent = 0;
        foreach (Buff buff in buffs)
            buffPercent += buff.healthPercentPerSecondBonus;
        return Convert.ToInt32(passivePercent * health.max) + Convert.ToInt32(buffPercent * health.max);
    }

    public int GetManaBonus(int baseMana)
    {
        int passiveBonus = 0;
        foreach (Skill skill in skills)
            if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                passiveBonus += passiveSkill.manaMaxBonus.Get(skill.level);
        int buffBonus = 0;
        foreach (Buff buff in buffs)
            buffBonus += buff.manaMaxBonus;
        return passiveBonus + buffBonus;
    }

    public int GetManaRecoveryBonus()
    {
        float passivePercent = 0;
        foreach (Skill skill in skills)
            if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                passivePercent += passiveSkill.manaPercentPerSecondBonus.Get(skill.level);
        float buffPercent = 0;
        foreach (Buff buff in buffs)
            buffPercent += buff.manaPercentPerSecondBonus;
        return Convert.ToInt32(passivePercent * mana.max) + Convert.ToInt32(buffPercent * mana.max);
    }

    public int GetDamageBonus()
    {
        int passiveBonus = 0;
        foreach (Skill skill in skills)
            if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                passiveBonus += passiveSkill.damageBonus.Get(skill.level);
        int buffBonus = 0;
        foreach (Buff buff in buffs)
            buffBonus += buff.damageBonus;
        return passiveBonus + buffBonus;
    }

    public int GetDefenseBonus()
    {
        int passiveBonus = 0;
        foreach (Skill skill in skills)
            if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                passiveBonus += passiveSkill.defenseBonus.Get(skill.level);
        int buffBonus = 0;
        foreach (Buff buff in buffs)
            buffBonus += buff.defenseBonus;

        return passiveBonus + buffBonus;
    }

    public float GetCriticalChanceBonus()
    {
        float passiveBonus = 0;
        foreach (Skill skill in skills)
            if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                passiveBonus += passiveSkill.criticalChanceBonus.Get(skill.level);
        float buffBonus = 0;
        foreach (Buff buff in buffs)
            buffBonus += buff.criticalChanceBonus;
        return passiveBonus + buffBonus;
    }

    public float GetBlockChanceBonus()
    {
        float passiveBonus = 0;
        foreach (Skill skill in skills)
            if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                passiveBonus += passiveSkill.blockChanceBonus.Get(skill.level);
        float buffBonus = 0;
        foreach (Buff buff in buffs)
            buffBonus += buff.blockChanceBonus;
        return passiveBonus + buffBonus;
    }
    
    void Update()
    {
        if (isServer && entity.IsWorthUpdating())
            CleanupBuffs();
    }
    
    public int GetSkillIndexByName(string skillName)
    {
        for (int i = 0; i < skills.Count; ++i)
            if (skills[i].name == skillName)
                return i;
        return -1;
    }

    public int GetBuffIndexByName(string buffName)
    {
        for (int i = 0; i < buffs.Count; ++i)
            if (buffs[i].name == buffName)
                return i;
        return -1;
    }

    public bool CastCheckSelf(Skill skill, bool checkSkillReady = true) =>
        skill.CheckSelf(entity, checkSkillReady);
    
    public bool CastCheckTarget(Skill skill) =>
        skill.CheckTarget(entity);

    public bool CastCheckDistance(Skill skill, out Vector3 destination) =>
        skill.CheckDistance(entity, out destination);
    
    [Server]
    public void StartCast(Skill skill)
    {
        skill.castTimeEnd = NetworkTime.time + skill.castTime;
        skills[currentSkill] = skill;
        RpcCastStarted(skill);
    }
    
    [Server]
    public void CancelCast(bool resetCurrentSkill = true)
    {
        if (currentSkill != -1)
        {
            Skill skill = skills[currentSkill];
            skill.castTimeEnd = NetworkTime.time - skill.castTime;
            skills[currentSkill] = skill;
            if (resetCurrentSkill)
                currentSkill = -1;
        }
    }
    
    [Server]
    public void FinishCast(Skill skill)
    {
        if (CastCheckSelf(skill, false) && CastCheckTarget(skill))
        {
            skill.Apply(entity);
            RpcCastFinished(skill);
            mana.current -= skill.manaCosts;
            skill.cooldownEnd = NetworkTime.time + skill.cooldown;
            skills[currentSkill] = skill;
        }
        else
        {
            currentSkill = -1;
        }
    }
    
    [ClientRpc]
    public void RpcCastStarted(Skill skill)
    {
        if (health.current > 0)
        {
            skill.data.OnCastStarted(entity);
            onSkillCastStarted.Invoke(skill);
        }
    }
    
    [ClientRpc]
    public void RpcCastFinished(Skill skill)
    {
        if (health.current > 0)
        {
            skill.data.OnCastFinished(entity);
            onSkillCastFinished.Invoke(skill);
        }
    }
    
    public void AddOrRefreshBuff(Buff buff)
    {
        int index = GetBuffIndexByName(buff.name);
        if (index != -1) buffs[index] = buff;
        else buffs.Add(buff);
    }

    
    public void CleanupBuffs()
    {
        for (int i = 0; i < buffs.Count; ++i)
        {
            if (buffs[i].BuffTimeRemaining() == 0)
            {
                buffs.RemoveAt(i);
                --i;
            }
        }
    }

    [Server]
    public void OnDeath()
    {
        for (int i = 0; i < buffs.Count; ++i)
        {
            if (!buffs[i].remainAfterDeath)
            {
                buffs.RemoveAt(i);
                --i;
            }
        }
        CancelCast();
    }
}
