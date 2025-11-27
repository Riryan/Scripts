

public struct Party
{
    
    public static Party Empty = new Party();

    
    public int partyId;
    public string[] members; 
    public bool shareExperience;
    public bool shareGold;

    
    public string master => members != null && members.Length > 0 ? members[0] : "";

    
    public static int Capacity = 8;
    public static float BonusExperiencePerMember = 0.1f;

    
    public Party(int partyId, string master, string firstMember)
    {
        
        this.partyId = partyId;
        members = new string[]{master, firstMember};
        shareExperience = false;
        shareGold = false;
    }

    public bool Contains(string memberName)
    {
        if (members != null)
            foreach (string member in members)
                if (member == memberName)
                    return true;
        return false;
    }

    public bool IsFull()
    {
        return members != null && members.Length == Capacity;
    }
}