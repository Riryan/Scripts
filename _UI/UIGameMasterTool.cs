


using System;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGameMasterTool : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.F10;
    public GameObject panel;

    [Header("Server")]
    public Text connectionsText;
    public Text maxConnectionsText;
    public Text onlinePlayerText;
    public Text uptimeText;
    public Text tickRateText;
    public InputField globalChatInput;
    public Button globalChatSendButton;
    public Button shutdownButton;

    [Header("Character")]
    public Toggle invincibleToggle;
    public InputField levelInput;
    public InputField experienceInput;
    public InputField skillExperienceInput;
    public InputField goldInput;
    public InputField coinsInput;

    [Header("Actions")]
    public InputField playerNameInput;
    public Button warpButton;
    public Button summonButton;
    public Button killButton;
    public Button kickButton;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player != null && player.isGameMaster)
        {
            
            if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                panel.SetActive(!panel.activeSelf);

            
            if (panel.activeSelf)
            {
                
                connectionsText.text = player.gameMasterTool.connections.ToString();
                maxConnectionsText.text = player.gameMasterTool.maxConnections.ToString();
                onlinePlayerText.text = player.gameMasterTool.onlinePlayers.ToString();
                uptimeText.text = Utils.PrettySeconds(player.gameMasterTool.uptime);
                tickRateText.text = player.gameMasterTool.tickRate.ToString();

                
                globalChatSendButton.interactable = !string.IsNullOrWhiteSpace(globalChatInput.text);
                globalChatSendButton.onClick.SetListener(() => {
                    player.gameMasterTool.CmdSendGlobalMessage(globalChatInput.text);
                    globalChatInput.text = string.Empty;
                });

                
                shutdownButton.onClick.SetListener(() => {
                    UIConfirmation.singleton.Show("Are you sure to shut down the server?", () => {
                        player.gameMasterTool.CmdShutdown();
                    });
                });

                
                invincibleToggle.onValueChanged.SetListener((value) => {}); 
                invincibleToggle.isOn = player.combat.invincible;
                invincibleToggle.onValueChanged.SetListener((value) => {
                    player.gameMasterTool.CmdSetCharacterInvincible(value);
                });

                
                if (!levelInput.isFocused)
                    levelInput.text = player.level.current.ToString();

                levelInput.onEndEdit.SetListener((value) => {
                    player.gameMasterTool.CmdSetCharacterLevel(Convert.ToInt32(value));
                });

                
                if (!experienceInput.isFocused)
                    experienceInput.text = player.experience.current.ToString();

                experienceInput.onEndEdit.SetListener((value) => {
                    player.gameMasterTool.CmdSetCharacterExperience(Convert.ToInt64(value));
                });

                
                if (!skillExperienceInput.isFocused)
                    skillExperienceInput.text = ((PlayerSkills)player.skills).skillExperience.ToString();

                skillExperienceInput.onEndEdit.SetListener((value) => {
                    player.gameMasterTool.CmdSetCharacterSkillExperience(Convert.ToInt64(value));
                });

                
                if (!goldInput.isFocused)
                    goldInput.text = player.gold.ToString();

                goldInput.onEndEdit.SetListener((value) => {
                    player.gameMasterTool.CmdSetCharacterGold(Convert.ToInt64(value));
                });

                
                if (!coinsInput.isFocused)
                    coinsInput.text = player.itemMall.coins.ToString();
                coinsInput.onEndEdit.SetListener((value) => {
                    player.gameMasterTool.CmdSetCharacterCoins(Convert.ToInt64(value));
                });

                
                
                warpButton.interactable = !string.IsNullOrWhiteSpace(playerNameInput.text);
                warpButton.onClick.SetListener(() => {
                    player.gameMasterTool.CmdWarp(playerNameInput.text);
                });

                
                summonButton.interactable = !string.IsNullOrWhiteSpace(playerNameInput.text);
                summonButton.onClick.SetListener(() => {
                    player.gameMasterTool.CmdSummon(playerNameInput.text);
                });

                
                killButton.interactable = !string.IsNullOrWhiteSpace(playerNameInput.text);
                killButton.onClick.SetListener(() => {
                    player.gameMasterTool.CmdKill(playerNameInput.text);
                });

                
                kickButton.interactable = !string.IsNullOrWhiteSpace(playerNameInput.text);
                kickButton.onClick.SetListener(() => {
                    player.gameMasterTool.CmdKick(playerNameInput.text);
                });
            }
        }
        else panel.SetActive(false);
    }
}
