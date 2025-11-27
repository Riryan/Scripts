using UnityEngine;

[CreateAssetMenu(menuName = "uMMORPG/Harvest/Rule")]
public class HarvestRule : ScriptableObject
{
    [Tooltip("Unique per prototype (0..1023 is plenty)")]
    public int PrototypeId = 1;

    [Header("Generation")]
    [Tooltip("Average count per 64m cell")]
    public float averageCountPerCell = 12f;
    [Tooltip("Minimum spacing between nodes (meters)")]
    public float minSpacing = 3f;

    [Header("Respawn (seconds)")]
    public Vector2 respawnSecondsRange = new Vector2(30, 60);

    [Header("Client Visuals")]
    public GameObject prefabWhole;
    public GameObject prefabStump;
}
