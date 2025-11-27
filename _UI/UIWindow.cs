

using UnityEngine;
using UnityEngine.EventSystems;

public enum CloseOption
{
    DoNothing,
    DeactivateWindow,
    DestroyWindow
}

public class UIWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    
    public CloseOption onClose = CloseOption.DeactivateWindow;

    
    public static UIWindow currentlyDragged;

    
    Transform window;

    void Awake()
    {
        
        window = transform.parent;
    }

    public void HandleDrag(PointerEventData d)
    {
        
        window.SendMessage("OnWindowDrag", d, SendMessageOptions.DontRequireReceiver);

        
        window.Translate(d.delta);
    }

    public void OnBeginDrag(PointerEventData d)
    {
        currentlyDragged = this;
        HandleDrag(d);
    }

    public void OnDrag(PointerEventData d)
    {
        HandleDrag(d);
    }

    public void OnEndDrag(PointerEventData d)
    {
        HandleDrag(d);
        currentlyDragged = null;
    }

    
    public void OnClose()
    {
        
        
        
        window.SendMessage("OnWindowClose", SendMessageOptions.DontRequireReceiver);

        
        if (onClose == CloseOption.DeactivateWindow)
            window.gameObject.SetActive(false);

        
        if (onClose == CloseOption.DestroyWindow)
            Destroy(window.gameObject);
    }
}
