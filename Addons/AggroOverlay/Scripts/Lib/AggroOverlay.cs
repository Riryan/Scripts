using UnityEngine;

// AggroOverlay
public class AggroOverlay : MonoBehaviour
{
    public GameObject childObject;
    [Range(0f, 2f)] public float hideAfter = 0.5f;

#if _CLIENT
    // -----------------------------------------------------------------------------------
    // Awake
    // -----------------------------------------------------------------------------------
    private void Awake()
    {
        childObject.SetActive(false);
    }

    // -----------------------------------------------------------------------------------
    // Show
    // -----------------------------------------------------------------------------------
    public void Show()
    {
        childObject.SetActive(true);
        Invoke("Hide", hideAfter);
    }

    // -----------------------------------------------------------------------------------
    // Hide
    // -----------------------------------------------------------------------------------
    public void Hide()
    {
        CancelInvoke();
        childObject.SetActive(false);
    }
#endif 
    // -----------------------------------------------------------------------------------
}