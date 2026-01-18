#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using uMMORPG;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(PlayerMeshSwitcher))]
public class PlayerMeshSwitcherEditor : Editor
{
    int selectedSlot = 0;
    int selectedMeshIndex = 0; // ALWAYS >= 0 (mesh[0] = default)

    Material previewMaterial;
    string colorProperty = "_Color";
    Color previewColor = Color.white;

    EquipmentItem targetEquipmentItem;

    Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();

    public override void OnInspectorGUI()
    {
        if (target == null)
            return;
        DrawDefaultInspector();

        PlayerMeshSwitcher switcher = (PlayerMeshSwitcher)target;
        PlayerEquipment equipment = switcher.GetComponent<PlayerEquipment>();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mesh Switcher – Editor Preview", EditorStyles.boldLabel);

        if (equipment == null)
        {
            EditorGUILayout.HelpBox("PlayerEquipment not found.", MessageType.Warning);
            return;
        }

        // ----------------------------------------------------
        // SLOT DROPDOWN
        // ----------------------------------------------------
        string[] slotNames = equipment.slotInfo
            .Select((s, i) => $"{i}: {s.requiredCategory}")
            .ToArray();

        selectedSlot = EditorGUILayout.Popup("Equipment Slot", selectedSlot, slotNames);

        EquipmentInfo info = equipment.slotInfo[selectedSlot];

        if (info.mesh == null || info.mesh.Length == 0)
        {
            EditorGUILayout.HelpBox("No meshes configured for this slot.", MessageType.Info);
            return;
        }

        // Enforce default index 0
        if (selectedMeshIndex < 0 || selectedMeshIndex >= info.mesh.Length)
            selectedMeshIndex = 0;

        // ----------------------------------------------------
        // MESH DROPDOWN (NO 'NONE')
        // ----------------------------------------------------
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mesh Selection", EditorStyles.boldLabel);

        string[] meshOptions = new string[info.mesh.Length];
        for (int i = 0; i < info.mesh.Length; i++)
        {
            meshOptions[i] = info.mesh[i]?.mesh != null
                ? info.mesh[i].mesh.name
                : $"Mesh {i} (empty)";
        }

        selectedMeshIndex = EditorGUILayout.Popup(
            "Active Mesh",
            selectedMeshIndex,
            meshOptions
        );

        // ----------------------------------------------------
        // MATERIAL + COLOR PREVIEW
        // ----------------------------------------------------
        EditorGUILayout.Space();
        previewMaterial = (Material)EditorGUILayout.ObjectField(
            "Material Override",
            previewMaterial,
            typeof(Material),
            false
        );

        colorProperty = EditorGUILayout.TextField("Color Property", colorProperty);
        previewColor = EditorGUILayout.ColorField("Preview Color", previewColor);

        // ----------------------------------------------------
        // PREVIEW CONTROLS
        // ----------------------------------------------------
        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Preview"))
            ApplyPreview(info);

        if (GUILayout.Button("Revert To Defaults"))
            RevertDefaults(info);

        // ----------------------------------------------------
        // WRITE TO EQUIPMENT ITEM
        // ----------------------------------------------------
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Write To Equipment Item", EditorStyles.boldLabel);

        targetEquipmentItem = (EquipmentItem)EditorGUILayout.ObjectField(
            "Equipment Item",
            targetEquipmentItem,
            typeof(EquipmentItem),
            false
        );

        if (targetEquipmentItem != null)
        {
            if (GUILayout.Button("Write Selection → EquipmentItem"))
                WriteMeshIndexToItem();
        }
    }

    // ----------------------------------------------------

    void CacheOriginal(Renderer r)
    {
        if (r != null && !originalMaterials.ContainsKey(r))
            originalMaterials[r] = r.sharedMaterial;
    }

    void ApplyPreview(EquipmentInfo info)
    {
        for (int i = 0; i < info.mesh.Length; i++)
        {
            SwitchableMesh sm = info.mesh[i];
            if (sm?.mesh == null)
                continue;

            Renderer r = sm.mesh.GetComponent<Renderer>();
            if (r == null)
                continue;

            CacheOriginal(r);

            bool enable = (i == selectedMeshIndex);
            sm.mesh.SetActive(enable);

            if (!enable)
                continue;

            r.sharedMaterial = previewMaterial != null
                ? previewMaterial
                : originalMaterials[r];

            if (r.sharedMaterial != null &&
                r.sharedMaterial.HasProperty(colorProperty))
            {
                r.sharedMaterial.SetColor(colorProperty, previewColor);
            }
        }

        EditorUtility.SetDirty(target);
    }

    void RevertDefaults(EquipmentInfo info)
    {
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
                kvp.Key.sharedMaterial = kvp.Value;
        }

        originalMaterials.Clear();

        for (int i = 0; i < info.mesh.Length; i++)
        {
            if (info.mesh[i]?.mesh != null)
                info.mesh[i].mesh.SetActive(i == 0);
        }

        EditorUtility.SetDirty(target);
    }
    void OnDisable()
    {
        originalMaterials.Clear();
        targetEquipmentItem = null;
    }

    void WriteMeshIndexToItem()
    {
        targetEquipmentItem.meshIndex = new[] { selectedMeshIndex };

        EditorUtility.SetDirty(targetEquipmentItem);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"MeshSwitcher: Wrote [{selectedMeshIndex}] to {targetEquipmentItem.name}"
        );
    }
}
#endif
