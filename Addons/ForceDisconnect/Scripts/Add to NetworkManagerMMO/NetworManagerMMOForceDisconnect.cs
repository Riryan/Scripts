using UnityEngine;

public class NetworManagerMMOForceDisconnect : MonoBehaviour
{
    public NetworkManagerMMO networkManagerMMO;
#if _SERVER
    void Start()
    {
        if (networkManagerMMO)
            networkManagerMMO.onStartServer.AddListener(StartForceDisconnect);
        else
            Debug.LogWarning("Warning, the NetworkManagerMMO component has not been entered in NetworkManagerMMOForceDisconnect");

    }

    private void StartForceDisconnect()
    {
        InvokeRepeating(nameof(KickDisconnectedPlayer), 60, 60);
    }

    private void KickDisconnectedPlayer()
    {
        if (Player.onlinePlayers.Count > 0)
        {
            foreach (Player player in Player.onlinePlayers.Values)
            {
                string ip = player.connectionToClient.address;
                if (string.IsNullOrEmpty(ip))
                {
                    Player.onlinePlayers.Remove(player.name);
                    Debug.Log("Player " + player.name + " is disconnected !, remove from player online");
                }
            }
        }
    }
#endif
}
