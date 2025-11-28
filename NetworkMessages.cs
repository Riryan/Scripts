using System.Collections.Generic;
using System.Linq;
using Mirror;

public partial struct LoginMsg : NetworkMessage
{
    public string account;
    public string password;
    public string version;
}

public partial struct CharacterCreateMsg : NetworkMessage
{
    public string name;
    public int classIndex;
    public bool gameMaster; 
}

public partial struct CharacterSelectMsg : NetworkMessage
{
    public int index;
}

public partial struct CharacterDeleteMsg : NetworkMessage
{
    public int index;
}


public partial struct ErrorMsg : NetworkMessage
{
    public string text;
    public bool causesDisconnect;
}

public partial struct LoginSuccessMsg : NetworkMessage
{
}

public partial struct CharactersAvailableMsg : NetworkMessage
{
    public partial struct CharacterPreview
    {
        public string name;
        public string className; 
        public bool isGameMaster; 
        public ItemSlot[] equipment;
    }
    public CharacterPreview[] characters;


    public void Load(List<Player> players)
    {
        characters = new CharacterPreview[players.Count];
        for (int i = 0; i < players.Count; ++i)
        {
            Player player = players[i];
            characters[i] = new CharacterPreview
            {
                name = player.name,
                className = player.className,
                isGameMaster = player.isGameMaster,
                equipment = player.equipment.slots.ToArray()
                //equipment = System.Array.Empty<ItemSlot>()
            };
        }
        Utils.InvokeMany(typeof(CharactersAvailableMsg), this, "Load_", players);
    }
}
    // Server → Client: tells client to connect to another zone
    public struct ZoneTransferMsg : NetworkMessage
    {
        public string targetZoneId;     // e.g. "DungeonA_1"
        public string targetHost;       // IP or DNS (e.g. "192.168.1.5" or "worldb.mmo.net")
        public ushort targetPort;       // e.g. 7778
        public string transferToken;    // short-lived HMAC token
        public string targetPortalId;   // where to appear on the new server
    }

    // Client → Server: used instead of LoginMsg when connecting after a portal hop
    public struct ZoneTransferLoginMsg : NetworkMessage
    {
        public string account;          // (optional) for validation / logs
        public string characterName;    // the character being transferred
        public string transferToken;    // same token issued by old server
        public string targetPortalId;   // where to appear on the new server
    }