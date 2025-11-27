using UnityEngine;
using UnityEngine.UI;

// REMEMBER ME

public partial class UILogin
{
    public Toggle remember;
    public bool savePassword = false;

    // -----------------------------------------------------------------------------------
    // Awake
    // Set our listener to save our account name, grab our account name if remember is checked.
    // -----------------------------------------------------------------------------------
    private void Awake()
    {
        if (remember && remember.isOn && PlayerPrefs.HasKey("Account"))
            accountInput.text = PlayerPrefs.GetString("Account");
        if (remember && savePassword && remember.isOn && PlayerPrefs.HasKey("Password"))
            passwordInput.text = PlayerPrefs.GetString("Password");
    }

    // -----------------------------------------------------------------------------------
    // Save Account
    // Save our account name when login is clicked.
    // -----------------------------------------------------------------------------------
    public void SaveChooseAccount()
    {
        PlayerPrefs.SetString("Account", accountInput.text);
        if(savePassword)
            PlayerPrefs.SetString("Password", passwordInput.text);
    }
}
