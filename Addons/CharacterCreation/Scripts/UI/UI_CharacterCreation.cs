using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using uMMORPG;

public class UI_CharacterCreation : MonoBehaviour
{
    // =====================================================
    // UI
    // =====================================================
    [Header("Panels")]
    public GameObject panel;
    public GameObject centerPanel2;

    [Header("Class Selection")]
    public Transform content;
    public UI_CharacterSlot characterSlot;

    [Header("Camera")]
    public Transform creationCameraLocation;
    public bool lookAtCamera;

    [Header("Spawn")]
    public Transform spawnPoint;

    [Header("Create")]
    //public TMP_InputField nameInput;
    public InputField nameInput;
    public Button createButton;
    public Button cancelButton;
    public Toggle gameMasterToggle;

    // =====================================================
    // Customization UI (DATA-DRIVEN)
    // =====================================================
    [System.Serializable]
    public class CustomizationRow
    {
        public string slotName;
        public Button prev;
        public Button next;
    }

    [Header("Customization Rows")]
    public List<CustomizationRow> customizationRows = new();

    // =====================================================
    // Internal
    // =====================================================
    NetworkManagerMMO manager;
    List<Player> playerClasses;
    int classIndex;
    bool initialized;

    GameObject previewInstance;
    PlayerCustomizationVisuals visuals;
    PlayerCustomizationData previewData;

    // =====================================================
    void Awake()
    {
        //manager = NetworkManager.singleton as NetworkManagerMMO;
        panel.SetActive(false);
    }

    // =====================================================
    // SHOW / HIDE
    // =====================================================
    public void Show()
    {
#if !UNITY_SERVER
        if (panel.activeSelf)
            return;

if (creationCameraLocation != null)
{
    Camera cam = Camera.main;
    if (cam != null)
    {
        cam.transform.SetPositionAndRotation(
            creationCameraLocation.position,
            creationCameraLocation.rotation
        );
    }
}
manager = NetworkManager.singleton as NetworkManagerMMO;
if (manager == null)
{
    Debug.LogError("UI_CharacterCreation.Show: NetworkManagerMMO not ready yet");
    return;
}
        playerClasses = manager.playerClasses;
        if (playerClasses == null || playerClasses.Count == 0)
            return;

        if (centerPanel2 != null)
            centerPanel2.SetActive(false);

        UIUtils.BalancePrefabs(characterSlot.gameObject, playerClasses.Count, content);
        for (int i = 0; i < playerClasses.Count; i++)
        {
            int idx = i;
            UI_CharacterSlot slot = content.GetChild(i).GetComponent<UI_CharacterSlot>();
            slot.characterName.text = playerClasses[i].name;
            slot.button.onClick.SetListener(() => SetCharacterClass(idx));
            slot.button.gameObject.SetActive(true);
        }

        SetCharacterClass(0);

        createButton.onClick.SetListener(CreateCharacter);
        cancelButton.onClick.SetListener(Hide);

        panel.SetActive(true);
        initialized = true;
#endif
    }

    public void Hide()
    {
        if (spawnPoint.childCount > 0)
            Destroy(spawnPoint.GetChild(0).gameObject);

        panel.SetActive(false);
        initialized = false;
    }

    void Update()
    {
        if (!initialized)
            return;

        if (manager != null)
            createButton.interactable =
                manager.IsAllowedCharacterName(nameInput.text);

        if (gameMasterToggle != null)
            gameMasterToggle.gameObject.SetActive(NetworkServer.activeHost);
    }

    // =====================================================
    // CLASS / PREVIEW
    // =====================================================
    void SetCharacterClass(int index)
    {
        classIndex = index;

        if (spawnPoint.childCount > 0)
            Destroy(spawnPoint.GetChild(0).gameObject);

        previewInstance = Instantiate(
            playerClasses[classIndex].gameObject,
            spawnPoint.position,
            spawnPoint.rotation,
            spawnPoint
        );

        if (lookAtCamera && creationCameraLocation != null)
            previewInstance.transform.LookAt(creationCameraLocation);

        Player player = previewInstance.GetComponent<Player>();
        player.nameOverlay.gameObject.SetActive(false);

        visuals = previewInstance.GetComponent<PlayerCustomizationVisuals>();
        previewData = default;

        BuildCustomizationUI();
        ApplyPreview();
    }

    // =====================================================
    // CUSTOMIZATION
    // =====================================================
    void BuildCustomizationUI()
    {
        if (visuals == null || visuals.slots == null)
            return;

        for (int i = 0; i < customizationRows.Count; i++)
        {
            int slotIndex = i;
            var row = customizationRows[i];

            if (slotIndex >= visuals.slots.Length)
            {
                row.prev.gameObject.SetActive(false);
                row.next.gameObject.SetActive(false);
                continue;
            }

            int meshCount = visuals.slots[slotIndex].meshes.Length;
            bool usable = meshCount > 1;

            row.prev.gameObject.SetActive(usable);
            row.next.gameObject.SetActive(usable);

            if (!usable)
                continue;

            row.prev.onClick.RemoveAllListeners();
            row.next.onClick.RemoveAllListeners();

            row.prev.onClick.AddListener(() =>
            {  SetIndex(slotIndex, (GetIndex(slotIndex) - 1 + meshCount) % meshCount); });

            row.next.onClick.AddListener(() =>
            { SetIndex(slotIndex, (GetIndex(slotIndex) + 1) % meshCount); });
        }
    }

    void SetIndex(int slot, int value)
    {
        switch (slot)
        {
            case 0: previewData.hair = value; break;
            case 1: previewData.beard = value; break;
            case 2: previewData.face = value; break;
            case 3: previewData.brows = value; break;
            case 4: previewData.ears = value; break;
        }

        ApplyPreview();
    }

    int GetIndex(int slot)
    {
        return slot switch
        {
            0 => previewData.hair,
            1 => previewData.beard,
            2 => previewData.face,
            3 => previewData.brows,
            4 => previewData.ears,
            _ => 0
        };
    }

    void ApplyPreview()
    {
        if (visuals != null)
            visuals.Apply(previewData);
    }

    // =====================================================
    // CREATE
    // =====================================================
    void CreateCharacter()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text))
            return;

        CharacterCreateMsg msg = new CharacterCreateMsg
        {
            name = nameInput.text,
            classIndex = classIndex,
            customization = previewData
        };

        NetworkClient.Send(msg);
        Hide();
    }

    public bool IsVisible() => panel.activeSelf;
}
