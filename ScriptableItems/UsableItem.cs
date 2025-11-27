
using System.Text;
using UnityEngine;

public abstract partial class UsableItem : ScriptableItem
{
    [Header("Usage")]
    public int minLevel; 

    [Header("Cooldown")]
    public float cooldown; 
    [Tooltip("Cooldown category can be used if different potion items should share the same cooldown. Cooldown applies only to this item name if empty.")]
#pragma warning disable CS0649 
    [SerializeField] string _cooldownCategory; 
#pragma warning restore CS0649 
    public string cooldownCategory =>
        
        string.IsNullOrWhiteSpace(_cooldownCategory) ? name : _cooldownCategory;

    
    
    public virtual bool CanUse(Player player, int inventoryIndex)
    {
        
        
        return player.level.current >= minLevel &&
               player.GetItemCooldown(cooldownCategory) == 0 &&
               player.inventory.slots[inventoryIndex].item.CheckDurability();
    }

    
    public virtual void Use(Player player, int inventoryIndex)
    {
        
        
        if (cooldown > 0)
            player.SetItemCooldown(cooldownCategory, cooldown);
    }

    
    
    public virtual void OnUsed(Player player) {}

    
    public override string ToolTip()
    {
        StringBuilder tip = new StringBuilder(base.ToolTip());
        tip.Replace("{MINLEVEL}", minLevel.ToString());
        return tip.ToString();
    }
}
