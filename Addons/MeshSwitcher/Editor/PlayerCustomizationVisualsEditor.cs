#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerCustomizationVisuals))]
public class PlayerCustomizationVisualsEditor : Editor
{
    PlayerCustomizationVisuals visuals;

    int previewSlot = 0;
    int previewIndex = 0;

    void OnEnable()
    {
        visuals = (PlayerCustomizationVisuals)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Customization Preview", EditorStyles.boldLabel);

        if (visuals.slots == null || visuals.slots.Length == 0)
        {
            EditorGUILayout.HelpBox("No customization slots defined.", MessageType.Info);
            return;
        }

        previewSlot = EditorGUILayout.Popup(
            "Slot",
            previewSlot,
            GetSlotNames()
        );

        var slot = visuals.slots[previewSlot];
        if (slot.meshes == null || slot.meshes.Length == 0)
        {
            EditorGUILayout.HelpBox("Slot has no meshes.", MessageType.Warning);
            return;
        }

        previewIndex = EditorGUILayout.IntSlider(
            "Mesh Index",
            previewIndex,
            0,
            slot.meshes.Length - 1
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Preview"))
        {
            Preview(slot, previewIndex);
        }

        if (GUILayout.Button("Reset Slot"))
        {
            ResetSlot(slot);
        }

        if (GUILayout.Button("Reset All"))
        {
            ResetAll();
        }

        EditorGUILayout.EndHorizontal();
    }

    string[] GetSlotNames()
    {
        string[] names = new string[visuals.slots.Length];
        for (int i = 0; i < names.Length; i++)
            names[i] = visuals.slots[i].name;
        return names;
    }

    void Preview(PlayerCustomizationVisuals.CustomizationSlot slot, int index)
    {
        for (int i = 0; i < slot.meshes.Length; i++)
        {
            if (slot.meshes[i]?.mesh != null)
                slot.meshes[i].mesh.SetActive(i == index);
        }

        SceneView.RepaintAll();
    }

    void ResetSlot(PlayerCustomizationVisuals.CustomizationSlot slot)
    {
        for (int i = 0; i < slot.meshes.Length; i++)
        {
            if (slot.meshes[i]?.mesh != null)
                slot.meshes[i].mesh.SetActive(i == 0);
        }

        SceneView.RepaintAll();
    }

    void ResetAll()
    {
        foreach (var slot in visuals.slots)
            ResetSlot(slot);
    }
}
#endif
