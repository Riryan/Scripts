using UnityEngine;
using Mirror;

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class PlayerQuests : NetworkBehaviour
{
    [Header("Components")]
    public Player player;
    public PlayerInventory inventory;

    [Header("Quests")] 
    public int activeQuestLimit = 10;
    public readonly SyncList<Quest> quests = new SyncList<Quest>();

    
    public int GetIndexByName(string questName)
    {
        
        for (int i = 0; i < quests.Count; ++i)
            if (quests[i].name == questName)
                return i;
        return -1;
    }

    
    public bool HasCompleted(string questName)
    {
        
        foreach (Quest quest in quests)
            if (quest.name == questName && quest.completed)
                return true;
        return false;
    }

    
    public int CountIncomplete()
    {
        int count = 0;
        foreach (Quest quest in quests)
            if (!quest.completed)
                ++count;
        return count;
    }

    
    public bool HasActive(string questName)
    {
        
        foreach (Quest quest in quests)
            if (quest.name == questName && !quest.completed)
                return true;
        return false;
    }

    
    
    
    public bool CanAccept(ScriptableQuest quest)
    {
        
        
        
        
        return CountIncomplete() < activeQuestLimit &&
               player.level.current >= quest.requiredLevel &&  
               GetIndexByName(quest.name) == -1 &&     
               (quest.predecessor == null || HasCompleted(quest.predecessor.name));
    }

    [Command]
    public void CmdAccept(int npcQuestIndex)
    {
        
        
        if (player.state == "IDLE" &&
            player.target != null &&
            player.target.health.current > 0 &&
            player.target is Npc npc &&
            0 <= npcQuestIndex && npcQuestIndex < npc.quests.quests.Length &&
            Utils.ClosestDistance(player, npc) <= player.interactionRange)
        {
            ScriptableQuestOffer npcQuest = npc.quests.quests[npcQuestIndex];
            if (npcQuest.acceptHere && CanAccept(npcQuest.quest))
                quests.Add(new Quest(npcQuest.quest));
        }
    }

    
    public bool CanComplete(string questName)
    {
        
        int index = GetIndexByName(questName);
        if (index != -1 && !quests[index].completed)
        {
            
            Quest quest = quests[index];
            if(quest.IsFulfilled(player))
            {
                
                return quest.rewardItem == null ||
                       inventory.CanAdd(new Item(quest.rewardItem), 1);
            }
        }
        return false;
    }

    [Command]
    public void CmdComplete(int npcQuestIndex)
    {
        
        
        if (player.state == "IDLE" &&
            player.target != null &&
            player.target.health.current > 0 &&
            player.target is Npc npc &&
            0 <= npcQuestIndex && npcQuestIndex < npc.quests.quests.Length &&
            Utils.ClosestDistance(player, npc) <= player.interactionRange)
        {
            ScriptableQuestOffer npcQuest = npc.quests.quests[npcQuestIndex];
            if (npcQuest.completeHere)
            {
                int index = GetIndexByName(npcQuest.quest.name);
                if (index != -1)
                {
                    
                    Quest quest = quests[index];
                    if (CanComplete(quest.name))
                    {
                        
                        
                        quest.OnCompleted(player);

                        
                        player.gold += quest.rewardGold;
                        player.experience.current += quest.rewardExperience;
                        if (quest.rewardItem != null)
                            inventory.Add(new Item(quest.rewardItem), 1);

                        
                        quest.completed = true;
                        quests[index] = quest;
                    }
                }
            }
        }
    }

    
    [Server]
    public void OnKilledEnemy(Entity victim)
    {
        
        for (int i = 0; i < quests.Count; ++i)
            if (!quests[i].completed)
                quests[i].OnKilled(player, i, victim);
    }

    
    [ServerCallback]
    void OnTriggerEnter(Collider col)
    {
        
        
        if (col.CompareTag("QuestLocation"))
        {
            for (int i = 0; i < quests.Count; ++i)
                if (!quests[i].completed)
                    quests[i].OnLocation(player, i, col);
        }
    }
}
