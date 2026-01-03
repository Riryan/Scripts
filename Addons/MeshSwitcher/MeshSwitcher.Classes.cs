using UnityEngine;

// SWITCHABLE MESH
[System.Serializable]
public partial class SwitchableMesh
{
    public GameObject mesh;
    [HideInInspector] public Material defaultMaterial;
}

// SWITCHABLE COLOR
[System.Serializable]
public partial class SwitchableColor
{
    [Tooltip("Required ")]
    public ColorPropertName propertyName;
    public Color color;
}

public enum ColorPropertName{
    _Color,
    _EmissionColor,
}
