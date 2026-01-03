using UnityEngine;

namespace uMMORPG
{
    public interface INonTargetSkill
    {
        // Build server-validated aim data
        bool CheckAim(Entity caster, int skillLevel, out Vector3 origin, out Vector3 direction);

        // Apply logic using aim instead of caster.target
        void ApplyNonTarget(Entity caster, int skillLevel, Vector3 origin, Vector3 direction);
    }
}
