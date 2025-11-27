using UnityEngine;

[CreateAssetMenu(menuName="uMMORPG Skill/Target Buff", order=999)]
public class TargetBuffSkill : BuffSkill
{
    public bool canBuffSelf = true;
    public bool canBuffOthers = false; 
    public bool canBuffEnemies = false; 

    
    
    Entity CorrectedTarget(Entity caster)
    {
        
        if (caster.target == null)
            return canBuffSelf ? caster : null;

        
        if (caster.target == caster)
            return canBuffSelf ? caster : null;

        
        if (caster.target.GetType() == caster.GetType())
        {
            if (canBuffOthers)
                return caster.target;
            else if (canBuffSelf)
                return caster;
            else
                return null;
        }

        
        if (caster.CanAttack(caster.target))
        {
            if (canBuffEnemies)
                return caster.target;
            else if (canBuffSelf)
                return caster;
            else
                return null;
        }

        
        return canBuffSelf ? caster : null;
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
            
            caster.target.skills.AddOrRefreshBuff(new Buff(this, skillLevel));

            
            SpawnEffect(caster, caster.target);
        }
    }
}
