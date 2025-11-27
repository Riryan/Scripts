using UnityEngine;
using Mirror;

// NETWORK MANAGER MMO
public partial class NetworkManagerMMODatabaseCleaner : MonoBehaviour
{
    public NetworkManagerMMO networkManagerMMO;
    public Tmpl_DatabaseCleaner databaseCleaner;

    #region SERVER ONLY
#if UNITY_SERVER || UNITY_EDITOR

    // -----------------------------------------------------------------------------------
    // OnStartServer_Tools_DatabaseCleaner
    // -----------------------------------------------------------------------------------
    public void Start()
    {
        networkManagerMMO.onStartServer.AddListener(OnStartServer_DatabaseCleaner);
        networkManagerMMO.onServerDisconnect.AddListener(OnServerDisconnect_DatabaseCleaner);
    }

    private void OnStartServer_DatabaseCleaner()
    {
        if (databaseCleaner && databaseCleaner.isActive)
            Database.singleton.Cleanup(databaseCleaner);
        else
            Debug.LogWarning("DatabaseCleaner: Either inactive or ScriptableObject not found!");
    }

    // -----------------------------------------------------------------------------------
    // OnServerDisconnect_Tools_DatabaseCleaner
    // -----------------------------------------------------------------------------------
    private void OnServerDisconnect_DatabaseCleaner(NetworkConnection conn)
    {
        if (conn.identity != null)
        {
            var accountName = conn.identity.gameObject.GetComponent<Player>().account;
            Database.singleton.DatabaseCleanerAccountLastOnline(accountName);
        }
    }

    [Server]
    public void Cleanup()
    {
        OnStartServer_DatabaseCleaner();
    }

#endif
    #endregion SERVER ONLY
    // -----------------------------------------------------------------------------------
}
