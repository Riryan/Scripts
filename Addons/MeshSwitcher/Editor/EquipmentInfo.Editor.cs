#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace uMMORPG
{
    // Editor-only helpers for EquipmentInfo
    public partial struct EquipmentInfo
    {
        public bool IsCustomizationSlot()
        {
            return !string.IsNullOrEmpty(requiredCategory)
                   && requiredCategory.StartsWith("__");
        }
    }

    // This hides customization slots in arrays (slotInfo)
    [CustomPropertyDrawer(typeof(EquipmentInfo))]
    public class EquipmentInfoDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty cat =
                property.FindPropertyRelative("requiredCategory");

            if (cat != null && cat.stringValue.StartsWith("__"))
                return 0f; // hide completely

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty cat =
                property.FindPropertyRelative("requiredCategory");

            if (cat != null && cat.stringValue.StartsWith("__"))
                return; // draw nothing

            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
#endif
