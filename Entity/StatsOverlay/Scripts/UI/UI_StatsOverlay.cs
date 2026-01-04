using UnityEngine;

namespace uMMORPG
{
    public class UI_StatsOverlay : MonoBehaviour
    {
#if !UNITY_SERVER
        public Entity entity;
        public GameObject healthBar;

        [Tooltip("Optional distance cull (0 = disabled)")]
        public float maxDistance = 0f;

        MaterialPropertyBlock matBlock;
        MeshRenderer meshRenderer;

        void Awake()
        {
            if (healthBar)
                meshRenderer = healthBar.GetComponent<MeshRenderer>();

            matBlock = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            if (!entity || !healthBar || !meshRenderer)
            {
                if (healthBar) healthBar.SetActive(false);
                return;
            }

            entity.health.onEmpty.SetListener(OnDeathHide);
            UpdateHealthBar();
        }

        void Update()
        {
            // Lightweight client-side update only
            UpdateHealthBar();
        }

        void UpdateHealthBar()
        {
            if (!entity || !healthBar || meshRenderer == null)
                return;

            // Rule 1: hide at full or zero health
            if (entity.health.current <= 0 || entity.health.current >= entity.health.max)
            {
                DisableBar();
                return;
            }

            // Rule 2: optional distance cull (client-only, no network)
            if (maxDistance > 0f && Player.localPlayer != null)
            {
                float dist = Vector3.Distance(
                    Player.localPlayer.transform.position,
                    entity.transform.position
                );

                if (dist > maxDistance)
                {
                    DisableBar();
                    return;
                }
            }

            EnableBar();

            float pct = (float)entity.health.current / entity.health.max;
            meshRenderer.GetPropertyBlock(matBlock);
            matBlock.SetFloat("_Fill", pct);
            meshRenderer.SetPropertyBlock(matBlock);
        }

        void EnableBar()
        {
            if (!healthBar.activeSelf)
                healthBar.SetActive(true);

            meshRenderer.enabled = true;
        }

        void DisableBar()
        {
            if (healthBar.activeSelf)
                healthBar.SetActive(false);

            if (meshRenderer)
                meshRenderer.enabled = false;
        }

        void OnDeathHide()
        {
            DisableBar();
        }
#endif
    }
}
