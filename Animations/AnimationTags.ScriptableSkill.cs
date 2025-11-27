using UnityEngine;

public enum SkillAnimationType { ScriptableName, AnimationTag, NoAnimation }
public abstract partial class ScriptableSkill
{
    [Header("[-=-[ Animation Tag ]-=-]")]
    public SkillAnimationType animationType = SkillAnimationType.ScriptableName;
    //[StringShowConditional(conditionFieldName: nameof(animationType), conditionValue: nameof(SkillAnimationType.AnimationTag))]
    public string animationTag;
}