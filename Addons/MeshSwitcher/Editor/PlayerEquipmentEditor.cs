#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace uMMORPG
{
    [CustomEditor(typeof(PlayerEquipment))]
    public class PlayerEquipmentEditor : Editor
    {
        SerializedProperty slotInfoProp;
        bool showCustomizationSlots = false;

        void OnEnable()
        {
            slotInfoProp = serializedObject.FindProperty("slotInfo");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw everything except slotInfo normally
            DrawPropertiesExcluding(serializedObject, "slotInfo");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Equipment Slots", EditorStyles.boldLabel);

            showCustomizationSlots =
                EditorGUILayout.ToggleLeft("Show Customization Slots (__Customization__)", showCustomizationSlots);

            EditorGUILayout.Space();

            for (int i = 0; i < slotInfoProp.arraySize; i++)
            {
                SerializedProperty element = slotInfoProp.GetArrayElementAtIndex(i);
                SerializedProperty categoryProp = element.FindPropertyRelative("requiredCategory");

                string category = categoryProp.stringValue;

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
