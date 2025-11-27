using UnityEngine;

[CreateAssetMenu(menuName="uMMORPG Skill/Target Heal", order=999)]
public class TargetHealSkill : HealSkill
{
    public bool canHealSelf = true;
    public bool canHealOthers = false;

    
    
    Entity CorrectedTarget(Entity caster)
    {
        
        if (caster.target == null)
            return canHealSelf ? caster : null;

        
        if (caster.target == caster)
            return canHealSelf ? caster : null;

        
        if (caster.target.GetType() == caster.GetType())
        {
            if (canHealOthers)
                return caster.target;
            else if (canHealSelf)
                return caster;
            else
                return null;
        }

        
        return canHealSelf ? caster : null;
    }

    public override bool CheckTarget(Entity caster)
    {
        
        caster.target = CorrectedTarget(caster);

        
        return caster.target != null && caster.target.health.current > 0;
    }

    
    public override bool CheckDistance(Entity caster, int skillLevel, out Vector3 destination)
    {
        
        
        
        Entity target = CorrectedTarget(caster);

        
        if (target != null)
        {
            destination = Utils.ClosestPoint(target, caster.transform.position);
            return Utils.ClosestDistance(caster, target) <= castRange.Get(skillLevel);
        }
        destination = caster.transform.position;
        return false;
    }

    
    public override void Apply(Entity caster, int skillLevel)
    {
        
        if (caster.target != null && caster.target.health.current > 0)
        {
            caster.target.health.current += healsHealth.Get(skillLevel);
            caster.target.mana.current += healsMana.Get(skillLevel);

            
            SpawnEffect(caster, caster.target);
        }
    }
}
