


using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="uMMORPG Skill/Area Heal", order=999)]
public class AreaHealSkill : HealSkill
{
    
    
    
    
    static Collider[] hitsBuffer = new Collider[10000];

    public override bool CheckTarget(Entity caster)
    {
        
        
        caster.target = caster;
        return true;
    }

    public override bool CheckDistance(Entity caster, int skillLevel, out Vector3 destination)
    {
        
        destination = caster.transform.position;
        return true;
    }

    public override void Apply(Entity caster, int skillLevel)
    {
        
        
        
        HashSet<Entity> candidates = new HashSet<Entity>();

        
        int hits = Physics.OverlapSphereNonAlloc(caster.transform.position, castRange.Get(skillLevel), hitsBuffer);
        for (int i = 0; i < hits; ++i)
        {
            Collider co = hitsBuffer[i];
            Entity candidate = co.GetComponentInParent<Entity>();
            if (candidate != null &&
                candidate.health.current > 0 && 
                candidate.GetType() == caster.GetType()) 
            {
                candidates.Add(candidate);
            }
        }

        
        foreach (Entity candidate in candidates)
        {
            candidate.health.current += healsHealth.Get(skillLevel);
            candidate.mana.current += healsMana.Get(skillLevel);

            
            SpawnEffect(caster, candidate);
        }
    }
}
