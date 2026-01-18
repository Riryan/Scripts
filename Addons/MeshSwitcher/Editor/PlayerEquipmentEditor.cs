#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace uMMORPG
{
    [CustomEditor(typeof(PlayerEquipment))]
    public class PlayerEquipmentEditor : Editor
    {
        bool showCustomizationSlots = false;

        SerializedProperty SlotInfoProp =>
            serializedObject != null
                ? serializedObject.FindProperty("slotInfo")
                : null;

        void OnEnable()
        {
            // Domain reload safe
            if (target == null)
                return;
        }

        void OnDisable()
        {
            // Never hold editor state across reloads
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null)
                return;

            serializedObject.Update();

            SerializedProperty slotInfoProp = SlotInfoProp;
            if (slotInfoProp == null || !slotInfoProp.isArray)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Draw all fields except slotInfo
            DrawPropertiesExcluding(serializedObject, "slotInfo");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Equipment Slots", EditorStyles.boldLabel);

            showCustomizationSlots = EditorGUILayout.ToggleLeft(
                "Show Customization Slots (__Customization__)",
                showCustomizationSlots
            );

            EditorGUILayout.Space();

            for (int i = 0; i < slotInfoProp.arraySize; i++)
            {
                SerializedProperty element =
                    slotInfoProp.GetArrayElementAtIndex(i);

                if (element == null)
                    continue;

                SerializedProperty categoryProp =
                    element.FindPropertyRelative("requiredCategory");

                string category = categoryProp != null
                    ? categoryProp.stringValue
                    : string.Empty;

                bool isCustomization =
                    !string.IsNullOrEmpty(category) &&
                    category.StartsWith("__Customization__");

                if (isCustomization && !showCustomizationSlots)
                    continue;

                EditorGUILayout.PropertyField(
                    element,
                    new GUIContent($"Slot {i}: {category}"),
                    true
                );

                EditorGUILayout.Space(2);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
