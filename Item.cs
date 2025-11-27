







using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Mirror;

[Serializable]
public partial struct Item
{
    
    
    
    public int hash;

    
    public int durability;

    
    public NetworkIdentity summoned; 
    public int summonedHealth; 
    public int summonedLevel; 
    public long summonedExperience; 

    
    public Item(ScriptableItem data)
    {
        hash = data.name.GetStableHashCode();
        durability = data.maxDurability;
        summoned = null;
        summonedHealth = data is SummonableItem summonable ? summonable.summonPrefab.health.max : 0;
        summonedLevel = data is SummonableItem ? 1 : 0;
        summonedExperience = 0;
    }

    
    public ScriptableItem data
    {
        get
        {
            
            
            
            
            if (!ScriptableItem.All.ContainsKey(hash))
                throw new KeyNotFoundException("There is no ScriptableItem with hash=" + hash + ". Make sure that all ScriptableItems are in the Resources folder so they are loaded properly.");
            return ScriptableItem.All[hash];
        }
    }
    public string name => data.name;
    public int maxStack => data.maxStack;
    public int maxDurability => data.maxDurability;
    public float DurabilityPercent()
    {
        return (durability != 0 && maxDurability != 0) ? (float)durability / (float)maxDurability : 0;
    }
    public long buyPrice => data.buyPrice;
    public long sellPrice => data.sellPrice;
    public long itemMallPrice => data.itemMallPrice;
    public bool sellable => data.sellable;
    public bool tradable => data.tradable;
    public bool destroyable => data.destroyable;
    public Sprite image => data.image;

    
    public bool CheckDurability() =>
        maxDurability == 0 || durability > 0;

    
    public string ToolTip()
    {
        
        StringBuilder tip = new StringBuilder(data.ToolTip());

        
        if (maxDurability > 0)
            tip.Replace("{DURABILITY}", (DurabilityPercent() * 100).ToString("F0"));

        tip.Replace("{SUMMONEDHEALTH}", summonedHealth.ToString());
        tip.Replace("{SUMMONEDLEVEL}", summonedLevel.ToString());
        tip.Replace("{SUMMONEDEXPERIENCE}", summonedExperience.ToString());

        
        Utils.InvokeMany(typeof(Item), this, "ToolTip_", tip);

        return tip.ToString();
    }
}
