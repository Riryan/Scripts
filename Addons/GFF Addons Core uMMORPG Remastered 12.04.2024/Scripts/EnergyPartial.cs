using UnityEngine;

public partial class Energy
{
    //Stamina, GMTool, CombatSkills
    [Header("Components")]
    public Entity entity;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (syncInterval == 0)
        {
            syncInterval = 0.1f;
        }

        if (entity == null) entity = gameObject.GetComponent<Entity>();
        if (health == null) health = gameObject.GetComponent<Health>();
    }
#endif
}
