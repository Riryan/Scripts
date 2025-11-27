using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace GFFAddons
{
    public class UICharacterSelectionExtended : MonoBehaviour
    {
        [Header("Character Selection UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button quitButton;

        [Header("AAA Character List (from V2)")]
        [SerializeField] private UI_CharacterSlotV2 slotCharacter;
        [SerializeField] private Transform content;
        [SerializeField] private bool useSelectionAnimation = false;
        [SerializeField] private string boolNameAnimation = "";
        [SerializeField] private Transform mycamera;
        [HideInInspector] public int curid = 0;

        [Header("Stats")]
        [SerializeField] private GameObject panelStats;
        [SerializeField] private Text textNameValue;
        [SerializeField] private Text textRace;
        [SerializeField] private Text textRaceValue;
        [SerializeField] private Text textClassValue;
        [SerializeField] private Text textSpecialization;
        [SerializeField] private Text textSpecializationValue;
        [SerializeField] private Text textGender;
        [SerializeField] private Text textGenderValue;
        [SerializeField] private Text textLevelValue;
        [SerializeField] private Text textGoldValue;
        [SerializeField] private Text textLocationValue;

        [Header("GFF GameControl Panel Addon")]
        [SerializeField] private Button buttonGameControl;
        [SerializeField] private GameObject panelGameControl;
        [SerializeField] private GameObject panelPremium;

        [Header("Components")]
        [SerializeField] private NetworkManagerMMO manager; // singleton is null until update
        [SerializeField] private UICharacterCreationExtended characterCreationExtended;
        [SerializeField] private UICharacterCreation uiCharacterCreation;
        [SerializeField] private UIConfirmation uiConfirmation;

        private void Start()
        {
            // create button -> prefer extended creation if present
            if (createButton != null)
            {
                createButton.onClick.SetListener(() =>
                {
                    if (panel != null)
                        panel.SetActive(false);

                    if (characterCreationExtended == null && uiCharacterCreation != null)
                        uiCharacterCreation.Show();
                    else if (characterCreationExtended != null)
                        characterCreationExtended.Show();
                });
            }

            // quit button
            if (quitButton != null)
            {
                quitButton.onClick.SetListener(() => { NetworkManagerMMO.Quit(); });
            }

            // game control button
            if (buttonGameControl != null)
            {
                buttonGameControl.onClick.SetListener(() =>
                {
                    if (panelGameControl != null)
                        panelGameControl.SetActive(true);
                });
            }
        }

        private void Update()
        {
            if (manager == null)
                return;

            bool creating =
                (characterCreationExtended != null && characterCreationExtended.IsVisible()) ||
                (uiCharacterCreation != null && uiCharacterCreation.IsVisible());

            if (manager.state == NetworkState.Lobby && !creating)
            {
                if (panel != null)
                    panel.SetActive(true);

                // characters available message received already?
                if (manager.charactersAvailableMsg.characters != null)
                {
                    CharactersAvailableMsg.CharacterPreview[] characters = manager.charactersAvailableMsg.characters;

                    // build AAA character card list if configured
                    BuildCharacterSlots(characters);

                    // buttons and stats from GFF original
                    UpdateButtonsAndStats(characters);
                }
            }
            else
            {
                if (panel != null)
                    panel.SetActive(false);
            }
        }

        private void BuildCharacterSlots(CharactersAvailableMsg.CharacterPreview[] characters)
        {
            if (slotCharacter == null || content == null)
            {
                // keep selection valid even without cards configured
                if (characters.Length > 0)
                {
                    if (manager.selection < 0 || manager.selection >= characters.Length)
                        manager.selection = 0;
                }
                else manager.selection = -1;

                return;
            }

            // balance prefabs (same as V2)
            UIUtils.BalancePrefabs(slotCharacter.gameObject, characters.Length, content);

            // ensure selection index
            if (characters.Length > 0)
            {
                if (manager.selection < 0 || manager.selection >= characters.Length)
                    manager.selection = 0;
            }
            else
            {
                manager.selection = -1;
            }

            for (int c = 0; c < characters.Length; ++c)
            {
                int currentID = c;

                Player plyer = null;
                if (manager.selectionLocations != null &&
                    currentID < manager.selectionLocations.Length &&
                    manager.selectionLocations[currentID] != null)
                {
                    plyer = manager.selectionLocations[currentID].gameObject.GetComponentInChildren<Player>();
                }

                Transform slotTransform = content.GetChild(currentID);
                UI_CharacterSlotV2 slot = slotTransform.GetComponent<UI_CharacterSlotV2>();
                if (slot == null)
                    continue;

                var preview = characters[currentID];

                slot.characterName.text = preview.name;
                slot.characterLevel.text = preview.level.ToString();
                slot.characterClasse.text = preview.className;
                slot.isGM.SetActive(preview.isGameMaster);

                slot.button.interactable = (currentID != manager.selection);
                slot.button.onClick.SetListener(() =>
                {
                    manager.selection = currentID;
                });

                if (manager.selectionLocations != null &&
                    currentID < manager.selectionLocations.Length &&
                    manager.selectionLocations[currentID] != null)
                {
                    manager.selectionLocations[currentID].gameObject.SetActive(manager.selection == currentID);
                }

                if (useSelectionAnimation &&
                    !string.IsNullOrEmpty(boolNameAnimation) &&
                    plyer != null &&
                    plyer.animator != null)
                {
                    plyer.animator.SetBool(boolNameAnimation, manager.selection == currentID);
                }

                if (plyer != null)
                {
                    if (plyer.nameOverlay != null)
                        plyer.nameOverlay.gameObject.SetActive(false);

                    if (plyer.portraitIcon != null && slot.characterImage != null)
                        slot.characterImage.sprite = plyer.portraitIcon;
                }
            }

            curid = manager.selection;
        }

        private void UpdateButtonsAndStats(CharactersAvailableMsg.CharacterPreview[] characters)
        {
            // start button: calls AddPlayer / OnServerAddPlayer
            if (startButton != null)
            {
                startButton.gameObject.SetActive(manager.selection != -1);
                startButton.onClick.SetListener(() =>
                {
                    // set client "ready". we will receive world messages from monsters etc.
                    if (NetworkClient.connection != null && !NetworkClient.ready)
                        NetworkClient.Ready();

#if _iMMOLOBBY
                    Tools_UI_Tools.FadeOutScreen(false);
#endif

                    // send CharacterSelect message (need to be ready first!)
                    NetworkClient.Send(new CharacterSelectMsg { index = manager.selection });

                    // clear character selection previews
                    manager.ClearPreviews();

                    // make sure we can't select twice and call AddPlayer twice
                    if (panel != null)
                        panel.SetActive(false);
                });
            }

            // delete button
            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(manager.selection != -1);
                deleteButton.onClick.SetListener(() =>
                {
                    if (uiConfirmation != null &&
                        manager.selection != -1 &&
                        manager.selection < characters.Length)
                    {
                        string charName = characters[manager.selection].name;
                        uiConfirmation.Show(
                            "Do you really want to delete <b>" + charName + "</b>?",
                            () => { NetworkClient.Send(new CharacterDeleteMsg { index = manager.selection }); }
                        );
                    }
                });
            }

            // create button interactable (onClick wired in Start)
            if (createButton != null)
            {
                createButton.interactable = characters.Length < manager.characterLimit;
            }

            // stats panel
            if (panelStats != null)
            {
                bool hasSelection = manager.selection != -1 &&
                                    manager.selection < characters.Length;
                panelStats.SetActive(hasSelection);

                if (hasSelection)
                {
                    var ch = characters[manager.selection];

                    if (textNameValue != null)
                        textNameValue.text = ch.name;

                    if (textLevelValue != null)
                        textLevelValue.text = ch.level.ToString();

                    if (textGoldValue != null)
                    {
                        if (ch.gold == 0) textGoldValue.text = "0";
                        else textGoldValue.text = ch.gold.ToString("###,###,###,###");
                    }

                    if (textClassValue != null)
                        textClassValue.text = ch.className;

                    if (characterCreationExtended != null)
                    {
                        if (textRaceValue != null)
                            textRaceValue.text = ch.race.ToString();

                        if (textGenderValue != null)
                            textGenderValue.text = ch.gender;
                    }
                }
            }
        }

        public void MoveCameraToPlayer(Vector3 playerpos)
        {
            if (mycamera != null)
                mycamera.position = playerpos - new Vector3(-2f, -2f, 0f);
        }
    }
}


/*
//Default code UICharacterSelectionExtended.cs 
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace GFFAddons
{
    public class UICharacterSelectionExtended : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button quitButton;

        [Header("Stats")]
        [SerializeField] private GameObject panelStats;
        [SerializeField] private Text textNameValue;
        [SerializeField] private Text textRace;
        [SerializeField] private Text textRaceValue;
        [SerializeField] private Text textClassValue;
        [SerializeField] private Text textSpecialization;
        [SerializeField] private Text textSpecializationValue;
        [SerializeField] private Text textGender;
        [SerializeField] private Text textGenderValue;
        [SerializeField] private Text textLevelValue;
        [SerializeField] private Text textGoldValue;
        [SerializeField] private Text textLocationValue;

        [Header("GFF GameControl Panel Addon")]
        [SerializeField] private Button buttonGameControl;
        [SerializeField] private GameObject panelGameControl;
        [SerializeField] private GameObject panelPremium;

        [Header("Components")]
        [SerializeField] private NetworkManagerMMO manager; // singleton is null until update
        [SerializeField] private UICharacterCreationExtended characterCreationExtended;
        [SerializeField] private UICharacterCreation uiCharacterCreation;
        [SerializeField] private UIConfirmation uiConfirmation;

        private void Start()
        {
            // create button
            createButton.onClick.SetListener(() =>
            {
                panel.SetActive(false);
                if (characterCreationExtended == null) uiCharacterCreation.Show();
                else characterCreationExtended.Show();
            });

            // quit button
            quitButton.onClick.SetListener(() => { NetworkManagerMMO.Quit(); });

            // game control button
            buttonGameControl.onClick.SetListener(() =>
            {
                panelGameControl.SetActive(true);
            });
        }

        private void Update()
        {
            // show while in lobby and while not creating a character
            if (manager.state == NetworkState.Lobby &&
                ((characterCreationExtended != null && !characterCreationExtended.IsVisible()) || (uiCharacterCreation != null && !uiCharacterCreation.IsVisible())))
            {
                panel.SetActive(true);

                // characters available message received already?
                if (manager.charactersAvailableMsg.characters != null)
                {
                    CharactersAvailableMsg.CharacterPreview[] characters = manager.charactersAvailableMsg.characters;

                    // start button: calls AddPLayer which calls OnServerAddPlayer
                    // -> button sends a request to the server
                    // -> if we press button again while request hasn't finished
                    //    then we will get the error:
                    //    'ClientScene::AddPlayer: playerControllerId of 0 already in use.'
                    //    which will happen sometimes at low-fps or high-latency
                    // -> internally ClientScene.AddPlayer adds to localPlayers
                    //    immediately, so let's check that first
                    startButton.gameObject.SetActive(manager.selection != -1);
                    startButton.onClick.SetListener(() =>
                    {
                        // set client "ready". we will receive world messages from
                        // monsters etc. then.
                        if (NetworkClient.connection != null && !NetworkClient.ready) NetworkClient.Ready();
                        //NetworkClient.Ready();

                        // send CharacterSelect message (need to be ready first!)
                        NetworkClient.Send(new CharacterSelectMsg { index = manager.selection });

                        // clear character selection previews
                        manager.ClearPreviews();

                        // make sure we can't select twice and call AddPlayer twice
                        panel.SetActive(false);
                    });

                    // delete button
                    deleteButton.gameObject.SetActive(manager.selection != -1);
                    deleteButton.onClick.SetListener(() =>
                    {
                        uiConfirmation.Show(
                            "Do you really want to delete <b>" + characters[manager.selection].name + "</b>?",
                            () => { NetworkClient.Send(new CharacterDeleteMsg { index = manager.selection }); }
                        );
                    });

                    // panelStats
                    panelStats.SetActive(manager.selection != -1);
                    if (manager.selection != -1)
                    {
                        textNameValue.text = characters[manager.selection].name;
                        textLevelValue.text = characters[manager.selection].level.ToString();

                        // paint gold value
                        if (characters[manager.selection].gold == 0) textGoldValue.text = "0";
                        else textGoldValue.text = characters[manager.selection].gold.ToString("###,###,###,###");

                        textClassValue.text = characters[manager.selection].className;

                        if (characterCreationExtended != null)
                        {
                            textRaceValue.text = characters[manager.selection].race.ToString();
                            //textSpecializationValue.text = characters[manager.selection].specialization;
                            textGenderValue.text = characters[manager.selection].gender;
                        }
                    }
                }
            }
            else panel.SetActive(false);
        }
    }
}
*/