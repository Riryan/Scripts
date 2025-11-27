using UnityEngine;
using Mirror;
using TMPro;

[RequireComponent(typeof(PlayerChat))]
[DisallowMultipleComponent]
public class PlayerGuild : NetworkBehaviour
{
    [Header("Components")]
    public Player player;
    public PlayerChat chat;

    [Header("Text Meshes")]
    public TextMeshPro overlay;
    public string overlayPrefix = "[";
    public string overlaySuffix = "]";

    
    
    [Header("Guild")]
    [SyncVar, HideInInspector] public string inviteFrom = "";
    [SyncVar, HideInInspector] public Guild guild; 
    public float inviteWaitSeconds = 3;

    void Start()
    {
        
        if (!isServer && !isClient) return;

        
        
        
        if (isServer)
            SetOnline(true);
    }

    void Update()
    {
        
        
        if (!isServerOnly)
        {
            if (overlay != null)
                overlay.text = !string.IsNullOrWhiteSpace(guild.name) ? overlayPrefix + guild.name + overlaySuffix : "";
        }
    }

    void OnDestroy()
    {
        
        if (!isServer && !isClient) return;

        
        if (isServer)
            SetOnline(false);
    }

    
    public bool InGuild() => !string.IsNullOrWhiteSpace(guild.name);

    
    
    
    [ServerCallback]
    public void SetOnline(bool online)
    {
        
        if (InGuild())
            GuildSystem.SetGuildOnline(guild.name, name, online);
    }

    [Command]
    public void CmdInviteTarget()
    {
        
        if (player.target != null &&
            player.target is Player targetPlayer &&
            InGuild() && !targetPlayer.guild.InGuild() &&
            guild.CanInvite(name, targetPlayer.name) &&
            NetworkTime.time >= player.nextRiskyActionTime &&
            Utils.ClosestDistance(player, targetPlayer) <= player.interactionRange)
        {
            
            targetPlayer.guild.inviteFrom = name;
            Debug.Log(name + " invited " + player.target.name + " to guild");
        }

        
        
        
        player.nextRiskyActionTime = NetworkTime.time + inviteWaitSeconds;
    }

    [Command]
    public void CmdInviteAccept()
    {
        
        
        if (!InGuild() && inviteFrom != "" &&
            Player.onlinePlayers.TryGetValue(inviteFrom, out Player sender) &&
            sender.guild.InGuild())
        {
            
            GuildSystem.AddToGuild(sender.guild.guild.name, sender.name, name, player.level.current);
        }

        
        inviteFrom = "";
    }

    [Command]
    public void CmdInviteDecline()
    {
        inviteFrom = "";
    }

    [Command]
    public void CmdKick(string memberName)
    {
        
        if (InGuild())
            GuildSystem.KickFromGuild(guild.name, name, memberName);
    }

    [Command]
    public void CmdPromote(string memberName)
    {
        
        if (InGuild())
            GuildSystem.PromoteMember(guild.name, name, memberName);
    }

    [Command]
    public void CmdDemote(string memberName)
    {
        
        if (InGuild())
            GuildSystem.DemoteMember(guild.name, name, memberName);
    }

    [Command]
    public void CmdSetNotice(string notice)
    {
        
        
        if (InGuild() && NetworkTime.time >= player.nextRiskyActionTime)
        {
            
            GuildSystem.SetGuildNotice(guild.name, name, notice);
        }

        
        
        player.nextRiskyActionTime = NetworkTime.time + GuildSystem.NoticeWaitSeconds;
    }

    
    public bool IsGuildManagerNear()
    {
        return player.target != null &&
               player.target is Npc npc &&
               npc.guildManagement != null && 
               Utils.ClosestDistance(player, player.target) <= player.interactionRange;
    }

    [Command]
    public void CmdTerminate()
    {
        
        if (InGuild() && IsGuildManagerNear())
            GuildSystem.TerminateGuild(guild.name, name);
    }

    [Command]
    public void CmdCreate(string guildName)
    {
        
        if (player.health.current > 0 && player.gold >= GuildSystem.CreationPrice &&
            !InGuild() && IsGuildManagerNear())
        {
            
            if (GuildSystem.CreateGuild(name, player.level.current, guildName))
                player.gold -= GuildSystem.CreationPrice;
            else
                chat.TargetMsgInfo("Guild name invalid!");
        }
    }

    [Command]
    public void CmdLeave()
    {
        
        if (InGuild())
            GuildSystem.LeaveGuild(guild.name, name);
    }
}
