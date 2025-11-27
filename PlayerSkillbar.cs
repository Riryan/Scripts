using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

[Serializable]
public struct SkillbarEntry
{
    public string reference;
    public KeyCode hotKey;
}

[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerSkills))]
public class PlayerSkillbar : NetworkBehaviour
{
    [Header("Components")]
    public PlayerEquipment equipment;
    public PlayerInventory inventory;
    public PlayerSkills skills;
    [Header("Unarmed Defaults")]
    [Tooltip("Drag your Unarmed Strike ScriptableSkill here.")]
    [SerializeField] ScriptableSkill unarmedDefaultSkill = null;
    bool slot1ManualOverride = false; 

    [Header("Skillbar")]
    public SkillbarEntry[] slots =
    {
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha1},
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha2},
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha3},
        // add more if desired
    };

    // cache last equipped weapon name to detect changes
    string _lastEquippedWeaponName = "";

    public override void OnStartLocalPlayer()
    {
        Load();
        AutoPopulateFromEquippedWeaponIfChanged();
    }

    public override void OnStopClient()
    {
        if (isLocalPlayer) Save();
    }

    void Save()
    {
        for (int i = 0; i < slots.Length; ++i)
            PlayerPrefs.SetString(name + "_skillbar_" + i, slots[i].reference);
                PlayerPrefs.SetInt(name + "_skillbar_slot1_override", slot1ManualOverride ? 1 : 0);
        PlayerPrefs.Save();
    }

    [Client]
    void Load()
    {
        if (PlayerPrefs.HasKey(name + "_skillbar_slot1_override"))
            slot1ManualOverride = PlayerPrefs.GetInt(name + "_skillbar_slot1_override", 0) != 0;
        List<Skill> learned = skills.skills.Where(s => s.level > 0).ToList();
        for (int i = 0; i < slots.Length; ++i)
        {
            if (PlayerPrefs.HasKey(name + "_skillbar_" + i))
            {
                string entry = PlayerPrefs.GetString(name + "_skillbar_" + i, "");
                if (skills.HasLearned(entry) ||
                    inventory.GetItemIndexByName(entry) != -1 ||
                    equipment.GetItemIndexByName(entry) != -1)
                {
                    slots[i].reference = entry;

                }
            }
            else if (i < learned.Count)
            {
                slots[i].reference = learned[i].name;
            }
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        AutoPopulateFromEquippedWeaponIfChanged();
    }

    void AutoPopulateFromEquippedWeaponIfChanged()
    {
        WeaponItem weapon = GetCurrentlyEquippedWeapon();
        string current = weapon != null ? weapon.name : "";

        if (current == _lastEquippedWeaponName) return; // nothing changed
        _lastEquippedWeaponName = current;

        if (weapon != null)
        {
            if (slots.Length > 0 && !slot1ManualOverride) slots[0].reference = PickBestSkillName(weapon.slot1Options);
            if (slots.Length > 1) slots[1].reference = PickBestSkillName(weapon.slot2Options);
            if (slots.Length > 2) slots[2].reference = PickBestSkillName(weapon.slot3Options);
        }
        else
        {
            if (slots.Length > 0)
            {
                // When unarmed, auto-fill slot 1 with the configured ScriptableSkill
                string unarmedRef = unarmedDefaultSkill != null ? unarmedDefaultSkill.name : "";
                if (!slot1ManualOverride && !string.IsNullOrWhiteSpace(unarmedRef))
                    slots[0].reference = unarmedRef;
            }
            if (slots.Length > 1) slots[1].reference = "";
            if (slots.Length > 2) slots[2].reference = "";
        }

        Save();
    }

    WeaponItem GetCurrentlyEquippedWeapon()
    {
        for (int i = 0; i < equipment.slots.Count; ++i) // SyncList -> Count
        {
            ItemSlot s = equipment.slots[i];
            if (s.amount > 0 && s.item.data is WeaponItem wi)
                return wi;
        }
        return null;
    }

    string PickBestSkillName(ScriptableSkill[] options)
    {
        if (options == null || options.Length == 0) return "";

        // 1) Prefer learned AND unlocked
        foreach (var sk in options)
            if (sk != null && skills.HasLearned(sk.name) &&
                skills.IsUnlockedForEquippedWeapon(sk, out _, out _, out _))
                return sk.name;

        // 2) Prefer unlocked even if not learned
        foreach (var sk in options)
            if (sk != null && skills.IsUnlockedForEquippedWeapon(sk, out _, out _, out _))
                return sk.name;

        // 3) Fallback: first non-null
        foreach (var sk in options)
            if (sk != null) return sk.name;

        return "";
    }

    // ----------------------------
    // EXISTING DRAG/DROP HANDLERS
    // (left unchanged as requested)
    // ----------------------------
    void OnDragAndDrop_InventorySlot_SkillbarSlot(int[] slotIndices)
    {
        slots[slotIndices[1]].reference = inventory.slots[slotIndices[0]].item.name;
        if (slotIndices[1] == 0) { slot1ManualOverride = true; Save(); }
    }

    void OnDragAndDrop_EquipmentSlot_SkillbarSlot(int[] slotIndices)
    {
        slots[slotIndices[1]].reference = equipment.slots[slotIndices[0]].item.name;
        if (slotIndices[1] == 0) { slot1ManualOverride = true; Save(); }
    }

    void OnDragAndDrop_SkillsSlot_SkillbarSlot(int[] slotIndices)
    {
        slots[slotIndices[1]].reference = skills.skills[slotIndices[0]].name;
        if (slotIndices[1] == 0) { slot1ManualOverride = true; Save(); }
    }

    void OnDragAndDrop_SkillbarSlot_SkillbarSlot(int[] slotIndices)
    {
        string temp = slots[slotIndices[0]].reference;
        slots[slotIndices[0]].reference = slots[slotIndices[1]].reference;
        slots[slotIndices[1]].reference = temp;
        if (slotIndices[0] == 0 || slotIndices[1] == 0) { slot1ManualOverride = true; Save(); }
    }

    void OnDragAndClear_SkillbarSlot(int slotIndex)
    {
        slots[slotIndex].reference = "";
        if (slotIndex == 0) { slot1ManualOverride = false; Save(); }
    }
}
