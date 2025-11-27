
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Mirror;

[Serializable]
public partial struct Buff
{
    
    
    
    public int hash;

    
    public int level;
    public double buffTimeEnd; 

    
    public Buff(BuffSkill data, int level)
    {
        hash = data.name.GetStableHashCode();
        this.level = level;
        buffTimeEnd = NetworkTime.time + data.buffTime.Get(level); 
    }

    
    public BuffSkill data
    {
        get
        {
            
            
            
            
            if (!ScriptableSkill.All.ContainsKey(hash))
                throw new KeyNotFoundException("There is no ScriptableSkill with hash=" + hash + ". Make sure that all ScriptableSkills are in the Resources folder so they are loaded properly.");
            return (BuffSkill)ScriptableSkill.All[hash];
        }
    }
    public string name => data.name;
    public Sprite image => data.image;
    public float buffTime => data.buffTime.Get(level);
    public bool remainAfterDeath => data.remainAfterDeath;
    public int healthMaxBonus => data.healthMaxBonus.Get(level);
    public int manaMaxBonus => data.manaMaxBonus.Get(level);
    public int damageBonus => data.damageBonus.Get(level);
    public int defenseBonus => data.defenseBonus.Get(level);
    public float blockChanceBonus => data.blockChanceBonus.Get(level);
    public float criticalChanceBonus => data.criticalChanceBonus.Get(level);
    public float healthPercentPerSecondBonus => data.healthPercentPerSecondBonus.Get(level);
    public float manaPercentPerSecondBonus => data.manaPercentPerSecondBonus.Get(level);
    public float speedBonus => data.speedBonus.Get(level);
    public int maxLevel => data.maxLevel;

    
    public string ToolTip()
    {
        
        
        StringBuilder tip = new StringBuilder(data.ToolTip(level));

        
        Utils.InvokeMany(typeof(Buff), this, "ToolTip_", tip);

        return tip.ToString();
    }

    public float BuffTimeRemaining()
    {
        
        return NetworkTime.time >= buffTimeEnd ? 0 : (float)(buffTimeEnd - NetworkTime.time);
    }
}
