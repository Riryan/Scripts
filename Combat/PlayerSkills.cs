using UnityEngine;
using Mirror;

[RequireComponent(typeof(Level))]
[RequireComponent(typeof(Movement))]
[RequireComponent(typeof(PlayerParty))]
[DisallowMultipleComponent]
public partial class PlayerSkills : Skills
{
    [Header("Components")]
    public Level level;
    public Movement movement;
    public PlayerParty party;

    [Header("Skill Experience")]
    [SyncVar] public long skillExperience = 0;

    // Make the entire PlayerSkills component owner-only.
    // Only the owning client receives skillExperience & skill state.
    protected override void OnValidate()
    {
        base.OnValidate();

        // Force owner-only sync to avoid sending skillExperience etc. to all observers
        if (syncMode != SyncMode.Owner)
            syncMode = SyncMode.Owner;
    }

    void Start()
    {
        if (!isServer && !isClient) return;

        if (isServer)
            for (int i = 0; i < buffs.Count; ++i)
                if (buffs[i].BuffTimeRemaining() > 0)
                    buffs[i].data.SpawnEffect(entity, entity);
    }

    [Command]
    public void CmdUse(int skillIndex)
    {
        if ((entity.state == "IDLE" || entity.state == "MOVING" || entity.state == "CASTING") &&
            0 <= skillIndex && skillIndex < skills.Count)
        {
            if (skills[skillIndex].level > 0 && skills[skillIndex].IsReady())
            {
                currentSkill = skillIndex;
            }
        }
    }

    [Client]
    public void TryUse(int skillIndex, bool ignoreState = false)
    {
        if (entity.state != "CASTING" || ignoreState)
        {
            Skill skill = skills[skillIndex];
            bool checkSelf = CastCheckSelf(skill, !ignoreState);
            bool checkTarget = CastCheckTarget(skill);
            if (checkSelf && checkTarget)
            {
                Vector3 destination;
                if (CastCheckDistance(skill, out destination))
                {
                    CmdUse(skillIndex);
                }
                else
                {
                    float stoppingDistance = skill.castRange * ((Player)entity).attackToMoveRangeRatio;
                    movement.Navigate(destination, stoppingDistance);
                    ((Player)entity).useSkillWhenCloser = skillIndex;
                }
            }
        }
        else
        {
            ((Player)entity).pendingSkill = skillIndex;
        }
    }

    public bool HasLearned(string skillName)
    {
        return HasLearnedWithLevel(skillName, 1);
    }

    public bool HasLearnedWithLevel(string skillName, int skillLevel)
    {
        foreach (Skill skill in skills)
            if (skill.level >= skillLevel && skill.name == skillName)
                return true;
        return false;
    }

    // Returns true if 'data' is unlocked for the currently equipped weapon.
    // Also tells you the required level, current level, and lineId for UI.
    public bool IsUnlockedForEquippedWeapon(ScriptableSkill data,
                                            out int requiredLevel,
                                            out int currentLevel,
                                            out string lineId)
    {
        requiredLevel = 0;
        currentLevel  = 0;
        lineId        = null;

        if (data == null) return true; // nothing to gate

        // Find equipped weapon (first WeaponItem)
        var equip = GetComponent<PlayerEquipment>();
        if (equip == null) return true;

        WeaponItem weapon = null;
        for (int i = 0; i < equip.slots.Count; ++i)
        {
            var s = equip.slots[i];
            if (s.amount > 0 && s.item.data is WeaponItem wi) { weapon = wi; break; }
        }
        if (weapon == null) return true; // not a weapon skill context

        lineId = weapon.GetWeaponLineId();

        // Player’s mastery for this line
        var exp = GetComponent<PlayerExperience>();
        currentLevel = exp != null ? exp.GetWeaponMasteryLevel(lineId) : 0;

        // Required unlock level for this skill on this weapon (scan slots 1/2/3)
        requiredLevel = GetRequiredUnlockForSkill(weapon, data);
        return currentLevel >= requiredLevel;
    }

    // Helper: find the required unlock level for a given skill from the weapon's 1/2/3 option arrays
    static int GetRequiredUnlockForSkill(WeaponItem weapon, ScriptableSkill data)
    {
        // Slot 1
        for (int i = 0; i < (weapon.slot1Options?.Length ?? 0); ++i)
            if (weapon.slot1Options[i] == data)
                return (weapon.slot1UnlockLevels != null && i < weapon.slot1UnlockLevels.Length)
                       ? weapon.slot1UnlockLevels[i] : 0;

        // Slot 2
        for (int i = 0; i < (weapon.slot2Options?.Length ?? 0); ++i)
            if (weapon.slot2Options[i] == data)
                return (weapon.slot2UnlockLevels != null && i < weapon.slot2UnlockLevels.Length)
                       ? weapon.slot2UnlockLevels[i] : 0;

        // Slot 3
        for (int i = 0; i < (weapon.slot3Options?.Length ?? 0); ++i)
            if (weapon.slot3Options[i] == data)
                return (weapon.slot3UnlockLevels != null && i < weapon.slot3UnlockLevels.Length)
                       ? weapon.slot3UnlockLevels[i] : 0;

        // Not a weapon-kit skill; don’t gate it.
        return 0;
    }

    public bool CanUpgrade(Skill skill)
    {
        return skill.level < skill.maxLevel &&
               level.current >= skill.upgradeRequiredLevel &&
               skillExperience >= skill.upgradeRequiredSkillExperience &&
               (skill.predecessor == null || HasLearnedWithLevel(skill.predecessor.name, skill.predecessorLevel));
    }

    [Command]
    public void CmdUpgrade(int skillIndex)
    {
        if ((entity.state == "IDLE" || entity.state == "MOVING" || entity.state == "CASTING") &&
            0 <= skillIndex && skillIndex < skills.Count)
        {
            Skill skill = skills[skillIndex];
            if (CanUpgrade(skill))
            {
                skillExperience -= skill.upgradeRequiredSkillExperience;
                ++skill.level;
                skills[skillIndex] = skill;
            }
        }
    }

    [Server]
    public void OnKilledEnemy(Entity victim)
    {
        if (victim is Monster monster)
        {
            if (!party.InParty() || !party.party.shareExperience)
                skillExperience += Experience.BalanceExperienceReward(
                    monster.rewardSkillExperience, level.current, monster.level.current);
        }
    }
}
