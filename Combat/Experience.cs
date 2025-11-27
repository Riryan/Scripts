using System;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

[RequireComponent(typeof(Level))]
[DisallowMultipleComponent]
public class Experience : NetworkBehaviour
{
    [Header("Components")]
    public Level level;

    [Header("Experience")]
    [SyncVar, SerializeField] long _current = 0;
    public long current
    {
        get => _current;
        set
        {
            // Clamp to non-negative first
            long clamped = Math.Max(value, 0);

            // If nothing actually changed, do nothing.
            // This guarantees no bandwidth if XP doesn't change.
            if (clamped == _current)
                return;

            // XP loss or manual clamp
            if (clamped <= _current)
            {
                _current = clamped;
            }
            else
            {
                // XP gain
                _current = clamped;

                // Handle level-ups while we have enough XP
                while (_current >= max && level.current < level.max)
                {
                    _current -= max;
                    ++level.current;
                    onLevelUp.Invoke();
                }

                // Don't store more than one level's worth of XP
                if (_current > max)
                    _current = max;
            }
        }
    }

    [SerializeField] protected ExponentialLong _max =
        new ExponentialLong { multiplier = 100, baseValue = 1.1f };

    public long max => _max.Get(level.current);

    [Header("Death")]
    public float deathLossPercent = 0.05f;

    [Header("Events")]
    public UnityEvent onLevelUp;

    // Make XP owner-only so only the owning player receives current XP.
    protected override void OnValidate()
    {
        base.OnValidate();

        if (syncMode != SyncMode.Owner)
            syncMode = SyncMode.Owner;
    }

    public float Percent() =>
        (current != 0 && max != 0) ? (float)current / (float)max : 0;

    // Static helper for reward balancing. Only used where you call it.
    public static long BalanceExperienceReward(long reward,
                                               int attackerLevel,
                                               int victimLevel,
                                               int maxLevelDifference = 20)
    {
        float percentagePerLevel = 1f / maxLevelDifference;

        int levelDiff = Mathf.Clamp(victimLevel - attackerLevel,
                                    -maxLevelDifference,
                                    maxLevelDifference);

        float multiplier = 1 + levelDiff * percentagePerLevel;

        return Convert.ToInt64(reward * multiplier);
    }

    [Server]
    public virtual void OnDeath()
    {
        // XP loss on death. This is a real XP change, so it WILL replicate,
        // but only to the owner because of syncMode = Owner.
        current -= Convert.ToInt64(max * deathLossPercent);
    }
}
