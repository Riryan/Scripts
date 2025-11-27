using System;
using UnityEngine;
using UnityEngine.Events;

namespace GFFAddons
{
    public enum ItemType { Armor, Weapon, Potions, Ammo, Resources, Accessories, Pets, Mounts }
    public enum CharacterArmorType { None, Helmet, Shoulders, Upper, Lower, Gloves, Shoes, Shield };
    public enum MountArmorType { None, Shoulders, Helmet, Upper, Lower, Gloves, Shoes };
    public enum WeaponType { None, Knife, Sword, Axe, Mace, Staff, Spear, Bow, Crossbow, Throwing, Shield };
    public enum PotionType { none, health, mana, stamina, pets, mounts };

    public enum BuffType { Normal, Doping, Single, Double, Totem, Exp, Health, Mana, Stamina, Damage, Defense, Crit, Block, Dodge, Accuracy }
    //public enum MonsterType { normal, expert, champions, elite, hero, pitBosses, securityMobs }

    //for gathering addon
    [Serializable] public class ScriptableItemAndRandomAmount
    {
        [HideInInspector] public string name;
        public ScriptableItem item;
        public int amountMin = 1;
        public int amountMax = 1;
        public int weight = 1;
    }

    //[Serializable] public class UnityEventInt : UnityEvent<int> { }
    [Serializable] public class UnityEventDamageType : UnityEvent<DamageType> { }
}