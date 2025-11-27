using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName="uMMORPG Item/Potion", order=999)]
public class PotionItem : UsableItem
{
    [Header("Potion")]
    public int usageHealth;
    public int usageMana;
    public int usageExperience;
    public int usagePetHealth; 

    
    public override void Use(Player player, int inventoryIndex)
    {
        
        base.Use(player, inventoryIndex);

        
        player.health.current += usageHealth;
        player.mana.current += usageMana;
        player.experience.current += usageExperience;
        if (player.petControl.activePet != null)
            player.petControl.activePet.health.current += usagePetHealth;

        
        ItemSlot slot = player.inventory.slots[inventoryIndex];
        slot.DecreaseAmount(1);
        player.inventory.slots[inventoryIndex] = slot;
    }

    
    public override string ToolTip()
    {
        StringBuilder tip = new StringBuilder(base.ToolTip());
        tip.Replace("{USAGEHEALTH}", usageHealth.ToString());
        tip.Replace("{USAGEMANA}", usageMana.ToString());
        tip.Replace("{USAGEEXPERIENCE}", usageExperience.ToString());
        tip.Replace("{USAGEPETHEALTH}", usagePetHealth.ToString());
        return tip.ToString();
    }
}
