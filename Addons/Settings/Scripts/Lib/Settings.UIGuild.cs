using UnityEngine;

// Sets our new hotkeys for guild.
public partial class UIGuild : MonoBehaviour
{
    private UI_SettingsVariables settingsVariables;

    // Grabs our settings variables.
    private void Start()
    {
        settingsVariables = FindObjectOfType<UI_SettingsVariables>().GetComponent<UI_SettingsVariables>();
    }

    // Set our hotkey based on the players selection.
    private void FixedUpdate()
    {
        if (settingsVariables != null)
            if (settingsVariables.keybindUpdate[19])
            {
                hotKey = settingsVariables.keybindings[19];
                settingsVariables.keybindUpdate[19] = false;
            }
    }
}