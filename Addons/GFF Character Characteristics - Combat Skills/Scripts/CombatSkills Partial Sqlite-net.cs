﻿using UnityEngine; // Debug.Log, Mathf
using System;              // Exception
using System.Linq;         // FirstOrDefault

public partial class Database
{
    // PRAGMA table_info mapping for robust schema inspection
    private class _PragmaTableInfo
    {
        // columns of PRAGMA table_info('table')
        public int    cid         { get; set; }
        public string name        { get; set; }
        public string type        { get; set; }
        public int    notnull     { get; set; }
        public string dflt_value  { get; set; }
        public int    pk          { get; set; }
    }

    // Table model: one row per (character, skill)
    public class character_combatSkills
    {
        public string character { get; set; }
        public string skill     { get; set; }
        public int    level     { get; set; }
        public long   exp       { get; set; } // INTEGER to match runtime long
    }

    public void Connect_CombatSkills()
    {
        // Ensure table exists (current schema)
        connection.CreateTable<character_combatSkills>();
        connection.CreateIndex(nameof(character_combatSkills), new[] { "character", "skill" });

        // --- One-time MIGRATION: if older installs stored EXP as REAL/FLOAT, convert to INTEGER (long) ---
        try
        {
            // Use PRAGMA to read actual column types (works across sqlite-net variants)
            var info  = connection.Query<_PragmaTableInfo>("PRAGMA table_info('character_combatSkills');");
            var expCi = info.FirstOrDefault(c => c.name == "exp");

            // sqlite reports "REAL" for floats; we want INTEGER now.
            if (expCi != null && !string.IsNullOrEmpty(expCi.type) &&
                expCi.type.Trim().ToUpperInvariant() == "REAL")
            {
                // 1) Rename old table
                connection.Execute("ALTER TABLE character_combatSkills RENAME TO _character_combatSkills_old;");

                // 2) Recreate with the new schema (exp as INTEGER)
                connection.CreateTable<character_combatSkills>();
                connection.CreateIndex(nameof(character_combatSkills), new[] { "character", "skill" });

                // 3) Copy data, casting exp to INTEGER
                connection.Execute(@"
                    INSERT INTO character_combatSkills(character, skill, level, exp)
                    SELECT character, skill, level, CAST(exp AS INTEGER)
                    FROM _character_combatSkills_old;
                ");

                // 4) Drop old table
                connection.Execute("DROP TABLE _character_combatSkills_old;");

                Debug.Log("[DB] CombatSkills migration: exp column REAL → INTEGER completed.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[DB] CombatSkills migration check failed: " + e.Message);
        }
    }

    public void CharacterLoad_CombatSkills(Player player)
    {
        // Seed from templates (clear first to avoid duplicates if load is called more than once)
        player.combatSkills.skills.Clear();
        foreach (CombatSkillItem skillData in player.combatSkills.skillTemplates)
            player.combatSkills.skills.Add(new CombatSkill(skillData));

        // Overlay saved values (single batch query is faster)
        foreach (character_combatSkills row in connection.Query<character_combatSkills>(
                     "SELECT * FROM character_combatSkills WHERE character=?", player.name))
        {
            int index = player.combatSkills.GetSkillIndexByName(row.skill);
            if (index != -1)
            {
                CombatSkill skill = player.combatSkills.skills[index];

                // Clamp level against valid range
                skill.level = Mathf.Clamp(row.level, 1, skill.maxLevel);

                // Clamp EXP in integer space (avoid float/precision issues)
                long maxExp = (long)skill.data._experienceMax.Get(skill.level);
                long loaded = row.exp;
                if (loaded < 0)      loaded = 0;
                if (loaded > maxExp) loaded = maxExp;
                skill.exp = loaded;

                player.combatSkills.skills[index] = skill;
            }
        }
    }

    public void CharacterSave_CombatSkills(Player player)
    {
        // Remove old rows, then write current snapshot for this character
        connection.Execute("DELETE FROM character_combatSkills WHERE character=?", player.name);

        foreach (CombatSkill skill in player.combatSkills.skills)
        {
            if (skill.level > 0) // only persist learned skills
            {
                connection.InsertOrReplace(new character_combatSkills
                {
                    character = player.name,
                    skill     = skill.name,
                    level     = skill.level,
                    exp       = skill.exp
                });
            }
        }
    }
}
