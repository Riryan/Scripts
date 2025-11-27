using UnityEngine;

public class UI_SettingsVariables : MonoBehaviour
{
    public KeyCode[] keybindings = new KeyCode[] { 
        KeyCode.W, 
        KeyCode.S, 
        KeyCode.A, 
        KeyCode.D, 
        KeyCode.P, // -> Party
        KeyCode.U, // -> Equipment
        KeyCode.Space, // Jump -> Jump 
        KeyCode.Alpha1, 
        KeyCode.Alpha2, 
        KeyCode.Alpha3, 
        KeyCode.Alpha4,
        KeyCode.Alpha5, 
        KeyCode.Alpha6, 
        KeyCode.Alpha7, 
        KeyCode.Alpha8, 
        KeyCode.Alpha9, 
        KeyCode.Alpha0, 
        KeyCode.X, // 17 -> ItemMall
        KeyCode.R, // 18 -> Skill
        KeyCode.G, //19 -> Guild
        KeyCode.L, // 20 -> Quest
        KeyCode.T, // 21 -> CharacterInfo
        KeyCode.I, // 22 -> Inventory
        KeyCode.C  // 23 -> Crafting
    };

    [HideInInspector] public bool isShowOverhead = true;
    [HideInInspector] public bool isShowChat = true;
    [HideInInspector] public bool isShowPing = true;
    [HideInInspector] public bool isShowFps = true;

    [HideInInspector]
    public bool[] keybindUpdate = new bool[] { false, false, false, false, false, false, false,
     false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false};
}