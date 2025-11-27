using UnityEngine;

// Generic interface for anything a Player can interact with.
//
// Implement this on a MonoBehaviour that lives on the same GameObject
// as an InteractionTarget (or a parent/child you can reach via GetComponent).
public interface IPlayerInteractable
{
    // Called on the SERVER when the player interacts with a server-authoritative object.
    void OnInteractServer(Player player);

    // Called on the LOCAL CLIENT when the interaction is local-only (no networking needed).
    // You can leave the implementation empty if you don't need client-local behavior.
    void OnInteractClient(Player player);
}
