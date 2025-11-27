﻿using Mirror;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace GFFAddons
{
    [DisallowMultipleComponent]
    public class PlayerCombatSkills : NetworkBehaviour
    {
        [Header("Components")] // to be assigned in inspector
        public Player player;
        public Level level;
        public Combat combat;
        public Equipment equipment;
        public Health health;
        public Mana mana;

        public CombatSkillItem[] skillTemplates;
        [HideInInspector] public readonly SyncListCombatSkill skills = new SyncListCombatSkill();

        [Header("Settings")]
        public bool useMinDamageForCombatSkills;
        public float minDamageForCombatSkills = 0;

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

            if (skillTemplates == null || skills == null) return;
            int count = Mathf.Min(skillTemplates.Length, skills.Count);

            for (int i = 0; i < count; i++)
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
            // in order for the points to be counted, it is necessary that the damage is not lower than the minimum
            if (useMinDamageForCombatSkills == false || amount >= (target.health.max / 100) * minDamageForCombatSkills)
            {
                // locate weapon
                int weaponIndex = player.equipment.GetEquippedWeaponIndex();

                // determine unarmed:
                //  - true if no weapon equipped
                //  - OR if the equipped item is the Auto Fists (TEST) weapon
                bool isUnarmed = weaponIndex == -1;
                if (!isUnarmed)
                {
                    var pe = player.equipment as PlayerEquipment;
                    if (pe != null && pe.autoFistsTestEnabled && pe.autoFistWeaponTest != null)
                    {
                        ItemSlot wslot = player.equipment.slots[weaponIndex];
                        if (wslot.amount > 0 && wslot.item.data == pe.autoFistWeaponTest)
                            isUnarmed = true;
                    }
                }

                // award UNARMED xp (distinct from melee)
                if (isUnarmed)
                {
                    IncreaseExp(IncreaseExpType.UnarmedDamage);
                    return;
                }

                // armed: route by weapon type
                WeaponItem weapon = (WeaponItem)player.equipment.slots[weaponIndex].item.data;

                // melee
                if (weapon.weaponType == WeaponType.Sword || weapon.weaponType == WeaponType.Spear || weapon.weaponType == WeaponType.Axe)
                {
                    IncreaseExp(IncreaseExpType.MeleeDamage);
                    return;
                }

                // range
                if (weapon.weaponType == WeaponType.Bow || weapon.weaponType == WeaponType.Crossbow)
                {
                    IncreaseExp(IncreaseExpType.RangeDamage);
                    return;
                }

                // force
                if (weapon.weaponType == WeaponType.Staff)
                {
                    IncreaseExp(IncreaseExpType.ForceDamage);
                    return;
                }
            }
        }

        //health
        public int BonusHealth()
        {
            if (skillTemplates == null) return 0;

            int bonus = 0;
            int count = Mathf.Min(skills.Count, skillTemplates.Length);

            for (int i = 0; i < count; i++)
            {
                //fixed amount
                if (skillTemplates[i].bonusHealth > 0 && skills[i].level > 1)
                    bonus += skillTemplates[i].bonusHealth * (skills[i].level - 1);

                //percent
                if (skillTemplates[i].bonusHealthPercent > 0 && skills[i].level > 1)
                    bonus += Convert.ToInt32(health.baseHealth.Get(level.current) * ((skills[i].level - 1) * skillTemplates[i].bonusHealthPercent));
            }

            return bonus;
        }

        //mana
        public int BonusMana()
        {
            if (skillTemplates == null) return 0;

            int bonus = 0;
            int count = Mathf.Min(skills.Count, skillTemplates.Length);

            for (int i = 0; i < count; i++)
            {
                //fixed amount
                if (skillTemplates[i].bonusMana > 0 && skills[i].level > 1)
                    bonus += skillTemplates[i].bonusMana * (skills[i].level - 1);

                //percent
                if (skillTemplates[i].bonusManaPercent > 0 && skills[i].level > 1)
                    bonus += Convert.ToInt32(mana.baseMana.Get(level.current) * ((skills[i].level - 1) * skillTemplates[i].bonusManaPercent));
            }

            return bonus;
        }

        //stamina
        /*public int BonusStamina()
        {
            if (skillTemplates == null) return 0;

            int bonus = 0;
            int count = Mathf.Min(skills.Count, skillTemplates.Length);

            for (int i = 0; i < count; i++)
            {
                //fixed amount
                if (skillTemplates[i].bonusStamina > 0 && skills[i].level > 1)
                    bonus += skillTemplates[i].bonusStamina * (skills[i].level - 1);

                //percent
                if (skillTemplates[i].bonusStaminaPercent > 0 && skills[i].level > 1)
                    bonus += Convert.ToInt32(stamina.baseStamina.Get(level.current) * ((skills[i].level - 1) * skillTemplates[i].bonusStaminaPercent));
            }

            return bonus;
        }*/

        //damage
        public int GetDamageBonus(int damageAndEquipmentBonus)
        {
            if (skillTemplates == null) return 0;

            int bonus = 0;

            //find weapon in equipment
            int weaponIndex = equipment.GetEquippedWeaponIndex();
            if (weaponIndex != -1)
            {
                WeaponItem weapon = (WeaponItem)equipment.slots[weaponIndex].item.data;

                //melee weapon
                if (weapon.weaponType == WeaponType.Sword || weapon.weaponType == WeaponType.Spear || weapon.weaponType == WeaponType.Axe)
                {
                    int count = Mathf.Min(skills.Count, skillTemplates.Length);
                    for (int i = 0; i < count; i++)
                    {
                        if (skillTemplates[i].increaseExp == IncreaseExpType.MeleeDamage)
                        {
                            //percent
                            if (skillTemplates[i].bonusDamagePercent > 0 && skills[i].level > 1)
                                bonus += Convert.ToInt32(damageAndEquipmentBonus * ((skills[i].level - 1) * skillTemplates[i].bonusDamagePercent));
                        }
                    }
                }

                //range weapon
                else if (weapon.weaponType == WeaponType.Bow || weapon.weaponType == WeaponType.Crossbow)
                {
                    int count = Mathf.Min(skills.Count, skillTemplates.Length);
                    for (int i = 0; i < count; i++)
                    {
                        if (skillTemplates[i].increaseExp == IncreaseExpType.RangeDamage)
                        {
                            //percent
                            if (skillTemplates[i].bonusDamagePercent > 0 && skills[i].level > 1)
                                bonus += Convert.ToInt32(damageAndEquipmentBonus * ((skills[i].level - 1) * skillTemplates[i].bonusDamagePercent));
                        }
                    }
                }

                //force(magic) weapon
                else if (weapon.weaponType == WeaponType.Staff)
                {
                    int count = Mathf.Min(skills.Count, skillTemplates.Length);
                    for (int i = 0; i < count; i++)
                    {
                        if (skillTemplates[i].increaseExp == IncreaseExpType.ForceDamage)
                        {
                            //percent
                            if (skillTemplates[i].bonusDamagePercent > 0 && skills[i].level > 1)
                                bonus += Convert.ToInt32(damageAndEquipmentBonus * ((skills[i].level - 1) * skillTemplates[i].bonusDamagePercent));
                        }
                    }
                }
            }

            return bonus;
        }

        //defense
        public int GetDefenseBonus()
        {
            if (skillTemplates == null) return 0;

            int bonus = 0;
            int count = Mathf.Min(skills.Count, skillTemplates.Length);

            for (int i = 0; i < count; i++)
            {
                //fixed amount
                if (skillTemplates[i].bonusDefense > 0 && skills[i].level > 1)
                    bonus += skillTemplates[i].bonusDefense * (skills[i].level - 1);

                //percent
                if (skillTemplates[i].bonusDefensePercent > 0 && skills[i].level > 1)
                    bonus += Convert.ToInt32(combat.baseDefense.Get(level.current) * ((skills[i].level - 1) * skillTemplates[i].bonusDefensePercent));
            }

            return bonus;
        }

        //block chance
        public float GetBlockChanceBonus()
        {
            if (skillTemplates == null) return 0f;

            float bonus = 0f;
            int count = Mathf.Min(skills.Count, skillTemplates.Length);

            for (int i = 0; i < count; i++)
            {
                //percent
                if (skillTemplates[i].bonusBlockChance > 0 && skills[i].level > 1)
                {
                    float defaultValue = combat.baseBlockChance.Get(level.current);
                    if (defaultValue == 0) defaultValue = 1;
                    bonus += defaultValue * ((skills[i].level - 1) * skillTemplates[i].bonusBlockChance);
                }
            }

            return bonus;
        }

        //critical chance

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

/*            if (syncInterval == 0)
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
            }*/
        }
#endif

    }
}
