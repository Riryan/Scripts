using UnityEngine;
[CreateAssetMenu(menuName = "ADDON/Templates/Terms And Conditions", order = 998)]
public class Tmpl_TermsAndConditions : ScriptableObject
{
    [Header("[-=-=-[ Terms And Conditions ]-=-=-]")]
    public string version;
    [TextArea(1, 50)] public string termsAndCondition;
}
