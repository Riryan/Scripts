


















using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public abstract partial class ScriptableSkill : ScriptableObject
{
    [Header("Info")]
    public bool followupDefaultAttack;
    [SerializeField, TextArea(1, 30)] protected string toolTip; 
    public Sprite image;
    public bool learnDefault; 
    public bool showCastBar;
    public bool cancelCastIfTargetDied; 
    public bool allowMovement; 

    [Header("Requirements")]
    public ScriptableSkill predecessor; 
    public int predecessorLevel = 1; 
    public string requiredWeaponCategory = ""; 
    public LinearInt requiredLevel; 
    public LinearLong requiredSkillExperience;

    [Header("Properties")]
    public int maxLevel = 1;
    public LinearInt manaCosts;
    public LinearFloat castTime;
    public LinearFloat cooldown;
    public LinearFloat castRange;

    [Header("Sound")]
    public AudioClip castSound;

    
    
    
    
    bool CheckWeapon(Entity caster)
    {
        
        if (string.IsNullOrWhiteSpace(requiredWeaponCategory))
            return true;

        
        if (caster.equipment.GetEquippedWeaponCategory().StartsWith(requiredWeaponCategory))
        {
            
            int weaponIndex = caster.equipment.GetEquippedWeaponIndex();
            if (weaponIndex != -1)
            {
                return caster.equipment.slots[weaponIndex].item.CheckDurability();
            }
        }
        return false;
    }

    
    
    
    public virtual bool CheckSelf(Entity caster, int skillLevel)
    {

        
        
        return caster.health.current > 0 &&
               caster.mana.current >= manaCosts.Get(skillLevel) &&
               CheckWeapon(caster);
    }

    
    
    
    
    
    
    
    public abstract bool CheckTarget(Entity caster);

    
    
    
    
    public abstract bool CheckDistance(Entity caster, int skillLevel, out Vector3 destination);

    
    
    public abstract void Apply(Entity caster, int skillLevel);

    
    
    public virtual void OnCastStarted(Entity caster)
    {
        if (caster.audioSource != null && castSound != null)
            caster.audioSource.PlayOneShot(castSound);
    }

    
    public virtual void OnCastFinished(Entity caster) {}

    
    

    
    
    
    
    
    
    










    public virtual string ToolTip(int level, bool showRequirements = false)
    {
        
        StringBuilder tip = new StringBuilder(toolTip);
        tip.Replace("{NAME}", name);
        tip.Replace("{LEVEL}", level.ToString());
        tip.Replace("{CASTTIME}", Utils.PrettySeconds(castTime.Get(level)));
        tip.Replace("{COOLDOWN}", Utils.PrettySeconds(cooldown.Get(level)));
        tip.Replace("{CASTRANGE}", castRange.Get(level).ToString());
        tip.Replace("{MANACOSTS}", manaCosts.Get(level).ToString());

        
        if (showRequirements)
        {
            tip.Append("\n<b><i>Required Level: " + requiredLevel.Get(1) + "</i></b>\n" +
                       "<b><i>Required Skill Exp.: " + requiredSkillExperience.Get(1) + "</i></b>\n");
            if (predecessor != null)
                tip.Append("<b><i>Required Skill: " + predecessor.name + " Lv. " + predecessorLevel + " </i></b>\n");
        }

        return tip.ToString();
    }

    
    
    
    
    
    
    static Dictionary<int, ScriptableSkill> cache;
    public static Dictionary<int, ScriptableSkill> All
    {
        get
        {
            
            if (cache == null)
            {
                
                ScriptableSkill[] skills = Resources.LoadAll<ScriptableSkill>("");

                
                List<string> duplicates = skills.ToList().FindDuplicates(skill => skill.name);
                if (duplicates.Count == 0)
                {
                    cache = skills.ToDictionary(skill => skill.name.GetStableHashCode(), skill => skill);
                }
                else
                {
                    foreach (string duplicate in duplicates)
                        Debug.LogError("Resources folder contains multiple ScriptableSkills with the name " + duplicate + ". If you are using subfolders like 'Warrior/NormalAttack' and 'Archer/NormalAttack', then rename them to 'Warrior/(Warrior)NormalAttack' and 'Archer/(Archer)NormalAttack' instead.");
                }
            }
            return cache;
        }
    }
}
