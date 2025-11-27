using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIChatEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Text text;

    
    [HideInInspector] public ChatMessage message;
    public FontStyle mouseOverStyle = FontStyle.Italic;
    FontStyle defaultStyle;

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        
        if (!string.IsNullOrWhiteSpace(message.replyPrefix))
        {
            defaultStyle = text.fontStyle;
            text.fontStyle = mouseOverStyle;
        }
    }

    
    public void OnPointerExit(PointerEventData pointerEventData)
    {
        text.fontStyle = defaultStyle;
    }

    public void OnPointerClick(PointerEventData data)
    {
        
        GetComponentInParent<UIChat>().OnEntryClicked(this);
    }
}
