using UnityEngine;
using Mirror;
using System;

[RequireComponent(typeof(PlayerChat))]
[RequireComponent(typeof(PlayerParty))]
[RequireComponent(typeof(PlayerEquipment))] // we read the equipped weapon
public class PlayerExperience : Experience
{
    [Header("Components")]
    public PlayerChat chat;
    public PlayerParty party;
    public PlayerEquipment equipment;

    void Awake()
    {
        // One-time, allocation-free caching. Safe on server/headless.
        if (chat == null) chat = GetComponent<PlayerChat>();
        if (party == null) party = GetComponent<PlayerParty>();
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();
    }

    // Make sure this whole component (including weaponMasteries) is owner-only.
    protected override void OnValidate()
    {
        base.OnValidate();
        if (syncMode != SyncMode.Owner)
            syncMode = SyncMode.Owner;
    }

    [Header("Death")]
    public string deathMessage = "You died and lost experience.";

    // ========= WEAPON MASTERY (Albion-style) =========
    [Serializable]
    public struct WeaponMastery
    {
        public string id;    // e.g., "Bow", "Sword" (or weapon name if no line set)
        public int level;    // 0..weaponLevelCap
        public long current; // current fame within this level
    }

    // Mirror syncs this list; owner-only because syncMode = Owner.
    public SyncList<WeaponMastery> weaponMasteries = new SyncList<WeaponMastery>();

    [Header("Weapon Mastery Levels")]
    [SerializeField] ExponentialLong weaponLevelMaxCurve = new ExponentialLong { multiplier = 100, baseValue = 1.10f };
    [Tooltip("Hard cap for weapon mastery levels.")]
    public int weaponLevelCap = 100;

    long WeaponLevelMax(int lvl) => weaponLevelMaxCurve.Get(lvl);

    int FindMasteryIndex(string id)
    {
        for (int i = 0; i < weaponMasteries.Count; ++i)
            if (weaponMasteries[i].id == id) return i;
        return -1;
    }

    void EnsureMasteryEntry(string id)
    {
        if (FindMasteryIndex(id) == -1)
            weaponMasteries.Add(new WeaponMastery { id = id, level = 0, current = 0 });
    }

    [Server]
    public void AddWeaponFame(string id, long amount)
    {
        if (string.IsNullOrWhiteSpace(id) || amount <= 0) return;

        EnsureMasteryEntry(id);
        int i = FindMasteryIndex(id);
        var e = weaponMasteries[i];

        e.current += amount;

        // Level up loop (reuses your ExponentialLong style)
        while (e.level < weaponLevelCap && e.current >= WeaponLevelMax(e.level))
        {
            e.current -= WeaponLevelMax(e.level);
            ++e.level;
            // (Optional) fire an event or chat message here for weapon level up
        }

        // Clamp overflow to current level's max
        long cap = WeaponLevelMax(e.level);
        if (e.current > cap) e.current = cap;

        weaponMasteries[i] = e; // write back to SyncList
    }

    public int GetWeaponMasteryLevel(string id)
    {
        int idx = FindMasteryIndex(id);
        return idx >= 0 ? weaponMasteries[idx].level : 0;
    }

    public float GetWeaponMasteryPercent(string id)
    {
        int idx = FindMasteryIndex(id);
        if (idx < 0) return 0f;
        var e = weaponMasteries[idx];
        long max = WeaponLevelMax(e.level);
        return (max > 0) ? (float)e.current / (float)max : 0f;
    }

    // Returns null if no weapon line (unarmed or nothing equipped).
    string GetEquippedWeaponLineId()
    {
        // Null-safe guards: component, slots list, and contents can be transiently null.
        if (equipment == null || equipment.slots == null || equipment.slots.Count == 0)
            return null;

        // Scan for the first equipped Weapon item. No LINQ (no allocs).
        for (int i = 0; i < equipment.slots.Count; ++i)
        {
            var slot = equipment.slots[i];
            if (slot.amount <= 0) continue;

            var runtimeItem = slot.item;
            if (runtimeItem.data == null) continue; 

            var data = runtimeItem.data;
            if (data == null) continue;

            if (data is WeaponItem wi)
                return wi.GetWeaponLineId();
        }
        return null;
    }
    // ========= / WEAPON MASTERY =========

    [Server]
    public override void OnDeath()
    {
        base.OnDeath();
        if (chat != null)
            chat.TargetMsgInfo(deathMessage);
    }

    [Server]
    public void OnKilledEnemy(Entity victim)
    {
        if (victim is Monster monster)
        {
            // character XP grant (via Experience.current, owner-only & change-only)
            if (party == null || !party.InParty() || !party.party.shareExperience)
                current += BalanceExperienceReward(monster.rewardExperience, level.current, monster.level.current);

            // weapon fame on kill, only if rewardExperience > 0, owner-only via syncMode
            string lineId = GetEquippedWeaponLineId();
            if (!string.IsNullOrWhiteSpace(lineId))
            {
                long fame = BalanceExperienceReward(monster.rewardExperience, level.current, monster.level.current);
                AddWeaponFame(lineId, fame);
            }
        }
    }
}
