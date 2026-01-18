﻿using UnityEngine;
using Mirror;

namespace uMMORPG
{
    [RequireComponent(typeof(Level))]
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(PlayerParty))]
    [DisallowMultipleComponent]
    public class PlayerSkills : Skills
    {
        [Header("Components")]
        public Level level;
        public Movement movement;
        public PlayerParty party;

        [Header("Skill Experience")]
        [SyncVar] public long skillExperience = 0;

        void Start()
        {
            if (!isServer && !isClient) return;

            if (isServer)
                for (int i = 0; i < buffs.Count; ++i)
                    if (buffs[i].BuffTimeRemaining() > 0)
                        buffs[i].data.SpawnEffect(entity, entity);

            onSkillCastFinished.AddListener(skill =>
            {
                int index = GetSkillIndexByName(skill.name);
                if (index != -1)
                    OnSkillFinished(skill, index);
            });
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
                        float stoppingDistance = ((Player)entity).autoCloseDistance;
                        if (movement.CanNavigate())
                        {
                            movement.Navigate(destination, stoppingDistance);
                        }
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

        public bool CanUpgrade(Skill skill)
        {
            return skill.level < skill.maxLevel &&
                   level.current >= skill.upgradeRequiredLevel &&
                   skillExperience >= skill.upgradeRequiredSkillExperience &&
                   (skill.predecessor == null ||
                    HasLearnedWithLevel(skill.predecessor.name, skill.predecessorLevel));
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

        public void OnSkillFinished(Skill skill, int skillIndex)
        {
            if (!isLocalPlayer) return;
            if (!skill.loopAttack) return;

            // OLD BEHAVIOR:
            // ((Player)entity).pendingSkill = skillIndex;

            // NEW BEHAVIOR:
            // Loop using the current attack (weapon or unarmed)
            ((Player)entity).pendingSkill = ((Player)entity).GetCurrentAttack();
        }

        [Server]
        public void OnKilledEnemy(Entity victim)
        {
            if (victim is Monster monster)
            {
                if (!party.InParty() || !party.party.shareExperience)
                    skillExperience += Experience.BalanceExperienceReward(
                        monster.rewardSkillExperience,
                        level.current,
                        monster.level.current
                    );
            }
        }
    }
}
