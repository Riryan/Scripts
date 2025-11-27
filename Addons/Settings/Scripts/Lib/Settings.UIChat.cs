using UnityEngine;

// Grabs our dagger settings variables for chat to know if its visible or not.
#if _iMMOCOMPLETECHAT
public partial class UICompleteChat : MonoBehaviour
#else
public partial class UIChat : MonoBehaviour

#endif
{
#if !_iMMOCOMPLETECHAT
    private UI_SettingsVariables settingsVariables;
    // Grabs our settings variables.
    private void Start()
    {
        settingsVariables = FindObjectOfType<UI_SettingsVariables>().GetComponent<UI_SettingsVariables>();
    }
#endif
}