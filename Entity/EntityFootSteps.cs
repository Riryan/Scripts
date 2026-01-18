using UnityEngine;
using UnityEngine.AI;

namespace uMMORPG
{
    // Client-side, movement-agnostic footstep system.
    // - No networking
    // - No animation events
    // - Works for NavMesh + CharacterController
    // - Safe for headless servers
    public abstract partial class Entity
    {
        [Header("Footsteps")]
        [Tooltip("Default footstep sounds (ground / stone / generic).")]
        public AudioClip[] footstepDefault;

        [Tooltip("Footstep sounds while swimming / shallow water.")]
        public AudioClip[] footstepWater;

        [Tooltip("Footstep sounds while mounted.")]
        public AudioClip[] footstepMounted;

        [Tooltip("Distance (meters) required to trigger the next step while walking.")]
        [Min(0.1f)]
        public float stepDistance = 2.2f;

        [Tooltip("Maximum distance at which other entities can hear footsteps.")]
        [Min(0f)]
        public float footstepHearDistance = 12f;

        [Tooltip("Base volume for footstep sounds.")]
        [Range(0f, 1f)]
        public float baseFootstepVolume = 0.9f;

        // internal state ---------------------------------------------------------
        Vector3 lastStepPosition;
        float nextAllowedStepTime;
        int cachedNavMeshAreaMask;

        // -----------------------------------------------------------------------
        // called once per frame from UpdateClient() of Player / Monster / NPC
        // -----------------------------------------------------------------------
        protected void UpdateFootsteps()
        {
            // client-only guard (server & headless safe)
            if (!isClient) return;

            // no movement component => no footsteps
            if (movement == null) return;

            // only while actually moving
            if (!movement.IsMoving()) return;

            // entity state gate (defensive)
            if (state == "DEAD" || state == "STUNNED") return;

            // anti-spam time gate
            if (Time.time < nextAllowedStepTime) return;

            // distance gate
            float travelled = Vector3.Distance(transform.position, lastStepPosition);
            if (travelled < stepDistance) return;

            // hearing gate for non-local entities
            if (!isLocalPlayer && !IsWithinHearingRange()) return;

            // commit step
            lastStepPosition = transform.position;
            nextAllowedStepTime = Time.time + 0.15f; // hard upper cap

            PlayFootstepInternal();
        }

        // -----------------------------------------------------------------------
        bool IsWithinHearingRange()
        {
            if (Player.localPlayer == null) return false;

            float distance =
                Vector3.Distance(
                    Player.localPlayer.transform.position,
                    transform.position
                );

            return distance <= footstepHearDistance;
        }

        // -----------------------------------------------------------------------
        void PlayFootstepInternal()
        {
            AudioClip[] clips = ResolveFootstepSet();
            if (clips == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];

            audioSource.spatialBlend = 1f; // fully 3D
            audioSource.volume = baseFootstepVolume;
            audioSource.PlayOneShot(clip);
        }

        // -----------------------------------------------------------------------
        AudioClip[] ResolveFootstepSet()
        {
            // mounted has highest priority
            if (this is Player player &&
                player.mountControl != null &&
                player.mountControl.IsMounted())
            {
                return footstepMounted;
            }

            // NavMesh surface sampling (cheap, cached)
            if (NavMesh.SamplePosition(
                    transform.position,
                    out NavMeshHit hit,
                    0.4f,
                    NavMesh.AllAreas))
            {
                cachedNavMeshAreaMask = hit.mask;

                int waterArea = NavMesh.GetAreaFromName("Water");
                if (waterArea >= 0 &&
                    (cachedNavMeshAreaMask & (1 << waterArea)) != 0)
                {
                    return footstepWater;
                }
            }

            return footstepDefault;
        }
    }
}
