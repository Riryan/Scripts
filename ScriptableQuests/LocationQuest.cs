











using UnityEngine;
using System.Text;

[CreateAssetMenu(menuName="uMMORPG Quest/Location Quest", order=999)]
public class LocationQuest : ScriptableQuest
{
    
    public override void OnLocation(Player player, int questIndex, Collider location)
    {
        
        
        if (location.name == name)
        {
            Quest quest = player.quests.quests[questIndex];
            quest.progress = 1;
            player.quests.quests[questIndex] = quest;
        }
    }

    
    public override bool IsFulfilled(Player player, Quest quest)
    {
        return quest.progress == 1;
    }

    
    public override string ToolTip(Player player, Quest quest)
    {
        
        
        StringBuilder tip = new StringBuilder(base.ToolTip(player, quest));
        tip.Replace("{LOCATIONSTATUS}", quest.progress == 0 ? "Pending" : "Done");
        return tip.ToString();
    }
}
