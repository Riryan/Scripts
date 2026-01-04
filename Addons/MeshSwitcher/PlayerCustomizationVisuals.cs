using UnityEngine;
using uMMORPG;

[DisallowMultipleComponent]
public sealed class PlayerCustomizationVisuals : MonoBehaviour
{
#if UNITY_SERVER
    void Awake() => enabled = false;
#else
    [System.Serializable]
    public class CustomizationSlot
    {
        public string name; // Hair, Beard, Face (editor clarity only)
        public SwitchableMesh[] meshes;

        [Tooltip("Equipment category strings that hide this slot (e.g. Helmet, Mask)")]
        public string[] suppressedByCategories;
    }

    [Header("Customization Slots (Prefab-defined)")]
    public CustomizationSlot[] slots;

    PlayerCustomizationData data;
    bool[] suppressed;

    void Awake()
    {
        suppressed = new bool[slots.Length];
    }

    // ----------------------------------------------------
    // Called once after DB load
    // ----------------------------------------------------
    public void Apply(PlayerCustomizationData customization)
    {
        data = customization;
        RefreshAll();
    }

    // ----------------------------------------------------
    // Called by PlayerEquipment.RefreshLocation(...)
    // ----------------------------------------------------
    public void RefreshSuppression(PlayerEquipment equipment)
    {
        for (int i = 0; i < slots.Length; i++)
            suppressed[i] = IsSuppressed(slots[i], equipment);

        RefreshAll();
    }

    // ----------------------------------------------------

    void RefreshAll()
    {
        for (int i = 0; i < slots.Length; i++)
            ApplySlot(i);
    }

    void ApplySlot(int index)
    {
        if (index < 0 || index >= slots.Length)
            return;

        CustomizationSlot slot = slots[index];
        if (slot.meshes == null || slot.meshes.Length == 0)
            return;

        int selected = Mathf.Clamp(
            data.GetByIndex(index),
            0,
            slot.meshes.Length - 1
        );

        for (int i = 0; i < slot.meshes.Length; i++)
        {
            var sm = slot.meshes[i];
            if (sm?.mesh == null)
                continue;

            bool enable = !suppressed[index] && (i == selected);
            sm.mesh.SetActive(enable);
        }
    }

bool IsSuppressed(CustomizationSlot slot, PlayerEquipment equipment)
{
    if (slot.suppressedByCategories == null ||
        slot.suppressedByCategories.Length == 0)
        return false;

    for (int i = 0; i < equipment.slots.Count; i++)
    {
        ItemSlot s = equipment.slots[i];
        if (s.amount == 0)
            continue;

        string equippedCategory = equipment.slotInfo[i].requiredCategory;
        if (string.IsNullOrWhiteSpace(equippedCategory))
            continue;

        for (int c = 0; c < slot.suppressedByCategories.Length; c++)
        {
            if (equippedCategory == slot.suppressedByCategories[c])
                return true;
        }
    }

    return false;
}

#endif
}
