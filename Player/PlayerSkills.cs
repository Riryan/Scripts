using UnityEngine;
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
        public void TryUse(int skillIndex, bool ignoreState=false)
        {
            if (entity.state != "CASTING" || ignoreState)
            {
                Skill skill = skills[skillIndex];

                // fix skill auto-recasts:
                // Server calls Skills::RpcCastFinished when castTimeRemaining==0.
                // Rpc may arrive before the SyncList or NetworkTime updates,
                // so when trying to auto re-cast, CastTimeRemaining is still a bit >0.
                // => RpcCastFinished is only ever called exactly when castRemaining==0,
                //    so let's simply ignore the ready check here by passing 'ignoreState',
                //    which is 'true' when auto recasting the next skill after one was finished.
                bool checkSelf = CastCheckSelf(skill, !ignoreState);
                bool checkTarget = CastCheckTarget(skill);
                if (checkSelf && checkTarget)
                {
                    // check distance between self and target
                    Vector3 destination;
                    if (CastCheckDistance(skill, out destination))
                    {
                        // cast
                        CmdUse(skillIndex);
                    }
                    else
                    {
                        // move to the target first
                        // (use collider point(s) to also work with big entities)
                        //float stoppingDistance = skill.castRange * ((Player)entity).attackToMoveRangeRatio;
                        //movement.Navigate(destination, stoppingDistance);

                        // use skill when there
                        //((Player)entity).useSkillWhenCloser = skillIndex;
                        //float stoppingDistance = skill.castRange * ((Player)entity).attackToMoveRangeRatio;
                        float stoppingDistance = ((Player)entity).autoCloseDistance;
                        if (movement.CanNavigate())
                        {
                            movement.Navigate(destination, stoppingDistance);
                        }
                        else if (movement is PlayerCharacterControllerMovement cc)
                        {
                            cc.StartAutoCloseDistance(destination);
                        }

                        // persist intent for BOTH controllers
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
            // has this skill with at least level 1 (=learned)?
            return HasLearnedWithLevel(skillName, 1);
        }

        public bool HasLearnedWithLevel(string skillName, int skillLevel)
        {
            // (avoid Linq because it is HEAVY(!) on GC and performance)
            foreach (Skill skill in skills)
                if (skill.level >= skillLevel && skill.name == skillName)
                    return true;
            return false;
        }

        // helper function for command and UI
        // -> this is for learning and upgrading!
        public bool CanUpgrade(Skill skill)
        {
            return skill.level < skill.maxLevel &&
                   level.current >= skill.upgradeRequiredLevel &&
                   skillExperience >= skill.upgradeRequiredSkillExperience &&
                   (skill.predecessor == null || (HasLearnedWithLevel(skill.predecessor.name, skill.predecessorLevel)));
        }

        // -> this is for learning and upgrading!
        [Command]
        public void CmdUpgrade(int skillIndex)
        {
            // validate
            if ((entity.state == "IDLE" || entity.state == "MOVING" || entity.state == "CASTING") &&
                0 <= skillIndex && skillIndex < skills.Count)
            {
                // can be upgraded?
                Skill skill = skills[skillIndex];
                if (CanUpgrade(skill))
                {
                    // decrease skill experience
                    skillExperience -= skill.upgradeRequiredSkillExperience;

                    // upgrade
                    ++skill.level;
                    skills[skillIndex] = skill;
                }
            }
        }

        // events //////////////////////////////////////////////////////////////////
        [Server]
        public void OnKilledEnemy(Entity victim)
        {
            // killed a monster
            if (victim is Monster monster)
            {
                // gain exp if not in a party or if in a party without exp share
                if (!party.InParty() || !party.party.shareExperience)
                    skillExperience += Experience.BalanceExperienceReward(monster.rewardSkillExperience, level.current, monster.level.current);
            }
        }
    }
}