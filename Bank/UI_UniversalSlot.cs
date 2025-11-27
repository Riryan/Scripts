// Attach to the prefab for easier component access by the UI Scripts.
// Otherwise we would need slot.GetChild(0).GetComponentInChildren<Text> etc.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_UniversalSlot : MonoBehaviour
{
    public UIShowToolTip tooltip;
    public Button button;
    public UIDragAndDropable dragAndDropable;
    public Image image;
    public Image cooldownCircle;
#if _iMMOITEMRARITY
    public Image rarityOutline;
#endif
    public GameObject amountOverlay;
    public TMP_Text amountText;
}
