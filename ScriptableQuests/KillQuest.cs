


using UnityEngine;
using System.Text;

[CreateAssetMenu(menuName="uMMORPG Quest/Kill Quest", order=999)]
public class KillQuest : ScriptableQuest
{
    [Header("Fulfillment")]
    public Monster killTarget;
    public int killAmount;

    
    public override void OnKilled(Player player, int questIndex, Entity victim)
    {
        
        Quest quest = player.quests.quests[questIndex];
        if (quest.progress < killAmount && victim.name == killTarget.name)
        {
            
            ++quest.progress;
            player.quests.quests[questIndex] = quest;
        }
    }

    
    public override bool IsFulfilled(Player player, Quest quest)
    {
        return quest.progress >= killAmount;
    }

    
    public override string ToolTip(Player player, Quest quest)
    {
        
        
        StringBuilder tip = new StringBuilder(base.ToolTip(player, quest));
        tip.Replace("{KILLTARGET}", killTarget != null ? killTarget.name : "");
        tip.Replace("{KILLAMOUNT}", killAmount.ToString());
        tip.Replace("{KILLED}", quest.progress.ToString());
        return tip.ToString();
    }
}
