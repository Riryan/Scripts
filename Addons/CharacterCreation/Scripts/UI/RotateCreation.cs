using UnityEngine;

public class RotateCreation : MonoBehaviour
{
    public UI_CharacterCreation characterCreation;
    public float RotationSpeed = 5;
    GameObject go;
    public RectTransform rectTransform;
    bool isRotating = false;
    Vector3 previousMousePosition;
    void Update()
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
        {
            go = characterCreation.spawnPoint.transform.GetChild(0).gameObject;

            if (Input.GetMouseButtonDown(0))
            {
                previousMousePosition = Input.mousePosition;
                isRotating = true;
            }

            if (isRotating && Input.GetMouseButton(0))
            {
                float deltaX = Input.mousePosition.x - previousMousePosition.x;
                go.transform.Rotate(0, -deltaX * RotationSpeed * Time.deltaTime, 0);
                previousMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isRotating = false;
            }
        }
    }
}