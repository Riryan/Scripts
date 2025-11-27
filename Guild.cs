using System;

public enum GuildRank : byte
{
    Member = 0,
    Vice   = 1,
    Master = 2
}

[Serializable]
public partial struct GuildMember
{
    public string name;
    public int level;
    public bool online;
    public GuildRank rank;

    public GuildMember(string name, int level, bool online, GuildRank rank)
    {
        this.name   = name;
        this.level  = level;
        this.online = online;
        this.rank   = rank;
    }
}

[Serializable]
public struct Guild
{
    public static Guild Empty = new Guild();

    public string name;
    public string notice;
    public GuildMember[] members;

    // Non-allocating master lookup; returns empty string if not found
    public string master
    {
        get
        {
            if (members != null)
            {
                for (int i = 0; i < members.Length; ++i)
                {
                    if (members[i].rank == GuildRank.Master)
                        return members[i].name ?? string.Empty;
                }
            }
            return string.Empty;
        }
    }

    public Guild(string name, string firstMember, int firstMemberLevel)
    {
        this.name = name;
        this.notice = string.Empty;
        var member = new GuildMember(firstMember, firstMemberLevel, true, GuildRank.Master);
        this.members = new GuildMember[] { member };
    }

    public int GetMemberIndex(string memberName)
    {
        if (members != null && !string.IsNullOrEmpty(memberName))
        {
            for (int i = 0; i < members.Length; ++i)
            {
                if (string.Equals(members[i].name, memberName, StringComparison.Ordinal))
                    return i;
            }
        }
        return -1;
    }

    public bool IsMember(string memberName) => GetMemberIndex(memberName) != -1;

    // === Permissions ===

    // New name
    public bool CanNotice(string requesterName)
    {
        int index = GetMemberIndex(requesterName);
        return index != -1 && members[index].rank >= GuildSystem.NotifyMinRank;
    }

    // Back-compat alias for older callers (e.g., GuildSystem)
    public bool CanNotify(string requesterName) => CanNotice(requesterName);

    public bool CanKick(string requesterName, string targetName)
    {
        if (members == null) return false;

        int requesterIndex = GetMemberIndex(requesterName);
        int targetIndex    = GetMemberIndex(targetName);
        if (requesterIndex == -1 || targetIndex == -1) return false;

        GuildMember requester = members[requesterIndex];
        GuildMember target    = members[targetIndex];

        if (requesterName == targetName) return false;
        if (requester.rank < GuildSystem.KickMinRank) return false;
        if (requester.rank <= target.rank) return false;

        // never kick the Master via this rule (transfer first)
        if (target.rank == GuildRank.Master) return false;

        return true;
    }

    public bool CanPromote(string requesterName, string targetName)
    {
        if (members == null) return false;

        int requesterIndex = GetMemberIndex(requesterName);
        int targetIndex    = GetMemberIndex(targetName);
        if (requesterIndex == -1 || targetIndex == -1) return false;

        GuildMember requester = members[requesterIndex];
        GuildMember target    = members[targetIndex];

        if (requesterName == targetName) return false;
        if (requester.rank < GuildSystem.PromoteMinRank) return false;
        if (target.rank >= GuildRank.Master) return false; // already max rank
        // after promotion, target must still be below requester
        if ((byte)(target.rank + 1) >= (byte)requester.rank) return false;

        return true;
    }

    public bool CanDemote(string requesterName, string targetName)
    {
        if (members == null) return false;

        int requesterIndex = GetMemberIndex(requesterName);
        int targetIndex    = GetMemberIndex(targetName);
        if (requesterIndex == -1 || targetIndex == -1) return false;

        GuildMember requester = members[requesterIndex];
        GuildMember target    = members[targetIndex];

        if (requesterName == targetName) return false;
        if (requester.rank < GuildSystem.PromoteMinRank) return false;
        if (target.rank <= GuildRank.Member) return false;
        if (requester.rank <= target.rank) return false;

        return true;
    }

    // Original call sites expect (requester, invitedMember)
    public bool CanInvite(string requesterName, string memberName)
    {
        if (members == null) return false;
        int requesterIndex = GetMemberIndex(requesterName);
        if (requesterIndex == -1) return false;
        if (members.Length >= GuildSystem.MaxMembers) return false;
        if (GetMemberIndex(memberName) != -1) return false; // already in guild
        return members[requesterIndex].rank >= GuildSystem.InviteMinRank;
    }

    // Keep the one-arg variant for UI convenience if needed
    public bool CanInvite(string requesterName)
    {
        if (members == null) return false;
        int requesterIndex = GetMemberIndex(requesterName);
        if (requesterIndex == -1) return false;
        if (members.Length >= GuildSystem.MaxMembers) return false;
        return members[requesterIndex].rank >= GuildSystem.InviteMinRank;
    }

    public bool CanLeave(string requesterName)
    {
        if (members == null) return false;
        int me = GetMemberIndex(requesterName);
        if (me == -1) return false;
        GuildMember self = members[me];
        // leave if not Master, or Master and sole member
        return self.rank != GuildRank.Master || members.Length == 1;
    }

    public bool CanTerminate(string requesterName)
    {
        if (members == null) return false;
        if (members.Length != 1) return false;
        return string.Equals(members[0].name, requesterName, StringComparison.Ordinal) &&
               members[0].rank == GuildRank.Master;
    }
}
