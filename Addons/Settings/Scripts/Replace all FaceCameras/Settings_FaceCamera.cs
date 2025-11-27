using UnityEngine;

public partial class Settings_FaceCamera : MonoBehaviour
{
    private UI_SettingsVariables settingsVariables;
    private MeshRenderer mesh;
    private SpriteRenderer sprite;

    // Grab UI_SettingsVariables and the mesh for use later.
    private void Start()
    {
        settingsVariables = FindObjectOfType<UI_Settings>().GetComponent<UI_SettingsVariables>();

        mesh = GetComponent<MeshRenderer>();

        if (mesh != null) sprite = GetComponent<SpriteRenderer>();
    }

    // Check if our overhead mesh is allowed to show.
    private void Update()
    {
        if (!settingsVariables.isShowOverhead) 
            if (mesh != null) mesh.enabled = false;
            else if (sprite != null) sprite.enabled = false;
            else return;

        if (settingsVariables.isShowOverhead) 
            if (mesh != null) mesh.enabled = true; 
            else if (sprite != null) sprite.enabled = true; 
            else return;
    }

    // LateUpdate so that all camera updates are finished.
    private void LateUpdate()
    {
        if (settingsVariables.isShowOverhead)
            transform.forward = Camera.main.transform.forward;
    }
}