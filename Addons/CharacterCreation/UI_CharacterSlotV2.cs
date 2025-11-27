using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CharacterSlotV2 : MonoBehaviour
{
    public Image characterImage;

    // TMP version
    public TMP_Text characterName;

    // Optional legacy Text version
    public Text characterNameText;

    public TMP_Text characterLevel;
    public TMP_Text characterClasse;
    public Button button;
    public GameObject isGM;

    public void SetCharacterName(string playerName)
    {
        if (characterName != null)
            characterName.text = playerName;

        if (characterNameText != null)
            characterNameText.text = playerName;
    }
}
