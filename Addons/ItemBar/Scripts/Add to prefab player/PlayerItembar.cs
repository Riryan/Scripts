using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

[Serializable]
public struct ItembarEntry
{
    public string reference;
    public KeyCode hotKey;
}

[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(PlayerInventory))]
public class PlayerItembar : NetworkBehaviour
{
    [Header("Components")]
    public PlayerEquipment equipment;
    public PlayerInventory inventory;

    [Header("Itembar")]
	public ItembarEntry[] slots =
    {
		new ItembarEntry{reference="", hotKey=KeyCode.Alpha1},
		new ItembarEntry{reference="", hotKey=KeyCode.Alpha2},
		new ItembarEntry{reference="", hotKey=KeyCode.Alpha3},
		new ItembarEntry{reference="", hotKey=KeyCode.Alpha4},
		new ItembarEntry{reference="", hotKey=KeyCode.Alpha5},
		new ItembarEntry{reference="", hotKey=KeyCode.Alpha6},
		new ItembarEntry{reference="", hotKey=KeyCode.Alpha7},
		new ItembarEntry{reference="", hotKey=KeyCode.Alpha8},
		new ItembarEntry{reference="", hotKey=KeyCode.Alpha9},
		new ItembarEntry{reference="", hotKey=KeyCode.Alpha0},
    };
    
    public override void OnStartLocalPlayer()
    {
        // load skillbar after player data was loaded
        Load();
    }
    

    public override void OnStopClient()
    {
        if (isLocalPlayer)
            Save();
    }

    // skillbar ////////////////////////////////////////////////////////////////
    //[Client] <- disabled while UNET OnDestroy isLocalPlayer bug exists
    void Save()
    {
        // save skillbar to player prefs (based on player name, so that
        // each character can have a different skillbar)
        for (int i = 0; i < slots.Length; ++i)
            PlayerPrefs.SetString(name + "_itembar_" + i, slots[i].reference);

        // force saving playerprefs, otherwise they aren't saved for some reason
        PlayerPrefs.Save();
    }

    [Client]
    void Load()
    {
        //Debug.Log("loading itembar for " + name);
        for (int i = 0; i < slots.Length; ++i)
        {
            // try loading an existing entry
            if (PlayerPrefs.HasKey(name + "_itembar_" + i))
            {
                string entry = PlayerPrefs.GetString(name + "_itembar_" + i, "");

                // is this a valid item/equipment/learned skill?
                // (might be an old character's playerprefs)
                // => only allow learned skills (in case it's an old character's
                //    skill that we also have, but haven't learned yet)
                if (inventory.GetItemIndexByName(entry) != -1 || equipment.GetItemIndexByName(entry) != -1)
                {
                    slots[i].reference = entry;
                }
            }
        }
    }

    // drag & drop /////////////////////////////////////////////////////////////
	void OnDragAndDrop_InventorySlot_ItembarSlot(int[] slotIndices)
    {
        //Debug.Log("error ici ?");
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        slots[slotIndices[1]].reference = inventory.slots[slotIndices[0]].item.name; // just save it clientsided
    }

	void OnDragAndDrop_EquipmentSlot_ItembarSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        slots[slotIndices[1]].reference = equipment.slots[slotIndices[0]].item.name; // just save it clientsided
    }
		
	void OnDragAndDrop_ItembarSlot_ItembarSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        // just swap them clientsided
        string temp = slots[slotIndices[0]].reference;
        slots[slotIndices[0]].reference = slots[slotIndices[1]].reference;
        slots[slotIndices[1]].reference = temp;
    }

	void OnDragAndClear_ItembarSlot(int slotIndex)
    {
        slots[slotIndex].reference = "";
    }
}
