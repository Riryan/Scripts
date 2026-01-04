using UnityEngine;
using UnityEngine.UI;
using Mirror;
using uMMORPG;

public partial class UI_CharacterSelectionV2 : MonoBehaviour
{
    [Header("Assign this variable")]
    [Space(20)]
    public UI_CharacterCreation uiCharacterCreation;
    public UIConfirmation uiConfirmation;
    public NetworkManagerMMO manager; // singleton is null until update
    public Transform mycamera;

    [Header("[-=-[ Default Configuration ]-=-]")]
    [SerializeField] bool showConfiguration;
    public UI_CharacterSlotV2 slotCharacter;
    public Transform content;
    public GameObject panel;
    public Button startButton;
    public Button deleteButton;
    public Button createButton;
    public Button quitButton;

    public bool useSelectionAnimation = false;
    public string boolNameAnimation;
    [HideInInspector] public int curid = 0;

    void Update()
    {
        if (manager.state == NetworkState.Lobby && !uiCharacterCreation.IsVisible())
        {
            panel.SetActive(true);

            if (manager.charactersAvailableMsg.characters != null)
            {
                CharactersAvailableMsg.CharacterPreview[] characters = manager.charactersAvailableMsg.characters;
                UIUtils.BalancePrefabs(slotCharacter.gameObject, characters.Length, content);
                manager.selection = (manager.selection >=0 && characters.Length >= 1) ? manager.selection : 0;
                for (int c = 0; c < characters.Length; c++)
                {
                    int currentID = c;
                    
                    Player plyer = manager.selectionLocations[currentID].gameObject.GetComponentInChildren<Player>();
                    UI_CharacterSlotV2 slot = content.GetChild(currentID).GetComponent<UI_CharacterSlotV2>();

                    slot.characterName.text = characters[currentID].name;
                    slot.characterLevel.text = "";
                    slot.characterClasse.text = characters[currentID].className;
                    slot.isGM.SetActive(
                        characters[currentID].isGameMaster
                    );


                    slot.button.interactable = (currentID != manager.selection);
                    slot.button.onClick.SetListener(() =>
                    {
                        manager.selection = currentID;
                    });
                    manager.selectionLocations[currentID].gameObject.SetActive(manager.selection == currentID);
                    if (useSelectionAnimation && boolNameAnimation != "")
                    {
                        if (plyer != null)
                            plyer.animator.SetBool(boolNameAnimation, true);
                    }
                    if(plyer != null) 
                        plyer.nameOverlay.gameObject.SetActive(false);
                    if(plyer && plyer.portraitIcon != null)
                        slot.characterImage.sprite = plyer.portraitIcon;
                    curid = manager.selection;
                }
                startButton.gameObject.SetActive(manager.selection != -1);
                startButton.onClick.SetListener(() => {


                    NetworkClient.Ready();
#if _iMMOLOBBY
                    Tools_UI_Tools.FadeOutScreen(false);
#endif

                    // send CharacterSelect message (need to be ready first!)
                    NetworkClient.Send(new CharacterSelectMsg { index = manager.selection });
                    // clear character selection previews
                    manager.ClearPreviews();
                    // make sure we can't select twice and call AddPlayer twice
                    panel.SetActive(false);

                });

                // delete button
                deleteButton.gameObject.SetActive(manager.selection != -1);
                deleteButton.onClick.SetListener(() => {
                    uiConfirmation.Show(
                        "Do you really want to delete <b>" + characters[manager.selection].name + "</b>?",
                        () => { NetworkClient.Send(new CharacterDeleteMsg { index = manager.selection }); }
                    );
                });

                createButton.interactable = characters.Length < manager.characterLimit;
                createButton.onClick.SetListener(() => {
                    panel.SetActive(false);
                    uiCharacterCreation.Show();
                });

                // quit button
                quitButton.onClick.SetListener(() => { NetworkManagerMMO.Quit(); });
            }
        }
        else panel.SetActive(false);
    }

    public void MoveCameraToPlayer(Vector3 playerpos)
    {
            mycamera.position = playerpos - new Vector3(-2, -2,0); // Ajustez les valeurs selon vos besoins
    }
}
