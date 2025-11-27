



using UnityEngine;
using Mirror;

public class BuffSkillEffect : SkillEffect
{
    float lastRemainingTime = Mathf.Infinity;
    [SyncVar, HideInInspector] public string buffName;

    void Update()
    {
        
        
        if (target != null)
        {
            int index = target.skills.GetBuffIndexByName(buffName);
            if (index != -1)
            {
                Buff buff = target.skills.buffs[index];
                if (lastRemainingTime >= buff.BuffTimeRemaining()) {
                    transform.position = target.collider.bounds.center;
                    lastRemainingTime = buff.BuffTimeRemaining();
                    return;
                }
            }
        }

        
        if (isServer) NetworkServer.Destroy(gameObject);
    }
}
