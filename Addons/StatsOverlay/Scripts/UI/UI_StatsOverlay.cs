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

    // Cache last values so we only update when health actually changes
    int lastCurrent = int.MinValue;
    int lastMax     = int.MinValue;

    private void Awake()
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

    private void OnEnable()
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

            // Nothing useful to do without these, so disable the script
            enabled = false;
            return;
        }

        meshRenderer.enabled = true;
        healthBar.SetActive(true);

        // Force an initial update so the bar starts in a correct state
        lastCurrent = int.MinValue;
        lastMax     = int.MinValue;
        UpdateHealBar();
    }

    private void LateUpdate()
    {
        if (entity == null)
            return;

        // Safety: avoid division by zero / nonsense states
        if (entity.health == null || entity.health.max <= 0)
            return;

        int current = entity.health.current;
        int max     = entity.health.max;

        // Only update the material if something actually changed
        if (current == lastCurrent && max == lastMax)
            return;

        UpdateHealBar();
    }

    public void UpdateHealBar()
    {
        if (entity == null || entity.health == null || healthBar == null || meshRenderer == null)
            return;

        int current = entity.health.current;
        int max     = Mathf.Max(1, entity.health.max); // clamp to avoid zero

        lastCurrent = current;
        lastMax     = max;

        bool alive   = current > 0;
        bool damaged = current < max;

        // Show bar only while alive and damaged
        meshRenderer.enabled = alive;
        healthBar.SetActive(alive && damaged);

        float percentLife = current / (float)max;

        meshRenderer.GetPropertyBlock(matBlock);
        matBlock.SetFloat("_Fill", percentLife);
        meshRenderer.SetPropertyBlock(matBlock);
    }

    // Kept in case you want to manually hook something else to hide the bar.
    private void onDeathHide()
    {
        if (healthBar != null)
            healthBar.SetActive(false);
    }
#endif
}
