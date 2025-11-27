using UnityEngine;
using UnityEngine.UI;

public class UI_MobileControls : MonoBehaviour
{
    public Button jumpButton;
    public Button autoRunButton;

    public bool alwaysActive;

    private bool initalized = false;


    public void OnClick_Btn_SelectTarget()
    {
        Player player = Player.localPlayer;
        if (!player) return;
        player.TargetNearestButton();
    }
    
    public void OnClick_Btn_Jump()
    {
        MobileControls.SetJump();
    }

    public void OnClick_Btn_AutoRun()
    {
        MobileControls.SetAutoRun();
    }
    private void Awake()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            GameObject go = transform.GetChild(i).gameObject;
            if (go)
                go.SetActive(false);
        }

#if _iMMO2D
        jumpButton.gameObject.SetActive(false);
#endif
    }

    private void Update()
    {
        if (!initalized)
        {
            Player player = Player.localPlayer;

            if (!player)
                return;

            if (Input.touchSupported || alwaysActive)
            {
                for (int i = 0; i < transform.childCount; ++i)
                {
                    GameObject go = transform.GetChild(i).gameObject;
                    if (go)
                        go.SetActive(true);
                }
            }
            initalized = true;
        }
    }

}