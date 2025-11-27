











using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Mirror;

[Serializable]
public partial struct Skill
{
    
    
    
    public int hash;

    
    public int level; 
    public double castTimeEnd; 
    public double cooldownEnd; 

    
    public Skill(ScriptableSkill data)
    {
        hash = data.name.GetStableHashCode();

        
        level = data.learnDefault ? 1 : 0;

        
        castTimeEnd = cooldownEnd = NetworkTime.time;
    }

    
    public ScriptableSkill data
    {
        get
        {
            
            
            
            
            if (!ScriptableSkill.All.ContainsKey(hash))
                throw new KeyNotFoundException("There is no ScriptableSkill with hash=" + hash + ". Make sure that all ScriptableSkills are in the Resources folder so they are loaded properly.");
            return ScriptableSkill.All[hash];
        }
    }
    public string name => data.name;
    public float castTime => data.castTime.Get(level);
    public float cooldown => data.cooldown.Get(level);
    public float castRange => data.castRange.Get(level);
    public int manaCosts => data.manaCosts.Get(level);
    public bool followupDefaultAttack => data.followupDefaultAttack;
    public Sprite image => data.image;
    public bool learnDefault => data.learnDefault;
    public bool showCastBar => data.showCastBar;
    public bool cancelCastIfTargetDied => data.cancelCastIfTargetDied;
    public bool allowMovement => data.allowMovement;
    public int maxLevel => data.maxLevel;
    public ScriptableSkill predecessor => data.predecessor;
    public int predecessorLevel => data.predecessorLevel;
    public string requiredWeaponCategory => data.requiredWeaponCategory;
    public int upgradeRequiredLevel => data.requiredLevel.Get(level+1);
    public long upgradeRequiredSkillExperience => data.requiredSkillExperience.Get(level+1);

    
    public bool CheckSelf(Entity caster, bool checkSkillReady=true) =>
        (!checkSkillReady || IsReady()) &&
        data.CheckSelf(caster, level);

    public bool CheckTarget(Entity caster) => data.CheckTarget(caster);
    public bool CheckDistance(Entity caster, out Vector3 destination) => data.CheckDistance(caster, level, out destination);
    public void Apply(Entity caster) => data.Apply(caster, level);

    
    public string ToolTip(bool showRequirements = false)
    {
        
        int showLevel = Mathf.Max(level, 1);

        
        StringBuilder tip = new StringBuilder(data.ToolTip(showLevel, showRequirements));

        
        Utils.InvokeMany(typeof(Skill), this, "ToolTip_", tip);

        
        if (0 < level && level < maxLevel)
        {
            tip.Append("\n<i>Upgrade:</i>\n" +
                       "<i>  Required Level: " + upgradeRequiredLevel + "</i>\n" +
                       "<i>  Required Skill Exp.: " + upgradeRequiredSkillExperience + "</i>\n");
        }

        return tip.ToString();
    }

    
    public float CastTimeRemaining() => NetworkTime.time >= castTimeEnd ? 0 : (float)(castTimeEnd - NetworkTime.time);

    
    public bool IsCasting() => CastTimeRemaining() > 0;

    
    public float CooldownRemaining() => NetworkTime.time >= cooldownEnd ? 0 : (float)(cooldownEnd - NetworkTime.time);

    public bool IsOnCooldown() => CooldownRemaining() > 0;

    public bool IsReady() => !IsCasting() && !IsOnCooldown();
}
