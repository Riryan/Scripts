using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public partial class UICharacterCreation : MonoBehaviour
{
    public NetworkManagerMMO manager; 
    public GameObject panel;
    public InputField nameInput;
    public Dropdown classDropdown;
    public Toggle gameMasterToggle;
    public Button createButton;
    public Button cancelButton;

    void Update()
    {
        
        if (panel.activeSelf)
        {
            
            if (manager.state == NetworkState.Lobby)
            {
                Show();

                
                classDropdown.options = manager.playerClasses.Select(
                    p => new Dropdown.OptionData(p.name)
                ).ToList();

                
                
                
                gameMasterToggle.gameObject.SetActive(NetworkServer.activeHost);

                
                createButton.interactable = manager.IsAllowedCharacterName(nameInput.text);
                createButton.onClick.SetListener(() => {
                    CharacterCreateMsg message = new CharacterCreateMsg {
                        name = nameInput.text,
                        classIndex = classDropdown.value,
                        gameMaster = gameMasterToggle.isOn
                    };
                    NetworkClient.Send(message);
                    Hide();
                });

                
                cancelButton.onClick.SetListener(() => {
                    nameInput.text = "";
                    Hide();
                });
            }
            else Hide();
        }
    }

    public void Hide() { panel.SetActive(false); }
    public void Show() { panel.SetActive(true); }
    public bool IsVisible() { return panel.activeSelf; }
}
