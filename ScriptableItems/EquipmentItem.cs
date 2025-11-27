using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName="uMMORPG Item/Equipment", order=999)]
public partial class EquipmentItem : UsableItem
{
    [Header("Equipment")]
    public string category;
    public int healthBonus;
    public int manaBonus;
    public int damageBonus;
    public int defenseBonus;
    [Range(0, 1)] public float blockChanceBonus;
    [Range(0, 1)] public float criticalChanceBonus;
    public GameObject modelPrefab;
    [Header("Appearance Override (Optional)")]
    [Tooltip("If >= 0, this item temporarily sets the player's customization option for the given type while equipped. On unequip, that type reverts to index 0.")]
    public int overrideCustomizationIndex = -1; // -1 = no override

    [Tooltip("Which customization type to affect (Torso/Jackets, Legs/Pants, Hands/Gloves, Feet/Boots, Head/Helmet, Shoulders). Used only if overrideCustomizationIndex >= 0.")]
    public GFFAddons.EquipmentItemType targetCustomizationType = GFFAddons.EquipmentItemType.Jackets;

    [Tooltip("Optional list of customization types to hide while this item is equipped (e.g., hide Hair under a Helmet).")]
    public GFFAddons.EquipmentItemType[] hideWhileEquipped;
    [Header("Animation Override (Optional)")]
    [Tooltip("Animator int parameter to control idle variants. Leave empty to use default 'IdleStyle'.")]
    public string idleParam = "IdleStyle";

    [Tooltip("Value for the idle parameter while this item is equipped. -1 = no override.")]
    public int idleParamValue = -1;

    [Tooltip("Higher priority wins when multiple equipped items set the idle. If equal, lowest slot index wins.")]
    [Range(0, 255)] public int idlePriority = 0;
    
    public override bool CanUse(Player player, int inventoryIndex)
    {
        return FindEquipableSlotFor(player, inventoryIndex) != -1;
    }

    
    public bool CanEquip(Player player, int inventoryIndex, int equipmentIndex)
    {
        EquipmentInfo slotInfo = ((PlayerEquipment)player.equipment).slotInfo[equipmentIndex];
        string requiredCategory = slotInfo.requiredCategory;
        return base.CanUse(player, inventoryIndex) &&
               requiredCategory != "" &&
               category.StartsWith(requiredCategory);
    }

    int FindEquipableSlotFor(Player player, int inventoryIndex)
    {
        for (int i = 0; i < player.equipment.slots.Count; ++i)
            if (CanEquip(player, inventoryIndex, i))
                return i;
        return -1;
    }

    public override void Use(Player player, int inventoryIndex)
    {
        
        base.Use(player, inventoryIndex);

        
        int equipmentIndex = FindEquipableSlotFor(player, inventoryIndex);
        if (equipmentIndex != -1)
        {
            ItemSlot inventorySlot = player.inventory.slots[inventoryIndex];
            ItemSlot equipmentSlot = player.equipment.slots[equipmentIndex];

            
            
            if (inventorySlot.amount > 0 && equipmentSlot.amount > 0 &&
                inventorySlot.item.Equals(equipmentSlot.item))
            {
                ((PlayerEquipment)player.equipment).MergeInventoryEquip(inventoryIndex, equipmentIndex);
            }
            
            else
            {
                ((PlayerEquipment)player.equipment).SwapInventoryEquip(inventoryIndex, equipmentIndex);
            }
        }
    }

    
    public override string ToolTip()
    {
        StringBuilder tip = new StringBuilder(base.ToolTip());
        tip.Replace("{CATEGORY}", category);
        tip.Replace("{DAMAGEBONUS}", damageBonus.ToString());
        tip.Replace("{DEFENSEBONUS}", defenseBonus.ToString());
        tip.Replace("{HEALTHBONUS}", healthBonus.ToString());
        tip.Replace("{MANABONUS}", manaBonus.ToString());
        tip.Replace("{BLOCKCHANCEBONUS}", Mathf.RoundToInt(blockChanceBonus * 100).ToString());
        tip.Replace("{CRITICALCHANCEBONUS}", Mathf.RoundToInt(criticalChanceBonus * 100).ToString());
        Utils.InvokeMany(typeof(EquipmentItem), this, "ToolTip_", tip);
        return tip.ToString();
    }
}
