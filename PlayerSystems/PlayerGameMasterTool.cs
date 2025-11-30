
using UnityEditor;
using UnityEngine;
using Mirror;

public class PlayerGameMasterTool : NetworkBehaviour
{
    [Header("Components")]
    public Player player;

    

    
    [HideInInspector, SyncVar] public int connections;
    [HideInInspector, SyncVar] public int maxConnections;
    [HideInInspector, SyncVar] public int onlinePlayers;
    [HideInInspector, SyncVar] public float uptime;
    [HideInInspector, SyncVar] public int tickRate;

    
    int tickRateCounter;
    double tickRateStart;

    
    public override void OnStartServer()
    {
        
        if (!player.isGameMaster) return;

        
        InvokeRepeating(nameof(RefreshData), syncInterval, syncInterval);
    }

    [ServerCallback]
    void Update()
    {
        
        if (!player.isGameMaster) return;

        
        ++tickRateCounter;
        if (NetworkTime.time >= tickRateStart + 1)
        {
            
            tickRate = tickRateCounter;

            
            tickRateCounter = 0;
            tickRateStart = NetworkTime.time;
        }
    }

    [Server]
    void RefreshData()
    {
        
        if (!player.isGameMaster) return;

        
        connections = NetworkServer.connections.Count;
        maxConnections = NetworkManager.singleton.maxConnections;
        onlinePlayers = Player.onlinePlayers.Count;
        uptime = Time.realtimeSinceStartup;
    }

    [Command]
    public void CmdSendGlobalMessage(string message)
    {
        
        if (!player.isGameMaster) return;

        player.chat.SendGlobalMessage(message);
    }

    [Command]
    public void CmdShutdown()
    {
        
        if (!player.isGameMaster) return;

        NetworkManagerMMO.Quit();
    }

    
    [Command]
    public void CmdSetCharacterInvincible(bool value)
    {
        
        if (!player.isGameMaster) return;

        player.combat.invincible = value;
    }

    [Command]
    public void CmdSetCharacterLevel(int value)
    {
        
        if (!player.isGameMaster) return;

        player.level.current = Mathf.Clamp(value, 1, player.level.max);
    }

    [Command]
    public void CmdSetCharacterExperience(long value)
    {
        
        if (!player.isGameMaster) return;

        player.experience.current = Utils.Clamp(value, 0, player.experience.max);
    }

    [Command]
    public void CmdSetCharacterSkillExperience(long value)
    {
        
        if (!player.isGameMaster) return;

        if (value > 0)
            ((PlayerSkills)player.skills).skillExperience = value;
    }

    [Command]
    public void CmdSetCharacterGold(long value)
    {
        
        if (!player.isGameMaster) return;

        if (value > 0)
            player.gold = value;
    }

    [Command]
    public void CmdSetCharacterCoins(long value)
    {
        
        if (!player.isGameMaster) return;

        if (value > 0)
            player.itemMall.coins = value;
    }

    
    [Command]
    public void CmdWarp(string otherPlayer)
    {
        
        if (!player.isGameMaster) return;

        
        if (Player.onlinePlayers.TryGetValue(otherPlayer, out Player other))
            player.movement.Warp(other.transform.position);
    }

    [Command]
    public void CmdSummon(string otherPlayer)
    {
        
        if (!player.isGameMaster) return;

        
        
        if (Player.onlinePlayers.TryGetValue(otherPlayer, out Player other))
        {
            other.movement.Warp(player.transform.position);
            other.chat.TargetMsgInfo("A GM summoned you.");
        }
    }

    [Command]
    public void CmdKill(string otherPlayer)
    {
        
        if (!player.isGameMaster) return;

        
        if (Player.onlinePlayers.TryGetValue(otherPlayer, out Player other))
        {
            other.health.current = 0;
            other.chat.TargetMsgInfo("A GM killed you.");
        }
    }

    [Command]
    public void CmdKick(string otherPlayer)
    {
        
        if (!player.isGameMaster) return;

        
        if (Player.onlinePlayers.TryGetValue(otherPlayer, out Player other))
            
            other.connectionToClient.Disconnect();
    }

    
    protected override void OnValidate()
    {
        base.OnValidate();

        
        
        if (syncMode != SyncMode.Owner)
        {
            syncMode = SyncMode.Owner;
#if UNITY_EDITOR
            Undo.RecordObject(this, name + " " + GetType() + " component syncMode changed to Owner.");
#endif
        }
    }
}
