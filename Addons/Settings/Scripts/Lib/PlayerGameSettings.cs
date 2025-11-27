using Mirror;
using UnityEngine;

public partial class PlayerAddonsConfigurator
{

    [HideInInspector] public UI_SettingsVariables settingsVariables;
    [HideInInspector] [SyncVar] public bool isBlockingTrade = false;
    [HideInInspector] [SyncVar] public bool isBlockingParty = false;
    [HideInInspector] [SyncVar] public bool isBlockingGuild = false;


    private void Start_PlayerGameSettings()
    {
       // settingsVariables = FindObjectOfType<UI_SettingsVariables>().GetComponent<UI_SettingsVariables>();
    }


    // If a skillbar hotkey is updated then set its new hotkey.
    private void Update_Hotkeys()
    {
        if (settingsVariables != null)
        {
            if (settingsVariables.keybindUpdate[7] || settingsVariables.keybindUpdate[8] || settingsVariables.keybindUpdate[9] || settingsVariables.keybindUpdate[10] ||
            settingsVariables.keybindUpdate[11] || settingsVariables.keybindUpdate[12] || settingsVariables.keybindUpdate[13] || settingsVariables.keybindUpdate[14] ||
            settingsVariables.keybindUpdate[15] || settingsVariables.keybindUpdate[16])
            {
                for (int i = 0; i < 10; i++)
                {
                  //  player.skillbar.slots[i].hotKey = settingsVariables.keybindings[(i + 1) + 6];
                   // settingsVariables.keybindUpdate[(i + 1) + 6] = false;
                }
            }
        }
    }

    #region Commands

    [Command]
    public void CmdBlockPartyInvite(bool block)
    {
        isBlockingParty = block;
    }

    [Command]
    public void CmdBlockGuildInvite(bool block)
    {
        isBlockingGuild = block;
    }

    [Command]
    public void CmdBlockTradeRequest(bool block)
    {
        isBlockingTrade = block;
    }

    #endregion Commands
}
