using UnityEngine;

public partial class Entity
{
    [Header("Footstep Audio")]
    public bool enableFootsteps = true;
    public float footstepHearingDistance = 15f;
    public AudioClip[] footstepClips;                  // Randomly selected
    public float footstepInterval = 0.5f;              // Delay between steps
    public float footstepMinVelocity = 0.1f;           // Min movement speed
    public float footstepVolume = 1f;                  // Global volume
    public LayerMask groundLayerMask;                  // Terrain detection
    public AudioClip grassClip;
    public AudioClip stoneClip;
    public AudioClip snowClip;

    float footstepNextTime = 0;

    protected void UpdateFootsteps()
    {
        if (!enableFootsteps || !isClient || audioSource == null) return;
        if (Player.localPlayer == null) return;
        float dist = Vector3.Distance(transform.position, Player.localPlayer.transform.position);
        if (dist > footstepHearingDistance) return;
        bool isMoving = state == "MOVING" && movement.GetVelocity().magnitude > footstepMinVelocity;
        if (!isMoving) return;

        if (Time.time >= footstepNextTime)
        {
            AudioClip clipToPlay = GetFootstepClip();
            if (clipToPlay != null)
                audioSource.PlayOneShot(clipToPlay, footstepVolume);

            footstepNextTime = Time.time + footstepInterval;
        }
    }
    AudioClip GetFootstepClip()
    {
        // Try terrain-aware first
        if (groundLayerMask != 0)
        {
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 3f, groundLayerMask))
            {
                string materialName = hit.collider.sharedMaterial != null ? hit.collider.sharedMaterial.name.ToLower() : "";
                if (materialName.Contains("grass") && grassClip != null)
                    return grassClip;
                if (materialName.Contains("stone") && stoneClip != null)
                    return stoneClip;
                if (materialName.Contains("snow") && snowClip != null)
                    return snowClip;
            }
        }

        if (footstepClips != null && footstepClips.Length > 0)
        {
            int index = Random.Range(0, footstepClips.Length);
            return footstepClips[index];
        }

        return null;
    }
}
