using UnityEngine;

// Sets our new hotkeys for equipment.
public partial class UIEquipment : MonoBehaviour
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
        if (settingsVariables != null && settingsVariables.keybindUpdate[5])
        {
            hotKey = settingsVariables.keybindings[5];
            settingsVariables.keybindUpdate[5] = false;
        }
    }
}