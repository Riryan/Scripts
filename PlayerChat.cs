using System;
using UnityEngine;
using Mirror;

[Serializable]
public class ChannelInfo
{
    public string command;
    public string identifierOut;
    public string identifierIn;
    public GameObject textPrefab;

    public ChannelInfo(string command, string identifierOut, string identifierIn, GameObject textPrefab)
    {
        this.command = command;
        this.identifierOut = identifierOut;
        this.identifierIn = identifierIn;
        this.textPrefab = textPrefab;
    }
}

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(PlayerGuild))]
[RequireComponent(typeof(PlayerParty))]
[DisallowMultipleComponent]
public class PlayerChat : NetworkBehaviour
{
    [Header("Components")]
    public PlayerGuild guild;
    public PlayerParty party;

    [Header("Channels")]
    public ChannelInfo whisperChannel = new ChannelInfo("/w", "(TO)", "(FROM)", null);
    public ChannelInfo localChannel   = new ChannelInfo("", "", "", null);
    public ChannelInfo partyChannel   = new ChannelInfo("/p", "(Party)", "(Party)", null);
    public ChannelInfo guildChannel   = new ChannelInfo("/g", "(Guild)", "(Guild)", null);
    public ChannelInfo infoChannel    = new ChannelInfo("", "(Info)", "(Info)", null);

    [Header("Other")]
    public int maxLength = 70;

    [Header("Spam Protection")]
    [Tooltip("Seconds between messages to prevent spam.")]
    public float chatCooldown = 0.5f;
    double lastChatTimeServer;
    float  lastChatTimeClient;

    [Header("Events")]
    public UnityEventString onSubmit; // defined in Utils.cs

    public override void OnStartLocalPlayer()
    {
        // Example welcome lines could go here if desired
    }

    // -------------------------------------------------------------------------
    // CLIENT
    // -------------------------------------------------------------------------
    public string OnSubmit(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        // client-side anti-spam
        if (Time.time < lastChatTimeClient + chatCooldown)
            return ""; // ignore if too soon
        lastChatTimeClient = Time.time;

        string lastCommand = "";

        if (text.StartsWith(whisperChannel.command))
        {
            (string user, string message) = ParsePM(whisperChannel.command, text);
            if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(message))
            {
                if (user != name)
                {
                    lastCommand = whisperChannel.command + " " + user + " ";
                    CmdMsgWhisper(user, message);
                }
                else Debug.Log("Can't whisper to self");
            }
        }
        else if (!text.StartsWith("/"))
        {
            lastCommand = "";
            CmdMsgLocal(text);
        }
        else if (text.StartsWith(partyChannel.command))
        {
            string msg = ParseGeneral(partyChannel.command, text);
            if (!string.IsNullOrWhiteSpace(msg))
            {
                lastCommand = partyChannel.command + " ";
                CmdMsgParty(msg);
            }
        }
        else if (text.StartsWith(guildChannel.command))
        {
            string msg = ParseGeneral(guildChannel.command, text);
            if (!string.IsNullOrWhiteSpace(msg))
            {
                lastCommand = guildChannel.command + " ";
                CmdMsgGuild(msg);
            }
        }
        // -------------------------------------------------------------------------
        // Debug / Utility Commands
        // -------------------------------------------------------------------------
        else if (text.StartsWith("/bank"))
        {
            // let server handle warehouse open (no message broadcast)
            CmdOpenWarehouse();
        }
        onSubmit.Invoke(text);
        return lastCommand;
    }

    // -------------------------------------------------------------------------
    // PARSING HELPERS
    // -------------------------------------------------------------------------
    internal static string ParseGeneral(string command, string msg)
    {
        return msg.StartsWith(command + " ") ? msg.Substring(command.Length + 1) : "";
    }

    internal static (string user, string message) ParsePM(string command, string pm)
    {
        string content = ParseGeneral(command, pm);
        if (content != "")
        {
            int i = content.IndexOf(" ");
            if (i >= 0)
            {
                string user = content.Substring(0, i);
                string msg = content.Substring(i + 1);
                return (user, msg);
            }
        }
        return ("", "");
    }

    // -------------------------------------------------------------------------
    // SERVER ANTI-SPAM CHECK
    // -------------------------------------------------------------------------
    bool CanSendChatServer(string message)
    {
        if (message.Length > maxLength) return false;
        if (NetworkTime.time < lastChatTimeServer + chatCooldown)
            return false;
        lastChatTimeServer = NetworkTime.time;
        return true;
    }

    // -------------------------------------------------------------------------
    // COMMANDS & RPCs
    // -------------------------------------------------------------------------
    [Command]
    void CmdMsgLocal(string message)
    {
        if (!CanSendChatServer(message)) return;
        RpcMsgLocal(name, message);
    }

    [Command]
    void CmdMsgParty(string message)
    {
        if (!CanSendChatServer(message)) return;
        if (party.InParty())
        {
            foreach (string member in party.party.members)
                if (Player.onlinePlayers.TryGetValue(member, out Player onlinePlayer))
                    onlinePlayer.chat.TargetMsgParty(name, message);
        }
    }

    [Command]
    void CmdMsgGuild(string message)
    {
        if (!CanSendChatServer(message)) return;
        if (guild.InGuild())
        {
            foreach (GuildMember member in guild.guild.members)
                if (Player.onlinePlayers.TryGetValue(member.name, out Player onlinePlayer))
                    onlinePlayer.chat.TargetMsgGuild(name, message);
        }
    }

    [Command]
    void CmdMsgWhisper(string playerName, string message)
    {
        if (!CanSendChatServer(message)) return;
        if (Player.onlinePlayers.TryGetValue(playerName, out Player onlinePlayer))
        {
            onlinePlayer.chat.TargetMsgWhisperFrom(name, message);
            TargetMsgWhisperTo(playerName, message);
        }
    }
[Command]
void CmdOpenWarehouse()
{
    if (!CanSendChatServer("/bank")) return; // reuse spam timer

    // PlayerChat has [RequireComponent(typeof(Player))],
    // so we are guaranteed to have a Player component.
    Player player = GetComponent<Player>();
    if (player != null)
    {
       // player.TargetShowWarehouseUI();
       player.ServerOpenWarehouse();
    }
}

    [Server]
    public void SendGlobalMessage(string message)
    {
        foreach (Player player in Player.onlinePlayers.Values)
            player.chat.TargetMsgInfo(message);
    }

    // -------------------------------------------------------------------------
    // TARGET RPCs
    // -------------------------------------------------------------------------
    [TargetRpc]
    public void TargetMsgWhisperFrom(string sender, string message)
    {
        string identifier = whisperChannel.identifierIn;
        string reply = whisperChannel.command + " " + sender + " ";
        UIChat.singleton.AddMessage(new ChatMessage(sender, identifier, message, reply, whisperChannel.textPrefab));
    }

    [TargetRpc]
    public void TargetMsgWhisperTo(string receiver, string message)
    {
        string identifier = whisperChannel.identifierOut;
        string reply = whisperChannel.command + " " + receiver + " ";
        UIChat.singleton.AddMessage(new ChatMessage(receiver, identifier, message, reply, whisperChannel.textPrefab));
    }

    [ClientRpc]
    public void RpcMsgLocal(string sender, string message)
    {
        string identifier = sender != name ? localChannel.identifierIn : localChannel.identifierOut;
        string reply = whisperChannel.command + " " + sender + " ";
        UIChat.singleton.AddMessage(new ChatMessage(sender, identifier, message, reply, localChannel.textPrefab));
    }

    [TargetRpc]
    public void TargetMsgGuild(string sender, string message)
    {
        string reply = whisperChannel.command + " " + sender + " ";
        UIChat.singleton.AddMessage(new ChatMessage(sender, guildChannel.identifierIn, message, reply, guildChannel.textPrefab));
    }

    [TargetRpc]
    public void TargetMsgParty(string sender, string message)
    {
        string reply = whisperChannel.command + " " + sender + " ";
        UIChat.singleton.AddMessage(new ChatMessage(sender, partyChannel.identifierIn, message, reply, partyChannel.textPrefab));
    }

    [TargetRpc]
    public void TargetMsgInfo(string message)
    {
        AddMsgInfo(message);
    }

    public void AddMsgInfo(string message)
    {
        UIChat.singleton.AddMessage(new ChatMessage("", infoChannel.identifierIn, message, "", infoChannel.textPrefab));
    }
}
