using UnityEngine;
using UnityEngine.UI;

public partial class UIParty : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.P;
    public GameObject panel;
    public Text currentCapacityText;
    public Text maximumCapacityText;
    public UIPartyMemberSlot slotPrefab;
    public Transform memberContent;
    public Toggle experienceShareToggle;
    public Toggle goldShareToggle;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player)
        {
            
            if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                panel.SetActive(!panel.activeSelf);

            
            if (panel.activeSelf)
            {
                Party party = player.party.party;
                int memberCount = party.members != null ? party.members.Length : 0;

                
                currentCapacityText.text = memberCount.ToString();
                maximumCapacityText.text = Party.Capacity.ToString();

                
                UIUtils.BalancePrefabs(slotPrefab.gameObject, memberCount, memberContent);

                
                for (int i = 0; i < memberCount; ++i)
                {
                    UIPartyMemberSlot slot = memberContent.GetChild(i).GetComponent<UIPartyMemberSlot>();
                    string memberName = party.members[i];

                    slot.nameText.text = memberName;
                    slot.masterIndicatorText.gameObject.SetActive(i == 0);

                    
                    
                    
                    
                    
                    

                    
                    
                    if (Player.onlinePlayers.ContainsKey(memberName))
                    {
                        Player member = Player.onlinePlayers[memberName];
                        slot.icon.sprite = member.classIcon;
                        slot.levelText.text = member.level.current.ToString();
                        slot.guildText.text = member.guild.guild.name;
                        slot.healthSlider.value = member.health.Percent();
                        slot.manaSlider.value = member.mana.Percent();
                    }

                    
                    
                    
                    
                    if (memberName == player.name && i == 0)
                    {
                        slot.actionButton.gameObject.SetActive(true);
                        slot.actionButton.GetComponentInChildren<Text>().text = "Dismiss";
                        slot.actionButton.onClick.SetListener(() => {
                            player.party.CmdDismiss();
                        });
                    }
                    else if (memberName == player.name && i > 0)
                    {
                        slot.actionButton.gameObject.SetActive(true);
                        slot.actionButton.GetComponentInChildren<Text>().text = "Leave";
                        slot.actionButton.onClick.SetListener(() => {
                            player.party.CmdLeave();
                        });
                    }
                    else if (party.members[0] == player.name && i > 0)
                    {
                        slot.actionButton.gameObject.SetActive(true);
                        slot.actionButton.GetComponentInChildren<Text>().text = "Kick";
                        slot.actionButton.onClick.SetListener(() => {
                            player.party.CmdKick(memberName);
                        });
                    }
                    else
                    {
                        slot.actionButton.gameObject.SetActive(false);
                    }
                }

                
                experienceShareToggle.interactable = player.party.InParty() && party.members[0] == player.name;
                experienceShareToggle.onValueChanged.SetListener((val) => {}); 
                experienceShareToggle.isOn = party.shareExperience;
                experienceShareToggle.onValueChanged.SetListener((val) => {
                    player.party.CmdSetExperienceShare(val);
                });

                
                goldShareToggle.interactable = player.party.InParty() && party.members[0] == player.name;
                goldShareToggle.onValueChanged.SetListener((val) => {}); 
                goldShareToggle.isOn = party.shareGold;
                goldShareToggle.onValueChanged.SetListener((val) => {
                    player.party.CmdSetGoldShare(val);
                });
            }
        }
        else panel.SetActive(false);
    }
}
