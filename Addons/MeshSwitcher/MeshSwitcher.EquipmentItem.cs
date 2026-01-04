using UnityEngine;

namespace uMMORPG
{
    public enum EquipmentVisualMode
    {
        MeshSwitch,
        Prefab
    }
    // EQUIPMENT ITEM (Mesh Switcher extension)
    public partial class EquipmentItem
    {
        [Header("Mesh Switcher")]
        [Tooltip("Indices into PlayerEquipment.slotInfo[x].mesh[] that should be enabled")]
        public int[] meshIndex;

        [Tooltip("Optional material override for enabled meshes")]
        public Material meshMaterial;

        [Tooltip("Optional color overrides applied to the material")]
        public SwitchableColor[] switchableColors;



        [Header("Visual Mode")]
        public EquipmentVisualMode visualMode = EquipmentVisualMode.Prefab;
    

#if UNITY_EDITOR
        void OnValidate()
        {
            if (meshIndex == null)
                return;

            // sanitize indices (editor safety only)
            for (int i = 0; i < meshIndex.Length; i++)
            {
                if (meshIndex[i] < 0)
                    meshIndex[i] = 0;
            }
        }
#endif
    }
}
