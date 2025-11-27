using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using GFFAddons;

[Serializable]
public partial struct EquipmentInfo
{
    public string requiredCategory;
    public Transform location;
    public ScriptableItemAndAmount defaultItem;
    [Tooltip("UI label override for this slot. If empty, UI falls back to parsing requiredCategory.")]
    public string displayName;
}

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInventory))]
public class PlayerEquipment : Equipment
{
    [Header("Components")]
    public Player player;
    public Animator animator;
    public PlayerInventory inventory;
    [Header("Avatar")]
    public Camera avatarCamera;

    [Header("Equipment Info")]
    public EquipmentInfo[] slotInfo = {
        new EquipmentInfo{requiredCategory="Weapon", location=null, defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Head", location=null, defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Chest", location=null, defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Legs", location=null, defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Shield", location=null, defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Shoulders", location=null, defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Hands", location=null, defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Feet", location=null, defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Back",     location=null, defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Arms",     location=null, defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Hips",     location=null, defaultItem=new ScriptableItemAndAmount()},
        new EquipmentInfo{requiredCategory="Jewelry",  location=null, defaultItem=new ScriptableItemAndAmount()},

    };
    [Header("Auto Fists (TEST)")]
    public bool autoFistsTestEnabled = true;          // flip off when done testing
    public WeaponItem autoFistWeaponTest;   

    
    Dictionary<string, Transform> skinBones = new Dictionary<string, Transform>();

    void Awake()
    {
        foreach (SkinnedMeshRenderer skin in GetComponentsInChildren<SkinnedMeshRenderer>())
            foreach (Transform bone in skin.bones)
                skinBones[bone.name] = bone;
    }

    public override void OnStartClient()
    {
#pragma warning disable CS0618
        slots.Callback += OnEquipmentChanged;
#pragma warning restore CS0618
        for (int i = 0; i < slots.Count; ++i)
            RefreshLocation(i);
#if !UNITY_SERVER
        RefreshIdleAnimation();
        // Ensure customization visuals reflect currently equipped items at login.
        var _cust = player != null ? player.customization : null;
        if (_cust != null) Customization_OnRebuildRequested(_cust);
#endif
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
#pragma warning disable CS0618
        slots.Callback += OnEquipmentChangedServer;
#pragma warning restore CS0618
        // cover login/respawn when slot is empty
        EnsureAutoFists();
    }


    int GetWeaponSlotIndex()
    {
        // 'Weapon' is index 0 in slotInfo today, but let's be safe:
        for (int i = 0; i < slotInfo.Length; ++i)
            if (slotInfo[i].requiredCategory == "Weapon")
                return i;
        return 0;
    }
    bool IsAutoFist(ItemSlot slot)
    {
        return slot.amount > 0 && slot.item.data != null && slot.item.data == autoFistWeaponTest;
    }
    // server-only guard: equip fists if empty; never creates an inventory entry
    [Server]
    void EnsureAutoFists()
    {
        if (!autoFistsTestEnabled || autoFistWeaponTest == null) return;

        int w = GetWeaponSlotIndex();
        if (0 <= w && w < slots.Count)
        {
            var s = slots[w];
            if (s.amount == 0)
            {
                // equip test fists directly into the weapon slot
                s.item = new Item(autoFistWeaponTest);
                s.amount = 1;
                // optional: keep full durability; we also skip drains below
                s.item.durability = s.item.maxDurability;
                slots[w] = s;
            }
        }
    }

    void OnEquipmentChanged(SyncList<ItemSlot>.Operation op, int index, ItemSlot oldSlot, ItemSlot newSlot)
    {
        ScriptableItem oldItem = oldSlot.amount > 0 ? oldSlot.item.data : null;
        ScriptableItem newItem = newSlot.amount > 0 ? newSlot.item.data : null;

    #if !UNITY_SERVER
        // client-only: drive customization overrides/hides on slot change
        var cust = player != null ? player.customization : null; // Player has this field
        if (cust != null)
        {
            if (oldItem is EquipmentItem oldEq)
                cust.OnSlotUnequipped(index, oldEq.targetCustomizationType, oldEq.overrideCustomizationIndex, oldEq.hideWhileEquipped);

            if (newItem is EquipmentItem newEq)
                cust.OnSlotEquipped(index, newEq.targetCustomizationType, newEq.overrideCustomizationIndex, newEq.hideWhileEquipped);
        }
    #endif

        if (oldItem != newItem)
        {
            RefreshLocation(index); // keep current prefab attach/detach behavior (back-compat)
#if !UNITY_SERVER
            RefreshIdleAnimation();
#endif
        }
    }

    public bool CanReplaceAllBones(SkinnedMeshRenderer equipmentSkin)
    {
        foreach (Transform bone in equipmentSkin.bones)
            if (!skinBones.ContainsKey(bone.name))
                return false;
        return true;
    }
    
    public void ReplaceAllBones(SkinnedMeshRenderer equipmentSkin)
    {
        Transform[] bones = equipmentSkin.bones;
        for (int i = 0; i < bones.Length; ++i)
        {
            string boneName = bones[i].name;
            if (!skinBones.TryGetValue(boneName, out bones[i]))
                Debug.LogWarning(equipmentSkin.name + " bone " + boneName + " not found in original player bones. Make sure to check CanReplaceAllBones before.");
        }
        equipmentSkin.bones = bones;
    }

    public void RebindAnimators()
    {
        foreach (Animator anim in GetComponentsInChildren<Animator>())
            anim.Rebind();
    }

    public void RefreshLocation(int index)
    {
        ItemSlot slot = slots[index];
        EquipmentInfo info = slotInfo[index];
        if (info.requiredCategory != "" && info.location != null)
        {
            if (info.location.childCount > 0) Destroy(info.location.GetChild(0).gameObject);
            if (slot.amount > 0)
            {
                EquipmentItem itemData = (EquipmentItem)slot.item.data;
                if (itemData.modelPrefab != null)
                {
                    GameObject go = Instantiate(itemData.modelPrefab, info.location, false);
                    go.name = itemData.modelPrefab.name; 
                  
                    SkinnedMeshRenderer equipmentSkin = go.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (equipmentSkin != null && CanReplaceAllBones(equipmentSkin))
                        ReplaceAllBones(equipmentSkin);
                    Animator anim = go.GetComponent<Animator>();
                    if (anim != null)
                    {
                        anim.runtimeAnimatorController = animator.runtimeAnimatorController;
                        RebindAnimators();
                    }
                }
            }
        }
    }
    [Server]
    void OnEquipmentChangedServer(SyncList<ItemSlot>.Operation op, int index, ItemSlot oldSlot, ItemSlot newSlot)
    {
        EnsureAutoFists();
    }

    [Server]
    public void SwapInventoryEquip(int inventoryIndex, int equipmentIndex)
    {
        if (inventory.InventoryOperationsAllowed() &&
            0 <= inventoryIndex && inventoryIndex < inventory.slots.Count &&
            0 <= equipmentIndex && equipmentIndex < slots.Count)
        {
            ItemSlot inv = inventory.slots[inventoryIndex];

            // allow swap if empty OR if the inventory item is equippable in that slot
            bool canEquip = inv.amount == 0 ||
                            (inv.item.data is EquipmentItem itemData &&
                             itemData.CanEquip(player, inventoryIndex, equipmentIndex));

            if (canEquip)
            {
                ItemSlot equip = slots[equipmentIndex];

                // SPECIAL-CASE: if the equipment slot currently holds the TEST auto-fists,
                // we REPLACE them (do NOT put them into inventory).
                if (IsAutoFist(equip))
                {
                    slots[equipmentIndex] = inv;          // place real item into equipment
                    inv.amount = 0;                       // clear inventory slot (no test fists backfill)
                    inventory.slots[inventoryIndex] = inv;
                }
                else
                {
                    // normal swap
                    ItemSlot temp = equip;
                    slots[equipmentIndex] = inv;
                    inventory.slots[inventoryIndex] = temp;
                }
            }
        }
    }

    [Command]
    public void CmdSwapInventoryEquip(int inventoryIndex, int equipmentIndex)
    {
        SwapInventoryEquip(inventoryIndex, equipmentIndex);
    }

    [Server]
    public void MergeInventoryEquip(int inventoryIndex, int equipmentIndex)
    {
        if (inventory.InventoryOperationsAllowed() &&
            0 <= inventoryIndex && inventoryIndex < inventory.slots.Count &&
            0 <= equipmentIndex && equipmentIndex < slots.Count)
        {
            ItemSlot slotFrom = inventory.slots[inventoryIndex];
            ItemSlot slotTo = slots[equipmentIndex];
            if (slotFrom.amount > 0 && slotTo.amount > 0)
            {
                if (slotFrom.item.Equals(slotTo.item))
                {
                    int put = slotTo.IncreaseAmount(slotFrom.amount);
                    slotFrom.DecreaseAmount(put);
                    inventory.slots[inventoryIndex] = slotFrom;
                    slots[equipmentIndex] = slotTo;
                }
            }
        }
    }

    [Command]
    public void CmdMergeInventoryEquip(int equipmentIndex, int inventoryIndex)
    {
        MergeInventoryEquip(equipmentIndex, inventoryIndex);
    }

    [Command]
    public void CmdMergeEquipInventory(int equipmentIndex, int inventoryIndex)
    {
        if (inventory.InventoryOperationsAllowed() &&
            0 <= inventoryIndex && inventoryIndex < inventory.slots.Count &&
            0 <= equipmentIndex && equipmentIndex < slots.Count)
        {
            ItemSlot slotFrom = slots[equipmentIndex];
            ItemSlot slotTo = inventory.slots[inventoryIndex];
            if (slotFrom.amount > 0 && slotTo.amount > 0)
            {
                if (slotFrom.item.Equals(slotTo.item))
                {
                    int put = slotTo.IncreaseAmount(slotFrom.amount);
                    slotFrom.DecreaseAmount(put);
                    slots[equipmentIndex] = slotFrom;
                    inventory.slots[inventoryIndex] = slotTo;
                }
            }
        }
    }

    public void OnDamageDealtTo(Entity victim)
    {
        int weaponIndex = GetEquippedWeaponIndex();
        if (weaponIndex != -1)
        {
            ItemSlot slot = slots[weaponIndex];
            if (autoFistsTestEnabled && autoFistWeaponTest != null && IsAutoFist(slot))
                return;
            slot.item.durability = Mathf.Clamp(slot.item.durability - 1, 0, slot.item.maxDurability);
            slots[weaponIndex] = slot;
        }
    }

    public void OnReceivedDamage(Entity attacker, int damage)
    {
        for (int i = 0; i < slots.Count; ++i)
        {
            if (slots[i].amount > 0)
            {
                ItemSlot slot = slots[i];
                if (autoFistsTestEnabled && autoFistWeaponTest != null && IsAutoFist(slots[i]))
                    continue;
                slot.item.durability = Mathf.Clamp(slot.item.durability - 1, 0, slot.item.maxDurability);
                slots[i] = slot;
            }
        }
    }

    
    void OnDragAndDrop_InventorySlot_EquipmentSlot(int[] slotIndices)
    {        
        if (inventory.slots[slotIndices[0]].amount > 0 && slots[slotIndices[1]].amount > 0 &&
            inventory.slots[slotIndices[0]].item.Equals(slots[slotIndices[1]].item))
        {
            CmdMergeInventoryEquip(slotIndices[0], slotIndices[1]);
        }
        else
        {
            CmdSwapInventoryEquip(slotIndices[0], slotIndices[1]);
        }
    }

    void OnDragAndDrop_EquipmentSlot_InventorySlot(int[] slotIndices)
    {
        if (slots[slotIndices[0]].amount > 0 && inventory.slots[slotIndices[1]].amount > 0 &&
            slots[slotIndices[0]].item.Equals(inventory.slots[slotIndices[1]].item))
        {
            CmdMergeEquipInventory(slotIndices[0], slotIndices[1]);
        }
        else
        {
            CmdSwapInventoryEquip(slotIndices[1], slotIndices[0]); 
        }
    }
    
    protected override void OnValidate()
    {
        base.OnValidate();        
        for (int i = 0; i < slotInfo.Length; ++i)
            if (slotInfo[i].defaultItem.item != null && slotInfo[i].defaultItem.amount == 0)
                slotInfo[i].defaultItem.amount = 1;
    }

#if !UNITY_SERVER
    // Check if animator has a parameter of the given name & type.
    bool HasAnimatorParameter(Animator anim, string paramName, AnimatorControllerParameterType type)
    {
        if (anim == null) return false; // will fix typo shortly
        var ps = anim.parameters;
        for (int i = 0; i < ps.Length; ++i)
            if (ps[i].type == type && ps[i].name == paramName)
                return true;
        return false;
    }

    // Choose an idle parameter value based on equipped items.
    // Winner selection: highest idlePriority wins; ties broken by lowest slot index.
    void RefreshIdleAnimation()
    {
        if (animator == null) return;

        string winnerParam = "IdleStyle";
        int winnerValue = 0; // default baseline idle
        int winnerPriority = int.MinValue;
        int winnerSlot = int.MaxValue;
        bool found = false; // will fix typo shortly

        for (int i = 0; i < slots.Count; ++i)
        {
            if (slots[i].amount == 0) continue;
            if (slots[i].item.data is EquipmentItem eq)
            {
                if (eq.idleParamValue >= 0)
                {
                    int p = eq.idlePriority;
                    if (p > winnerPriority || (p == winnerPriority && i < winnerSlot))
                    {
                        winnerPriority = p;
                        winnerSlot = i;
                        winnerValue = eq.idleParamValue;
                        winnerParam = string.IsNullOrWhiteSpace(eq.idleParam) ? "IdleStyle" : eq.idleParam;
                        found = true; // fix later
                    }
                }
            }
        }

        // Only set if the Animator has the parameter; silently ignore otherwise (back-compat).
        try
        {
            if (found && HasAnimatorParameter(animator, winnerParam, AnimatorControllerParameterType.Int))
                animator.SetInteger(winnerParam, winnerValue);
            else if (HasAnimatorParameter(animator, winnerParam, AnimatorControllerParameterType.Int))
                animator.SetInteger(winnerParam, 0);
        }
        catch { /* ignore mismatched params; keep back-compat */ }
    }
#endif


    
#if !UNITY_SERVER
    // Called by PlayerCustomization via SendMessage at login to replay equipped visuals.
    // Also safe to call directly.
    void Customization_OnRebuildRequested(GFFAddons.PlayerCustomization target)
    {
        if (target == null) return;
        // Replay all currently equipped items into customization system.
        for (int i = 0; i < slots.Count; ++i)
        {
            if (slots[i].amount > 0 && slots[i].item.data is EquipmentItem eq)
            {
                target.OnSlotEquipped(i, eq.targetCustomizationType, eq.overrideCustomizationIndex, eq.hideWhileEquipped);
            }
        }
    }
#endif


    // === Trash handlers & commands (minimal addition) ===
    // Client drop handler: EquipmentSlot -> Trash
    void OnDragAndDrop_EquipmentSlot_Trash(int[] slotIndices)
    {
        // slotIndices[0] = equipmentIndex
        CmdDestroyEquipSlot(slotIndices[0]);
    }

    // Some UIs may name the drop target 'TrashSlot'. Support both.
    void OnDragAndDrop_EquipmentSlot_TrashSlot(int[] slotIndices)
    {
        CmdDestroyEquipSlot(slotIndices[0]);
    }

    // Client drop handler: InventorySlot -> Trash
    void OnDragAndDrop_InventorySlot_Trash(int[] slotIndices)
    {
        // slotIndices[0] = inventoryIndex
        CmdDestroyInventorySlot(slotIndices[0]);
    }

    // Support alternate 'TrashSlot' target name as well.
    void OnDragAndDrop_InventorySlot_TrashSlot(int[] slotIndices)
    {
        CmdDestroyInventorySlot(slotIndices[0]);
    }

    // Server authority: destroy the equipped item in-place
    [Server]
    public void DestroyEquipSlot(int equipmentIndex)
    {
        if (inventory.InventoryOperationsAllowed() &&
            0 <= equipmentIndex && equipmentIndex < slots.Count)
        {
            ItemSlot slot = slots[equipmentIndex];
            if (slot.amount > 0)
            {
                slot.amount = 0; // full delete
                slots[equipmentIndex] = slot;
            }
        }
    }

    // Server authority: destroy an inventory item in-place
    [Server]
    public void DestroyInventorySlot(int inventoryIndex)
    {
        if (inventory.InventoryOperationsAllowed() &&
            0 <= inventoryIndex && inventoryIndex < inventory.slots.Count)
        {
            ItemSlot slot = inventory.slots[inventoryIndex];
            if (slot.amount > 0)
            {
                slot.amount = 0; // full delete
                inventory.slots[inventoryIndex] = slot;
            }
        }
    }

    // Command wrappers
    [Command]
    public void CmdDestroyEquipSlot(int equipmentIndex)
    {
        // block destroying the test auto-fists
        if (autoFistsTestEnabled && autoFistWeaponTest != null &&
            0 <= equipmentIndex && equipmentIndex < slots.Count &&
            IsAutoFist(slots[equipmentIndex]))
            return;

        DestroyEquipSlot(equipmentIndex);
    }

    [Command]
    public void CmdDestroyInventorySlot(int inventoryIndex)
    {
        DestroyInventorySlot(inventoryIndex);
    }
}
