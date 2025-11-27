using UnityEngine;

// Sets our new hotkeys for inventory.
public partial class UIInventory : MonoBehaviour
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
            if (settingsVariables.keybindUpdate[22])
            {
                hotKey = settingsVariables.keybindings[22];
                settingsVariables.keybindUpdate[22] = false;
            }
    }
}