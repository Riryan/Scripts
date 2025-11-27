using UnityEngine;

// Sets our new hotkeys for item mall.
public partial class UIItemMall : MonoBehaviour
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
            if (settingsVariables.keybindUpdate[17])
            {
                hotKey = settingsVariables.keybindings[17];
                settingsVariables.keybindUpdate[17] = false;
            }
    }
}