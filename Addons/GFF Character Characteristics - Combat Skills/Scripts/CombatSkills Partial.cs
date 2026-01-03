using System.Text;
using UnityEngine;

namespace uMMORPG
{
    public partial class EquipmentItem
    {
        [Header("GFF Combat Skills")]
        public CombatSkillItem requiredCombatSkill;
        public int requiredLevel = 1;

        private bool CheckCombatSkills(Player player)
        {
            if (requiredCombatSkill == null) return true;

            for (int i = 0; i < player.combatSkills.skillTemplates.Length; i++)
            {
                if (player.combatSkills.skillTemplates[i].Equals(requiredCombatSkill))
                {
                    return player.combatSkills.skills[i].level >= requiredLevel;
                }
            }
            return false;
        }

        // tooltip
        private void ToolTip_CombatSkills(StringBuilder tip)
        {
            if (requiredCombatSkill != null)
                tip.Replace("{COMBATSKILL}", requiredCombatSkill.name + ": " + requiredLevel);
        }
    }

    public partial class Player
    {
        [Header("GFF Combat Skills")]
        public PlayerCombatSkills combatSkills;
    }

    public partial class UICharacterInfoExtended
    {
        /* [Header("Settings: Combat Skill")]
         public bool limitToCharacterLevel;
         public bool limitToCharacterClass;
         public int minTargetLevel = 1;
         public int maxTargetLevel = 1;*/

        void Start_CombatSkills()
        {
            panelCombatSkills.SetActive(true);
        }

        void Update_CombatSkills(Player player)
        {
            if (player.combatSkills.skills.Count > 0)
            {
                // instantiate/destroy enough slots
                UIUtils.BalancePrefabs(combatSkillsPrefab, player.combatSkills.skillTemplates.Length, combatSkillsContentForPrefabs);

                for (int i = 0; i < player.combatSkills.skillTemplates.Length; i++)
                {
                    UICombatSkillSlot slot = combatSkillsContentForPrefabs.GetChild(i).GetComponent<UICombatSkillSlot>();

                    slot.textName.text = player.combatSkills.skillTemplates[i].name;
                    slot.sliderExp.value = player.combatSkills.skills[i].GetPercent();
                    slot.textPercent.text = (slot.sliderExp.value * 100).ToString("F") + "%";
                    slot.textLevel.text = player.combatSkills.skills[i].level + " lv";
                }
            }
        }
    }
}