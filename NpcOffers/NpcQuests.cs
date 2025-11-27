using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;



[Serializable]
public class ScriptableQuestOffer
{
    public ScriptableQuest quest;
    public bool acceptHere = true;
    public bool completeHere = true;
}

public class NpcQuests : NpcOffer
{
    [Header("Text Meshes")]
    public TextMeshPro questOverlay;

    [Header("Quests")]
    public ScriptableQuestOffer[] quests;

    public override bool HasOffer(Player player) =>
        QuestsVisibleFor(player).Count > 0;

    public override string GetOfferName() => "Quests";

    public override void OnSelect(Player player)
    {
        UINpcQuests.singleton.panel.SetActive(true);
        UINpcDialogue.singleton.panel.SetActive(false);
    }

    
    public int GetIndexByName(string questName)
    {
        
        for (int i = 0; i < quests.Length; ++i)
            if (quests[i].quest.name == questName)
                return i;
        return -1;
    }

    
    
    
    
    public List<ScriptableQuest> QuestsVisibleFor(Player player)
    {
        
        List<ScriptableQuest> visibleQuests = new List<ScriptableQuest>();
        foreach (ScriptableQuestOffer entry in quests)
            if (entry.acceptHere && player.quests.CanAccept(entry.quest) ||
                entry.completeHere && player.quests.HasActive(entry.quest.name))
                visibleQuests.Add(entry.quest);
        return visibleQuests;
    }

    public bool CanPlayerCompleteAnyQuestHere(PlayerQuests playerQuests)
    {
        
        foreach (ScriptableQuestOffer entry in quests)
            if (entry.completeHere && playerQuests.CanComplete(entry.quest.name))
                return true;
        return false;
    }

    public bool CanPlayerAcceptAnyQuestHere(PlayerQuests playerQuests)
    {
        
        foreach (ScriptableQuestOffer entry in quests)
            if (entry.acceptHere && playerQuests.CanAccept(entry.quest))
                return true;
        return false;
    }

    void Update()
    {
        
        
        if (isServerOnly) return;

        if (questOverlay != null)
        {
            
            if (Player.localPlayer != null)
            {
                if (CanPlayerCompleteAnyQuestHere(Player.localPlayer.quests))
                    questOverlay.text = "!";
                else if (CanPlayerAcceptAnyQuestHere(Player.localPlayer.quests))
                    questOverlay.text = "?";
                else
                    questOverlay.text = "";
            }
        }
    }
}
