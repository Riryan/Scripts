using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName="uMMORPG Item/Weapon", order=999)]
public partial class WeaponItem : EquipmentItem
{
    [Header("Weapon")]
    public AmmoItem requiredAmmo;

    [Header("Weapon Line")]
    [Tooltip("Logical weapon line for mastery (e.g., Bow, Sword). Leave blank to use the item name.")]
    public string weaponLineId;

    public string GetWeaponLineId()
        => string.IsNullOrWhiteSpace(weaponLineId) ? name : weaponLineId;

    // ===== Weapon Kit (Slots 1/2/3) =====
    [Header("Weapon Kit (Slots 1/2/3)")]
    [FormerlySerializedAs("qOptions")]
    [Tooltip("Slot 1 skill options (formerly Q). First entry is default.")]
    public ScriptableSkill[] slot1Options;

    [FormerlySerializedAs("qUnlockLevels")]
    [Tooltip("Unlock levels for each Slot 1 option (matches slot1Options).")]
    public int[] slot1UnlockLevels;

    [FormerlySerializedAs("wOptions")]
    [Tooltip("Slot 2 skill options (formerly W).")]
    public ScriptableSkill[] slot2Options;

    [FormerlySerializedAs("wUnlockLevels")]
    [Tooltip("Unlock levels for each Slot 2 option (matches slot2Options).")]
    public int[] slot2UnlockLevels;

    [FormerlySerializedAs("eOptions")]
    [Tooltip("Slot 3 (signature) options (formerly E, often a single entry).")]
    public ScriptableSkill[] slot3Options;

    [FormerlySerializedAs("eUnlockLevels")]
    [Tooltip("Unlock levels for each Slot 3 option (matches slot3Options).")]
    public int[] slot3UnlockLevels;

    [Header("Passives (choose-one)")]
    [Tooltip("Passive option labels for tooltip (display only).")]
    public string[] passiveNames;

    [Tooltip("Unlock levels for passiveNames (same length/order).")]
    public int[] passiveUnlockLevels;

    [Header("Mastery Display")]
    [Tooltip("Optional mastery label shown in tooltips (e.g., \"Mastery: Bow\").")]
    public string masteryDisplayName = "";

    // ===== Tooltip =====
    public override string ToolTip()
    {
        var tip = new StringBuilder(base.ToolTip());

        // Existing ammo placeholder
        if (requiredAmmo != null)
            tip.Replace("{REQUIREDAMMO}", requiredAmmo.name);

        // Build once
        string s1List = BuildOptionsList(slot1Options, slot1UnlockLevels, "1");
        string s2List = BuildOptionsList(slot2Options, slot2UnlockLevels, "2");
        string s3List = BuildOptionsList(slot3Options, slot3UnlockLevels, "3");

        string s1Name = FirstOptionName(slot1Options);
        string s2Name = FirstOptionName(slot2Options);
        string s3Name = FirstOptionName(slot3Options);

        string passivesList = BuildPassivesList(passiveNames, passiveUnlockLevels);
        string masteryText = string.IsNullOrWhiteSpace(masteryDisplayName) ? "—" : masteryDisplayName;

        // Numeric tokens
        tip.Replace("{WEAPON1_OPTIONS}", s1List);
        tip.Replace("{WEAPON2_OPTIONS}", s2List);
        tip.Replace("{WEAPON3_OPTIONS}", s3List);
        tip.Replace("{WEAPON1}", s1Name);
        tip.Replace("{WEAPON2}", s2Name);
        tip.Replace("{WEAPON3}", s3Name);

        // Back-compat: old Q/W/E tokens still work
        tip.Replace("{WEAPONQ_OPTIONS}", s1List);
        tip.Replace("{WEAPONW_OPTIONS}", s2List);
        tip.Replace("{WEAPONE_OPTIONS}", s3List);
        tip.Replace("{WEAPONQ}", s1Name);
        tip.Replace("{WEAPONW}", s2Name);
        tip.Replace("{WEAPONE}", s3Name);

        // Passives & mastery label
        tip.Replace("{WEAPON_PASSIVES}", passivesList);
        tip.Replace("{WEAPON_MASTERY}", masteryText);

        return tip.ToString();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ValidatePairLengths(slot1Options, slot1UnlockLevels, nameof(slot1Options), nameof(slot1UnlockLevels));
        ValidatePairLengths(slot2Options, slot2UnlockLevels, nameof(slot2Options), nameof(slot2UnlockLevels));
        ValidatePairLengths(slot3Options, slot3UnlockLevels, nameof(slot3Options), nameof(slot3UnlockLevels));
        ValidatePairLengths(passiveNames, passiveUnlockLevels, nameof(passiveNames), nameof(passiveUnlockLevels));
    }

    private static void ValidatePairLengths(System.Array a, System.Array b, string an, string bn)
    {
        if (a != null && b != null && a.Length != b.Length)
            Debug.LogWarning($"[{nameof(WeaponItem)}] {an} length ({a.Length}) != {bn} length ({b.Length}).");
    }
#endif

    private static string FirstOptionName(ScriptableSkill[] opts)
        => (opts != null && opts.Length > 0 && opts[0] != null) ? opts[0].name : "—";

    private static string BuildOptionsList(ScriptableSkill[] opts, int[] unlocks, string label)
    {
        if (opts == null || opts.Length == 0)
            return $"{label}: —";

        var sb = new StringBuilder();
        sb.Append(label).Append(": ");
        for (int i = 0; i < opts.Length; ++i)
        {
            var sk = opts[i];
            if (sk == null) continue;
            int u = (unlocks != null && i < unlocks.Length) ? unlocks[i] : 0;
            sb.Append(sk.name).Append(" (").Append(u).Append(")");
            if (i < opts.Length - 1) sb.Append(" | ");
        }
        return sb.ToString();
    }

    private static string BuildPassivesList(string[] names, int[] unlocks)
    {
        if (names == null || names.Length == 0) return "—";
        var sb = new StringBuilder();
        for (int i = 0; i < names.Length; ++i)
        {
            string n = names[i];
            if (string.IsNullOrWhiteSpace(n)) continue;
            int u = (unlocks != null && i < unlocks.Length) ? unlocks[i] : 0;
            sb.Append(n).Append(" (").Append(u).Append(")");
            if (i < names.Length - 1) sb.Append(" | ");
        }
        return sb.ToString();
    }
}
