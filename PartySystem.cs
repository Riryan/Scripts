
























using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PartySystem
{
    static Dictionary<int, Party> parties = new Dictionary<int, Party>();

    
    
    static int nextPartyId = 1;

    
    static void BroadcastTo(string member, Party party)
    {
        if (Player.onlinePlayers.TryGetValue(member, out Player player))
            player.party.party = party;
    }

    
    static void BroadcastChanges(Party party)
    {
        foreach (string member in party.members)
            BroadcastTo(member, party);

        parties[party.partyId] = party;
    }

    
    public static bool PartyExists(int partyId)
    {
        return parties.ContainsKey(partyId);
    }

    
    
    public static void FormParty(string creator, string firstMember)
    {
        
        int partyId = nextPartyId++;
        Party party = new Party(partyId, creator, firstMember);

        
        BroadcastChanges(party);
        Debug.Log(creator + " formed a new party with " + firstMember);
    }

    public static void AddToParty(int partyId, string member)
    {
        
        Party party;
        if (parties.TryGetValue(partyId, out party) && !party.IsFull())
        {
            
            Array.Resize(ref party.members, party.members.Length + 1);
            party.members[party.members.Length - 1] = member;

            
            BroadcastChanges(party);
            Debug.Log(member + " was added to party " + partyId);
        }
    }

    public static void KickFromParty(int partyId, string requester, string member)
    {
        
        Party party;
        if (parties.TryGetValue(partyId, out party))
        {
            
            if (party.master == requester && party.Contains(member) && requester != member)
            {
                
                LeaveParty(partyId, member);
            }
        }
    }

    public static void LeaveParty(int partyId, string member)
    {
        
        Party party;
        if (parties.TryGetValue(partyId, out party))
        {
            
            if (party.master != member && party.Contains(member))
            {
                
                party.members = party.members.Where(name => name != member).ToArray();

                
                if (party.members.Length > 1)
                {
                    
                    BroadcastChanges(party);
                    BroadcastTo(member, Party.Empty); 
                }
                
                else
                {
                    
                    BroadcastTo(party.members[0], Party.Empty); 
                    BroadcastTo(member, Party.Empty); 
                    parties.Remove(partyId);
                }

                Debug.Log(member + " left the party");
            }
        }
    }

    public static void DismissParty(int partyId, string requester)
    {
        
        Party party;
        if (parties.TryGetValue(partyId, out party))
        {
            
            if (party.master == requester)
            {
                
                foreach (string member in party.members)
                    BroadcastTo(member, Party.Empty);

                
                parties.Remove(partyId);
                Debug.Log(requester + " dismissed the party");
            }
        }
    }

    public static void SetPartyExperienceShare(int partyId, string requester, bool value)
    {
        
        Party party;
        if (parties.TryGetValue(partyId, out party) && party.master == requester)
        {
            
            party.shareExperience = value;

            
            BroadcastChanges(party);
        }
    }

    public static void SetPartyGoldShare(int partyId, string requester, bool value)
    {
        
        Party party;
        if (parties.TryGetValue(partyId, out party) && party.master == requester)
        {
            
            party.shareGold = value;

            
            BroadcastChanges(party);
        }
    }
}
