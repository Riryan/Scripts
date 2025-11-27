using UnityEngine;
using UnityEngine.UI;
using Mirror;

public partial class UIGuild : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.G;
    public GameObject panel;
    public Text nameText;
    public Text masterText;
    public Text currentCapacityText;
    public Text maximumCapacityText;
    public InputField noticeInput;
    public Button noticeEditButton;
    public Button noticeSetButton;
    public UIGuildMemberSlot slotPrefab;
    public Transform memberContent;
    public Color onlineColor = Color.cyan;
    public Color offlineColor = Color.gray;
    public Button leaveButton;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player)
        {
            // open/close
            if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                panel.SetActive(!panel.activeSelf);

            if (panel.activeSelf)
            {
                Guild currentGuild = player.guild.guild;
                int memberCount = currentGuild.members != null ? currentGuild.members.Length : 0;

                // header
                nameText.text = currentGuild.name;
                masterText.text = currentGuild.master;
                currentCapacityText.text = memberCount.ToString();
                maximumCapacityText.text = GuildSystem.Capacity.ToString();

                // notice edit gate: CanNotice
                noticeEditButton.interactable = currentGuild.CanNotice(player.name) &&
                                                !noticeInput.interactable;
                noticeEditButton.onClick.SetListener(() => {
                    noticeInput.interactable = true;
                });

                // notice set gate: CanNotice
                noticeSetButton.interactable = currentGuild.CanNotice(player.name) &&
                                               noticeInput.interactable &&
                                               NetworkTime.time >= player.nextRiskyActionTime;
                noticeSetButton.onClick.SetListener(() => {
                    noticeInput.interactable = false;
                    if (noticeInput.text.Length > 0 &&
                        !string.IsNullOrWhiteSpace(noticeInput.text) &&
                        noticeInput.text != currentGuild.notice) {
                        player.guild.CmdSetNotice(noticeInput.text);
                    }
                });

                if (!noticeInput.interactable) noticeInput.text = currentGuild.notice ?? string.Empty;
                noticeInput.characterLimit = GuildSystem.NoticeMaxLength;

                // leave gate (server also enforces via Guild.CanLeave used in GuildSystem)
                leaveButton.interactable = currentGuild.CanLeave(player.name);
                leaveButton.onClick.SetListener(() => {
                    player.guild.CmdLeave();
                });

                // member slots
                UIUtils.BalancePrefabs(slotPrefab.gameObject, memberCount, memberContent);

                for (int i = 0; i < memberCount; ++i)
                {
                    UIGuildMemberSlot slot = memberContent.GetChild(i).GetComponent<UIGuildMemberSlot>();
                    GuildMember member = currentGuild.members[i];

                    // fields
                    slot.onlineStatusImage.color = member.online ? onlineColor : offlineColor;
                    slot.nameText.text = member.name;
                    slot.levelText.text = member.level.ToString();
                    slot.rankText.text = member.rank.ToString();

                    // capture to avoid closure-with-loop-var issue
                    string targetName = member.name;

                    slot.promoteButton.interactable = currentGuild.CanPromote(player.name, targetName);
                    slot.promoteButton.onClick.SetListener(() => {
                        player.guild.CmdPromote(targetName);
                    });

                    slot.demoteButton.interactable = currentGuild.CanDemote(player.name, targetName);
                    slot.demoteButton.onClick.SetListener(() => {
                        player.guild.CmdDemote(targetName);
                    });

                    slot.kickButton.interactable = currentGuild.CanKick(player.name, targetName);
                    slot.kickButton.onClick.SetListener(() => {
                        player.guild.CmdKick(targetName);
                    });
                }
            }
        }
        else panel.SetActive(false);
    }
}
