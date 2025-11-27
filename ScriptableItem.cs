

















using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName="uMMORPG Item/General", order=999)]
public partial class ScriptableItem : ScriptableObject
{
    [Header("Base Stats")]
    public int maxStack;
    [Tooltip("Durability is only allowed for non-stackable items (if MaxStack is 1))")]
    public int maxDurability = 0; 
    public long buyPrice;
    public long sellPrice;
    public long itemMallPrice;
    public bool sellable;
    public bool tradable;
    public bool destroyable;
    [SerializeField, TextArea(1, 30)] protected string toolTip; 
    public Sprite image;

    
    
    
    
    
    
    











    public virtual string ToolTip()
    {
        
        StringBuilder tip = new StringBuilder(toolTip);
        tip.Replace("{NAME}", name);
        tip.Replace("{DESTROYABLE}", (destroyable ? "Yes" : "No"));
        tip.Replace("{SELLABLE}", (sellable ? "Yes" : "No"));
        tip.Replace("{TRADABLE}", (tradable ? "Yes" : "No"));
        tip.Replace("{BUYPRICE}", buyPrice.ToString());
        tip.Replace("{SELLPRICE}", sellPrice.ToString());
        return tip.ToString();
    }

    
    
    
    
    
    
    static Dictionary<int, ScriptableItem> cache;
    public static Dictionary<int, ScriptableItem> All
    {
        get
        {
            
            if (cache == null)
            {
                
                ScriptableItem[] items = Resources.LoadAll<ScriptableItem>("");

                
                List<string> duplicates = items.ToList().FindDuplicates(item => item.name);
                if (duplicates.Count == 0)
                {
                    cache = items.ToDictionary(item => item.name.GetStableHashCode(), item => item);
                }
                else
                {
                    foreach (string duplicate in duplicates)
                        Debug.LogError("Resources folder contains multiple ScriptableItems with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                }
            }
            return cache;
        }
    }

    
    void OnValidate()
    {
        
        
        
        
        
        
        if (maxStack > 1 && maxDurability != 0)
        {
            maxDurability = 0;
            Debug.LogWarning(name + " maxDurability was reset to 0 because it's not stackable. Set maxStack to 1 if you want to use durability.");
        }

        
        
        sellPrice = Math.Min(sellPrice, buyPrice);
    }
}


[Serializable]
public struct ScriptableItemAndAmount
{
    public ScriptableItem item;
    public int amount;
}