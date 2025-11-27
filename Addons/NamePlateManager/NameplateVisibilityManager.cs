using System.Collections.Generic;
using UnityEngine;
using Mirror;

#if !UNITY_SERVER || UNITY_EDITOR
[DisallowMultipleComponent]
public sealed class NameplateVisibilityManager : MonoBehaviour
{
    private static NameplateVisibilityManager _instance;
    private static readonly List<MonsterNameplateVisibility> Entries =
        new List<MonsterNameplateVisibility>(256);

    [Header("Tick Settings")]
    [Tooltip("Seconds between visibility updates for a batch of nameplates.")]
    [SerializeField] private float tickInterval = 0.15f; // ~6-7 Hz

    [Tooltip("Maximum number of nameplates to update per tick.")]
    [SerializeField] private int maxUpdatesPerTick = 32;

    private float _nextTickTime;
    private int _nextIndex;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime < _nextTickTime)
            return;

        _nextTickTime = Time.unscaledTime + Mathf.Max(0.01f, tickInterval);

        int count = Entries.Count;
        if (count == 0)
            return;

        Player player = GetLocalPlayer();
        if (player == null)
        {
            // No local player yet: force-hide everything.
            for (int i = 0; i < Entries.Count; ++i)
            {
                var entry = Entries[i];
                if (entry != null && entry.isActiveAndEnabled)
                    entry.UpdateVisibility(null, null);
            }
            return;
        }

        Transform playerTransform = player.transform;
        Entity playerTarget = player.target;

        count = Entries.Count;
        if (count == 0)
            return;

        int toProcess = Mathf.Min(Mathf.Max(1, maxUpdatesPerTick), count);

        for (int i = 0; i < toProcess; ++i)
        {
            if (count == 0)
                break;

            if (_nextIndex >= count)
                _nextIndex = 0;

            var entry = Entries[_nextIndex];
            if (entry == null)
            {
                Entries.RemoveAt(_nextIndex);
                count = Entries.Count;
                if (count == 0)
                    break;

                if (_nextIndex >= count)
                    _nextIndex = 0;

                continue;
            }

            if (entry.isActiveAndEnabled)
                entry.UpdateVisibility(playerTransform, playerTarget);

            _nextIndex++;
        }
    }

    private static Player GetLocalPlayer()
    {
        if (!NetworkClient.isConnected)
            return null;

        var identity = NetworkClient.localPlayer;
        if (identity == null)
            return null;

        return identity.GetComponent<Player>();
    }

    // --- Registration API used by MonsterNameplateVisibility ---

    public static void Register(MonsterNameplateVisibility visibility)
    {
        if (visibility == null)
            return;

        if (!Entries.Contains(visibility))
            Entries.Add(visibility);
    }

    public static void Unregister(MonsterNameplateVisibility visibility)
    {
        if (visibility == null)
            return;

        Entries.Remove(visibility);
    }
}
#else
// Server/headless build: empty stub so scenes/prefabs still compile.
public sealed class NameplateVisibilityManager : MonoBehaviour
{
    public static void Register(MonsterNameplateVisibility visibility) { }
    public static void Unregister(MonsterNameplateVisibility visibility) { }
}
#endif
