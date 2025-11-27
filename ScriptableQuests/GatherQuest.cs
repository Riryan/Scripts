
using UnityEngine;
using System.Text;

[CreateAssetMenu(menuName="uMMORPG Quest/Gather Quest", order=999)]
public class GatherQuest : ScriptableQuest
{
    [Header("Fulfillment")]
    public ScriptableItem gatherItem;
    public int gatherAmount;

    
    public override bool IsFulfilled(Player player, Quest quest)
    {
        return gatherItem != null &&
               player.inventory.Count(new Item(gatherItem)) >= gatherAmount;
    }

    public override void OnCompleted(Player player, Quest quest)
    {
        
        if (gatherItem != null)
            player.inventory.Remove(new Item(gatherItem), gatherAmount);
    }

    
    public override string ToolTip(Player player, Quest quest)
    {
        
        
        StringBuilder tip = new StringBuilder(base.ToolTip(player, quest));
        tip.Replace("{GATHERAMOUNT}", gatherAmount.ToString());
        if (gatherItem != null)
        {
            int gathered = player.inventory.Count(new Item(gatherItem));
            tip.Replace("{GATHERITEM}", gatherItem.name);
            tip.Replace("{GATHERED}", Mathf.Min(gathered, gatherAmount).ToString());
        }
        return tip.ToString();
    }
}
