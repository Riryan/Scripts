using UnityEngine;

// Sets our new hotkeys for quests.
public partial class UIQuests : MonoBehaviour
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
            if (settingsVariables.keybindUpdate[20])
            {
                hotKey = settingsVariables.keybindings[20];
                settingsVariables.keybindUpdate[20] = false;
            }
    }
}