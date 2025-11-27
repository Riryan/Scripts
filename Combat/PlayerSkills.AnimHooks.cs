#if !UNITY_SERVER || UNITY_EDITOR
using UnityEngine;

public partial class PlayerSkills
{
    Animator _anim;
    PlayerEquipment _equip;

    // fallback when unarmed / no tag set on weapon
    [SerializeField] string _unarmedDefaultTag = "Unarmed Attack";

    public override void OnStartClient()
    {
        base.OnStartClient();
        _anim  = GetComponentInChildren<Animator>();
        _equip = GetComponent<PlayerEquipment>();

        onSkillCastStarted.AddListener(OnCastStarted_ClientAnim);
        onSkillCastFinished.AddListener(OnCastFinished_ClientAnim);
    }

    void OnDestroy()
    {
        onSkillCastStarted.RemoveListener(OnCastStarted_ClientAnim);
        onSkillCastFinished.RemoveListener(OnCastFinished_ClientAnim);
    }

    void OnCastStarted_ClientAnim(Skill skill)
    {
        if (!isLocalPlayer || _anim == null) return;

        // If the skill has its own animation tag, play that.
        if (skill.animationType == SkillAnimationType.AnimationTag &&
            !string.IsNullOrWhiteSpace(skill.animationTag))
        {
            _anim.CrossFadeInFixedTime(skill.animationTag, 0.05f);
            return;
        }

        // Otherwise use the weapon-driven default tag.
        if (skill.animationType == SkillAnimationType.NoAnimation)
            PlayDefaultAttack();
    }

    void OnCastFinished_ClientAnim(Skill skill)
    {
        if (!isLocalPlayer || _anim == null) return;

        // If the skill wants to chain into the default attack, play it now.
        if (skill.followupDefaultAttack)
            PlayDefaultAttack();
    }

    void PlayDefaultAttack()
    {
        string tag = GetCurrentWeaponDefaultTag();
        if (!string.IsNullOrWhiteSpace(tag))
            _anim.CrossFadeInFixedTime(tag, 0.05f);
    }

    string GetCurrentWeaponDefaultTag()
    {
        // Weapon slot = index 0 in your setup
        if (_equip && _equip.slots != null && _equip.slots.Count > 0)
        {
            var slot = _equip.slots[0];
            if (slot.amount > 0 && slot.item.data is WeaponItem weapon &&
                !string.IsNullOrWhiteSpace(weapon.defaultAttackTag))
                return weapon.defaultAttackTag;
        }
        return _unarmedDefaultTag;
    }
}
#endif
