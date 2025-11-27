using UnityEngine;
using UnityEngine.EventSystems;

// MobileControls
public static partial class MobileControls
{
    public static Vector2 joyVirtualAxis = Vector2.zero;
    public static bool camDrag;
    public static bool AutoRun = false;
    public static bool Jump = false;

    /******************************************************************************************************/
    public static int joy_tIdx = -1;
    public static bool pointerDown;

    // -----------------------------------------------------------------------------------
    // GetTouchDown
    // @Client
    // -----------------------------------------------------------------------------------
    public static bool GetTouchDown
    {
        //get { return (joy_tIdx == -1 && Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began); }
        get { return (joy_tIdx == -1 && Input.touchCount >= 1 && Input.GetTouch(0).phase == TouchPhase.Began); }
    }

    // -----------------------------------------------------------------------------------
    // GetTouchUp
    // @Client
    // -----------------------------------------------------------------------------------
    public static bool GetTouchUp
    {
        get { return (joy_tIdx == -1 && Input.touchCount == 1 && (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled)); }
    }
    /******************************************************************************************************/


    public static void SetAutoRun()
    {
        AutoRun = (AutoRun == true) ? false : true;
    }
    public static void SetJump()
    {
        Jump = (Jump == true) ? false : true;
    }
    public static bool GetAutorun()
    {
        return AutoRun;
    }
    public static bool GetJump()
    {
        return Jump;
    }

    // -----------------------------------------------------------------------------------
    // SetJoystickAxis
    // -----------------------------------------------------------------------------------
    public static void SetJoystickAxis(Vector2 axis)
    {
        joyVirtualAxis = axis;
    }
    public static Vector2 getJoystickAxis()
    {
        return joyVirtualAxis;
    }
    // -----------------------------------------------------------------------------------
    // IsTouchOverUserInterface
    // -----------------------------------------------------------------------------------
    public static bool IsTouchOverUserInterface(Touch touch)
    {
        return (EventSystem.current.IsPointerOverGameObject(touch.fingerId));
    }

    // -----------------------------------------------------------------------------------
    // Get1FingerPositionChange
    // -----------------------------------------------------------------------------------
    public static Vector2 Get1FingerPositionChange(Touch touch)
    {
        return new Vector2(touch.deltaPosition.y, touch.deltaPosition.x);
    }

    // -----------------------------------------------------------------------------------
}