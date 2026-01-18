using UnityEngine;

namespace uMMORPG
{
    [DisallowMultipleComponent]
    public abstract class Equipment : ItemContainer, IHealthBonus, IManaBonus, ICombatBonus
    {
        public int GetHealthBonus(int baseHealth)
        {
            int bonus = 0;
            foreach (ItemSlot slot in slots)
                if (slot.amount > 0 && slot.item.CheckDurability())
                    bonus += ((EquipmentItem)slot.item.data).healthBonus;
            return bonus;
        }

        public int GetHealthRecoveryBonus() => 0;

        public int GetManaBonus(int baseMana)
        {
            int bonus = 0;
            foreach (ItemSlot slot in slots)
                if (slot.amount > 0 && slot.item.CheckDurability())
                    bonus += ((EquipmentItem)slot.item.data).manaBonus;
            return bonus;
        }

        public int GetManaRecoveryBonus() => 0;

        public int GetDamageBonus()
        {
            int bonus = 0;
            foreach (ItemSlot slot in slots)
                if (slot.amount > 0 && slot.item.CheckDurability())
                    bonus += ((EquipmentItem)slot.item.data).damageBonus;
            return bonus;
        }

        public int GetDefenseBonus()
        {
            int bonus = 0;
            foreach (ItemSlot slot in slots)
                if (slot.amount > 0 && slot.item.CheckDurability())
                    bonus += ((EquipmentItem)slot.item.data).defenseBonus;
            return bonus;
        }

        public float GetCriticalChanceBonus()
        {
            float bonus = 0;
            foreach (ItemSlot slot in slots)
                if (slot.amount > 0 && slot.item.CheckDurability())
                    bonus += ((EquipmentItem)slot.item.data).criticalChanceBonus;
            return bonus;
        }

        public float GetBlockChanceBonus()
        {
            float bonus = 0;
            foreach (ItemSlot slot in slots)
                if (slot.amount > 0 && slot.item.CheckDurability())
                    bonus += ((EquipmentItem)slot.item.data).blockChanceBonus;
            return bonus;
        }

        public int GetEquippedWeaponIndex()
        {
            for (int i = 0; i < slots.Count; ++i)
            {
                ItemSlot slot = slots[i];
                if (slot.amount > 0 && slot.item.data is WeaponItem)
                    return i;
            }
            return -1;
        }

        public string GetEquippedWeaponCategory()
        {
            int index = GetEquippedWeaponIndex();
            return index != -1 ? ((WeaponItem)slots[index].item.data).category : "";
        }
    }
}