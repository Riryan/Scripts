using UnityEngine;

// Sets our new hotkeys for party.
public partial class UIParty : MonoBehaviour
{
    private UI_SettingsVariables settingsVariables;
    
    // Grabs our settings variables.
    private void Start()
    {
        settingsVariables = FindObjectOfType<UI_SettingsVariables>().GetComponent<UI_SettingsVariables>();
    }
    /*
    // Set our hotkey based on the players selection.
    private void FixedUpdate()
    {
        if (settingsVariables != null)
            if (settingsVariables.keybindUpdate[4])
            {
                hotKey = settingsVariables.keybindings[4];
                settingsVariables.keybindUpdate[4] = false;
            }
    }*/
}