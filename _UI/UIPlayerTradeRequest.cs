

using UnityEngine;
using UnityEngine.UI;

public partial class UIPlayerTradeRequest : MonoBehaviour
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
            
            if (player.trading.requestFrom != "" && player.state != "TRADING")
            {
                panel.SetActive(true);
                nameText.text = player.trading.requestFrom;
                acceptButton.onClick.SetListener(() => {
                    player.trading.CmdAcceptRequest();
                });
                declineButton.onClick.SetListener(() => {
                    player.trading.CmdDeclineRequest();
                });
            }
            else panel.SetActive(false);
        }
        else panel.SetActive(false);
    }
}
