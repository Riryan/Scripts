using UnityEngine;
using UnityEngine.UI;
using Mirror;

// Simple "[Key] Prompt" UI for interactions.
// - Uses the existing target system first (Player.target).
// - Falls back to world InteractionTarget (Tombstones, doors, harvestables, etc.).
// - Uses an OverlapSphere around the player to pick the best world target so it isn't "touchy".
public class UI_InteractionPrompt : MonoBehaviour
{
    [Header("References")]
    [Tooltip("UI Text to show the prompt, e.g. '[E] Interact' or '[E] Bind Graveyard'.")]
    public Text promptText;

    [Tooltip("Optional: camera to use for facing direction. If null, Camera.main is used, but only for direction.")]
    public Camera raycastCamera;

    [Header("World Interaction")]
    [Tooltip("Layers considered for world interactions (InteractionTarget).")]
    public LayerMask interactableLayers = ~0;

    [Header("Debug")]
    [Tooltip("Enable this to log why the prompt is shown/hidden.")]
    public bool debugLogs = false;

    Player player;
    string lastReason = "";

    void Awake()
    {
        if (promptText != null)
            promptText.enabled = false;

        if (debugLogs)
            Debug.Log("[UI_InteractionPrompt] Awake");
    }

    void Update()
    {
        string reason = "";

        // --- Resolve local player -------------------------------------------------
        if (player == null)
        {
            if (NetworkClient.localPlayer != null)
                player = NetworkClient.localPlayer.GetComponent<Player>();

            if (player == null)
            {
                if (promptText != null) promptText.enabled = false;
                reason = "NoLocalPlayer";
                LogReason(reason);
                return;
            }
            else if (debugLogs)
            {
                Debug.Log($"[UI_InteractionPrompt] Found local Player: {player.name}");
            }
        }

        if (!player.isLocalPlayer)
        {
            if (promptText != null) promptText.enabled = false;
            reason = "PlayerNotLocal";
            LogReason(reason);
            return;
        }

        if (promptText == null)
        {
            reason = "NoPromptTextAssigned";
            LogReason(reason);
            return;
        }

        // --- Only show when player can actually interact -------------------------
        if (!(player.state == "IDLE" ||
              player.state == "MOVING" ||
              player.state == "CASTING" ||
              player.state == "STUNNED"))
        {
            promptText.enabled = false;
            reason = $"StateDisallowed:{player.state}";
            LogReason(reason);
            return;
        }

        // --- 1) Entity Target Interaction (original target system) ---------------
        if (player.target != null && player.target != player)
        {
            float distance = Utils.ClosestDistance(player, player.target);
            if (distance <= player.interactionRange)
            {
                promptText.text = $"[{player.interactKey}] Interact";
                promptText.enabled = true;
                reason = $"EntityTarget:{player.target.name} (dist={distance:0.00})";
                LogReason(reason);
                return;
            }
            else
            {
                // still might have a world target, so don't early-return
                reason = $"EntityTooFar:{player.target.name} (dist={distance:0.00})";
            }
        }

        // --- 2) World Interaction using OverlapSphere (stable & less touchy) -----
        InteractionTarget bestWorldTarget = FindBestWorldTarget(out string worldReason);

        if (bestWorldTarget != null)
        {
            string label = string.IsNullOrWhiteSpace(bestWorldTarget.prompt)
                ? "Interact"
                : bestWorldTarget.prompt;

            promptText.text = $"[{player.interactKey}] {label}";
            promptText.enabled = true;
            reason = $"WorldTarget:{bestWorldTarget.name} label='{label}' ({worldReason})";
            LogReason(reason);
            return;
        }
        else if (!string.IsNullOrEmpty(worldReason))
        {
            reason = worldReason;
        }
        else
        {
            reason = "NoWorldTarget";
        }

        // --- 3) Nothing to interact with -----------------------------------------
        promptText.enabled = false;
        LogReason(reason);
    }

    // Finds the best world InteractionTarget around the player using OverlapSphere.
    // "Best" means: in range, on the right layer, and most in front of the player (by angle),
    // with some bias towards closer ones.
    InteractionTarget FindBestWorldTarget(out string debug)
    {
        debug = "";
        if (player == null) { debug = "NoPlayer"; return null; }

        float range = player.interactionRange;
        Vector3 origin = player.transform.position + Vector3.up * 1.2f; // chest height

        // ignore the player's own layer
        int mask = interactableLayers;
        mask &= ~(1 << player.gameObject.layer);

        Collider[] hits = Physics.OverlapSphere(origin, range, mask, QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0)
        {
            debug = "OverlapSphereNoHits";
            return null;
        }

        // Direction we consider "forward"
        Vector3 fwd;
        if (raycastCamera != null)
        {
            fwd = raycastCamera.transform.forward;
        }
        else
        {
            fwd = player.transform.forward;
        }
        fwd.y = 0;
        if (fwd.sqrMagnitude < 0.0001f)
            fwd = Vector3.forward;
        fwd.Normalize();

        InteractionTarget best = null;
        float bestScore = 0f;
        int candidateCount = 0;

        foreach (var col in hits)
        {
            if (col == null) continue;

            InteractionTarget t = col.GetComponentInParent<InteractionTarget>();
            if (t == null) continue;
            if (!t.IsInRange(player)) continue;

            // Vector from player to target (flattened on XZ)
            Vector3 to = t.transform.position - player.transform.position;
            float dist = new Vector2(to.x, to.z).magnitude;
            if (dist < 0.01f) dist = 0.01f;

            Vector3 toFlat = new Vector3(to.x, 0f, to.z).normalized;

            // angle alignment: 1 = straight in front, 0 = at 90 degrees
            float dot = Mathf.Clamp01(Vector3.Dot(fwd, toFlat));

            // score = angle alignment / (1 + distance) so closer + more in front wins
            float score = dot / (1f + dist);

            candidateCount++;

            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        if (best == null)
        {
            debug = $"OverlapSphereNoValidTargets (hits={hits.Length}, candidates={candidateCount})";
            return null;
        }

        debug = $"Best={best.name} score={bestScore:0.00} candidates={candidateCount}";
        return best;
    }

    void LogReason(string reason)
    {
        if (!debugLogs) return;
        if (reason == lastReason) return;

        lastReason = reason;
        Debug.Log($"[UI_InteractionPrompt] {reason}");
    }
}
