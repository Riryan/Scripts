using uMMORPG;
using UnityEngine;
using UnityEngine.UI;

namespace uMMORPG
{
    public class CharacterInfoExtendedSlot : MonoBehaviour
    {
        public string buttonName;
        public Button button;
        public GameObject panel;

        public string expandPrefix = "[+] ";
        public string hidePrefix = "[-] ";

        public void Update()
        {
            string prefix = panel.activeSelf ? hidePrefix : expandPrefix;
            button.GetComponentInChildren<Text>().text = prefix + buttonName;
            button.onClick.SetListener(() => {
                panel.SetActive(!panel.activeSelf);
            });
        }
    }
}