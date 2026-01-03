using UnityEngine;

namespace uMMORPG
{
    public partial struct EquipmentInfo
    {
        [Header("Mesh Switcher")]
        [Tooltip("List of mesh GameObjects that can be toggled by EquipmentItem.meshIndex")]
        public SwitchableMesh[] mesh;

#if UNITY_EDITOR
        public bool HasMeshSwitcher => mesh != null && mesh.Length > 0;
#endif
    }
}
