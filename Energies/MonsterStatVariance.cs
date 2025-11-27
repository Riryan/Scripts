// MonsterStatVariance.cs
// Add to Monster prefabs (not Players).
using UnityEngine;
#if MIRROR
using Mirror;
#endif

[DisallowMultipleComponent]
#if MIRROR
public class MonsterStatVariance : NetworkBehaviour, IHealthBonus, IManaBonus
#else
public class MonsterStatVariance : MonoBehaviour, IHealthBonus, IManaBonus
#endif
{
    [Header("Random variance (± percent of base at this level)")]
    [Range(0, 50)] public int healthVariancePercent = 10;
    [Range(0, 50)] public int manaVariancePercent   = 10;

#if MIRROR
    [SyncVar] int healthRollPercent;   // e.g., -10 .. +10
    [SyncVar] int manaRollPercent;     // e.g., -10 .. +10
#else
    int healthRollPercent;
    int manaRollPercent;
#endif

#if MIRROR
    public override void OnStartServer() => RollIfNeeded();
#else
    void Awake() => RollIfNeeded();    // or guard with your own "server-only" check
#endif

    void RollIfNeeded()
    {
        // Safety: if this ends up on a Player, disable effect
        if (GetComponent<Player>() != null)
        {
            healthRollPercent = 0;
            manaRollPercent   = 0;
            return;
        }

        healthRollPercent = Random.Range(-healthVariancePercent, healthVariancePercent + 1);
        manaRollPercent   = Random.Range(-manaVariancePercent,   manaVariancePercent   + 1);
    }

    // IHealthBonus
    public int GetHealthBonus(int baseHealth)
        => Mathf.RoundToInt(baseHealth * (healthRollPercent / 100f));
    public int GetHealthRecoveryBonus() => 0;

    // IManaBonus
    public int GetManaBonus(int baseMana)
        => Mathf.RoundToInt(baseMana * (manaRollPercent / 100f));
    public int GetManaRecoveryBonus() => 0;
}
