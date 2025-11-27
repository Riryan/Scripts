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

    void Update()
    {
        Player player = Player.localPlayer;
        if (player != null)
        {
            Entity target = player.nextTarget ?? player.target;
            if (target != null && target != player)
            {
                float distance = Utils.ClosestDistance(player, target);

                // main panel
                panel.SetActive(true);
                healthSlider.value = target.health.Percent();
                nameText.text = target.name;

                // buffs
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

                // trade button (player targets only)
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

                // guild invite button (player targets only, and only if in guild)
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

                // party invite button (player targets only)
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
                /*
                // --- E-key interaction prompt ---
                if (interactPromptText != null)
                {
                    // only show when the player can normally interact (same states as E handler)
                    if (player.state == "IDLE" ||
                        player.state == "MOVING" ||
                        player.state == "CASTING" ||
                        player.state == "STUNNED")
                    {
                        interactPromptText.gameObject.SetActive(true);
                        interactPromptText.text =
                            "Press " + player.interactKey.ToString() + " to interact";
                    }
                    else
                    {
                        interactPromptText.gameObject.SetActive(false);
                    }
                }*/
            }
           // else
            {
                // no valid target
                panel.SetActive(false);
              //  if (interactPromptText != null)
               //     interactPromptText.gameObject.SetActive(false);
            }
        }
        else panel.SetActive(false);
    }
}
