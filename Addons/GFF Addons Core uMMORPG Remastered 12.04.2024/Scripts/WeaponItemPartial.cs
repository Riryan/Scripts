using GFFAddons;
using UnityEngine;

public enum HandsUsed { one, two };

public partial class WeaponItem
{
    [Header("GFF Weapon Type")]

    public WeaponType weaponType;
    public HandsUsed handsUsed;
    [Header("Animation")]
    [Tooltip("Animator STATE name to play for this weapon's basic attack")]
    public string defaultAttackTag = "Unarmed_Attack"; // e.g., "1h_Attack", "Bow_Attack"

}