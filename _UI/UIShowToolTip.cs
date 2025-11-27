
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIShowToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject tooltipPrefab;
    [TextArea(1, 30)] public string text = "";

    
    GameObject current;

    void CreateToolTip()
    {
        
        current = Instantiate(tooltipPrefab, transform.position, Quaternion.identity);

        
        current.transform.SetParent(transform.root, true); 
        current.transform.SetAsLastSibling(); 

        
        
        current.GetComponentInChildren<Text>().text = text;
    }

    void ShowToolTip(float delay)
    {
        Invoke(nameof(CreateToolTip), delay);
    }

    
    
    public bool IsVisible() => current != null;

    void DestroyToolTip()
    {
        
        CancelInvoke(nameof(CreateToolTip));

        
        Destroy(current);
    }

    public void OnPointerEnter(PointerEventData d)
    {
        ShowToolTip(0.5f);
    }

    public void OnPointerExit(PointerEventData d)
    {
        DestroyToolTip();
    }

    void Update()
    {
        
        
        if (current) current.GetComponentInChildren<Text>().text = text;
    }

    void OnDisable()
    {
        DestroyToolTip();
    }

    void OnDestroy()
    {
        DestroyToolTip();
    }
}
