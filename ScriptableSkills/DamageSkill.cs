

using System.Text;
using UnityEngine;

public abstract class DamageSkill : ScriptableSkill
{
    [Header("Damage")]
    public LinearInt damage = new LinearInt{baseValue=1};
    public LinearFloat stunChance; 
    public LinearFloat stunTime; 

    
    public override string ToolTip(int skillLevel, bool showRequirements = false)
    {
        StringBuilder tip = new StringBuilder(base.ToolTip(skillLevel, showRequirements));
        tip.Replace("{DAMAGE}", damage.Get(skillLevel).ToString());
        tip.Replace("{STUNCHANCE}", Mathf.RoundToInt(stunChance.Get(skillLevel) * 100).ToString());
        tip.Replace("{STUNTIME}", stunTime.Get(skillLevel).ToString("F1"));
        return tip.ToString();
    }
}
