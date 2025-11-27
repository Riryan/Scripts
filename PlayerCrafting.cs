using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public enum CraftingState { None, InProgress, Success, Failed }

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class PlayerCrafting : NetworkBehaviour
{
    [Header("Components")]
    public Player player;
    public PlayerInventory inventory;

    [Header("Crafting")]
    public List<int> indices = Enumerable.Repeat(-1, ScriptableRecipe.recipeSize).ToList();
    [HideInInspector] public CraftingState state = CraftingState.None; 
    ScriptableRecipe currentRecipe; 
    [SyncVar, HideInInspector] public double endTime; 
    [HideInInspector] public bool requestPending; 

    
    
    
    
    
    
    
    
    

    
    
    
    
    [Command]
    public void CmdCraft(string recipeName, int[] clientIndices)
    {
        
        
        if ((player.state == "IDLE" || player.state == "MOVING") &&
            clientIndices.Length == ScriptableRecipe.recipeSize)
        {
            
            
            List<int> validIndices = clientIndices.Where(index => 0 <= index && index < inventory.slots.Count && inventory.slots[index].amount > 0).ToList();
            if (validIndices.Count > 0 && !validIndices.HasDuplicates())
            {
                
                if (ScriptableRecipe.All.TryGetValue(recipeName, out ScriptableRecipe recipe) &&
                    recipe.result != null)
                {
                    
                    Item result = new Item(recipe.result);
                    if (inventory.CanAdd(result, 1))
                    {
                        
                        
                        currentRecipe = recipe;

                        
                        
                        
                        indices = clientIndices.ToList();

                        
                        requestPending = true;
                        endTime = NetworkTime.time + recipe.craftingTime;
                    }
                }
            }
        }
    }

    
    [Server]
    public void Craft()
    {
        
        
        
        if (player.state == "CRAFTING" &&
            currentRecipe != null &&
            currentRecipe.result != null)
        {
            
            Item result = new Item(currentRecipe.result);
            if (inventory.CanAdd(result, 1))
            {
                
                foreach (ScriptableItemAndAmount ingredient in currentRecipe.ingredients)
                    if (ingredient.amount > 0 && ingredient.item != null)
                        inventory.Remove(new Item(ingredient.item), ingredient.amount);

                
                
                
                
                
                
                
                
                if (new System.Random().NextDouble() < currentRecipe.probability)
                {
                    
                    inventory.Add(new Item(currentRecipe.result), 1);
                    TargetCraftingSuccess();
                }
                else
                {
                    TargetCraftingFailed();
                }

                
                
                
                
                
                if (!isLocalPlayer)
                    for (int i = 0; i < ScriptableRecipe.recipeSize; ++i)
                        indices[i] = -1;

                
                currentRecipe = null;
            }
        }
    }

    
    [TargetRpc] 
    public void TargetCraftingSuccess()
    {
        state = CraftingState.Success;
    }

    [TargetRpc] 
    public void TargetCraftingFailed()
    {
        state = CraftingState.Failed;
    }

    
    void OnDragAndDrop_InventorySlot_CraftingIngredientSlot(int[] slotIndices)
    {
        
        
        if (state != CraftingState.InProgress)
        {
            if (!indices.Contains(slotIndices[0]))
            {
                indices[slotIndices[1]] = slotIndices[0];
                state = CraftingState.None; 
            }
        }
    }

    void OnDragAndDrop_CraftingIngredientSlot_CraftingIngredientSlot(int[] slotIndices)
    {
        
        
        if (state != CraftingState.InProgress)
        {
            
            int temp = indices[slotIndices[0]];
            indices[slotIndices[0]] = indices[slotIndices[1]];
            indices[slotIndices[1]] = temp;
            state = CraftingState.None; 
        }
    }

    void OnDragAndClear_CraftingIngredientSlot(int slotIndex)
    {
        
        if (state != CraftingState.InProgress)
        {
            indices[slotIndex] = -1;
            state = CraftingState.None; 
        }
    }
}
