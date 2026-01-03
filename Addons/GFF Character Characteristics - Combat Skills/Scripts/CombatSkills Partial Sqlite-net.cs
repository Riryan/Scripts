using UnityEngine; // from https://github.com/praeclarum/sqlite-net

namespace uMMORPG
{
    public partial class Database
    {
        public class character_combatSkills
        {
            public string character { get; set; }
            public string skill { get; set; }
            public int level { get; set; }
            public float exp { get; set; }
        }

        public void Connect_CombatSkills()
        {
            // create tables if they don't exist yet or were deleted
            connection.CreateTable<character_combatSkills>();
            connection.CreateIndex(nameof(character_combatSkills), new[] { "character", "skill" });
        }

        public void CharacterLoad_CombatSkills(Player player)
        {
            // load skills based on skill templates
            foreach (CombatSkillItem skillData in player.combatSkills.skillTemplates)
                player.combatSkills.skills.Add(new CombatSkill(skillData));

            // (one big query is A LOT faster than querying each slot separately)
            foreach (character_combatSkills row in connection.Query<character_combatSkills>("SELECT * FROM character_combatSkills WHERE character=?", player.name))
            {
                int index = player.combatSkills.GetSkillIndexByName(row.skill);
                if (index != -1)
                {
                    CombatSkill skill = player.combatSkills.skills[index];
                    // make sure that 1 <= level <= maxlevel (in case we removed a skill level etc)
                    skill.level = Mathf.Clamp(row.level, 1, skill.maxLevel);
                    // make sure that 1 <= level <= maxlevel (in case we removed a skill level etc)

                    skill.exp = (long)Mathf.Clamp(row.exp, 0, skill.data._experienceMax.Get(skill.level));

                    player.combatSkills.skills[index] = skill;
                }
            }
        }

        public void CharacterSave_CombatSkills(Player player)
        {
            // skills: remove old entries first, then add all new ones
            connection.Execute("DELETE FROM character_combatSkills WHERE character=?", player.name);

            foreach (CombatSkill skill in player.combatSkills.skills)
                if (skill.level > 0) // only learned skills to save queries/storage/time
                {
                    // castTimeEnd and cooldownEnd are based on NetworkTime.time,
                    // which will be different when restarting the server, so let's
                    // convert them to the remaining time for easier save & load
                    // note: this does NOT work when trying to save character data
                    //       shortly before closing the editor or game because
                    //       NetworkTime.time is 0 then.
                    connection.InsertOrReplace(new character_combatSkills
                    {
                        character = player.name,
                        skill = skill.name,
                        level = skill.level,
                        exp = skill.exp
                    });
                }
        }
    }
}