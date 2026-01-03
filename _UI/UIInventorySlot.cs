// Attach to the prefab for easier component access by the UI Scripts.
// Otherwise we would need slot.GetChild(0).GetComponentInChildren<Text> etc.
using UnityEngine;
using UnityEngine.UI;

namespace uMMORPG
{
    public class UIInventorySlot : MonoBehaviour
    {
        public UIShowToolTip tooltip;
        public Button button;
        public UIDragAndDropable dragAndDropable;
        public Image image;
        public Image cooldownCircle;
        public GameObject amountOverlay;
        public Text amountText;
    }
}