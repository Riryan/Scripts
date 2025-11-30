
using UnityEngine;

public class UI_StatsOverlay : MonoBehaviour
{
    [Tooltip("Optional. If not set, will try to find an Entity in parents.")]
    public Entity entity;

    [Tooltip("The health bar GameObject with a MeshRenderer. If not set, uses this GameObject.")]
    public GameObject healthBar;

#if !UNITY_SERVER || UNITY_EDITOR
    MaterialPropertyBlock matBlock;
    MeshRenderer meshRenderer;

    // Cache last values so we only update when something actually changes
    int  lastCurrent   = int.MinValue;
    int  lastMax       = int.MinValue;
    bool lastIsTarget  = false;
    bool isCurrentTarget = false;

    void Awake()
    {
        // Try to auto-resolve dependencies if they aren't wired in the prefab
        if (entity == null)
            entity = GetComponentInParent<Entity>();

        if (healthBar == null)
            healthBar = gameObject;

        if (healthBar != null)
            meshRenderer = healthBar.GetComponent<MeshRenderer>();

        matBlock = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        // Re-resolve in case things were set up at runtime
        if (entity == null)
            entity = GetComponentInParent<Entity>();

        if (healthBar == null)
            healthBar = gameObject;

        if (meshRenderer == null && healthBar != null)
            meshRenderer = healthBar.GetComponent<MeshRenderer>();

        // Validate dependencies
        if (entity == null || healthBar == null || meshRenderer == null)
        {
            if (healthBar != null)
                healthBar.SetActive(false);

            enabled = false;
            return;
        }

        meshRenderer.enabled = true;
        healthBar.SetActive(true);

        // Initialize target state
        Player player = Player.localPlayer;
        isCurrentTarget = (player != null) && ((player.nextTarget ?? player.target) == entity);

        // Force an initial update so the bar starts in a correct state
        lastCurrent  = int.MinValue;
        lastMax      = int.MinValue;
        lastIsTarget = !isCurrentTarget; // ensure first LateUpdate triggers
        UpdateHealBar();
    }

    void LateUpdate()
    {
        if (entity == null)
            return;

        if (entity.health == null || entity.health.max <= 0)
            return;

        int current = entity.health.current;
        int max     = entity.health.max;

        // Check if this entity is the current UITarget's target
        Player player = Player.localPlayer;
        bool isTargetNow = (player != null) && ((player.nextTarget ?? player.target) == entity);

        // Only update if health or target status changed
        if (current == lastCurrent && max == lastMax && isTargetNow == lastIsTarget)
            return;

        isCurrentTarget = isTargetNow;
        lastCurrent     = current;
        lastMax         = max;
        lastIsTarget    = isTargetNow;

        UpdateHealBar();
    }

    public void UpdateHealBar()
    {
        if (entity == null || entity.health == null || healthBar == null || meshRenderer == null)
            return;

        int current = entity.health.current;
        int max     = Mathf.Max(1, entity.health.max); // avoid zero

        bool alive   = current > 0;
        bool damaged = current < max;

        // Show bar while alive AND (damaged OR this is the current target)
        bool show = alive && (damaged || isCurrentTarget);

        meshRenderer.enabled = show;
        healthBar.SetActive(show);

        float percentLife = current / (float)max;

        meshRenderer.GetPropertyBlock(matBlock);
        matBlock.SetFloat("_Fill", percentLife);
        meshRenderer.SetPropertyBlock(matBlock);
    }

    // Kept in case you ever want to hook some other death logic in the future.
    void onDeathHide()
    {
        if (healthBar != null)
            healthBar.SetActive(false);
    }
#endif
}