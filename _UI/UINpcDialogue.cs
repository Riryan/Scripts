

using UnityEngine;
using UnityEngine.UI;

public partial class UINpcDialogue : MonoBehaviour
{
    public static UINpcDialogue singleton;
    public GameObject panel;
    public Text welcomeText;
    public Transform offerPanel;
    public GameObject offerButtonPrefab;

    public UINpcDialogue()
    {
        
        
        if (singleton == null) singleton = this;
    }

    void Update()
    {
        Player player = Player.localPlayer;

        
        if (player != null &&
            panel.activeSelf &&
            player.target != null &&
            player.target is Npc npc &&
            Utils.ClosestDistance(player, player.target) <= player.interactionRange)
        {
            
            welcomeText.text = npc.welcome;

            
            int validOffers = 0;
            foreach (NpcOffer offer in npc.offers)
                if (offer.HasOffer(player))
                    ++validOffers;

            
            UIUtils.BalancePrefabs(offerButtonPrefab, validOffers, offerPanel);

            
            int index = 0;
            foreach (NpcOffer offer in npc.offers)
            {
                if (offer.HasOffer(player))
                {
                    Button button = offerPanel.GetChild(index).GetComponent<Button>();
                    button.GetComponentInChildren<Text>().text = offer.GetOfferName();
                    button.onClick.SetListener(() => {
                        offer.OnSelect(player);
                    });
                    ++index;
                }
            }
        }
        else panel.SetActive(false);
    }

    public void Show() { panel.SetActive(true); }
}
