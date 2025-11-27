using UnityEngine;
using Mirror;

public class OneTimeTargetSkillEffect : SkillEffect
{
    void Update()
    {
        
        
        if (target != null)
            transform.position = target.collider.bounds.center;

        
        if (isServer)
            if (target == null || !GetComponent<ParticleSystem>().IsAlive())
                NetworkServer.Destroy(gameObject);
    }
}
