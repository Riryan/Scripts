using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace uMMORPG
{
    public enum IncreaseExpType : byte { None, GetHit, GetBlock, MeleeDamage, RangeDamage, ForceDamage };

    [CreateAssetMenu(menuName = "GFF Addons/Combat Skill", order = 999)]
    public class CombatSkillItem : ScriptableObject
    {
        [Header("way to gain experience")]
        public IncreaseExpType increaseExp;
        public int maxLevel = 100;
        [SerializeField] public ExponentialLong _experienceMax = new ExponentialLong { multiplier = 100, baseValue = 1.1f };

        [Header("Health")]
        public int bonusHealth;
        [Range(0, 1)] public float bonusHealthPercent;

        [Header("Mana")]
        public int bonusMana;
        [Range(0, 1)] public float bonusManaPercent;

        [Header("Stamina")]
        public int bonusStamina;
        [Range(0, 1)] public float bonusStaminaPercent;

        [Header("Damage")]
        public int bonusDamage;
        [Range(0, 1)] public float bonusDamagePercent;

        [Header("Defense")]
        public int bonusDefense;
        [Range(0, 1)] public float bonusDefensePercent;

        [Header("Block Chance")]
        [Range(0, 1)] public float bonusBlockChance;


        // caching /////////////////////////////////////////////////////////////////
        // we can only use Resources.Load in the main thread. we can't use it when
        // declaring static variables. so we have to use it as soon as 'dict' is
        // accessed for the first time from the main thread.
        // -> we save the hash so the dynamic item part doesn't have to contain and
        //    sync the whole name over the network
        static Dictionary<int, CombatSkillItem> cache;
        public static Dictionary<int, CombatSkillItem> All
        {
            get
            {
                // not loaded yet?
                if (cache == null)
                {
                    // get all ScriptableSkills in resources
                    CombatSkillItem[] skills = Resources.LoadAll<CombatSkillItem>("");

                    // check for duplicates, then add to cache
                    List<string> duplicates = skills.ToList().FindDuplicates(skill => skill.name);
                    if (duplicates.Count == 0)
                    {
                        cache = skills.ToDictionary(skill => skill.name.GetStableHashCode(), skill => skill);
                    }
                    else
                    {
                        foreach (string duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple CombatSkillItem with the name " + duplicate + ". If you are using subfolders like 'Warrior/NormalAttack' and 'Archer/NormalAttack', then rename them to 'Warrior/(Warrior)NormalAttack' and 'Archer/(Archer)NormalAttack' instead.");
                    }
                }
                return cache;
            }
        }
    }
}