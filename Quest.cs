





using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public partial struct Quest
{
    
    
    
    public int hash;

    
    
    
    
    
    
    
    public int progress;

    
    public bool completed;

    
    public Quest(ScriptableQuest data)
    {
        hash = data.name.GetStableHashCode();
        progress = 0;
        completed = false;
    }

    
    public ScriptableQuest data
    {
        get
        {
            
            
            
            
            if (!ScriptableQuest.All.ContainsKey(hash))
                throw new KeyNotFoundException("There is no ScriptableQuest with hash=" + hash + ". Make sure that all ScriptableQuests are in the Resources folder so they are loaded properly.");
            return ScriptableQuest.All[hash];
        }
    }
    public string name => data.name;
    public int requiredLevel => data.requiredLevel;
    public string predecessor => data.predecessor != null ? data.predecessor.name : "";
    public long rewardGold => data.rewardGold;
    public long rewardExperience => data.rewardExperience;
    public ScriptableItem rewardItem => data.rewardItem;

    
    public void OnKilled(Player player, int questIndex, Entity victim) { data.OnKilled(player, questIndex, victim); }
    public void OnLocation(Player player, int questIndex, Collider location) { data.OnLocation(player, questIndex, location); }

    
    public bool IsFulfilled(Player player) { return data.IsFulfilled(player, this); }
    public void OnCompleted(Player player) { data.OnCompleted(player, this); }

    
    
    
    
    
    
    public string ToolTip(Player player)
    {
        
        
        
        StringBuilder tip = new StringBuilder(data.ToolTip(player, this));
        tip.Replace("{STATUS}", IsFulfilled(player) ? "<i>Complete!</i>" : "");

        
        Utils.InvokeMany(typeof(Quest), this, "ToolTip_", tip);

        return tip.ToString();
    }
}