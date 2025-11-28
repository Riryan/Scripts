using TMPro;
using UnityEngine;

// UI TERMS AND CONDITIONS
public partial class UI_TermsAndConditions : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text termsVersionText;
    public TMP_Text termsAndConditionText;

    public Tmpl_TermsAndConditions termsAndConditions;


    // -----------------------------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------------------------
    private void Start()
    {

        if(PlayerPrefs.HasKey("TermsAndConditions") && PlayerPrefs.GetString("TermsAndConditions") == termsAndConditions.version)
        {
            Inactivate();
        }
        else
        {
            termsVersionText.text = "v."+termsAndConditions.version;
            termsAndConditionText.text = termsAndConditions.termsAndCondition;
            panel.SetActive(true);
        }
    }

    // -----------------------------------------------------------------------------------
    // OnClickAccept
    // -----------------------------------------------------------------------------------
    public void OnClickAccept()
    {
        PlayerPrefs.SetString("TermsAndConditions", termsAndConditions.version);
        PlayerPrefs.Save();
        Inactivate();
    }

    // -----------------------------------------------------------------------------------
    // OnClickDecline
    // -----------------------------------------------------------------------------------
    public void OnClickDecline()
    {
        Application.Quit();
    }

    // -----------------------------------------------------------------------------------
    // Inactivate
    // -----------------------------------------------------------------------------------
    private void Inactivate()
    {
        panel.SetActive(false);
        Destroy(gameObject);
    }
    // -----------------------------------------------------------------------------------
}
