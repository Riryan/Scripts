














using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class ScriptableQuest : ScriptableObject
{
    [Header("General")]
    [SerializeField, TextArea(1, 30)] protected string toolTip; 

    [Header("Requirements")]
    public int requiredLevel; 
    public ScriptableQuest predecessor; 

    [Header("Rewards")]
    public long rewardGold;
    public long rewardExperience;
    public ScriptableItem rewardItem;

    
    public virtual void OnKilled(Player player, int questIndex, Entity victim) {}
    public virtual void OnLocation(Player player, int questIndex, Collider location) {}

    
    
    
    public abstract bool IsFulfilled(Player player, Quest quest);

    
    
    public virtual void OnCompleted(Player player, Quest quest) {}

    
    
    
    
    
    
    
    
    
    
    













    public virtual string ToolTip(Player player, Quest quest)
    {
        
        StringBuilder tip = new StringBuilder(toolTip);
        tip.Replace("{NAME}", name);
        tip.Replace("{REWARDGOLD}", rewardGold.ToString());
        tip.Replace("{REWARDEXPERIENCE}", rewardExperience.ToString());
        tip.Replace("{REWARDITEM}", rewardItem != null ? rewardItem.name : "");
        return tip.ToString();
    }

    
    
    
    
    
    
    static Dictionary<int, ScriptableQuest> cache;
    public static Dictionary<int, ScriptableQuest> All
    {
        get
        {
            
            if (cache == null)
            {
                
                ScriptableQuest[] quests = Resources.LoadAll<ScriptableQuest>("");

                
                List<string> duplicates = quests.ToList().FindDuplicates(quest => quest.name);
                if (duplicates.Count == 0)
                {
                    cache = quests.ToDictionary(quest => quest.name.GetStableHashCode(), quest => quest);
                }
                else
                {
                    foreach (string duplicate in duplicates)
                        Debug.LogError("Resources folder contains multiple ScriptableQuests with the name " + duplicate + ". If you are using subfolders like 'Warrior/BeginnerQuest' and 'Archer/BeginnerQuest', then rename them to 'Warrior/(Warrior)BeginnerQuest' and 'Archer/(Archer)BeginnerQuest' instead.");
                }
            }
            return cache;
        }
    }
}
