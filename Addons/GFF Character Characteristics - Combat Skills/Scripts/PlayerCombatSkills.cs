using Mirror;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace uMMORPG
{
    [DisallowMultipleComponent]
    public class PlayerCombatSkills : NetworkBehaviour, ICombatBonus, IHealthBonus, IManaBonus
    {
        [Header("Components")] // to be assigned in inspector
        public Player player;
        public Level level;
        public Combat combat;
        public Equipment equipment;
        public Health health;
        public Mana mana;
#if GFF_Addons_Stamina
        public Stamina stamina;
#endif

        public CombatSkillItem[] skillTemplates;
        [HideInInspector] public readonly SyncListCombatSkill skills = new SyncListCombatSkill();

        [Header("Settings")]
        public bool useMinDamageForCombatSkills;
        public float minDamageForCombatSkills = 0;

        public int GetDamageBonus()
        {
            int bonus = 0;
            float bonusInPercentage = 0;

            //find weapon in equipment
            int weaponIndex = equipment.GetEquippedWeaponIndex();
            if (weaponIndex != -1)
            {
                WeaponItem weapon = (WeaponItem)equipment.slots[weaponIndex].item.data;

                for (int i = 0; i < skills.Count; i++)
                {
                    if (skills[i].level > 1)
                    {
                        if ((weapon.weaponType == WeaponType.Staff && skillTemplates[i].increaseExp == IncreaseExpType.ForceDamage) ||
                            ((weapon.weaponType == WeaponType.Sword || weapon.weaponType == WeaponType.Spear || weapon.weaponType == WeaponType.Axe) && skillTemplates[i].increaseExp == IncreaseExpType.MeleeDamage) ||
                            ((weapon.weaponType == WeaponType.Bow || weapon.weaponType == WeaponType.Crossbow) && skillTemplates[i].increaseExp == IncreaseExpType.RangeDamage))
                        {
                            //fixed amount
                            if (skillTemplates[i].bonusDamage > 0)
                                bonus += skillTemplates[i].bonusDamage * (skills[i].level - 1);

                            //percent
                            if (skillTemplates[i].bonusDamagePercent > 0 && skills[i].level > 1)
                                bonusInPercentage += skillTemplates[i].bonusDamagePercent * (skills[i].level - 1);
                        }
                    }
                }
            }

            return bonus + Convert.ToInt32(combat.baseDamage.Get(level.current) * bonusInPercentage);
        }

        public int GetDefenseBonus()
        {
            int bonus = 0;
            float bonusInPercentage = 0;

            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].level > 1)
                {
                    //fixed amount
                    if (skillTemplates[i].bonusDefense > 0)
                        bonus += skillTemplates[i].bonusDefense * (skills[i].level - 1);

                    //percent
                    if (skillTemplates[i].bonusDefensePercent > 0)
                        bonusInPercentage += skillTemplates[i].bonusDefensePercent * (skills[i].level - 1);
                }
            }

            return bonus + Convert.ToInt32(combat.baseDefense.Get(level.current) * bonusInPercentage);
        }

        public float GetBlockChanceBonus()
        {
            float bonus = 0;

            for (int i = 0; i < skills.Count; i++)
            {
                //percent
                if (skillTemplates[i].bonusBlockChance > 0 && skills[i].level > 1)
                {
                    bonus += ((skills[i].level - 1) * skillTemplates[i].bonusBlockChance);
                }
            }

            return bonus;
        }

        public float GetCriticalChanceBonus()
        {
            return 0;
        }

        public int GetHealthBonus(int baseHealth)
        {
            int bonus = 0;

            for (int i = 0; i < skills.Count; i++)
            {
                //fixed amount
                if (skillTemplates[i].bonusHealth > 0 && skills[i].level > 1)
                    bonus += skillTemplates[i].bonusHealth * (skills[i].level - 1);

                //percent
                if (skillTemplates[i].bonusHealthPercent > 0 && skills[i].level > 1)
                    bonus += Convert.ToInt32(baseHealth * ((skills[i].level - 1) * skillTemplates[i].bonusHealthPercent));
            }

            return bonus;
        }

        public int GetHealthRecoveryBonus()
        {
            return 0;
        }

        public int GetManaBonus(int baseMana)
        {
            int bonus = 0;

            for (int i = 0; i < skills.Count; i++)
            {
                //fixed amount
                if (skillTemplates[i].bonusMana > 0 && skills[i].level > 1)
                    bonus += skillTemplates[i].bonusMana * (skills[i].level - 1);

                //percent
                if (skillTemplates[i].bonusManaPercent > 0 && skills[i].level > 1)
                    bonus += Convert.ToInt32(baseMana * ((skills[i].level - 1) * skillTemplates[i].bonusManaPercent));
            }

            return bonus;
        }

        public int GetManaRecoveryBonus()
        {
            return 0;
        }

        public int GetStaminaBonus()
        {
            int bonus = 0;

#if GFF_Addons_Stamina
            for (int i = 0; i < skills.Count; i++)
            {
                //fixed amount
                if (skillTemplates[i].bonusStamina > 0 && skills[i].level > 1)
                    bonus += skillTemplates[i].bonusStamina * (skills[i].level - 1);

                //percent
                if (skillTemplates[i].bonusStaminaPercent > 0 && skills[i].level > 1)
                    bonus += Convert.ToInt32(stamina.baseStamina.Get(level.current) * ((skills[i].level - 1) * skillTemplates[i].bonusStaminaPercent));
            }
#endif

            return bonus;
        }

        public int GetStaminaRecoveryBonus()
        {
            return 0;
        }

        // helper function to find a skill index
        public int GetSkillIndexByName(string skillName)
        {
            // (avoid FindIndex to minimize allocations)
            for (int i = 0; i < skills.Count; ++i)
                if (skills[i].name == skillName)
                    return i;
            return -1;
        }

        public void IncreaseExp(IncreaseExpType type)
        {
            int value = 10;

            for (int i = 0; i < skillTemplates.Length; i++)
            {
                if (skillTemplates[i].increaseExp == type && skills[i].level < skillTemplates[i].maxLevel)
                {
                    CombatSkill skill = skills[i];

                    if (skill.exp + value < skill.data._experienceMax.Get(skill.level)) skill.exp += value;
                    else
                    {
                        if (skill.exp + value == skill.data._experienceMax.Get(skill.level))
                        {
                            skill.level++;
                            skill.exp = 0;
                        }
                        else
                        {
                            long temp = skill.data._experienceMax.Get(skill.level) - skill.exp;
                            skill.level++;
                            skill.exp = value - temp;
                        }
                    }

                    skills[i] = skill;
                }
            }
        }

        public void ReceivedDamage(DamageType damageType)
        {
            //defense
            if (damageType == DamageType.Normal || damageType == DamageType.Crit)
            {
                player.combatSkills.IncreaseExp(IncreaseExpType.GetHit);
            }

            //shield
            else if (damageType == DamageType.Block)
            {
                player.combatSkills.IncreaseExp(IncreaseExpType.GetBlock);
            }
        }

        public void HitEnemy(Entity target, int amount)
        {
            //in order for the points to be counted, it is necessary that the damage is not lower than the minimum
            if (useMinDamageForCombatSkills == false || amount >= (target.health.max / 100) * minDamageForCombatSkills)
            {
                //find weapon in equipment
                int weaponIndex = player.equipment.GetEquippedWeaponIndex();
                if (weaponIndex != -1)
                {
                    WeaponItem weapon = (WeaponItem)player.equipment.slots[weaponIndex].item.data;

                    //melee
                    if ((weapon.weaponType == WeaponType.Sword || weapon.weaponType == WeaponType.Spear || weapon.weaponType == WeaponType.Axe))
                    {
                        IncreaseExp(IncreaseExpType.MeleeDamage);
                    }

                    //range
                    if ((weapon.weaponType == WeaponType.Bow || weapon.weaponType == WeaponType.Crossbow))
                    {
                        IncreaseExp(IncreaseExpType.RangeDamage);
                    }

                    //force
                    if (weapon.weaponType == WeaponType.Staff)
                    {
                        IncreaseExp(IncreaseExpType.ForceDamage);
                    }
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (syncInterval == 0)
            {
                syncInterval = 0.1f;
            }

            if (player == null) player = gameObject.GetComponent<Player>();
            level = player.level;
            combat = player.combat;
            equipment = player.equipment;
            health = player.health;
            mana = player.mana;
            player.combatSkills = this;

            //add events to database
            Database database = FindAnyObjectByType<Database>();
            if (database)
            {
                UnityAction unityAction = new UnityAction(database.Connect_CombatSkills);
                EventsPartial.AddListenerOnceOnConnected(database.onConnected, unityAction, database);

                UnityAction<Player> load = new UnityAction<Player>(database.CharacterLoad_CombatSkills);
                EventsPartial.AddListenerOnceCharacterLoad(database.onCharacterLoad, load, database);

                UnityAction<Player> save = new UnityAction<Player>(database.CharacterSave_CombatSkills);
                EventsPartial.AddListenerOnceCharacterSave(database.onCharacterSave, save, database);
            }
        }
#endif
    }
}