/*
using UnityEngine;
using Mirror;

public partial class Player
{
#if !UNITY_SERVER || UNITY_EDITOR
    [Header("Interaction System")]
    [Tooltip("Max distance for interaction raycasts (client side).")]
    public float interactCheckDistance = 4f;

    [Tooltip("Radius of the spherecast used for interaction detection.")]
    public float interactRayRadius = 0.3f;

    [Tooltip("Layers considered for interaction. Set this to your 'Interactable' layer(s).")]
    public LayerMask interactableLayers = ~0;

//    [Tooltip("Key used to trigger interaction.")]
//    public KeyCode interactKey = KeyCode.E;

    // Current target the player is looking at (client-side only).
    public InteractionTarget currentInteractionTarget;
#endif

    // ------------- CLIENT SIDE (input + raycast) -------------
#if !UNITY_SERVER || UNITY_EDITOR
    // Call this from your local-player client update loop.
    [Client]
    public void InteractionClientUpdate()
    {
        if (!isLocalPlayer) return;

        FindInteractionTargetClient();
        HandleInteractionInputClient();
    }

    [Client]
    void FindInteractionTargetClient()
    {
        currentInteractionTarget = null;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.SphereCast(ray, interactRayRadius, out RaycastHit hit, interactCheckDistance, interactableLayers, QueryTriggerInteraction.Collide))
        {
            currentInteractionTarget = hit.collider.GetComponentInParent<InteractionTarget>();
        }

        // TODO: Hook this up to a UI prompt if you want.
        // Example: UI_InteractionPrompt.Show(currentInteractionTarget?.prompt);
    }

    [Client]
    void HandleInteractionInputClient()
    {
        if (currentInteractionTarget == null) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (currentInteractionTarget.serverAuthoritative)
            {
                // Server-authoritative: send a request to the server.
                if (currentInteractionTarget.Identity != null)
                {
                    CmdInteract(currentInteractionTarget.Identity);
                }
            }
            else
            {
                // Local-only: just run the client-side logic.
                if (currentInteractionTarget.TryGetInteractable(out var interactable))
                {
                    interactable.OnInteractClient(this);
                }
            }
        }
    }
#endif

    // ------------- SERVER SIDE (authoritative interaction) -------------

    // Command from client to server: perform an interaction on the given target.
    [Command]
    void CmdInteract(NetworkIdentity targetIdentity)
    {
        if (targetIdentity == null) return;

        InteractionTarget target = targetIdentity.GetComponent<InteractionTarget>();
        if (target == null) return;

        // Server-side range validation
        if (!target.IsInRange(this))
            return;

        if (!target.TryGetInteractable(out var interactable))
            return;

        interactable.OnInteractServer(this);
    }
}
*/