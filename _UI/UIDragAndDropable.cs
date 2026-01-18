// Drag and Drop support for UI elements. Drag and Drop actions will be sent to
// the local player GameObject.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace uMMORPG
{
    public class UIDragAndDropable : MonoBehaviour , IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public PointerEventData.InputButton button = PointerEventData.InputButton.Left;
        public GameObject drageePrefab;
        public static GameObject currentlyDragged;
        public bool dragable = true;
        public bool dropable = true;

        [HideInInspector] public bool draggedToSlot = false;

        public void OnBeginDrag(PointerEventData d)
        {
            if (dragable && d.button == button)
            {
                currentlyDragged = Instantiate(drageePrefab, transform.position, Quaternion.identity);
                currentlyDragged.GetComponent<Image>().sprite = GetComponent<Image>().sprite;
                currentlyDragged.GetComponent<Image>().color = GetComponent<Image>().color; // for durability etc.
                currentlyDragged.transform.SetParent(transform.root, true); // canvas
                currentlyDragged.transform.SetAsLastSibling(); // move to foreground
                GetComponent<Button>().interactable = false;
            }
        }

        public void OnDrag(PointerEventData d)
        {
            if (dragable && d.button == button)
                currentlyDragged.transform.position = d.position;
        }

        public void OnEndDrag(PointerEventData d)
        {
            Destroy(currentlyDragged);

            if (dragable && d.button == button)
            {
                if (!draggedToSlot && d.pointerEnter == null)
                {
                    Player.localPlayer.SendMessage("OnDragAndClear_" + tag,
                                                   name.ToInt(),
                                                   SendMessageOptions.DontRequireReceiver);
                }
                draggedToSlot = false;
                GetComponent<Button>().interactable = true;
            }
        }

        public void OnDrop(PointerEventData d)
        {
            if (dropable && d.button == button)
            {
                UIDragAndDropable dropDragable = d.pointerDrag.GetComponent<UIDragAndDropable>();
                if (dropDragable != null && dropDragable.dragable)
                {
                    dropDragable.draggedToSlot = true;

                    if (dropDragable != this)
                    {
                        int from = dropDragable.name.ToInt();
                        int to = name.ToInt();
                        Player.localPlayer.SendMessage("OnDragAndDrop_" + dropDragable.tag + "_" + tag,
                                                       new int[]{from, to},
                                                       SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
        }

        void OnDisable()
        {
            Destroy(currentlyDragged);
        }

        void OnDestroy()
        {
            Destroy(currentlyDragged);
        }
    }
}