using UnityEngine;

#if _iMMOATTRIBUTES

// Sets our new hotkeys for character info.
public partial class UI_CharacterInfoAttributes : MonoBehaviour
{
    private UI_SettingsVariables settingsVariables;

    // Grabs our settings variables.
    private void Start()
    {
        settingsVariables = FindFirstObjectOfType<UI_SettingsVariables>().GetComponent<UI_SettingsVariables>();
    }

    // Set our hotkey based on the players selection.
    private void FixedUpdate()
    {
        if (settingsVariables != null)
            if (settingsVariables.keybindUpdate[21])
            {
                //hotKey = settingsVariables.keybindings[21];
                settingsVariables.keybindUpdate[21] = false;
            }
    }
}

#else

// Sets our new hotkeys for character info.
public partial class UICharacterInfo : MonoBehaviour
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
            if (settingsVariables.keybindUpdate[21])
            {
                hotKey = settingsVariables.keybindings[21];
                settingsVariables.keybindUpdate[21] = false;
            }
    }
}

#endif
