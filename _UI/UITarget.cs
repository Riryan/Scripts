using UnityEngine;
using UnityEngine.UI;
using Mirror;

public partial class UITarget : MonoBehaviour
{
    public GameObject panel;
    public Slider healthSlider;
    public Text nameText;
    public Text interactPromptText;
    public Transform buffsPanel;
    public UIBuffSlot buffSlotPrefab;
    public Button tradeButton;
    public Button guildInviteButton;
    public Button partyInviteButton;

    void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);
        if (interactPromptText != null)
            interactPromptText.gameObject.SetActive(false);
        if (tradeButton != null)
            tradeButton.gameObject.SetActive(false);
        if (guildInviteButton != null)
            guildInviteButton.gameObject.SetActive(false);
        if (partyInviteButton != null)
            partyInviteButton.gameObject.SetActive(false);
    }

    void Update()
    {
        Player player = Player.localPlayer;
        if (player == null)
        {
            HidePanel();
            return;
        }

        Entity target = player.nextTarget ?? player.target;
        if (target != null && target != player)
        {
            if (target.health.current <= 0)
            {
                HidePanel();
                return;
            }
            float distance = Utils.ClosestDistance(player, target);
            panel.SetActive(true);
            healthSlider.value = target.health.Percent();
            nameText.text = target.name;

            UIUtils.BalancePrefabs(buffSlotPrefab.gameObject,
                                   target.skills.buffs.Count,
                                   buffsPanel);
            for (int i = 0; i < target.skills.buffs.Count; ++i)
            {
                Buff buff = target.skills.buffs[i];
                UIBuffSlot slot = buffsPanel.GetChild(i).GetComponent<UIBuffSlot>();
                slot.image.color = Color.white;
                slot.image.sprite = buff.image;

                if (slot.tooltip.IsVisible())
                    slot.tooltip.text = buff.ToolTip();

                slot.slider.maxValue = buff.buffTime;
                slot.slider.value = buff.BuffTimeRemaining();
            }

            if (target is Player)
            {
                tradeButton.gameObject.SetActive(true);
                tradeButton.interactable = player.trading.CanStartTradeWith(target);
                tradeButton.onClick.SetListener(() =>
                {
                    player.trading.CmdSendRequest();
                });
            }
            else tradeButton.gameObject.SetActive(false);

            if (target is Player targetPlayer && player.guild.InGuild())
            {
                guildInviteButton.gameObject.SetActive(true);
                guildInviteButton.interactable =
                    !targetPlayer.guild.InGuild() &&
                    player.guild.guild.CanInvite(player.name, target.name) &&
                    NetworkTime.time >= player.nextRiskyActionTime &&
                    distance <= player.interactionRange;

                guildInviteButton.onClick.SetListener(() =>
                {
                    player.guild.CmdInviteTarget();
                });
            }
            else guildInviteButton.gameObject.SetActive(false);

            if (target is Player targetPlayer2)
            {
                partyInviteButton.gameObject.SetActive(true);
                partyInviteButton.interactable =
                    (!player.party.InParty() || !player.party.party.IsFull()) &&
                    !targetPlayer2.party.InParty() &&
                    NetworkTime.time >= player.nextRiskyActionTime &&
                    distance <= player.interactionRange;

                partyInviteButton.onClick.SetListener(() =>
                {
                    player.party.CmdInvite(target.name);
                });
            }
            else partyInviteButton.gameObject.SetActive(false);
            return;
        }
        HidePanel();
    }
}
