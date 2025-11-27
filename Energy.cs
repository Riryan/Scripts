using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public abstract partial class Energy : NetworkBehaviour
{
    [SyncVar] int _current = 0;

    [SerializeField] bool spawnFull = false;
    [SerializeField] float recoveryInterval = 1f;

    int? _pendingApply;
    int _lastMax = -1;

    public abstract int max { get; }
    public abstract int recoveryRate { get; }
    public abstract int drainRate { get; }

    public float Percent()
    {
        int m = max;
        if (m <= 0) return 0f;
        int c = _current > m ? m : _current;
        return (float)c / m;
    }

    public int current
    {
        get
        {
            int m = max;
            return _current > m ? m : _current;
        }
        [Server]
        set
        {
            int v = value < 0 ? 0 : value;
            _current = v;
            if (max <= 0) _pendingApply = v;
            else _pendingApply = null;
        }
    }

    protected Health health;

    protected virtual void Awake()
    {
        health = GetComponent<Health>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _lastMax = max;
        if (spawnFull && _pendingApply == null && _current <= 0 && _lastMax > 0)
            _current = _lastMax;

        // reset any legacy timer state
        // and schedule our lightweight regen tick instead of per-frame Update.
        #if UNITY_SERVER || UNITY_EDITOR
        ScheduleRegen();
        #endif
    }

#if UNITY_SERVER || UNITY_EDITOR
    void OnDisable()
    {
        if (!isServer) return;
        CancelInvoke(nameof(ServerRegenTick));
    }

    void ScheduleRegen()
    {
        if (!isServer) return;
        CancelInvoke(nameof(ServerRegenTick));
        if (recoveryInterval > 0f && recoveryRate > 0 && max > 0)
        {
            InvokeRepeating(nameof(ServerRegenTick), recoveryInterval, recoveryInterval);
        }
    }

    [ServerCallback]
    void ServerRegenTick()
    {
        // if max was zero when someone set 'current', apply the deferred value once max becomes valid
        if (_pendingApply.HasValue && max > 0)
        {
            _current = _pendingApply.Value < 0 ? 0 : _pendingApply.Value;
            _pendingApply = null;
        }

        Recovering();
    }

    [Server]
    public void Recovering()
    {
        if (!enabled || health == null) return;
        if (health.current > 0 && recoveryRate > 0 && _current < max)
        {
            int next = _current + recoveryRate;
            if (next > max) next = max;
            current = next;
        }
    }

    [Server]
    public void Draining()
    {
        if (!enabled || health == null) return;
        if (health.current > 0 && drainRate > 0 && _current > 0)
        {
            int next = _current - drainRate;
            if (next < 0) next = 0;
            current = next;
        }
    }
#endif
}
