

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class UIQuests : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.L; 
    public GameObject panel;
    public Transform content;
    public UIQuestSlot slotPrefab;

    public string expandPrefix = "[+] ";
    public string hidePrefix = "[-] ";

    void Update()
    {
        Player player = Player.localPlayer;
        if (player != null)
        {
            
            if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                panel.SetActive(!panel.activeSelf);

            
            if (panel.activeSelf)
            {
                
                List<Quest> activeQuests = player.quests.quests.Where(q => !q.completed).ToList();

                
                UIUtils.BalancePrefabs(slotPrefab.gameObject, activeQuests.Count, content);

                
                for (int i = 0; i < activeQuests.Count; ++i)
                {
                    UIQuestSlot slot = content.GetChild(i).GetComponent<UIQuestSlot>();
                    Quest quest = activeQuests[i];

                    
                    GameObject descriptionPanel = slot.descriptionText.gameObject;
                    string prefix = descriptionPanel.activeSelf ? hidePrefix : expandPrefix;
                    slot.nameButton.GetComponentInChildren<Text>().text = prefix + quest.name;
                    slot.nameButton.onClick.SetListener(() => {
                        descriptionPanel.SetActive(!descriptionPanel.activeSelf);
                    });

                    
                    slot.descriptionText.text = quest.ToolTip(player);
                }
            }
        }
        else panel.SetActive(false);
    }
}
