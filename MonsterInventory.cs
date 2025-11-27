using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class MonsterInventory : Inventory
{
    [Header("Components")]
    public Monster monster;
    [Header("Loot")]
    public int lootGoldMin = 0;
    public int lootGoldMax = 10;
    public ItemDropChance[] dropChances;
    public ParticleSystem lootIndicator;

    [ClientCallback]
    void Update()
    {
        if (lootIndicator != null)
        {
            bool hasLoot = HasLoot();
            if (hasLoot && !lootIndicator.isPlaying)
                lootIndicator.Play();
            else if (!hasLoot && lootIndicator.isPlaying)
                lootIndicator.Stop();
        }
    }

    public bool HasLoot()
    { 
        return monster.gold > 0 || SlotsOccupied() > 0;
    }

    [Server]
    public void OnDeath()
    {
        monster.gold = Random.Range(lootGoldMin, lootGoldMax);
        foreach (ItemDropChance itemChance in dropChances)
            if (Random.value <= itemChance.probability)
            {
                slots.Add(new ItemSlot(new Item(itemChance.item)));
            }
    }
}
