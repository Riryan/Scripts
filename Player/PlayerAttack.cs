using UnityEngine;

namespace uMMORPG
{
    public partial class Player
    {
        /// <summary>
        /// Returns the skill index used for auto-attacks.
        /// Priority:
        /// 1) Equipped weapon attack skill
        /// 2) Default attack (index 0)
        /// </summary>
        public int GetCurrentAttack()
        {
            // Weapon-defined attack
            WeaponItem weapon = GetEquippedOrUnarmedWeapon();
            if (weapon != null && weapon.attackSkill != null)
            {
                for (int i = 0; i < skills.skills.Count; ++i)
                {
                    if (skills.skills[i].data == weapon.attackSkill)
                        return i;
                }
            }

            // Default uMMORPG attack = index 0
            return 0;
        }
public bool HasWeaponForInteraction()
{
    // Real weapon equipped
    if (equipment.GetEquippedWeaponIndex() != -1)
        return true;

    // Unarmed counts as a weapon
    return unarmedWeapon != null;
}
        WeaponItem GetEquippedOrUnarmedWeapon()
        {
            if (equipment != null)
            {
                int index = equipment.GetEquippedWeaponIndex();
                if (index != -1 && equipment.slots[index].amount > 0)
                    return equipment.slots[index].item.data as WeaponItem;
            }

            return unarmedWeapon;
        }
    }
}
