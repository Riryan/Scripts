using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class PlayerParty : NetworkBehaviour
{
    [Header("Components")]
    public Player player;

    
    
    [Header("Party")]
    [SyncVar, HideInInspector] public Party party; 
    [SyncVar, HideInInspector] public string inviteFrom = "";
    public float inviteWaitSeconds = 3;

    
    List<Player> proximity = new List<Player>();

    void OnDestroy()
    {
        
        if (!isServer && !isClient) return;

        if (isServer)
        {
            
            if (InParty())
            {
                
                if (party.master == name)
                    Dismiss();
                else
                    Leave();
            }
        }
    }

    
    public bool InParty()
    {
        
        return party.partyId > 0;
    }

    
    public List<Player> GetMembersInProximity()
    {
        
        proximity.Clear();

        if (InParty())
        {
            
            foreach (NetworkConnection conn in netIdentity.observers.Values)
            {
                Player observer = conn.identity.GetComponent<Player>();
                if (party.Contains(observer.name))
                    proximity.Add(observer);
            }
        }
        return proximity;
    }

    
    
    [Command]
    public void CmdInvite(string otherName)
    {
        
        if (otherName != name &&
            Player.onlinePlayers.TryGetValue(otherName, out Player other) &&
            NetworkTime.time >= player.nextRiskyActionTime)
        {
            
            
            if ((!InParty() || !party.IsFull()) && !other.party.InParty())
            {
                
                other.party.inviteFrom = name;
                Debug.Log(name + " invited " + other.name + " to party");
            }
        }

        
        
        
        player.nextRiskyActionTime = NetworkTime.time + inviteWaitSeconds;
    }

    [Command]
    public void CmdAcceptInvite()
    {
        
        
        if (!InParty() && inviteFrom != "" &&
            Player.onlinePlayers.TryGetValue(inviteFrom, out Player sender))
        {
            
            if (sender.party.InParty())
                PartySystem.AddToParty(sender.party.party.partyId, name);
            
            else
                PartySystem.FormParty(sender.name, name);
        }

        
        inviteFrom = "";
    }

    [Command]
    public void CmdDeclineInvite()
    {
        inviteFrom = "";
    }

    [Command]
    public void CmdKick(string member)
    {
        
        PartySystem.KickFromParty(party.partyId, name, member);
    }

    
    public void Leave()
    {
        
        PartySystem.LeaveParty(party.partyId, name);
    }
    [Command]
    public void CmdLeave() { Leave(); }

    
    public void Dismiss()
    {
        
        PartySystem.DismissParty(party.partyId, name);
    }
    [Command]
    public void CmdDismiss() { Dismiss(); }

    [Command]
    public void CmdSetExperienceShare(bool value)
    {
        
        PartySystem.SetPartyExperienceShare(party.partyId, name, value);
    }

    [Command]
    public void CmdSetGoldShare(bool value)
    {
        
        PartySystem.SetPartyGoldShare(party.partyId, name, value);
    }

    
    public static long CalculateExperienceShare(long total, int memberCount, float bonusPercentagePerMember, int memberLevel, int killedLevel)
    {
        
        float bonusPercentage = (memberCount-1) * bonusPercentagePerMember;

        
        
        
        long share = (long)Mathf.Ceil(total / (float)memberCount);

        
        
        
        long balanced = Experience.BalanceExperienceReward(share, memberLevel, killedLevel);
        long bonus = Convert.ToInt64(balanced * bonusPercentage);

        return balanced + bonus;
    }

    
    [Server]
    public void OnKilledEnemy(Entity victim)
    {
        
        if (InParty())
        {
            List<Player> closeMembers = GetMembersInProximity();

            
            
            foreach (Player member in closeMembers)
                if (member != player)
                    member.quests.OnKilledEnemy(victim);

            
            
            
            
            
            
            
            
            if (victim is Monster monster && party.shareExperience)
            {
                foreach (Player member in closeMembers)
                {
                    member.experience.current += CalculateExperienceShare(
                        monster.rewardExperience,
                        closeMembers.Count,
                        Party.BonusExperiencePerMember,
                        member.level.current,
                        victim.level.current
                    );
                    ((PlayerSkills)member.skills).skillExperience += CalculateExperienceShare(
                        monster.rewardSkillExperience,
                        closeMembers.Count,
                        Party.BonusExperiencePerMember,
                        member.level.current,
                        victim.level.current
                    );
                }
            }
        }
    }
}
