using UnityEngine;

// Sets our new hotkeys for crafting.
public partial class UICrafting : MonoBehaviour
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
#if _iMMOCRAFTING
        return;
#else
        if (settingsVariables != null &&settingsVariables.keybindUpdate[23])
        {
            hotKey = settingsVariables.keybindings[23];
            settingsVariables.keybindUpdate[23] = false;
        }
#endif
    }
}