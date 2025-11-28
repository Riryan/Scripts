#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace OnceAgain.Appearance.Editor
{
    [CustomEditor(typeof(EquipmentAppearanceController))]
    public class EquipmentAppearanceControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw normal fields first
            base.OnInspectorGUI();

            var controller = (EquipmentAppearanceController)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Equipment Appearance Preview", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Use these buttons in Play Mode on your female test prefab.\n" +
                "They will swap the body mesh to the selected body variant.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview: Full Body"))
            {
                controller.ApplyBodyVariant(BodyVariantType.FullBody);
            }
            if (GUILayout.Button("Preview: No Torso"))
            {
                controller.ApplyBodyVariant(BodyVariantType.NoTorso);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
