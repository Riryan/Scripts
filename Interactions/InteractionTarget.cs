using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class InteractionTarget : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Text to show in the interaction prompt, e.g. 'Bind Graveyard' or 'Open Door'.")]
    public string prompt = "Interact";

    [Tooltip("Maximum distance from the player to interact.")]
    public float maxDistance = 3f;

    [Tooltip("If true, interaction is sent to the server (authoritative). If false, it is handled locally.")]
    public bool serverAuthoritative = true;

    // Optional network identity (required if serverAuthoritative == true).
    public NetworkIdentity Identity { get; private set; }

    void Awake()
    {
        Identity = GetComponent<NetworkIdentity>();
    }

    // Helper: check if the player is close enough to interact.
    public bool IsInRange(Player player)
    {
        if (player == null) return false;
        return Vector3.Distance(player.transform.position, transform.position) <= maxDistance;
    }

    // Helper: find an IPlayerInteractable implementation on this GameObject.
    public bool TryGetInteractable(out IPlayerInteractable interactable)
    {
        interactable = GetComponent<IPlayerInteractable>();
        return interactable != null;
    }
}
