using UnityEngine;
using Mirror;

public partial class NetworkManagerMMO
{
    [Header("Tools — Database Cleaner")]
    [Tooltip("Assign a DatabaseCleaner asset. If left empty, loads Resources/DatabaseCleaner at runtime.")]
    public Tmpl_DatabaseCleaner databaseCleaner;

#if UNITY_SERVER || UNITY_EDITOR
    void __DBC_TryAutoWireAsset()
    {
        if (databaseCleaner == null)
            databaseCleaner = Resources.Load<Tmpl_DatabaseCleaner>("DatabaseCleaner");
    }

    [ServerCallback]
    void OnEnable()
    {
        __DBC_TryAutoWireAsset();
        onStartServer.AddListener(__DBC_OnStartServer);
        onServerDisconnect.AddListener(__DBC_OnServerDisconnect);
    }

    [ServerCallback]
    void OnDisable()
    {
        onStartServer.RemoveListener(__DBC_OnStartServer);
        onServerDisconnect.RemoveListener(__DBC_OnServerDisconnect);
    }

    [ServerCallback]
    void __DBC_OnStartServer()
    {
        if (databaseCleaner != null && databaseCleaner.isActive)
            Database.singleton.Cleanup(databaseCleaner);
        else
            Debug.LogWarning("DatabaseCleaner: Either inactive or ScriptableObject not found!");
    }

    [ServerCallback]
    void __DBC_OnServerDisconnect(NetworkConnection conn)
    {
        if (conn == null || conn.identity == null) return;

        var player = conn.identity.GetComponent<Player>();
        if (player == null || string.IsNullOrWhiteSpace(player.account)) return;

        // We only track our own last-online table; no accounts.lastLogin writes.
        Database.singleton.UpsertAccountLastOnline(player.account);
    }

    [ContextMenu("Tools/Run DB Cleanup Now (Server Only)")]
    [Server]
    public void RunDatabaseCleanupNow() => __DBC_OnStartServer();
#endif
}
