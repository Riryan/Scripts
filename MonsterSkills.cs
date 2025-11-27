using UnityEngine;

public class MonsterSkills : Skills
{
    
    [HideInInspector] public int lastSkill = -1;

    public override void OnStartServer()
    {
        
        foreach (ScriptableSkill skillData in skillTemplates)
            skills.Add(new Skill(skillData));
    }

    
    public float CurrentCastRange()
    {
        return 0 <= currentSkill && currentSkill < skills.Count
               ? skills[currentSkill].castRange
               : 0;
    }

    
    
    
    
    public int NextSkill()
    {
        
        
        
        
        for (int i = 0; i < skills.Count; ++i)
        {
            int index = (lastSkill + 1 + i) % skills.Count;
            
            if (CastCheckSelf(skills[index]))
                return index;
        }
        return -1;
    }
}
