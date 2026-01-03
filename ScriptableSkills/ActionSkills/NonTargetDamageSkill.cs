using UnityEngine;

namespace uMMORPG
{
    [CreateAssetMenu(menuName = "uMMORPG Skill/Non-Target Damage", order = 1000)]
    public class NonTargetDamageSkill : DamageSkill, INonTargetSkill
    {
        [Header("Non-Target Melee")]
        public float hitRange = 2.5f;
        public float hitRadius = 0.75f;
        [Range(0f, 180f)] public float hitAngle = 70f;
        public LayerMask hitMask = ~0;

        // no-GC buffer
        static readonly Collider[] hitBuffer = new Collider[16];

        // ---------- ScriptableSkill abstract overrides ----------
        // Non-target skills don't require caster.target, but we still gate the cast.
        public override bool CheckTarget(Entity caster)
        {
            // "Target" means: can we attempt an action strike at all?
            // base.CheckSelf() is handled elsewhere in the pipeline (mana/cooldown/etc),
            // but keeping this permissive avoids breaking existing cast flow.
            return true;
        }

        public override bool CheckDistance(Entity caster, int skillLevel, out Vector3 destination)
        {
            // destination is used by some movement systems to approach cast range.
            // For action melee, we just provide a point in front of the caster.
            Vector3 dir = caster.transform.forward;
            if (dir.sqrMagnitude < 0.001f)
                dir = Vector3.forward;

            destination = caster.transform.position + dir.normalized * hitRange;
            return hitRange > 0f;
        }

        public override void Apply(Entity caster, int skillLevel)
        {
            // Works even before your central hook exists:
            // treat Apply() as "perform non-target strike".
            if (CheckAim(caster, skillLevel, out Vector3 origin, out Vector3 direction))
                ApplyNonTarget(caster, skillLevel, origin, direction);
        }

        // ---------- INonTargetSkill ----------
        public bool CheckAim(Entity caster, int skillLevel, out Vector3 origin, out Vector3 direction)
        {
            origin = caster.transform.position + Vector3.up * 0.9f;
            direction = caster.transform.forward;

            if (direction.sqrMagnitude < 0.001f)
                return false;

            return hitRange > 0f;
        }

        public void ApplyNonTarget(Entity caster, int skillLevel, Vector3 origin, Vector3 direction)
        {
            // Center the overlap slightly forward to match "swing" feel
            Vector3 center = origin + direction.normalized * (hitRange * 0.5f);

            int count = Physics.OverlapSphereNonAlloc(
                center,
                hitRadius,
                hitBuffer,
                hitMask,
                QueryTriggerInteraction.Ignore
            );

            Entity bestTarget = null;
            float bestDot = -1f;

            float cosHalfAngle = Mathf.Cos(hitAngle * Mathf.Deg2Rad * 0.5f);

            for (int i = 0; i < count; ++i)
            {
                Collider col = hitBuffer[i];
                if (col == null) continue;

                Entity entity = col.GetComponentInParent<Entity>();
                if (entity == null || entity == caster) continue;

                if (!caster.CanAttack(entity)) continue;

                Vector3 toTarget = entity.transform.position - caster.transform.position;
                float dist = toTarget.magnitude;
                if (dist > hitRange) continue;

                Vector3 toNorm = toTarget / Mathf.Max(dist, 0.001f);
                float dot = Vector3.Dot(direction.normalized, toNorm);

                // Cone filter
                if (dot < cosHalfAngle) continue;

                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestTarget = entity;
                }
            }

            if (bestTarget != null)
            {
                caster.combat.DealDamageAt(
                    bestTarget,
                    caster.combat.damage + damage.Get(skillLevel),
                    stunChance.Get(skillLevel),
                    stunTime.Get(skillLevel)
                );
            }
        }
    }
}
