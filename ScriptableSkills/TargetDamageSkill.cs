using UnityEngine;

[CreateAssetMenu(menuName="uMMORPG Skill/Target Damage", order=999)]
public class TargetDamageSkill : DamageSkill
{
    public override bool CheckTarget(Entity caster)
    {
        
        return caster.target != null && caster.CanAttack(caster.target);
    }

    public override bool CheckDistance(Entity caster, int skillLevel, out Vector3 destination)
    {
        
        if (caster.target != null)
        {
            destination = Utils.ClosestPoint(caster.target, caster.transform.position);
            return Utils.ClosestDistance(caster, caster.target) <= castRange.Get(skillLevel);
        }
        destination = caster.transform.position;
        return false;
    }

    public override void Apply(Entity caster, int skillLevel)
    {
        
        caster.combat.DealDamageAt(caster.target,
                                   caster.combat.damage + damage.Get(skillLevel),
                                   stunChance.Get(skillLevel),
                                   stunTime.Get(skillLevel));
    }
}
