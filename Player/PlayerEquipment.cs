using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace uMMORPG
{
    [Serializable]
    public partial struct EquipmentInfo
    {
        public string requiredCategory;
        public Transform location;
        public ScriptableItemAndAmount defaultItem;
    }

    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerInventory))]
    public class PlayerEquipment : Equipment
    {
        [Header("Components")]
        public Player player;
        public Animator animator;
        public PlayerInventory inventory;

        // avatar Camera is only enabled while Equipment UI is active
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
            new EquipmentInfo{requiredCategory="Feet", location=null, defaultItem=new ScriptableItemAndAmount()}
        };
        public const int UNARMED_WEAPON_INDEX = -2;
        // cached SkinnedMeshRenderer bones without equipment, by name
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
        }

        void OnEquipmentChanged(SyncList<ItemSlot>.Operation op, int index, ItemSlot oldSlot, ItemSlot newSlot)
        {
            ScriptableItem oldItem = oldSlot.amount > 0 ? oldSlot.item.data : null;
            ScriptableItem newItem = newSlot.amount > 0 ? newSlot.item.data : null;
            if (oldItem != newItem)
            {
                RefreshLocation(index);
            }
        }

        bool CanReplaceAllBones(SkinnedMeshRenderer equipmentSkin)
        {
            foreach (Transform bone in equipmentSkin.bones)
                if (!skinBones.ContainsKey(bone.name))
                    return false;
            return true;
        }

        void ReplaceAllBones(SkinnedMeshRenderer equipmentSkin)
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

        void RebindAnimators()
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
                        go.name = itemData.modelPrefab.name; // avoid "(Clone)"

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
        public void SwapInventoryEquip(int inventoryIndex, int equipmentIndex)
        {
            if (inventory.InventoryOperationsAllowed() &&
                0 <= inventoryIndex && inventoryIndex < inventory.slots.Count &&
                0 <= equipmentIndex && equipmentIndex < slots.Count)
            {
                ItemSlot slot = inventory.slots[inventoryIndex];
                if (slot.amount == 0 ||
                    slot.item.data is EquipmentItem itemData &&
                    itemData.CanEquip(player, inventoryIndex, equipmentIndex))
                {
                    ItemSlot temp = slots[equipmentIndex];
                    slots[equipmentIndex] = slot;
                    inventory.slots[inventoryIndex] = temp;
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
                    slot.item.durability = Mathf.Clamp(slot.item.durability - 1, 0, slot.item.maxDurability);
                    slots[i] = slot;
                }
            }
        }

        // drag & drop /////////////////////////////////////////////////////////////
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
                CmdSwapInventoryEquip(slotIndices[1], slotIndices[0]); // reversed
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            for (int i = 0; i < slotInfo.Length; ++i)
                if (slotInfo[i].defaultItem.item != null && slotInfo[i].defaultItem.amount == 0)
                    slotInfo[i].defaultItem.amount = 1;
        }
    }
}