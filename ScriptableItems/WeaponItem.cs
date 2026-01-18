using System.Text;
using UnityEngine;

namespace uMMORPG
{
    public enum WeaponType
    {
        None,
        Knife,
        Sword,
        Axe,
        Mace,
        Staff,
        Spear,
        Bow,
        Crossbow,
        Throwing,
        Shield
    }

    public enum HandsUsed
    {
        one,
        two
    }

    [CreateAssetMenu(menuName = "uMMORPG Item/Weapon", order = 999)]
    public class WeaponItem : EquipmentItem
    {
        // =========================
        // CLASSIC / GFF FIELDS
        // =========================

        [Header("Weapon Type")]
        public WeaponType weaponType;
        public HandsUsed handsUsed;

        [Header("Legacy Skill (Modifier / Special Attack)")]
        [Tooltip("Optional skill used as a modifier or special attack (NOT used for basic attack looping)")]
        public ScriptableSkill attackSkill;

        [Header("Ammo")]
        [Tooltip("Null if no ammo is required")]
        public AmmoItem requiredAmmo;

        // =========================
        // WEAPON ACTION (UO STYLE)
        // =========================

        [Header("Weapon Action")]
        [Tooltip("Maximum distance at which this weapon can hit")]
        public float attackRange = 1.5f;

        [Tooltip("Time between attacks in seconds")]
        public float attackInterval = 1.0f;

        [Tooltip("Base damage before any modifiers")]
        public int baseDamage = 10;

        [Tooltip("Whether the character may move while auto-attacking")]
        public bool allowMovementWhileAttacking = true;

        // =========================
        // TOOLTIP
        // =========================

        public override string ToolTip()
        {
            StringBuilder tip = new StringBuilder(base.ToolTip());

            tip.AppendLine($"<b>Weapon Type:</b> {weaponType}");
            tip.AppendLine($"<b>Damage:</b> {baseDamage}");
            tip.AppendLine($"<b>Speed:</b> {attackInterval:0.00}s");
            tip.AppendLine($"<b>Attack Range:</b> {attackRange:0.0}");

            if (requiredAmmo != null)
                tip.AppendLine($"<b>Ammo:</b> {requiredAmmo.name}");

            return tip.ToString();
        }
    }
}
