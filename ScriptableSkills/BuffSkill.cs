


using System.Text;
using UnityEngine;
using Mirror;

public abstract class BuffSkill : BonusSkill
{
    public LinearFloat buffTime = new LinearFloat{baseValue=60};
    [Tooltip("Some buffs should remain after death, e.g. exp scrolls.")]
    public bool remainAfterDeath;
    public BuffSkillEffect effect;

    
    
    public void SpawnEffect(Entity caster, Entity spawnTarget)
    {
        if (effect != null)
        {
            GameObject go = Instantiate(effect.gameObject, spawnTarget.transform.position, Quaternion.identity);
            BuffSkillEffect effectComponent = go.GetComponent<BuffSkillEffect>();
            effectComponent.caster = caster;
            effectComponent.target = spawnTarget;
            effectComponent.buffName = name;
            NetworkServer.Spawn(go);
        }
    }

    
    public override string ToolTip(int skillLevel, bool showRequirements = false)
    {
        StringBuilder tip = new StringBuilder(base.ToolTip(skillLevel, showRequirements));
        tip.Replace("{BUFFTIME}", Utils.PrettySeconds(buffTime.Get(skillLevel)));
        return tip.ToString();
    }
}
