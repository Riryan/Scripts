using Mirror;
using System;
using System.Collections.Generic;

namespace uMMORPG
{
    [Serializable]
    public struct CombatSkill
    {
        // hashcode used to reference the real ItemTemplate (can't link to template
        // directly because synclist only supports simple types). and syncing a
        // string's hashcode instead of the string takes WAY less bandwidth.
        public int hash;

        // dynamic stats
        public int level;
        public long exp;

        // constructors
        public CombatSkill(CombatSkillItem data)
        {
            hash = data.name.GetStableHashCode();

            level = 1;
            exp = 0;
        }

        public float GetPercent()
        {
            return (exp != 0 && data._experienceMax.Get(level) != 0) ? (float)exp / (float)data._experienceMax.Get(level) : 0;
        }

        // wrappers for easier access
        public CombatSkillItem data
        {
            get
            {
                // show a useful error message if the key can't be found
                // note: ScriptableSkill.OnValidate 'is in resource folder' check
                //       causes Unity SendMessage warnings and false positives.
                //       this solution is a lot better.
                if (!CombatSkillItem.All.ContainsKey(hash))
                    throw new KeyNotFoundException("There is no ScriptableSkill with hash=" + hash + ". Make sure that all ScriptableSkills are in the Resources folder so they are loaded properly.");
                return CombatSkillItem.All[hash];
            }
        }
        public string name => data.name;
        public int maxLevel => data.maxLevel;
    }

    public class SyncListCombatSkill : SyncList<CombatSkill> { }
}