

using UnityEngine;
using UnityEngine.UI;
using Mirror;

public partial class UICharacterSelection : MonoBehaviour
{
    public UICharacterCreation uiCharacterCreation;
    public UIConfirmation uiConfirmation;
    public NetworkManagerMMO manager; 
    public GameObject panel;
    public Button startButton;
    public Button deleteButton;
    public Button createButton;
    public Button quitButton;

    void Update()
    {
        
        if (manager.state == NetworkState.Lobby && !uiCharacterCreation.IsVisible())
        {
            panel.SetActive(true);

            
            if (manager.charactersAvailableMsg.characters != null)
            {
                CharactersAvailableMsg.CharacterPreview[] characters = manager.charactersAvailableMsg.characters;

                
                
                
                
                
                
                
                
                startButton.gameObject.SetActive(manager.selection != -1);
                startButton.onClick.SetListener(() => {
                    
                    
                    NetworkClient.Ready();

                    
                    NetworkClient.Send(new CharacterSelectMsg{ index=manager.selection });

                    
                    manager.ClearPreviews();

                    
                    panel.SetActive(false);
                });

                
                deleteButton.gameObject.SetActive(manager.selection != -1);
                deleteButton.onClick.SetListener(() => {
                    uiConfirmation.Show(
                        "Do you really want to delete <b>" + characters[manager.selection].name + "</b>?",
                        () => { NetworkClient.Send(new CharacterDeleteMsg{ index=manager.selection }); }
                    );
                });

                
                createButton.interactable = characters.Length < manager.characterLimit;
                createButton.onClick.SetListener(() => {
                    panel.SetActive(false);
                    uiCharacterCreation.Show();
                });

                
                quitButton.onClick.SetListener(() => { NetworkManagerMMO.Quit(); });
            }
        }
        else panel.SetActive(false);
    }
}
