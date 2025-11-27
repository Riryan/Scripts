

using UnityEngine;
using Mirror;

[CreateAssetMenu(menuName="uMMORPG Skill/Target Projectile", order=999)]
public class TargetProjectileSkill : DamageSkill
{
    [Header("Projectile")]
    public ProjectileSkillEffect projectile; 

    bool HasRequiredWeaponAndAmmo(Entity caster)
    {
        
        
        
        
        if (string.IsNullOrWhiteSpace(requiredWeaponCategory))
            return true;

        int weaponIndex = caster.equipment.GetEquippedWeaponIndex();
        if (weaponIndex != -1)
        {
            
            WeaponItem itemData = (WeaponItem)caster.equipment.slots[weaponIndex].item.data;
            return itemData.requiredAmmo == null ||
                   caster.equipment.GetItemIndexByName(itemData.requiredAmmo.name) != -1;
        }
        return false;
    }

    void ConsumeRequiredWeaponsAmmo(Entity caster)
    {
        
        
        
        
        if (string.IsNullOrWhiteSpace(requiredWeaponCategory))
            return;

        int weaponIndex = caster.equipment.GetEquippedWeaponIndex();
        if (weaponIndex != -1)
        {
            
            WeaponItem itemData = (WeaponItem)caster.equipment.slots[weaponIndex].item.data;
            if (itemData.requiredAmmo != null)
            {
                int ammoIndex = caster.equipment.GetItemIndexByName(itemData.requiredAmmo.name);
                if (ammoIndex != 0)
                {
                    
                    ItemSlot slot = caster.equipment.slots[ammoIndex];
                    --slot.amount;
                    caster.equipment.slots[ammoIndex] = slot;
                }
            }
        }
    }

    public override bool CheckSelf(Entity caster, int skillLevel)
    {
        
        return base.CheckSelf(caster, skillLevel) &&
               HasRequiredWeaponAndAmmo(caster);
    }

    public override bool CheckTarget(Entity caster)
    {
        
        return caster.target != null && caster.CanAttack(caster.target);
    }

    public override bool CheckDistance(Entity caster, int skillLevel, out Vector3 destination)
    {
        
        if (caster.target != null)
        {
            destination = Utils.ClosestPoint(caster.target, caster.transform.position);
            return Utils.ClosestDistance(caster, caster.target) <= castRange.Get(skillLevel);
        }
        destination = caster.transform.position;
        return false;
    }

    public override void Apply(Entity caster, int skillLevel)
    {
        
        ConsumeRequiredWeaponsAmmo(caster);

        
        
        
        
        
        if (projectile != null)
        {
            GameObject go = Instantiate(projectile.gameObject, caster.skills.effectMount.position, caster.skills.effectMount.rotation);
            ProjectileSkillEffect effect = go.GetComponent<ProjectileSkillEffect>();
            effect.target = caster.target;
            effect.caster = caster;
            effect.damage = damage.Get(skillLevel);
            effect.stunChance = stunChance.Get(skillLevel);
            effect.stunTime = stunTime.Get(skillLevel);
            NetworkServer.Spawn(go);
        }
        else Debug.LogWarning(name + ": missing projectile");
    }
}
