/*
using UnityEngine;

public class RotateCreation : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Spawn point that holds the preview character as its first child.")]
    public Transform spawnPoint;

    public RectTransform rectTransform;

    [Header("Rotation")]
    public float RotationSpeed = 5f;

    bool isRotating = false;
    Vector3 previousMousePosition;

    void Update()
    {
        if (spawnPoint == null)
            return;

        // If a rectTransform is assigned, only rotate while the mouse is over it.
        bool pointerOverRect = rectTransform == null ||
                               RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition);

        if (!pointerOverRect)
        {
            if (Input.GetMouseButtonUp(0))
                isRotating = false;

            return;
        }

        if (spawnPoint.childCount == 0)
            return;

        Transform child = spawnPoint.GetChild(0);
        if (child == null)
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

            // Rotate around Y only
            child.Rotate(0f, -deltaX * RotationSpeed * Time.deltaTime, 0f, Space.World);

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
