///
/// Rebuilt to handle Synty Low Poly Models
/// Color Options for the Shader 
/// 
/// Stubbed for Synty Sidekick Models
/// 

using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace GFFAddons
{
    public class UICharacterCreationExtended : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject panelCreation;
        [SerializeField] private GameObject panelCustomization;
        [Header("Create Character right")]
        [SerializeField] private Dropdown raceDropdown;
        [SerializeField] private Dropdown classDropdown;
        [SerializeField] private Dropdown genderDropdown;
        [SerializeField] private InputField nameInput;
        [SerializeField] private Button cancelIButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button customizationButton;
        [SerializeField] private Button buttonRotate_L;
        [SerializeField] private Button buttonRotate_R;
        [SerializeField] private Toggle gameMasterToggle;
        [Header("Create Character left")]
        [SerializeField] private GameObject panelStats;
        [SerializeField] private Slider HP;
        [SerializeField] private Slider FP;
        [SerializeField] private Slider SP;
        [SerializeField] private Slider Damage;
        [SerializeField] private Slider Defense;
        [SerializeField] private Text textWeapon;
        [SerializeField] private Text textArmor;
        [Header("Customization")]
        [SerializeField] private Transform content;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Button buttonEquip;
        [SerializeField] private Button buttonRandomize;
        [SerializeField] private Button buttonCustomizationSave;
        [Header("Customization Scale")]
        [SerializeField] private GameObject panelScale;
        [SerializeField] private Slider scale;
        [Header("Components")]
        [SerializeField] private NetworkManagerMMO manager; // singleton is null until update
        [SerializeField] private CharcterCreationComponents positions;
        [Header("Settings")]
        [SerializeField] private ScriptableRacesData raceData;
        [SerializeField] private bool usePanelStats = true;
        private GameObject playerPreview;
        private bool isEquip;
        public static bool customizationInGame = false;

        public void Show()
        {
            panel.SetActive(true);

            Camera.main.transform.position = positions.cameraCustomizationPosition.position;
            Camera.main.transform.rotation = positions.cameraCustomizationPosition.rotation;

            InstantiatePrefab();
        }

        public bool IsVisible() { return panel.activeSelf; }

        private void Start()
        {
            LoadRaces();
            LoadClasses();
            LoadGender();
            genderDropdown.onValueChanged.AddListener(delegate { InstantiatePrefab(); });
            classDropdown.onValueChanged.AddListener(delegate { ChangeClasses(); });
            raceDropdown.onValueChanged.AddListener(delegate { ChangeRace(); });
        }

        private void Update()
        {
            if (panel.activeSelf)
            {
                if (manager.state == NetworkState.Lobby || manager.state == NetworkState.World)
                {
                    gameMasterToggle.gameObject.SetActive(NetworkServer.activeHost);
                    cancelIButton.gameObject.SetActive(!panelCustomization.activeSelf);
                    cancelIButton.onClick.SetListener(() => { PressButtonCancel(); });
                    createButton.interactable = manager.IsAllowedCharacterName(nameInput.text);
                    createButton.onClick.SetListener(() => {
                        SendMessageToServerCreateCharacter();
                        panel.SetActive(false);

                        Destroy(playerPreview);
                        nameInput.text = "";
                    });

                    Customization[] customizableItemTypes = playerPreview.GetComponent<PlayerCustomization>().GetItemTypesForCharacterCreate();

                    customizationButton.gameObject.SetActive(playerPreview != null && customizableItemTypes.Length > 0);
                    customizationButton.onClick.SetListener(() => { PressButtonCustomization(); });
                    buttonRotate_L.onClick.SetListener(() => { playerPreview.transform.Rotate(0, 25, 0); });
                    buttonRotate_R.onClick.SetListener(() => { playerPreview.transform.Rotate(0, -25, 0); });
                    if (isEquip) buttonEquip.GetComponentInChildren<Text>().text = "Non Equip";
                    else buttonEquip.GetComponentInChildren<Text>().text = "Equip";
                    buttonEquip.onClick.SetListener(() => {
                        isEquip = !isEquip;
                        EquipCharacter();
                    });

                    buttonRandomize.gameObject.SetActive(panelCustomization.activeSelf);
                    buttonCustomizationSave.gameObject.SetActive(panelCustomization.activeSelf);

                    if (panelCustomization.activeSelf)
                    {
                        PlayerCustomization customization = playerPreview.GetComponent<PlayerCustomization>();

                        UIUtils.BalancePrefabs(prefab.gameObject, customizableItemTypes.Length + 1, content);

                        for (int i = 0; i < customizableItemTypes.Length; i++)
                        {
                            UICustomizationSlot slot = content.GetChild(i + 1).GetComponent<UICustomizationSlot>();
slot.text.text = customizableItemTypes[i].type.ToString();
slot.slider.wholeNumbers = true;
slot.slider.minValue = 0;
int __count = 0;
switch (customizableItemTypes[i].customizationBy)
{
    case CustomizationType.byObjects:
        __count = (customizableItemTypes[i].objects != null) ? customizableItemTypes[i].objects.Length : 0;
        break;
    case CustomizationType.byMaterials:
        __count = (customizableItemTypes[i].materials != null && customizableItemTypes[i].materials.materials != null) ? customizableItemTypes[i].materials.materials.Length : 0;
        break;
    case CustomizationType.byTint:
        __count = (customizableItemTypes[i].tint != null && customizableItemTypes[i].tint.colors != null) ? customizableItemTypes[i].tint.colors.Length : 0;
        break;
}
slot.slider.maxValue = Mathf.Max(0, __count - 1);
slot.slider.value = Mathf.Clamp(slot.slider.value, 0, slot.slider.maxValue);
slot.slider.interactable = (slot.slider.maxValue > 0);

int icopy = i;
// map filtered 'customizableItemTypes' entry to absolute category index in customization array
int __catIndex = -1;
for (int __k = 0; __k < customization.customization.Length; ++__k)
{
    var __c = customization.customization[__k];
    if (__c != null && __c.type == customizableItemTypes[icopy].type) { __catIndex = __k; break; }
}
// initialize slider from saved value (if any)
if (__catIndex >= 0 && __catIndex < customization.values.Count && customization.values[__catIndex] >= 0)
    slot.slider.value = customization.values[__catIndex];

slot.slider.onValueChanged.RemoveAllListeners();
slot.slider.onValueChanged.AddListener((float v) => { customization.SetCustomizationLocalByType(customizableItemTypes[icopy].type, Mathf.RoundToInt(v), false); 
    // also persist the index into the values list for DB save
    if (__catIndex >= 0) {
        int __iv = Mathf.RoundToInt(v);
        while (customization.values.Count <= __catIndex) customization.values.Add(0);
        customization.values[__catIndex] = __iv;
    }
});

slot.buttonLeft.onClick.SetListener(() => { if (slot.slider.value > 0) slot.slider.value--; });
slot.buttonRight.onClick.SetListener(() => { if (slot.slider.value < slot.slider.maxValue) slot.slider.value++; });
                        }

                        buttonRandomize.onClick.SetListener(() => {
    for (int i = 0; i < customization.customization.Length; i++)
    {
        var cat = customization.customization[i];
        if (cat == null) continue;
        int choiceCount = 0;
        if (cat.customizationBy == CustomizationType.byObjects && cat.objects != null)
            choiceCount = cat.objects.Length;
        else if (cat.customizationBy == CustomizationType.byMaterials && cat.materials != null && cat.materials.materials != null)
            choiceCount = cat.materials.materials.Length;
        else if (cat.customizationBy == CustomizationType.byTint && cat.tint != null && cat.tint.colors != null)
            choiceCount = cat.tint.colors.Length;

        if (choiceCount > 0)
            customization.values[i] = Random.Range(0, choiceCount);

        UICustomizationSlot slot = content.GetChild(i + 1).GetComponent<UICustomizationSlot>();
        slot.slider.onValueChanged.RemoveAllListeners();
        slot.slider.maxValue = Mathf.Max(0, choiceCount - 1);
        slot.slider.value = customization.values[i];
        slot.slider.interactable = (slot.slider.maxValue > 0);
        slot.slider.onValueChanged.AddListener((float v) => { customization.SetCustomizationLocalByType(cat.type, Mathf.RoundToInt(v), false); });
    }

    if (customization.rescaling)
    {
        customization.scale = Random.Range(customization.scaleMin, customization.scaleMax);
        scale.value = customization.scale;
    }

    customization.SetCustomization();
});
                        buttonCustomizationSave.onClick.SetListener(() => { PressButtonCustomizationSave(); });

                        if (customization.rescaling)
                        {
                            panelScale.SetActive(true);
                            scale.minValue = customization.scaleMin;
                            scale.maxValue = customization.scaleMax;
                            scale.value = playerPreview.transform.localScale.x;
                            scale.onValueChanged.AddListener((float v) => {
                                customization.scale = v;
                                playerPreview.transform.localScale = new Vector3(v, v, v);
                            });
                        }
                        else panelScale.SetActive(false);
                    }
                }
                else panel.SetActive(false);
            }
            else panel.SetActive(false);
        }

        public void InstantiatePrefab()
        {
            Destroy(playerPreview);
            GameObject go = null;
            if (!customizationInGame) go = manager.playerClasses.Find(p => p.name == classDropdown.captionText.text).gameObject;
            else if (Player.localPlayer != null) go = manager.playerClasses.Find(p => p.name == Player.localPlayer.className).gameObject;

            if (go != null)
            {
                playerPreview = Instantiate(go, positions.characterPosition.position, positions.characterPosition.rotation);
                playerPreview.GetComponent<Player>().name = "";
                PlayerCustomization customization = playerPreview.GetComponent<PlayerCustomization>();

                if (!customizationInGame)
                {
                    panelStats.SetActive(usePanelStats);
                    if (panelStats.activeSelf)
                    {
                        Classes character = raceData.races[raceDropdown.value].classes[classDropdown.value];

                        HP.value = character.hp;
                        FP.value = character.fp;
                        SP.value = character.sp;
                        Damage.value = character.damage;
                        Defense.value = character.defense;
                        textWeapon.text = character.weapon;
                        textArmor.text = character.armor;
                    }
                }
                else {  }

                for (int i = 0; i < customization.customization.Length; i++)
                {
                    if (customization.customization[i].showWhenCharacterCreate) customization.values.Add(0);
                    else customization.values.Add(-1);
                }

                isEquip = true;
                EquipCharacter();
            }
            else
            {
                Debug.LogError("Class prefab not found");
            }
        }

        private void LoadRaces()
        {
            for (int i = 0; i < raceData.races.Length; i++)
            {
                Dropdown.OptionData index = new Dropdown.OptionData();
                index.text = raceData.races[i].name;
                raceDropdown.options.Add(index);
            }
        }
        private void LoadClasses()
        {
            classDropdown.ClearOptions();
            for (int i = 0; i < raceData.races[raceDropdown.value].classes.Count; i++)
            {
                Dropdown.OptionData index = new Dropdown.OptionData();
                index.text = raceData.races[raceDropdown.value].classes[i].name;
                classDropdown.options.Add(index);
            }
            classDropdown.value = 0;
            if (classDropdown.options.Count > 0) classDropdown.captionText.text = classDropdown.options[0].text;
        }
        private void LoadGender()
        {
            genderDropdown.ClearOptions();
            if (raceData.races[raceDropdown.value].classes[classDropdown.value].men)
            {
                Dropdown.OptionData index = new Dropdown.OptionData();
                index.text = "Men";
                genderDropdown.options.Add(index);
            }
            if (raceData.races[raceDropdown.value].classes[classDropdown.value].girl)
            {
                Dropdown.OptionData index = new Dropdown.OptionData();
                index.text = "Women";
                genderDropdown.options.Add(index);
            }
            genderDropdown.value = 0;
            if (genderDropdown.options.Count > 0) genderDropdown.captionText.text = genderDropdown.options[0].text;
        }

        private void ChangeRace()
        {
            LoadClasses();
            InstantiatePrefab();
        }
        private void ChangeClasses()
        {
            LoadGender();
            InstantiatePrefab();
        }

        private void PressButtonCustomization()
        {
            panelCreation.SetActive(false);
            panelCustomization.SetActive(true);
            Camera.main.transform.position = positions.cameraCustomizationPosition.position;
        }
        private void PressButtonCancel()
        {
            Camera.main.transform.position = manager.selectionCameraLocation.position;
            Camera.main.transform.rotation = manager.selectionCameraLocation.rotation;
            Destroy(playerPreview);
            nameInput.text = "";
            panel.SetActive(false);
        }

        private void EquipCharacter()
        {
            Player player = playerPreview.GetComponent<Player>();

            if (isEquip)
            {
                for (int i = 0; i < ((PlayerEquipment)player.equipment).slotInfo.Length; i++)
                {
                    if (((PlayerEquipment)player.equipment).slotInfo[i].defaultItem.item != null && ((PlayerEquipment)player.equipment).slotInfo[i].defaultItem.item is EquipmentItem eitem)
                    {
                        if (eitem.modelPrefab != null)
                        {

                            GameObject go = Instantiate(eitem.modelPrefab, ((PlayerEquipment)player.equipment).slotInfo[i].location, false);
                            SkinnedMeshRenderer equipmentSkin = go.GetComponentInChildren<SkinnedMeshRenderer>();
                            if (equipmentSkin != null && ((PlayerEquipment)player.equipment).CanReplaceAllBones(equipmentSkin))
                                ((PlayerEquipment)player.equipment).ReplaceAllBones(equipmentSkin);

                            Animator anim = go.GetComponent<Animator>();
                            if (anim != null)
                            {
                                anim.runtimeAnimatorController = player.animator.runtimeAnimatorController;
                            }
                            else
                            {
                                Debug.Log("for item " + eitem.name + " anim not found");
                            }
                        }
                    }
                }
                ((PlayerEquipment)player.equipment).RebindAnimators();
            }
            else
            {
                for (int i = 0; i < ((PlayerEquipment)player.equipment).slotInfo.Length; i++)
                    if (((PlayerEquipment)player.equipment).slotInfo[i].location != null && ((PlayerEquipment)player.equipment).slotInfo[i].location.childCount > 0)
                        Destroy(((PlayerEquipment)player.equipment).slotInfo[i].location.GetChild(0).gameObject);
            }
        }
        private void PressButtonCustomizationSave()
        {
            panelCreation.SetActive(true);
            panelCustomization.SetActive(false);

            if (!customizationInGame)
            {
                Camera.main.transform.position = positions.cameraPosition.position;
                Camera.main.transform.rotation = positions.cameraPosition.rotation;
            }
            else
            {
                Player player = Player.localPlayer;
                if (player != null)
                {
                    /*player.CmdSetCustomization(preview.transform.localScale.x,
                        preview.transform.localScale.y,
                        preview.transform.localScale.z,
                        skinColorId,
                        hairColor,
                        browColor,
                        eyeColor,
                        clothing);
                    player.customization.SetCustomization();*/
                }
                panel.SetActive(false);
                customizationInGame = false;
            }
        }

        private void SendMessageToServerCreateCharacter()
        {
            string _values = "";
            for (int i = 0; i < playerPreview.GetComponent<PlayerCustomization>().values.Count; i++)
            {
                _values += playerPreview.GetComponent<PlayerCustomization>().values[i] + ";";
            }
            CharacterCreateMsg message = new CharacterCreateMsg
            {
                name = nameInput.text,
                classIndex = manager.playerClasses.FindIndex(x => x.name == classDropdown.captionText.text),
                gameMaster = gameMasterToggle.isOn,
                race = (RaceList)raceDropdown.value + 1,
                gender = genderDropdown.captionText.text,
                customization = _values,
                scale = playerPreview.GetComponent<PlayerCustomization>().scale
            };
            NetworkClient.Send(message);
        }
    }
}