using UnityEngine;

// DATABASE CLEANER (Template)
[CreateAssetMenu(fileName = "DatabaseCleaner", menuName = "ADDON/Templates/New DatabaseCleaner", order = 999)]
public class Tmpl_DatabaseCleaner : ScriptableObject
{
    [Tooltip("One click deactivation")]
    public bool isActive = true;

    [Header("Retention (days; set 0 to disable each)")]
    [Tooltip("Delete inactive accounts after X days (set 0 to disable)")]
    public int PruneInactiveAfterDays = 1;

    [Tooltip("Delete banned accounts after X days (set 0 to disable)")]
    public int PruneBannedAfterDays = 1;

    [Tooltip("Enable pruning of empty accounts (=0 characters)")]
    public bool PruneEmptyAccounts = true;

    [Tooltip("Delete empty accounts after X days (set 0 to disable)")]
    public int PruneEmptyAccountsAfterDays = 1;
    [Header("Integrity")]
    [Tooltip("Also delete rows whose character/account no longer exists.")]
    public bool CleanOrphanRowsOnStartup = true;

    [Header("Tables")]
    [Tooltip("Per-character tables (rows keyed by 'character' name)")]
    public string[] characterTables;

    [Tooltip("Per-account tables (rows keyed by 'account' name)")]
    public string[] accountTables;
    [Header("Auto-Discovery")]
    [Tooltip("Delete from ANY table that has a 'character' column (safe: checks schema each time)")]
    public bool AutoDiscoverCharacterTables = true;

    [Tooltip("Extra default list to include even if the asset list is empty")]
    public string[] DefaultCharacterTables = new[]
    {
        "character_inventory",
        "character_equipment",
        "character_skills",
        "character_combatSkills",
        "character_customization",
    };
}