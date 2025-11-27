

using UnityEngine;
using UnityEngine.UI;

public partial class UIPartyInvite : MonoBehaviour
{
    public GameObject panel;
    public Text nameText;
    public Button acceptButton;
    public Button declineButton;

    void Update()
    {
        Player player = Player.localPlayer;

        if (player != null)
        {
            if (player.party.inviteFrom != "")
            {
                panel.SetActive(true);
                nameText.text = player.party.inviteFrom;
                acceptButton.onClick.SetListener(() => {
                    player.party.CmdAcceptInvite();
                });
                declineButton.onClick.SetListener(() => {
                    player.party.CmdDeclineInvite();
                });
            }
            else panel.SetActive(false);
        }
        else panel.SetActive(false); 
    }
}
