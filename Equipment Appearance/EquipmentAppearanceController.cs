using System;
using System.Collections.Generic;
using UnityEngine;

namespace OnceAgain.Appearance
{
    public enum BodyVariantType
    {
        FullBody = 0,
        NoTorso = 1,
        // Add more later if needed: NoLegs, NoArms, etc.
    }

    [Serializable]
    public class BodyVariant
    {
        public BodyVariantType type;

        [Tooltip("Mesh to use for this variant (must match the same skeleton).")]
        public Mesh mesh;

        [Tooltip("Materials to assign for this variant. If empty, existing materials are kept.")]
        public Material[] materials;
    }

    // Client-side only: strip from pure server builds
    #if !UNITY_SERVER || UNITY_EDITOR
    public class EquipmentAppearanceController : MonoBehaviour
    {
        [Header("Body Renderer")]
        [Tooltip("SkinnedMeshRenderer for the main body on this avatar (e.g. female test body).")]
        public SkinnedMeshRenderer bodyRenderer;

        [Header("Body Variants")]
        [Tooltip("Different mesh/material combinations for this avatar.")]
        public List<BodyVariant> bodyVariants = new List<BodyVariant>();

        [Header("Debug / Preview")]
        [Tooltip("Log debug info when preview methods are used.")]
        public bool debugLogging = true;

        // --- Variant lookup ---

        public BodyVariant GetVariant(BodyVariantType type)
        {
            for (int i = 0; i < bodyVariants.Count; ++i)
            {
                if (bodyVariants[i] != null && bodyVariants[i].type == type)
                    return bodyVariants[i];
            }

            return null;
        }

        // --- Core body variant apply logic ---

        public void ApplyBodyVariant(BodyVariantType type)
        {
            if (bodyRenderer == null)
            {
                Log("No bodyRenderer assigned. Cannot apply body variant.");
                return;
            }

            var variant = GetVariant(type);
            if (variant == null)
            {
                Log($"No body variant of type '{type}' found on this controller.");
                return;
            }

            if (variant.mesh == null)
            {
                Log($"Body variant '{type}' has no mesh assigned.");
                return;
            }

            bodyRenderer.sharedMesh = variant.mesh;

            if (variant.materials != null && variant.materials.Length > 0)
            {
                bodyRenderer.sharedMaterials = variant.materials;
            }

            Log($"Applied body variant: {type}");
        }

        private void Log(string msg)
        {
            if (!debugLogging) return;
            Debug.Log($"[EquipmentAppearance] {name}: {msg}");
        }

        // --- Editor context menu helpers (gear menu on the component) ---

        #if UNITY_EDITOR
        [ContextMenu("Equipment Appearance/Preview/Use Full Body")]
        private void ContextPreviewFullBody()
        {
            ApplyBodyVariant(BodyVariantType.FullBody);
        }

        [ContextMenu("Equipment Appearance/Preview/Use No Torso")]
        private void ContextPreviewNoTorso()
        {
            ApplyBodyVariant(BodyVariantType.NoTorso);
        }
        #endif
    }
    #endif
}
