
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Settings : MonoBehaviour
{
    #region Variables

    #region _iMMOMAINMENU

#if _iMMOMAINMENU

#else
    public KeyCode hotKey = KeyCode.Escape;         //The hotkey used to open the settings menu if Main Menu is not used.

#endif

    #endregion _iMMOMAINMENU

    public GameObject panel;                        //Options menu object.

    [Header("[-=-=-[ Key binding Settings ]-=-=-]")]
    public Text[] keybindingText;

    [Header("[-=-=-[ Gameplay Settings ]-=-=-]")]
    public Slider uiScalable;                 //Sliders for all gameplay settings.+
    public GameObject[] uiScalablePanel;                 //Array of all ui components you wish to have scale with uiscale.
    public Toggle blockTradeRequest;
    public Toggle blockPartiInvit;
    public Toggle blockGuildInvit;
    public Toggle showChat;
    public Toggle showOverheads;
    public Toggle showFPS;
    public Toggle showPing;

    [Header("[-=-=-[ Video Settings ]-=-=-]")]
    public Dropdown resolutionDropdown;             //Dropdown menu of resolutions.
    public Dropdown antiAliasing;                   //Dropdown menu of resolutions.
    public Dropdown vSync;                          //Dropdown menu of resolutions.
    public Dropdown textureQuality;                 //Dropdown menu of resolutions.
    public Dropdown anisotropic;                    //Dropdown menu of resolutions.
    public Dropdown overallQuality;                 //Dropdown menu of resolutions.
    public Dropdown shadow;                         //Dropdown menu of resolutions.
    public Dropdown shadowResolution;
    public Dropdown shadowDistance;
    public Dropdown shadowCascade;
    public Dropdown skinWeigth;
    public Toggle fullScreen;
    public Toggle softParticles;
    public Toggle shadowProjection;

    [Header("[-=-=-[ Sound Settings ]-=-=-]")]
    public Slider[] soundSliders;                   //Sliders for all sound volume.
    public Toggle[] soundToggles;                   //Togggles for all sound volume.
    public AudioSource[] musicPlayed;               //The audio to adjust, this can be made into an array to allow for multiple audio sources.
    public AudioSource[] effectsPlayed;             //The audio to adjust, this can be made into an array to allow for multiple audio sources.
    public AudioSource[] ambientPlayed;             //The audio to adjust, this can be made into an array to allow for multiple audio sources.

    [Header("[-=-=-[ Button Apply (inutile ?) ]-=-=-]")]
    public Button applyButton;                      //Button to apply the settings.

    public Resolution[] resolutions;                //Array of possible resolutions.


    [HideInInspector] public bool waitingForKey = false;
    [HideInInspector] public GameObject currentButton;
    [HideInInspector] public KeyCode currentKey = KeyCode.W;

    private UI_SettingsVariables settingsVariables;

    #endregion Variables

    #region Functions

    //Loads all of our settings on start.
    private void Start()
    {
        settingsVariables = FindObjectOfType<UI_SettingsVariables>().GetComponent<UI_SettingsVariables>();
        resolutionDropdown.options.Clear();
        resolutions = Screen.resolutions;                                                       //Set current resolution to screens resolution.
        foreach (Resolution resolution in resolutions)                                          //Loop through resolution possibilities in the array.
            resolutionDropdown.options.Add(new Dropdown.OptionData(resolution.ToString()));     //Populate teh dropdown with the resolution possibilities.
        LoadSettings();
    }

    //Loads all resolutions on enable incase screen swap happens.
    private void OnEnable()
    {
        resolutionDropdown.options.Clear();
        resolutions = Screen.resolutions;                                                       //Set current resolution to screens resolution.
        foreach (Resolution resolution in resolutions)                                          //Loop through resolution possibilities in the array.
            resolutionDropdown.options.Add(new Dropdown.OptionData(resolution.ToString()));     //Populate teh dropdown with the resolution possibilities.
    }

    #region _iMMOMAINMENU

#if _iMMOMAINMENU

#else
    //Initiates every frame.
    private void Update()
    {
        Player player = Player.localPlayer;                         //Grab the player from utils.
        if (player == null) return;                                 //Don't continue if there is no player found.

        if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())  //If the hotkey is pressed and chat is not active then progress.
        {
            ShowHideSetting();
        }
    }

#endif

    #endregion _iMMOMAINMENU

    #region Button

    //Close the options menu, all settings save on change.
    public void OnApplyClick()
    {
        LoadSettings();
        ShowHideSetting();
    }

    #endregion Button

    #region Save Settings

    #region Keybinding

    // Begins our keybinding assignment.
    public void StartKeybinding(int keyIndex)
    {
        if (!waitingForKey)
            StartCoroutine(AssignKey(keyIndex));
    }

    // Waits for a keybinding to be hit then assigns it.
    public IEnumerator AssignKey(int keyIndex)
    {
        waitingForKey = true;
        currentButton = EventSystem.current.currentSelectedGameObject;

        yield return WaitForKey(); //Executes endlessly until user presses a key

        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
            if (Input.GetKey(kcode))
                currentKey = kcode;

        for (int i = 0; i < settingsVariables.keybindings.Length; i++)
        {
            if (settingsVariables.keybindings[i] == currentKey)
            {
                settingsVariables.keybindings[i] = KeyCode.None;
                keybindingText[i].text = "";
                PlayerPrefs.SetString("keybindings[" + settingsVariables.keybindings[i] + "]", KeyCode.None.ToString());
            }
        }

        currentButton.GetComponentInChildren<Text>().text = currentKey.ToString();
        settingsVariables.keybindings[keyIndex] = currentKey;
        settingsVariables.keybindUpdate[keyIndex] = true;
        PlayerPrefs.SetString("keybindings[" + keyIndex + "]", currentKey.ToString());

        waitingForKey = false;

        yield return null;
    }

    // Waits for a key to be pressed.
    private IEnumerator WaitForKey()
    {
        while (!Input.anyKeyDown)
            yield return null;
    }

    #endregion Keybinding

    #region Gameplay

    // Set block trades and save its settings.
    public void SaveBlockTrades(Toggle toggle)
    {
    //    Player player = Player.localPlayer;
   //     if (player != null)
   //         player.playerAddonsConfigurator.CmdBlockTradeRequest(toggle.isOn);
   //     PlayerPrefs.SetInt("BlockTrades", toggle.isOn ? 1 : 0);
    }

    // Set block party invites and save its settings.
    public void SaveBlockParty(Toggle toggle)
    {
       // Player player = Player.localPlayer;
       // if (player != null)
       //     player.playerAddonsConfigurator.CmdBlockPartyInvite(toggle.isOn);
       // PlayerPrefs.SetInt("BlockParty", toggle.isOn ? 1 : 0);
    }

    // Set block guild invites and save its settings.
    public void SaveBlockGuild(Toggle toggle)
    {
     //   Player player = Player.localPlayer;
     //   if (player != null)
     //       player.playerAddonsConfigurator.CmdBlockGuildInvite(toggle.isOn);
     //   PlayerPrefs.SetInt("BlockGuild", toggle.isOn ? 1 : 0);
    }

    // Set show overhead health and save its settings.
    public void SaveShowOverhead(Toggle toggle)
    {
        settingsVariables.isShowOverhead = toggle.isOn;
        PlayerPrefs.SetInt("ShowOverhead", toggle.isOn ? 1 : 0);
    }

    // Set show chat and save its settings.
    public void SaveShowChat(Toggle toggle)
    {
        settingsVariables.isShowChat = toggle.isOn;
        PlayerPrefs.SetInt("ShowChat", toggle.isOn ? 1 : 0);
    }
    // Load show chat and save its settings.
    public void LoadShowFPS()
    {
        showFPS.isOn = !PlayerPrefs.HasKey("ShowFps") || (PlayerPrefs.GetInt("ShowFps") == 1);
    }
    // Set show chat and save its settings.
    public void SaveShowFPS(Toggle toggle)
    {
        settingsVariables.isShowFps = toggle.isOn;
        PlayerPrefs.SetInt("ShowFps", toggle.isOn ? 1 : 0);
    }

    // Load show chat and save its settings.
    public void LoadShowPing()
    {
        showPing.isOn = !PlayerPrefs.HasKey("ShowPing") || (PlayerPrefs.GetInt("ShowPing") == 1);
    }

    // Set show chat and save its settings.
    public void SaveShowPing(Toggle toggle)
    {
        settingsVariables.isShowPing = toggle.isOn;
        PlayerPrefs.SetInt("ShowPing", toggle.isOn ? 1 : 0);
    }

    // Set ui scale and save its settings.
    public void SaveUiScale(Slider slider)
    {
        for (int i = 0; i < uiScalablePanel.Length; i++)
        {
            uiScalablePanel[i].transform.localScale = new Vector3(slider.value, slider.value, 1);
        }

        PlayerPrefs.SetFloat("UiScale", slider.value);
    }

    #endregion Gameplay

    #region Video

    // Set the overall quality level and save its settings.
    public void SaveOverallQuality(Dropdown dropdown)
    {
        QualitySettings.SetQualityLevel(dropdown.value);
        PlayerPrefs.SetInt("OverallQuality", dropdown.value);
    }

    // Set the texture quality level and save its settings.
    public void SaveTextureQuality(Dropdown dropdown)
    {
        switch (dropdown.value)
        {
            case 0: QualitySettings.globalTextureMipmapLimit = 0;  break;
            case 1: QualitySettings.globalTextureMipmapLimit = 1; break;
            case 2: QualitySettings.globalTextureMipmapLimit = 2; break;
            case 3: QualitySettings.globalTextureMipmapLimit = 3; break;
        }

        PlayerPrefs.SetInt("TextureQuality", dropdown.value);
    }

    // Set the anisotropic level and save its settings.
    public void SaveAnisotropic(Dropdown dropdown)
    {
        switch (dropdown.value)
        {
            case 0: QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable; break;
            case 1: QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable; break;
            case 2: QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable; break;
        }

        PlayerPrefs.SetInt("Anisotropic", dropdown.value);
    }

    // Set the anti-aliasing level and save its settings.
    public void SaveAntiAliasing(Dropdown dropdown)
    {
        QualitySettings.antiAliasing = dropdown.value;
        PlayerPrefs.SetInt("AntiAliasing", dropdown.value);
    }

    // Set the soft particles and save its settings.
    public void SaveSoftParticles(Toggle toggle)
    {
        QualitySettings.softParticles = toggle.isOn;
        PlayerPrefs.SetInt("SoftParticles", toggle.isOn ? 1 : 0);
    }

    // Set the shadows level and save its settings.
    public void SaveShadows(Dropdown dropdown)
    {
        switch (dropdown.value)
        {
            case 0: QualitySettings.shadows = ShadowQuality.Disable; break;
            case 1: QualitySettings.shadows = ShadowQuality.HardOnly; break;
            case 2: QualitySettings.shadows = ShadowQuality.All; break;
        }

        PlayerPrefs.SetInt("Shadows", dropdown.value);
    }

    // Set the shadow resolution level and save its settings.
    public void SaveShadowResolution(Dropdown dropdown)
    {
        switch (dropdown.value)
        {
            case 0: QualitySettings.shadowResolution = ShadowResolution.Low; break;
            case 1: QualitySettings.shadowResolution = ShadowResolution.Medium; break;
            case 2: QualitySettings.shadowResolution = ShadowResolution.High; break;
            case 3:  QualitySettings.shadowResolution = ShadowResolution.VeryHigh; break;
        }

        PlayerPrefs.SetInt("ShadowResolution", dropdown.value);
    }

    // Set the shadow projection level and save its settings.
    public void SaveShadowProjection(Toggle toggle)
    {
        if (toggle.isOn)
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
        else
            QualitySettings.shadowProjection = ShadowProjection.CloseFit;

        PlayerPrefs.SetInt("ShadowProjection", toggle.isOn ? 1 : 0);
    }

    // Set the shadow distance and save its settings.
    public void SaveShadowDistance(Dropdown dropdown)
    {
        switch (dropdown.value)
        {
            case 0: QualitySettings.shadowDistance = 15; break;
            case 1: QualitySettings.shadowDistance = 25; break;
            case 2: QualitySettings.shadowDistance = 50; break;
            case 3: QualitySettings.shadowDistance = 75; break;
            case 4: QualitySettings.shadowDistance = 100; break;
            case 5: QualitySettings.shadowDistance = 125; break;
            case 6: QualitySettings.shadowDistance = 150; break;
        }
        PlayerPrefs.SetInt("ShadowDistance", dropdown.value);
    }

    // Set the shadow cascade level and save its settings.
    public void SaveShadowCascade(Dropdown dropdown)
    {
        QualitySettings.shadowCascades = dropdown.value;
        PlayerPrefs.SetInt("ShadowCascades", dropdown.value);
    }

    // Set the blend weight level and save its settings.
    public void SaveBlendWeight(Dropdown dropdown)
    {
        switch (dropdown.value)
        {
            case 0: QualitySettings.skinWeights = SkinWeights.OneBone; break;
            case 1: QualitySettings.skinWeights = SkinWeights.TwoBones; break;
            case 2: QualitySettings.skinWeights = SkinWeights.FourBones; break;
            case 3: QualitySettings.skinWeights = SkinWeights.Unlimited; break;
        }

        PlayerPrefs.SetInt("BlendWeights", dropdown.value);
    }

    // Set the vsync level and save its settings.
    public void SaveVsync(Dropdown dropdown)
    {
        QualitySettings.vSyncCount = dropdown.value;
        PlayerPrefs.SetInt("Vsync", dropdown.value);
    }

    // Set fullscreen and save its settings.
    public void SaveFullscreen(Toggle toggle)
    {
        Screen.fullScreen = toggle.isOn;
        PlayerPrefs.SetInt("Fullscreen", toggle.isOn ? 1 : 0);
    }

    // Set the resolution level and save its settings.
    public void SaveResolution(Dropdown dropdown)
    {
        Screen.SetResolution(resolutions[dropdown.value].width, resolutions[dropdown.value].height, Screen.fullScreen); //Set the game resolution to the dropdown value.
        PlayerPrefs.SetInt("Resolution", dropdown.value);
    }

    #endregion Video

    #region Sound

    // Set the music level and save its settings.
    public void SaveMusicLevel(Slider slider)
    {
        for (int i = 0; i < musicPlayed.Length; i++)
            musicPlayed[i].volume = slider.value;

       // Jukebox.singleton.SetVolume(slider.value);
        PlayerPrefs.SetFloat("MusicLevel", slider.value);
    }

    // Set the effects level and save its settings.
    public void SaveEffectLevel(Slider slider)
    {
        for (int i = 0; i < effectsPlayed.Length; i++)
            effectsPlayed[i].volume = slider.value;

        PlayerPrefs.SetFloat("EffectLevel", slider.value);
    }

    // Set the ambient level and save its settings.
    public void SaveAmbientLevel(Slider slider)
    {
        for (int i = 0; i < ambientPlayed.Length; i++)
            ambientPlayed[i].volume = slider.value;

        PlayerPrefs.SetFloat("AmbientLevel", slider.value);
    }

    // Set sound mute and save its settings.
    public void SaveSoundMute(Toggle toggle)
    {
        if (toggle.isOn)
        {
            for (int i = 0; i < musicPlayed.Length; i++)
                musicPlayed[i].mute = true;

            for (int i = 0; i < effectsPlayed.Length; i++)
                effectsPlayed[i].mute = true;

            for (int i = 0; i < ambientPlayed.Length; i++)
                ambientPlayed[i].mute = true;
        }
        else
        {
            for (int i = 0; i < musicPlayed.Length; i++)
                musicPlayed[i].mute = false;

            for (int i = 0; i < effectsPlayed.Length; i++)
                effectsPlayed[i].mute = false;

            for (int i = 0; i < ambientPlayed.Length; i++)
                ambientPlayed[i].mute = false;
        }

        PlayerPrefs.SetInt("SoundMute", toggle.isOn ? 1 : 0);
    }

    #endregion Sound

    #endregion Save Settings

    #region Load Settings

    public void ShowHideSetting()
    {
        panel.SetActive(!panel.activeSelf);
    }
    // Load all of the player saved settings.
    private void LoadSettings()
    {
        LoadKeybindings();
        LoadGameplay();
        LoadVideo();
        LoadSound();
    }

    #region Keybinding

    // Loads all of the players saved keybindings.
    private void LoadKeybindings()
    {
        int keyCount = settingsVariables.keybindings.Length;
        for (int i = 0; i < keyCount; i++)
        {
            if (PlayerPrefs.HasKey("keybindings[" + i + "]"))
                settingsVariables.keybindings[i] = (KeyCode)Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("keybindings[" + i + "]"));

            keybindingText[i].text = settingsVariables.keybindings[i].ToString();
            settingsVariables.keybindUpdate[i] = true;
        }
    }

    #endregion Keybinding

    #region Gameplay

    // Load all of the players saved gameplay settings.
    private void LoadGameplay()
    {
        LoadBlockTrades();
        LoadBlockParty();
        LoadBlockGuild();
        LoadShowOverhead();
        LoadShowChat();
        LoadUiScale();
        LoadShowFPS();
        LoadShowPing();
    }

    // Load block trades and save its settings.
    public void LoadBlockTrades()
    {
    //    blockTradeRequest.isOn = !PlayerPrefs.HasKey("BlockTrades") || (PlayerPrefs.GetInt("BlockTrades") == 1);
    //    Player player = Player.localPlayer;
    //    if (player != null)
    //        player.playerAddonsConfigurator.CmdBlockTradeRequest(blockTradeRequest.isOn);
    }

    // Load block party invites and save its settings.
    public void LoadBlockParty()
    {
    //    blockPartiInvit.isOn = PlayerPrefs.HasKey("BlockParty") || (PlayerPrefs.GetInt("BlockParty") == 1);
    //    Player player = Player.localPlayer;
    //    if (player != null)
    //        player.playerAddonsConfigurator.CmdBlockPartyInvite(blockPartiInvit.isOn);
    }

    // Load block guild invites and save its settings.
    public void LoadBlockGuild()
    {
    //    blockGuildInvit.isOn = PlayerPrefs.HasKey("BlockGuild") && ((PlayerPrefs.GetInt("BlockGuild") == 1));
    //    Player player = Player.localPlayer;
    //    if (player != null)
    //        player.playerAddonsConfigurator.CmdBlockGuildInvite(blockGuildInvit.isOn);
    }

    // Load show overhead health and save its settings.
    public void LoadShowOverhead()
    {
        showOverheads.isOn = PlayerPrefs.HasKey("ShowOverhead") && ((PlayerPrefs.GetInt("ShowOverhead") == 1));
    }

    // Load show chat and save its settings.
    public void LoadShowChat()
    {
        showChat.isOn = PlayerPrefs.HasKey("ShowChat") && ((PlayerPrefs.GetInt("ShowChat") == 1));
    }

    // Load ui scale and save its settings.
    public void LoadUiScale()
    {

        uiScalable.value = (PlayerPrefs.HasKey("UiScale")) ? PlayerPrefs.GetFloat("UiScale") : 1;
    }

    #endregion Gameplay

    #region Video

    // Load all of the player video settings.
    private void LoadVideo()
    {
        LoadOverallQuality();
        LoadTextureQuality();
        LoadAnisotropic();
        LoadAntiAliasing();
        LoadSoftParticles();
        LoadShadows();
        LoadShadowResolution();
        LoadShadowProjection();
        LoadShadowDistance();
        LoadShadowCascade();
        LoadBlendWeight();
        LoadVsync();
        LoadFullscreen();
        LoadResolution();
    }

    // Load the overall quality level and set its settings.
    public void LoadOverallQuality()
    {
        overallQuality.value = (PlayerPrefs.HasKey("OverallQuality")) ? PlayerPrefs.GetInt("OverallQuality") : 3;
    }

    // Load the texture quality level and set its settings.
    public void LoadTextureQuality()
    {
        textureQuality.value = (PlayerPrefs.HasKey("TextureQuality")) ? PlayerPrefs.GetInt("TextureQuality") : 0;
    }

    // Load the anisotropic level and set its settings.
    public void LoadAnisotropic()
    {
        anisotropic.value = (PlayerPrefs.HasKey("Anisotropic")) ? PlayerPrefs.GetInt("Anisotropic") : 1;
    }

    // Load the anti-aliasing level and set its settings.
    public void LoadAntiAliasing()
    {
        antiAliasing.value = (PlayerPrefs.HasKey("AntiAliasing")) ? PlayerPrefs.GetInt("AntiAliasing") : 1;
    }

    // Load the soft particles and set its settings.
    public void LoadSoftParticles()
    {
        softParticles.isOn = !PlayerPrefs.HasKey("SoftParticles") || (PlayerPrefs.GetInt("SoftParticles") == 1);
    }

    // Load the shadows level and set its settings.
    public void LoadShadows()
    {
        shadow.value = (PlayerPrefs.HasKey("Shadows")) ? PlayerPrefs.GetInt("Shadows") : 1;
    }

    // Load the shadow resolution level and set its settings.
    public void LoadShadowResolution()
    {
        shadowResolution.value = (PlayerPrefs.HasKey("ShadowResolution")) ? PlayerPrefs.GetInt("ShadowResolution") : 1;
    }

    // Load the shadow projection level and set its settings.
    public void LoadShadowProjection()
    {
        shadowProjection.isOn = !PlayerPrefs.HasKey("ShadowProjection") || (PlayerPrefs.GetInt("ShadowProjection") == 1);
    }

    // Load the shadow distance and set its settings.
    public void LoadShadowDistance()
    {
        shadowDistance.value = (PlayerPrefs.HasKey("ShadowDistance")) ? PlayerPrefs.GetInt("ShadowDistance") : 3;
    }

    // Load the shadow cascade level and set its settings.
    public void LoadShadowCascade()
    {
        shadowCascade.value = (PlayerPrefs.HasKey("ShadowCascades")) ? PlayerPrefs.GetInt("ShadowCascades") : 1;
    }

    // Load the blend weight level and set its settings.
    public void LoadBlendWeight()
    {
        skinWeigth.value = (PlayerPrefs.HasKey("BlendWeights")) ? PlayerPrefs.GetInt("BlendWeights") : 2;
    }

    // Load the vsync level and set its settings.
    public void LoadVsync()
    {
        vSync.value = (PlayerPrefs.HasKey("Vsync")) ? PlayerPrefs.GetInt("Vsync") : 1;
    }

    // Load the fullscreen and set its settings.
    public void LoadFullscreen()
    {
        fullScreen.isOn = !PlayerPrefs.HasKey("Fullscreen") || (PlayerPrefs.GetInt("Fullscreen") == 1);
    }

    // Load the screen resolution and set its settings.
    public void LoadResolution()
    {
        resolutionDropdown.value = (PlayerPrefs.HasKey("Resolution")) ? PlayerPrefs.GetInt("Resolution") : resolutionDropdown.options.Count;
    }

    #endregion Video

    #region Sound

    // Load all of the player sound settings.
    private void LoadSound()
    {
        LoadMusicLevel();
        LoadEffectLevel();
        LoadAmbientLevel();
        LoadSoundMute();
    }

    // Load the music level and set its settings.
    public void LoadMusicLevel()
    {
        soundSliders[0].value = (PlayerPrefs.HasKey("MusicLevel")) ? PlayerPrefs.GetFloat("MusicLevel") : 50;

#if _iMMOJUKEBOX
        Jukebox.singleton.SetVolume(soundSliders[0].value);
#endif
    }

    // Load the effects level and set its settings.
    public void LoadEffectLevel()
    {
        soundSliders[1].value = (PlayerPrefs.HasKey("EffectLevel")) ? PlayerPrefs.GetFloat("EffectLevel") : 50;
    }

    // Load the ambient level and set its settings.
    public void LoadAmbientLevel()
    {
        soundSliders[2].value = (PlayerPrefs.HasKey("AmbientLevel")) ? PlayerPrefs.GetFloat("AmbientLevel") : 50;
    }

    // Load sound mute and set its settings.
    public void LoadSoundMute()
    {
        if (PlayerPrefs.HasKey("SoundMute"))
        {
            if (PlayerPrefs.GetInt("SoundMute") == 1)
            {
                soundToggles[0].isOn = true;

                for (int i = 0; i < musicPlayed.Length; i++)
                    musicPlayed[i].mute = true;

                for (int i = 0; i < effectsPlayed.Length; i++)
                    effectsPlayed[i].mute = true;

                for (int i = 0; i < ambientPlayed.Length; i++)
                    ambientPlayed[i].mute = true;

#if _iMMOJUKEBOX
                Jukebox.singleton.Mute(true);
#endif
            }
            else
            {
                soundToggles[0].isOn = false;

                for (int i = 0; i < musicPlayed.Length; i++)
                    musicPlayed[i].mute = false;

                for (int i = 0; i < effectsPlayed.Length; i++)
                    effectsPlayed[i].mute = false;

                for (int i = 0; i < ambientPlayed.Length; i++)
                    ambientPlayed[i].mute = false;

#if _iMMOJUKEBOX
                Jukebox.singleton.Mute(false);
#endif
            }
        }
        else
        {
            soundToggles[0].isOn = false;

            for (int i = 0; i < musicPlayed.Length; i++)
                musicPlayed[i].mute = false;

            for (int i = 0; i < effectsPlayed.Length; i++)
                effectsPlayed[i].mute = false;

            for (int i = 0; i < ambientPlayed.Length; i++)
                ambientPlayed[i].mute = false;

#if _iMMOJUKEBOX
            Jukebox.singleton.Mute(false);
#endif
        }
    }

    #endregion Sound

    #endregion Load Settings

    #endregion Functions
}
