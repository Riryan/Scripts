using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterNameplateVisibility : MonoBehaviour
{
#if !UNITY_SERVER || UNITY_EDITOR
    [Header("References")]
    [Tooltip("Root GameObject for the monster's world-space nameplate (Canvas or parent object).")]
    [SerializeField] private GameObject nameplateRoot;

    [Tooltip("Optional: the aggro trigger collider used by this monster. " +
             "If set and is a SphereCollider, its world radius is used as base range.")]
    [SerializeField] private Collider aggroTrigger;

    [Header("Ranges")]
    [Tooltip("Fallback range in meters if no aggro trigger is assigned, or if trigger type isn't supported.")]
    [SerializeField] private float fallbackRange = 20f;

    [Tooltip("Nameplate shows at AggroRange * this multiplier.")]
    [SerializeField] private float rangeMultiplier = 1.2f;

    [Header("Behavior")]
    [Tooltip("If true, nameplate is always visible while this monster is the local player's target.")]
    [SerializeField] private bool alwaysShowWhenTargeted = true;

    private float visibleRangeSqr;
    private bool initialized;
    private bool lastVisible;
    private Entity owner;

    private void OnEnable()
    {
        if (nameplateRoot != null)
            nameplateRoot.SetActive(false);

        RecalculateRange();
        NameplateVisibilityManager.Register(this);
    }

    private void OnDisable()
    {
        NameplateVisibilityManager.Unregister(this);

        if (nameplateRoot != null)
            nameplateRoot.SetActive(false);

        lastVisible = false;
    }

    private void RecalculateRange()
    {
        float baseRange = fallbackRange > 0f ? fallbackRange : 20f;

        if (aggroTrigger != null)
        {
            // We only handle SphereCollider here (common for aggro areas).
            if (aggroTrigger is SphereCollider sphere)
            {
                Vector3 scale = aggroTrigger.transform.lossyScale;
                float maxScale = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                baseRange = sphere.radius * maxScale;
            }
            // Other collider types: just use fallbackRange.
        }

        float r = Mathf.Max(0.1f, baseRange * Mathf.Max(0.1f, rangeMultiplier));
        visibleRangeSqr = r * r;
        initialized = true;
    }

    // Called from NameplateVisibilityManager at a throttled rate.
    public void UpdateVisibility(Transform playerTransform, Entity playerTarget)
    {
        if (!initialized)
            RecalculateRange();

        // No player/target data? Hide and bail.
        if (nameplateRoot == null || playerTransform == null)
        {
            SetVisible(false);
            return;
        }

        Vector3 monsterPos = transform.position;
        Vector3 playerPos = playerTransform.position;

        float dx = playerPos.x - monsterPos.x;
        float dz = playerPos.z - monsterPos.z;
        float sqrDist = dx * dx + dz * dz;

        bool visible = sqrDist <= visibleRangeSqr;

        if (!visible && alwaysShowWhenTargeted)
        {
            if (owner == null)
                owner = GetComponent<Entity>();

            if (owner != null && playerTarget == owner)
                visible = true;
        }

        SetVisible(visible);
    }

    private void SetVisible(bool visible)
    {
        if (nameplateRoot == null)
            return;

        if (lastVisible == visible)
            return;

        lastVisible = visible;
        nameplateRoot.SetActive(visible);
    }
#endif
}
