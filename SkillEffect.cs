





















using UnityEngine;
using Mirror;

public abstract class SkillEffect : NetworkBehaviour
{
    [SyncVar, HideInInspector] public Entity target;
    [SyncVar, HideInInspector] public Entity caster;
}
