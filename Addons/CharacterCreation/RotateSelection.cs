/*
using UnityEngine;
using GFFAddons;

public class RotateSelection : MonoBehaviour
{
    [Header("References")]
    public UICharacterSelectionExtended characterSelection;
    public RectTransform rectTransform;

    [Header("Rotation")]
    public float RotationSpeed = 5f;

    bool isRotating = false;
    Vector3 previousMousePosition;

    void Update()
    {
        if (characterSelection == null || characterSelection.manager == null)
            return;

        // If a rectTransform is assigned, only rotate while the mouse is over it.
        bool pointerOverRect = rectTransform == null ||
                               RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition);

        if (!pointerOverRect)
        {
            // Make sure we stop rotating when the mouse is released, even if it's off the rect.
            if (Input.GetMouseButtonUp(0))
                isRotating = false;

            return;
        }

        // Get current preview object from the manager / selectionLocations
        var manager = characterSelection.manager;
        int index = characterSelection.curid;

        GameObject go = null;
        if (manager.selectionLocations != null &&
            index >= 0 && index < manager.selectionLocations.Length &&
            manager.selectionLocations[index] != null)
        {
            go = manager.selectionLocations[index].gameObject;
        }

        if (go == null)
            return;

        // Mouse down: start rotating
        if (Input.GetMouseButtonDown(0))
        {
            previousMousePosition = Input.mousePosition;
            isRotating = true;
        }

        // Mouse held: apply rotation
        if (isRotating && Input.GetMouseButton(0))
        {
            float deltaX = Input.mousePosition.x - previousMousePosition.x;
            go.transform.Rotate(0f, -deltaX * RotationSpeed * Time.deltaTime, 0f, Space.World);
            previousMousePosition = Input.mousePosition;
        }

        // Mouse up: stop rotating
        if (Input.GetMouseButtonUp(0))
        {
            isRotating = false;
        }
    }
}
*/