using UnityEngine;
using uMMORPG;

[DisallowMultipleComponent]
public sealed class PlayerMeshSwitcher : MonoBehaviour
{
#if UNITY_SERVER
    void Awake()
    {
        enabled = false;
    }
#else
    [SerializeField, HideInInspector]
    PlayerEquipment equipment;

    // Phase 1 caches
    int[] lastMeshIndex;
    Renderer[][] slotRenderers;

    void OnValidate()
    {
        if (equipment == null)
            equipment = GetComponent<PlayerEquipment>();
    }

    void Awake()
    {
        if (equipment == null)
            equipment = GetComponent<PlayerEquipment>();

        if (equipment == null)
        {
            enabled = false;
            return;
        }

        int slotCount = equipment.slotInfo.Length;
        lastMeshIndex = new int[slotCount];
        slotRenderers = new Renderer[slotCount][];

        for (int i = 0; i < slotCount; i++)
            lastMeshIndex[i] = int.MinValue; // force first refresh
    }

    /// <summary>
    /// Called by PlayerEquipment.RefreshLocation(index)
    /// </summary>
    public void RefreshMesh(int index)
    {
        if (index < 0 || index >= equipment.slotInfo.Length)
            return;

        EquipmentInfo info = equipment.slotInfo[index];
        if (info.mesh == null || info.mesh.Length == 0)
            return;

        ItemSlot slot = equipment.slots[index];

        // ----------------------------------------------------
        // LOCKED RULE:
        // mesh[0] is ALWAYS the default "nothing equipped"
        // ----------------------------------------------------
        int desiredMesh = 0;

        if (slot.amount > 0 && slot.item.data is EquipmentItem)
        {
            EquipmentItem item = (EquipmentItem)slot.item.data;
            if (item.meshIndex != null && item.meshIndex.Length > 0)
                desiredMesh = item.meshIndex[0];
        }

        // Early-out if nothing changed
        if (lastMeshIndex[index] == desiredMesh)
            return;

        lastMeshIndex[index] = desiredMesh;

        EnsureRendererCache(index, info);
        ApplySlot(index, info, desiredMesh);
    }

    // ----------------------------------------------------
    // Internals
    // ----------------------------------------------------

    void EnsureRendererCache(int index, EquipmentInfo info)
    {
        if (slotRenderers[index] != null)
            return;

        Renderer[] renderers = new Renderer[info.mesh.Length];

        for (int i = 0; i < info.mesh.Length; i++)
        {
            SwitchableMesh sm = info.mesh[i];
            if (sm?.mesh == null)
                continue;

            Renderer r = sm.mesh.GetComponent<Renderer>();
            renderers[i] = r;

            // Capture default material ONCE
            if (r != null && sm.defaultMaterial == null)
                sm.defaultMaterial = r.sharedMaterial;
        }

        slotRenderers[index] = renderers;
    }

    void ApplySlot(int index, EquipmentInfo info, int activeMesh)
    {
        Renderer[] renderers = slotRenderers[index];
        ItemSlot slot = equipment.slots[index];

        EquipmentItem item =
            (slot.amount > 0 && slot.item.data is EquipmentItem)
            ? (EquipmentItem)slot.item.data
            : null;

        for (int i = 0; i < info.mesh.Length; i++)
        {
            SwitchableMesh sm = info.mesh[i];
            if (sm?.mesh == null)
                continue;

            bool enable = (i == activeMesh);
            sm.mesh.SetActive(enable);

            if (!enable)
                continue;

            Renderer r = renderers[i];
            if (r == null)
                continue;

            // Material override or default
            r.material = (item != null && item.meshMaterial != null)
                ? item.meshMaterial
                : sm.defaultMaterial;

            // Optional color overrides
            if (item != null && item.switchableColors != null)
            {
                foreach (var c in item.switchableColors)
                    r.material.SetColor(c.propertyName.ToString(), c.color);
            }
        }
    }
#endif
}
