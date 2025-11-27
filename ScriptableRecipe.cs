















using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName="New Recipe", menuName="uMMORPG Recipe", order=999)]
public class ScriptableRecipe : ScriptableObject
{
    
    public static int recipeSize = 6;

    
    public List<ScriptableItemAndAmount> ingredients = new List<ScriptableItemAndAmount>(6);
    public ScriptableItem result;

    
    public float craftingTime = 1;

    
    [Range(0, 1)] public float probability = 1;

    
    bool IngredientsNotEmpty()
    {
        
        foreach (ScriptableItemAndAmount slot in ingredients)
            if (slot.amount > 0 && slot.item != null)
                return true;
        return false;
    }

    int FindMatchingStack(List<ItemSlot> items, ScriptableItemAndAmount ingredient)
    {
        
        for (int i = 0; i < items.Count; ++i)
            if (items[i].amount >= ingredient.amount &&
                items[i].item.data == ingredient.item)
                return i;
        return -1;
    }

    
    
    
    public virtual bool CanCraftWith(List<ItemSlot> items)
    {
        
        
        items = new List<ItemSlot>(items);

        
        if (IngredientsNotEmpty())
        {
            
            foreach (ScriptableItemAndAmount ingredient in ingredients)
            {
                if (ingredient.amount > 0 && ingredient.item != null)
                {
                    
                    int index = FindMatchingStack(items, ingredient);
                    if (index != -1)
                        items.RemoveAt(index);
                    else
                        return false;
                }
            }

            
            return items.Count == 0;
        }
        else return false;
    }

    
    
    
    
    static Dictionary<string, ScriptableRecipe> cache;
    public static Dictionary<string, ScriptableRecipe> All
    {
        get
        {
            
            if (cache == null)
            {
                
                ScriptableRecipe[] recipes = Resources.LoadAll<ScriptableRecipe>("");

                
                List<string> duplicates = recipes.ToList().FindDuplicates(recipe => recipe.name);
                if (duplicates.Count == 0)
                {
                    cache = recipes.ToDictionary(recipe => recipe.name, recipe => recipe);
                }
                else
                {
                    foreach (string duplicate in duplicates)
                        Debug.LogError("Resources folder contains multiple ScriptableRecipes with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                }
            }
            return cache;
        }
    }

    
    public static ScriptableRecipe Find(List<ItemSlot> items)
    {
        
        foreach (ScriptableRecipe recipe in All.Values)
            if (recipe.CanCraftWith(items))
                return recipe;
        return null;
    }

    
    void OnValidate()
    {
        
        
        for (int i = ingredients.Count; i < recipeSize; ++i)
            ingredients.Add(new ScriptableItemAndAmount());

        
        ingredients.RemoveRange(recipeSize, ingredients.Count - recipeSize);
    }
}
