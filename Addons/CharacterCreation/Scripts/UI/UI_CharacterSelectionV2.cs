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

    // prevents re-applying customization every frame
    int lastAppliedSelection = -1;

    void Update()
    {
        if (manager.state != NetworkState.Lobby || uiCharacterCreation.IsVisible())
        {
            panel.SetActive(false);
            return;
        }

        panel.SetActive(true);

        if (manager.charactersAvailableMsg.characters == null)
            return;

        CharactersAvailableMsg.CharacterPreview[] characters =
            manager.charactersAvailableMsg.characters;

        UIUtils.BalancePrefabs(
            slotCharacter.gameObject,
            characters.Length,
            content
        );

        manager.selection =
            (manager.selection >= 0 && characters.Length > 0)
                ? manager.selection
                : 0;

        for (int c = 0; c < characters.Length; c++)
        {
            int currentID = c;

            UI_CharacterSlotV2 slot =
                content.GetChild(currentID)
                .GetComponent<UI_CharacterSlotV2>();

            slot.characterName.text   = characters[currentID].name;
            slot.characterLevel.text  = "";
            slot.characterClasse.text = characters[currentID].className;
            slot.isGM.SetActive(characters[currentID].isGameMaster);

            slot.button.interactable = currentID != manager.selection;
            slot.button.onClick.SetListener(() =>
            {
                manager.selection = currentID;
            });

            GameObject previewRoot = manager.selectionLocations[currentID].gameObject;
            previewRoot.SetActive(manager.selection == currentID);

            Player player =
                previewRoot.GetComponentInChildren<Player>();

            if (player != null)
            {
// apply customization ONCE when selection changes
if (manager.selection == currentID &&
    lastAppliedSelection != currentID)
{
    PlayerCustomizationVisuals visuals =
        player.GetComponent<PlayerCustomizationVisuals>();

    if (visuals != null)
    {
        visuals.Apply(characters[currentID].customization);
        lastAppliedSelection = currentID;
    }
}


                if (useSelectionAnimation && !string.IsNullOrEmpty(boolNameAnimation))
                    player.animator.SetBool(boolNameAnimation, true);

                player.nameOverlay.gameObject.SetActive(false);

                if (player.portraitIcon != null)
                    slot.characterImage.sprite = player.portraitIcon;
            }

            curid = manager.selection;
        }

        // START
        startButton.gameObject.SetActive(manager.selection != -1);
        startButton.onClick.SetListener(() =>
        {
            NetworkClient.Ready();
#if _iMMOLOBBY
            Tools_UI_Tools.FadeOutScreen(false);
#endif
            NetworkClient.Send(
                new CharacterSelectMsg { index = manager.selection }
            );
            manager.ClearPreviews();
            panel.SetActive(false);
        });

        // DELETE
        deleteButton.gameObject.SetActive(manager.selection != -1);
        deleteButton.onClick.SetListener(() =>
        {
            uiConfirmation.Show(
                "Do you really want to delete <b>" +
                characters[manager.selection].name + "</b>?",
                () =>
                {
                    NetworkClient.Send(
                        new CharacterDeleteMsg
                        {
                            index = manager.selection
                        }
                    );
                }
            );
        });

        // CREATE
        createButton.interactable =
            characters.Length < manager.characterLimit;

        createButton.onClick.SetListener(() =>
        {
            panel.SetActive(false);
            uiCharacterCreation.Show();
        });

        // QUIT
        quitButton.onClick.SetListener(() =>
        {
            NetworkManagerMMO.Quit();
        });
    }

    public void MoveCameraToPlayer(Vector3 playerpos)
    {
        mycamera.position = playerpos - new Vector3(-2, -2, 0);
    }
}
