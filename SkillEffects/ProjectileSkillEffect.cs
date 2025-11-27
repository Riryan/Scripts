





using UnityEngine;
using UnityEngine.Events;
using Mirror;

public class ProjectileSkillEffect : SkillEffect
{
    public float speed = 35;
    [HideInInspector] public int damage = 1; 
    [HideInInspector] public float stunChance; 
    [HideInInspector] public float stunTime; 

    
    
    public UnityEvent onSetInitialPosition;

    public override void OnStartClient()
    {
        SetInitialPosition();
    }

    void SetInitialPosition()
    {
        
        
        
        
        
        if (target != null && caster != null)
        {
            transform.position = caster.skills.effectMount.position;
            transform.LookAt(target.collider.bounds.center);
            onSetInitialPosition.Invoke();
        }
    }

    
    
    void FixedUpdate()
    {
        
        
        
        if (target != null && caster != null)
        {
            
            Vector3 goal = target.collider.bounds.center;
            transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.fixedDeltaTime);
            transform.LookAt(goal);

            
            if (isServer && transform.position == goal)
            {
                if (target.health.current > 0)
                {
                    
                    caster.combat.DealDamageAt(target, caster.combat.damage + damage, stunChance, stunTime);
                }
                NetworkServer.Destroy(gameObject);
            }
        }
        else if (isServer) NetworkServer.Destroy(gameObject);
    }
}
