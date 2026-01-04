using UnityEngine;

public class RotateSelection : MonoBehaviour
{
    public UI_CharacterSelectionV2 characterSelection;
    public float RotationSpeed = 5;
    GameObject go;
    public RectTransform rectTransform;
    bool isRotating = false;
    Vector3 previousMousePosition;
    void Update()
    {
        // Convertir la position de la souris en coordonnées de l'écran à des coordonnées de rayon dans l'espace du monde
        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
        {
            go = characterSelection.manager.selectionLocations[characterSelection.curid].gameObject;

            // Détecter le clic gauche de la souris
            if (Input.GetMouseButtonDown(0))
            {
                // Récupérer la position de la souris au moment du clic
                previousMousePosition = Input.mousePosition;
                isRotating = true;
            }

            // Si le bouton de la souris est maintenu enfoncé
            if (isRotating && Input.GetMouseButton(0))
            {
                // Calculer la différence de position de la souris depuis le dernier frame
                float deltaX = Input.mousePosition.x - previousMousePosition.x;

                // Effectuer la rotation uniquement autour de l'axe Y
                go.transform.Rotate(0, -deltaX * RotationSpeed * Time.deltaTime, 0);
                // Mettre à jour la position de la souris pour la prochaine frame
                previousMousePosition = Input.mousePosition;
            }

            // Relâcher le bouton de la souris
            if (Input.GetMouseButtonUp(0))
            {
                isRotating = false;
            }
        }
    }
}