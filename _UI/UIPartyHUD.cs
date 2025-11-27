using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPartyHUD : MonoBehaviour
{
    public GameObject panel;
    public UIPartyHUDMemberSlot slotPrefab;
    public Transform memberContent;
    
    public AnimationCurve alphaCurve;

    void Update()
    {
        Player player = Player.localPlayer;

        
        if (player != null)
        {
            if (player.party.InParty())
            {
                panel.SetActive(true);
                Party party = player.party.party;

                
                List<string> members = player.party.InParty() ? party.members.Where(m => m != player.name).ToList() : new List<string>();

                
                UIUtils.BalancePrefabs(slotPrefab.gameObject, members.Count, memberContent);

                
                for (int i = 0; i < members.Count; ++i)
                {
                    UIPartyHUDMemberSlot slot = memberContent.GetChild(i).GetComponent<UIPartyHUDMemberSlot>();
                    string memberName = members[i];
                    float distance = Mathf.Infinity;
                    float visRange = player.VisRange();

                    slot.nameText.text = memberName;
                    slot.masterIndicatorText.gameObject.SetActive(party.master == memberName);

                    
                    
                    
                    
                    if (Player.onlinePlayers.ContainsKey(memberName))
                    {
                        Player member = Player.onlinePlayers[memberName];
                        slot.icon.sprite = member.classIcon;
                        slot.healthSlider.value = member.health.Percent();
                        slot.manaSlider.value = member.mana.Percent();
                        slot.backgroundButton.onClick.SetListener(() => {
                            
                            
                            
                            if (member != null)
                                player.CmdSetTarget(member.netIdentity);
                        });

                        
                        distance = Vector3.Distance(player.transform.position, member.transform.position);
                        visRange = member.VisRange(); 
                    }

                    
                    
                    
                    float ratio = visRange > 0 ? distance / visRange : 1f;
                    float alpha = alphaCurve.Evaluate(ratio);

                    
                    Color iconColor = slot.icon.color;
                    iconColor.a = alpha;
                    slot.icon.color = iconColor;

                    
                    foreach (Image image in slot.healthSlider.GetComponentsInChildren<Image>())
                    {
                        Color color = image.color;
                        color.a = alpha;
                        image.color = color;
                    }

                    
                    foreach (Image image in slot.manaSlider.GetComponentsInChildren<Image>())
                    {
                        Color color = image.color;
                        color.a = alpha;
                        image.color = color;
                    }
                }
            }
            else panel.SetActive(false);
        }
        else panel.SetActive(false);
    }
}
