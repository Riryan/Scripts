using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class GuildSystem
{
    public static Dictionary<string, Guild> guilds = new Dictionary<string, Guild>();

    public static int Capacity = 50;
    public static int NoticeMaxLength = 30;
    public static int NoticeWaitSeconds = 5;
    public static int CreationPrice = 100;
    public static int NameMaxLength = 16;

    // Compatibility alias for Guild.cs (uses MaxMembers)
    public static int MaxMembers => Capacity;

    public static GuildRank InviteMinRank = GuildRank.Vice;
    public static GuildRank KickMinRank = GuildRank.Vice;
    public static GuildRank PromoteMinRank = GuildRank.Master; 
    public static GuildRank NotifyMinRank = GuildRank.Vice;

    static readonly Regex GuildNameRegex = new Regex(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    static void BroadcastTo(string member, Guild guild)
    {
        if (Player.onlinePlayers.TryGetValue(member, out Player player))
            player.guild.guild = guild;
    }

    static void BroadcastChanges(Guild guild)
    {
        foreach (GuildMember member in guild.members)
            BroadcastTo(member.name, guild);

        guilds[guild.name] = guild;
    }

    public static bool IsValidGuildName(string guildName)
    {
        return guildName.Length <= NameMaxLength &&
               GuildNameRegex.IsMatch(guildName);
    }

    public static bool CreateGuild(string creator, int creatorLevel, string guildName)
    {
        if (IsValidGuildName(guildName) &&
            !Database.singleton.GuildExists(guildName))
        {
            Guild guild = new Guild(guildName, creator, creatorLevel);

            BroadcastChanges(guild);
            Debug.Log(creator + " created guild: " + guildName);
            return true;
        }

        return false;
    }

    public static void LeaveGuild(string guildName, string member)
    {
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanLeave(member))
        {
            int idx = guild.GetMemberIndex(member);
            if (idx != -1)
            {
                // shift down and shrink by 1 (no LINQ allocations)
                for (int i = idx; i < guild.members.Length - 1; ++i)
                    guild.members[i] = guild.members[i + 1];
                Array.Resize(ref guild.members, guild.members.Length - 1);
            }

            BroadcastTo(member, Guild.Empty);
            BroadcastChanges(guild);
        }
    }

    public static void TerminateGuild(string guildName, string requester)
    {
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanTerminate(requester))
        {
            Database.singleton.RemoveGuild(guildName);
            BroadcastTo(requester, Guild.Empty);
            guilds.Remove(guildName);
        }
    }

    public static bool SetGuildNotice(string guildName, string requester, string notice)
    {
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanNotify(requester) &&
            notice.Length <= NoticeMaxLength)
        {
            guild.notice = notice;
            BroadcastChanges(guild);
            Debug.Log(requester + " changed guild notice to: " + guild.notice);
            return true;
        }
        return false;
    }

    public static void KickFromGuild(string guildName, string requester, string member)
    {
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanKick(requester, member))
        {
            LeaveGuild(guildName, member);
            Debug.Log(requester + " kicked " + member + " from guild: " + guildName);
        }
    }

    public static bool AddToGuild(string guildName, string requester, string member, int memberLevel)
    {
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanInvite(requester, member))
        {
            Array.Resize(ref guild.members, guild.members.Length + 1);
            guild.members[guild.members.Length - 1] = new GuildMember(member, memberLevel, true, GuildRank.Member);

            BroadcastChanges(guild);
            Debug.Log(requester + " added " + member + " to guild: " + guildName);
            return true;
        }
        return false;
    }

    public static void SetGuildOnline(string guildName, string member, bool online)
    {
        if (guilds.TryGetValue(guildName, out Guild guild))
        {
            int index = guild.GetMemberIndex(member);
            if (index != -1 && guild.members[index].online != online)
            {
                guild.members[index].online = online;
                BroadcastChanges(guild);
            }
        }
    }

    public static void PromoteMember(string guildName, string requester, string member)
    {
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanPromote(requester, member))
        {
            int index = guild.GetMemberIndex(member);
            if (index != -1)
            {
                ++guild.members[index].rank; // in-place; no temp array
                BroadcastChanges(guild);
                Debug.Log(requester + " promoted " + member + " in guild: " + guildName);
            }
        }
    }

    public static void DemoteMember(string guildName, string requester, string member)
    {
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanDemote(requester, member))
        {
            int index = guild.GetMemberIndex(member);
            if (index != -1)
            {
                --guild.members[index].rank; // in-place; no temp array
                BroadcastChanges(guild);
                Debug.Log(requester + " demoted " + member + " in guild: " + guildName);
            }
        }
    }
}
