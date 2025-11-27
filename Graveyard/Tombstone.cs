using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class Tombstone : MonoBehaviour, IPlayerInteractable
{
    [Header("Graveyard")]
    [Tooltip("Unique ID across all scenes. Dev must ensure uniqueness.")]
    public int tombstoneId;

    [Tooltip("Optional custom spawn point; defaults to own transform.")]
    public Transform spawnPoint;

    void Awake()
    {
        if (!spawnPoint) spawnPoint = transform;
    }

#if UNITY_SERVER || UNITY_EDITOR
    void OnEnable()
    {
        // Register only if the active NetworkManager is our MMO manager
        if (NetworkManager.singleton is NetworkManagerMMO manager && manager != null)
        {
            manager.RegisterTombstone(this);
        }
    }

    void OnDisable()
    {
        if (NetworkManager.singleton is NetworkManagerMMO manager && manager != null)
        {
            manager.UnregisterTombstone(this);
        }
    }
#endif

    // -------- IPlayerInteractable implementation --------

    // Called on SERVER when interacted with via InteractionTarget
    public void OnInteractServer(Player player)
    {
        if (player == null) return;

        player.BindToTombstone(tombstoneId);
        Debug.Log($"[Graveyard] {player.name} bound to tombstone {tombstoneId}");
    }

    // Local-only branch isn't needed now, but you can use it for VFX or sounds later.
    public void OnInteractClient(Player player)
    {
        // e.g., play a local sound or UI flash
        Debug.Log($"[Tombstone] OnInteractServer fired for {player.name}, id={tombstoneId}");
    }
}
