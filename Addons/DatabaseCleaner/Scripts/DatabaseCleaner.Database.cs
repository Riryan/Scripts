#if UNITY_SERVER || UNITY_EDITOR
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using SQLite;

public partial class Database
{
    // -------------------------------------------------------------------------
    // POCO for our own last-online table
    // -------------------------------------------------------------------------
    class account_lastonline
    {
        [PrimaryKey]
        public string account { get; set; }
        // ISO-8601 ("s"), UTC
        public string lastOnline { get; set; }
    }

    // Lightweight DTO for the cleanup query
    class AccountRow
    {
        public string name { get; set; }
        public string lastOnline { get; set; }  // from LEFT JOIN account_lastonline
        public object banned { get; set; }      // may be bool/int in DB; normalize with Convert
    }

    // Helper DTO for sqlite_master queries
    class _SqliteName { public string name { get; set; } }

    // -------------------------------------------------------------------------
    // Lifecycle hook: create our table when DB connects
    // (Database already invokes Start_* hooks internally)
    // -------------------------------------------------------------------------
    void Start_Tools_DatabaseCleaner()
    {
        onConnected.AddListener(Connect_DatabaseCleaner);
    }

    void Connect_DatabaseCleaner()
    {
        connection.CreateTable<account_lastonline>();
    }

    // -------------------------------------------------------------------------
    // Public helpers used elsewhere
    // -------------------------------------------------------------------------
    // Legacy name kept for compatibility
    public void DatabaseCleanerAccountLastOnline(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName)) return;

        connection.Execute("DELETE FROM account_lastonline WHERE account=?", accountName);
        connection.Insert(new account_lastonline
        {
            account = accountName,
            lastOnline = DateTime.UtcNow.ToString("s", CultureInfo.InvariantCulture)
        });
    }

    // Preferred helper (calls the legacy one)
    public void UpsertAccountLastOnline(string accountName)
        => DatabaseCleanerAccountLastOnline(accountName);

    // -------------------------------------------------------------------------
    // Orphan sweeper
    // -------------------------------------------------------------------------
    static bool IsSafeTableName(string name) => Regex.IsMatch(name ?? "", @"^[A-Za-z_][A-Za-z0-9_]*$");

    IEnumerable<string> GetCharacterScopedTables(Tmpl_DatabaseCleaner cfg)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Include asset-configured tables
        if (cfg.characterTables != null)
            foreach (var t in cfg.characterTables)
                if (!string.IsNullOrWhiteSpace(t)) set.Add(t);

        // Auto-discover any table that has a 'character' column (covers your screenshots)
        var names = connection.Query<_SqliteName>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'character_%'");
        foreach (var n in names)
        {
            if (string.Equals(n.name, "characters", StringComparison.OrdinalIgnoreCase))
                continue;
            var info = connection.GetTableInfo(n.name);
            bool hasCharacterCol = false;
            foreach (var col in info)
                if (string.Equals(col.Name, "character", StringComparison.OrdinalIgnoreCase))
                { hasCharacterCol = true; break; }
            if (hasCharacterCol) set.Add(n.name);
        }

        return set;
    }

    IEnumerable<string> GetAccountScopedTables(Tmpl_DatabaseCleaner cfg)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (cfg.accountTables != null)
            foreach (var t in cfg.accountTables)
                if (!string.IsNullOrWhiteSpace(t)) set.Add(t);
        return set;
    }

    void PurgeOrphanRows(Tmpl_DatabaseCleaner cfg)
    {
        var charTables = GetCharacterScopedTables(cfg);
        var accTables  = GetAccountScopedTables(cfg);

        connection.RunInTransaction(() =>
        {
            // character-scoped
            foreach (var t in charTables)
            {
                if (!IsSafeTableName(t)) continue;
                var n = connection.Execute($"DELETE FROM {t} WHERE character NOT IN (SELECT name FROM characters)");
                if (n > 0) Debug.Log($"[OrphanCleaner] {t}: deleted {n} orphan row(s).");
            }

            // account-scoped (from configured list only, to stay conservative)
            foreach (var t in accTables)
            {
                if (!IsSafeTableName(t)) continue;
                var n = connection.Execute($"DELETE FROM {t} WHERE account NOT IN (SELECT name FROM accounts)");
                if (n > 0) Debug.Log($"[OrphanCleaner] {t}: deleted {n} orphan row(s).");
            }
        });
    }

    // -------------------------------------------------------------------------
    // Cleanup runner (call on server start; does NOT read/write accounts.lastLogin)
    // -------------------------------------------------------------------------
    public void Cleanup(Tmpl_DatabaseCleaner databaseCleaner)
    {
        if (databaseCleaner == null || !databaseCleaner.isActive)
        {
            Debug.LogWarning("DatabaseCleaner: Either inactive or ScriptableObject not found!");
            return;
        }

        // Always purge orphans first so the rest of the logic doesn't see stale rows.
        PurgeOrphanRows(databaseCleaner);

        int pruned = 0;

        bool checkInactive = databaseCleaner.PruneInactiveAfterDays > 0;
        bool checkBanned   = databaseCleaner.PruneBannedAfterDays   > 0;
        bool checkEmpty    = databaseCleaner.PruneEmptyAccounts && databaseCleaner.PruneEmptyAccountsAfterDays > 0;

        if (checkInactive || checkBanned || checkEmpty)
        {
            // NOTE: no dependency on accounts.lastLogin
            var rows = connection.Query<AccountRow>(@"
                SELECT a.name      AS name,
                       al.lastOnline AS lastOnline,
                       a.banned     AS banned
                FROM accounts a
                LEFT JOIN account_lastonline al ON al.account = a.name
            ");

            foreach (var row in rows)
            {
                string accountName = row.name;
                if (string.IsNullOrWhiteSpace(accountName)) continue;

                // Determine age only if we have lastOnline recorded
                bool   hasLast = !string.IsNullOrWhiteSpace(row.lastOnline);
                double daysSince = double.NaN;

                if (hasLast)
                {
                    if (!DateTime.TryParseExact(row.lastOnline, "s",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                            out var last))
                    {
                        // fallback if old data isn't strict ISO
                        DateTime.TryParse(row.lastOnline, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out last);
                    }
                    daysSince = (DateTime.UtcNow - last).TotalDays;
                }

                bool banned = Convert.ToBoolean(row.banned);

                bool inactiveHit = checkInactive &&
                                   hasLast &&
                                   daysSince > databaseCleaner.PruneInactiveAfterDays;

                bool bannedHit   = checkBanned &&
                                   banned &&
                                   hasLast &&
                                   daysSince > databaseCleaner.PruneBannedAfterDays;

                bool emptyHit    = checkEmpty &&
                                   hasLast &&
                                   daysSince > databaseCleaner.PruneEmptyAccountsAfterDays &&
                                   CharactersForAccount(accountName).Count < 1;

                if (!(inactiveHit || bannedHit || emptyHit))
                    continue;

                DatabaseCleanup(databaseCleaner, accountName);
                pruned++;
            }
        }

    #if UNITY_EDITOR
        Debug.Log($"DatabaseCleaner checking accounts ... pruned [{pruned}] account(s)");
    #else
        Console.WriteLine($"DatabaseCleaner checking accounts ... pruned [{pruned}] account(s)");
    #endif
    }

    // -------------------------------------------------------------------------
    // Delete all data for an account (SQLite-only)
    // -------------------------------------------------------------------------
    public void DatabaseCleanup(Tmpl_DatabaseCleaner databaseCleaner, string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName)) return;

        var accountChars = CharactersForAccount(accountName);
        var charTables   = GetCharacterScopedTables(databaseCleaner);

        connection.RunInTransaction(() =>
        {
            // Character-scoped tables
            foreach (string accountChar in accountChars)
            {
                foreach (string charTable in charTables)
                {
                    if (string.IsNullOrWhiteSpace(charTable)) continue;

                    var info = connection.GetTableInfo(charTable);
                    if (info.Count > 0)
                        connection.Execute($"DELETE FROM {charTable} WHERE character=?", accountChar);
                    else
                        Debug.LogWarning($"DatabaseCleaner: table \"{charTable}\" does not exist (character-scope).");
                }
            }

            // Account-scoped tables (once per account)
            if (databaseCleaner.accountTables != null)
            {
                foreach (string accountTable in databaseCleaner.accountTables)
                {
                    if (string.IsNullOrWhiteSpace(accountTable)) continue;

                    var info = connection.GetTableInfo(accountTable);
                    if (info.Count > 0)
                        connection.Execute($"DELETE FROM {accountTable} WHERE account=?", accountName);
                    else
                        Debug.LogWarning($"DatabaseCleaner: table \"{accountTable}\" does not exist (account-scope).");
                }
            }

            // Core tables
            connection.Execute("DELETE FROM characters WHERE account=?", accountName);
            connection.Execute("DELETE FROM account_lastonline WHERE account=?", accountName);
            connection.Execute("DELETE FROM accounts WHERE name=?", accountName);
        });

        Debug.Log($"DatabaseCleaner deleted account '{accountName}', {accountChars.Count} character(s), and related data.");
    }
}
#endif