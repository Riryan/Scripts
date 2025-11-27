














































using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;


public class AddonExample : NetworkBehaviour
{
    
    public void OnDeath()
    {
        Debug.LogWarning("ExampleAddon: OnDeath!");
    }

    
    
    
    public UnityEvent TestEvent;
    void Start()
    {
        TestEvent.Invoke();
    }

    
    public UnityEventString TestEventString;
    void Update()
    {
        TestEventString.Invoke("42");
    }
}


public partial class ItemTemplate
{
    
    
}


public partial struct Item
{
    
    
    

    void ToolTip_Example(StringBuilder tip)
    {
        
    }
}


public partial class SkillTemplate
{
    
    

    public partial struct SkillLevel
    {
        
        
    }
}


public partial struct Skill
{
    
    
    
    

    void ToolTip_Example(StringBuilder tip)
    {
        
    }
}


public partial class QuestTemplate
{
    
    
}


public partial struct Quest
{
    
    
    
    

    void ToolTip_Example(StringBuilder tip)
    {
        
    }
}



public partial struct LoginMsg
{
}



public partial struct CharactersAvailableMsg
{
    public partial struct CharacterPreview
    {
        
    }
    void Load_Example(List<Player> players)
    {
        
        
    }
}
