using UnityEngine;

// Sets our new hotkeys for skills.
public partial class UISkills : MonoBehaviour
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
            if (settingsVariables.keybindUpdate[18])
            {
                hotKey = settingsVariables.keybindings[18];
                settingsVariables.keybindUpdate[18] = false;
            }
    }
}