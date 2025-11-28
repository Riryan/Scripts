using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using UnityEngine.Rendering; // needed for GraphicsDeviceType (server/headless builds)
#if UNITY_EDITOR
using UnityEditor;
#endif
using GFFAddons;

public enum NetworkState { Offline, Handshake, Lobby, World, ZoneTransfer }

[Serializable] public class UnityEventCharactersAvailableMsg : UnityEvent<CharactersAvailableMsg> {}
[Serializable] public class UnityEventCharacterCreateMsgPlayer : UnityEvent<CharacterCreateMsg, Player> {}
[Serializable] public class UnityEventStringGameObjectNetworkConnectionCharacterSelectMsg : UnityEvent<string, GameObject, NetworkConnection, CharacterSelectMsg> {}
[Serializable] public class UnityEventCharacterDeleteMsg : UnityEvent<CharacterDeleteMsg> {}
[Serializable] public class UnityEventNetworkConnection : UnityEvent<NetworkConnection> { }
public struct CharacterSwitchRequestMsg : NetworkMessage {}

[RequireComponent(typeof(Database))]
[DisallowMultipleComponent]
public partial class NetworkManagerMMO : NetworkManager
{
    public NetworkState state = NetworkState.Offline;

    public Dictionary<NetworkConnection, string> lobby = new Dictionary<NetworkConnection, string>();

    [Header("UI")]
    public UIPopup uiPopup;

    // ───── ZONE TRANSFER (CLIENT) ─────────────────────────────────────────────
    [Header("Zone Transfer (Client)")]
    [Tooltip("Filled when the server instructs this client to move to another zone/server.")]
    [HideInInspector] public string pendingZoneHost = "";
    [HideInInspector] public ushort pendingZonePort;
    [HideInInspector] public string pendingZoneId = "";
    [HideInInspector] public string pendingZonePortalId = "";
    // ──────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class ServerInfo
    {
        public string name;
        public string ip;
    }
    public List<ServerInfo> serverList = new List<ServerInfo>() {
        new ServerInfo{name="Local", ip="localhost"}
    };

    [Header("Logout")]
    [Tooltip("Players shouldn't be able to log out instantly to flee combat. There should be a delay.")]
    public float combatLogoutDelay = 30;
    [HideInInspector] public bool changingCharacters = false;
    [Header("Character Selection")]
    public int selection = -1;
    public Transform[] selectionLocations;
    public Transform selectionCameraLocation;
    [HideInInspector] public List<Player> playerClasses = new List<Player>();

    [Header("Database")]
    public int characterLimit = 4;
    public int characterNameMaxLength = 16;
    public float saveInterval = 60f;

    [Header("Events")]
    public UnityEvent onStartClient;
    public UnityEvent onStopClient;
    public UnityEvent onStartServer;
    public UnityEvent onStopServer;
    public UnityEventNetworkConnection onClientConnect;
    public UnityEventNetworkConnection onServerConnect;
    public UnityEventCharactersAvailableMsg onClientCharactersAvailable;
    public UnityEventCharacterCreateMsgPlayer onServerCharacterCreate;
    public UnityEventStringGameObjectNetworkConnectionCharacterSelectMsg onServerCharacterSelect;
    public UnityEventCharacterDeleteMsg onServerCharacterDelete;
    public UnityEventNetworkConnection onClientDisconnect;
    public UnityEventNetworkConnection onServerDisconnect;

    [HideInInspector] public CharactersAvailableMsg charactersAvailableMsg;

    [Header("Client Rendering")]
    [Tooltip("If true, the options below will apply to client builds (including Host client).")]
    public bool clientFpsOverrideEnabled = true;

    public enum ClientFpsMode { Unlimited, VSync, TargetFps }
    [Tooltip("How to control client framerate.")]
    public ClientFpsMode clientFpsMode = ClientFpsMode.VSync;

    [Tooltip("Used when ClientFpsMode=TargetFps.")]
    public int clientTargetFps = 60;

    [Tooltip("Used when ClientFpsMode=VSync. 0=Off, 1=Every V-Blank, 2=Every Second V-Blank.")]
    public int clientVSyncCount = 1;

    [Tooltip("Allow overriding client FPS settings via command line (e.g., -client.maxfps=144, -client.vsync=0).")]
    public bool clientAllowCommandLineOverride = true;

    static readonly Regex allowedNameRegex = new Regex(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    public virtual bool IsAllowedCharacterName(string characterName)
    {
        return characterName.Length <= characterNameMaxLength &&
               allowedNameRegex.IsMatch(characterName);
    }

    public static Transform GetNearestStartPosition(Vector3 from) =>
        Utils.GetNearestTransform(startPositions, from);

    public List<Player> FindPlayerClasses()
    {
        List<Player> classes = new List<Player>();
        foreach (GameObject prefab in spawnPrefabs)
        {
            Player player = prefab.GetComponent<Player>();
            if (player != null)
                classes.Add(player);
        }
        return classes;
    }

    public override void Awake()
    {
        base.Awake();
        playerClasses = FindPlayerClasses();
    }

    public override void Update()
    {
        base.Update();
        if (NetworkClient.localPlayer != null)
            state = NetworkState.World;
    } 

    public void ServerSendError(NetworkConnection conn, string error, bool disconnect)
    {
        conn.Send(new ErrorMsg { text = error, causesDisconnect = disconnect });
    }

    void OnClientError(ErrorMsg message)
    {
        Debug.Log("OnClientError: " + message.text);
#if !UNITY_SERVER || UNITY_EDITOR
        if (uiPopup != null) uiPopup.Show(message.text);
#endif
        if (message.causesDisconnect)
        {
            NetworkClient.connection.Disconnect();
            if (NetworkServer.active) StopHost();
        }
    } 

    // ───── CLIENT ZONE TRANSFER HANDLER ──────────────────────────────────────
void OnClientZoneTransfer(ZoneTransferMsg msg)
{
    Debug.Log($"Zone transfer requested to {msg.targetZoneId} @ {msg.targetHost}:{msg.targetPort} portal={msg.targetPortalId}");

    // store target info for reconnect
    pendingZoneId = msg.targetZoneId;
    pendingZonePortalId = msg.targetPortalId;
    pendingZoneHost = string.IsNullOrWhiteSpace(msg.targetHost) ? networkAddress : msg.targetHost;
    pendingZonePort = msg.targetPort;

    // prepare authenticator for token-based login on next connection
    if (authenticator is NetworkAuthenticatorMMO auth)
    {
        auth.pendingTransferAccount = auth.loginAccount;

        Player localPlayer = NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.GetComponent<Player>()
            : null;

        auth.pendingTransferCharacter = localPlayer != null ? localPlayer.name : string.Empty;
        auth.pendingTransferToken = msg.transferToken;
        auth.pendingTransferPortalId  = msg.targetPortalId; 
    }

    // mark that we are in a planned zone transfer. actual reconnect will
    // be kicked off from OnClientDisconnect when the server drops us.
    state = NetworkState.ZoneTransfer;
}

IEnumerator ClientZoneTransferReconnect()
{
    // let Mirror fully tear down the old client connection this frame
    yield return null;

    if (!string.IsNullOrWhiteSpace(pendingZoneHost))
        networkAddress = pendingZoneHost;

    Debug.Log($"ZoneTransfer: connecting to {pendingZoneId} @ {networkAddress}:{pendingZonePort}");

    // NOTE: if/when you start using per-zone ports, you’ll also
    // need to push pendingZonePort into your transport here.
    StartClient();
}


    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<ErrorMsg>(OnClientError, false);
        NetworkClient.RegisterHandler<CharactersAvailableMsg>(OnClientCharactersAvailable);
        // zone transfer handler
        NetworkClient.RegisterHandler<ZoneTransferMsg>(OnClientZoneTransfer, false);
        onStartClient.Invoke();

#if !UNITY_SERVER || UNITY_EDITOR
        ApplyClientFpsPolicy();
#endif
    } 

    public override void OnStartServer()
    {
        Debug.Log("[OnStartServer] Registering character handlers.");
        Database.singleton.Connect();
        NetworkServer.RegisterHandler<CharacterCreateMsg>(OnServerCharacterCreate);
        NetworkServer.RegisterHandler<CharacterSelectMsg>(OnServerCharacterSelect);
        NetworkServer.RegisterHandler<CharacterDeleteMsg>(OnServerCharacterDelete);
        NetworkServer.RegisterHandler<CharacterSwitchRequestMsg>(OnServerCharacterSwitchRequest);
        InvokeRepeating(nameof(SavePlayers), saveInterval, saveInterval);
        onStartServer.Invoke();
    } 

    public override void OnStopClient()
    {
        onStopClient.Invoke();
    } 

    public override void OnStopServer()
    {
        CancelInvoke(nameof(SavePlayers));
        onStopServer.Invoke();
    } 

    public override void OnClientConnect()
    {
        onClientConnect.Invoke(NetworkClient.connection);
    } 

public override void OnServerConnect(NetworkConnectionToClient conn)
{
    // two valid cases:
    //  - normal login (in lobby) -> send character list
    //  - zone transfer login (already has identity) -> skip list
    if (lobby.TryGetValue(conn, out string account))
    {
        // normal account->lobby login: send character list
        conn.Send(MakeCharactersAvailableMessage(account));
        onServerConnect.Invoke(conn);
    }
    else if (conn.identity != null)
    {
        // zone transfer: already spawned in world; do NOT send CharactersAvailableMsg
        Debug.Log($"Zone-transfer connection {conn.connectionId} authenticated; skipping character list.");
        onServerConnect.Invoke(conn);
    }
    else
    {
        Debug.LogWarning($"OnServerConnect: connection {conn} not found in lobby (no account, no identity).");
    }
}


    public override void OnClientSceneChanged() { }

    CharactersAvailableMsg MakeCharactersAvailableMessage(string account)
    {
        List<Player> characters = new List<Player>();
        foreach (string characterName in Database.singleton.CharactersForAccount(account))
        {
            GameObject player = Database.singleton.CharacterLoad(characterName, playerClasses, true);
            characters.Add(player.GetComponent<Player>());
        }
        CharactersAvailableMsg message = new CharactersAvailableMsg();
        message.Load(characters);
        characters.ForEach(player => Destroy(player.gameObject));
        return message;
    }
    void OnServerCharacterSwitchRequest(NetworkConnectionToClient conn, CharacterSwitchRequestMsg _)
    {
        // if currently in-world, enforce the same combat/logout gate you already use
        if (conn.identity != null)
        {
            Player player = conn.identity.GetComponent<Player>();
            double remaining = player.remainingLogoutTime;

            if (remaining > 0)
            {
                // non-disconnecting error; client UI will show it
                ServerSendError(conn, $"You can change characters in {Mathf.CeilToInt((float)remaining)}s", false);
                return;
            }

            // save before we drop the player object
            Database.singleton.CharacterSave(player, false);
            Debug.Log("saved before character switch: " + player.name);

            string account = player.account;

            // remove the player object but keep the connection alive
            // Mirror 50+:
            NetworkServer.RemovePlayerForConnection(conn, false);

            // move connection back to lobby and send fresh character list
            lobby[conn] = account;
            conn.Send(MakeCharactersAvailableMessage(account));
            return;
        }

        // already lobby-side? just resend the list if we know the account
        if (lobby.TryGetValue(conn, out string lobbyAccount))
        {
            conn.Send(MakeCharactersAvailableMessage(lobbyAccount));
        }
        else
        {
            ServerSendError(conn, "Not in world.", false);
        }
    }

void LoadPreview(GameObject prefab,
                 Transform location,
                 int selectionIndex,
                 CharactersAvailableMsg.CharacterPreview character)
{
    // --- 0) Spawn & basic setup ---
    GameObject preview = Instantiate(prefab.gameObject, location.position, location.rotation);
    preview.transform.SetParent(location, true);

    Player player = preview.GetComponent<Player>();
    player.name         = character.name;
    player.isGameMaster = character.isGameMaster;

    PlayerEquipment equip = (PlayerEquipment)player.equipment;

    // Debug: see what the client actually received
    int receivedCount = character.equipment != null ? character.equipment.Length : -1;
    //Debug.Log($"[Preview Client] {character.name} received equipment length: {receivedCount}");

    // --- 1) Apply base customization (body/face/race/etc.) ---
    // Uses CharacterCreation Partial data: race, gender, customization string, scale
    SetCustomization(character, player);

    // --- 2) Prepare equipment slots on the preview player ---
    player.equipment.slots.Clear();
    int slotCount = equip.slotInfo.Length;
    for (int i = 0; i < slotCount; ++i)
        player.equipment.slots.Add(new ItemSlot());

    if (character.equipment == null)
    {
        // nothing to preview
        goto PreviewSelectable;
    }

    int n = Mathf.Min(slotCount, character.equipment.Length);

    // --- 3) Rebuild items from their ScriptableItem names & apply them ---
    for (int i = 0; i < n; ++i)
    {
        ItemSlot src = character.equipment[i];

        if (src.amount <= 0)
            continue;

        string itemName = src.item.name;

        if (string.IsNullOrWhiteSpace(itemName))
            continue;

        // look up ScriptableItem on the client
        if (!ScriptableItem.All.TryGetValue(itemName.GetStableHashCode(), out ScriptableItem itemData))
        {
            //Debug.LogWarning($"[Preview Client] {character.name} slot {i}: unknown item '{itemName}'");
            continue;
        }

        // rebuild an Item instance from the ScriptableItem, copying runtime state
        Item item = new Item(itemData);
        item.durability         = src.item.durability;
        item.summonedHealth     = src.item.summonedHealth;
        item.summonedLevel      = src.item.summonedLevel;
        item.summonedExperience = src.item.summonedExperience;

        ItemSlot rebuilt = new ItemSlot(item, src.amount);
        player.equipment.slots[i] = rebuilt;

#if !UNITY_SERVER
        // --- 4) Drive PlayerCustomization with gear (same idea as OnSlotEquipped) ---
        PlayerCustomization cust = player.customization;
        if (cust != null && itemData is EquipmentItem eq)
        {
            cust.OnSlotEquipped(
                i,
                eq.targetCustomizationType,
                eq.overrideCustomizationIndex,
                eq.hideWhileEquipped
            );
        }
#endif

        // --- 5) Spawn or update the mesh / model for this slot ---
        equip.RefreshLocation(i);
    }

PreviewSelectable:
    // --- 6) Make preview selectable in UI ---
    var selectable = preview.AddComponent<SelectableCharacter>();
    selectable.index = selectionIndex;
}


    public void ClearPreviews()
    {
        selection = -1;
        foreach (Transform location in selectionLocations)
            if (location.childCount > 0)
                Destroy(location.GetChild(0).gameObject);
    } 

    void OnClientCharactersAvailable(CharactersAvailableMsg message)
    {
        charactersAvailableMsg = message;
        //Debug.Log("characters available:" + charactersAvailableMsg.characters.Length);

        state = NetworkState.Lobby;

        ClearPreviews();

        for (int i = 0; i < charactersAvailableMsg.characters.Length; ++i)
        {
            CharactersAvailableMsg.CharacterPreview character = charactersAvailableMsg.characters[i];
            Player prefab = playerClasses.Find(p => p.name == character.className);
            if (prefab != null)
                LoadPreview(prefab.gameObject, selectionLocations[i], i, character);
            else
                Debug.LogWarning("Character Selection: no prefab found for class " + character.className);
        }

#if !UNITY_SERVER || UNITY_EDITOR
        if (Camera.main != null)
        {
            Camera.main.transform.position = selectionCameraLocation.position;
            Camera.main.transform.rotation = selectionCameraLocation.rotation;
        }
#endif
        onClientCharactersAvailable.Invoke(charactersAvailableMsg);
    } 

    public Transform GetStartPositionFor(string className)
    {
        foreach (Transform startPosition in startPositions)
        {
            NetworkStartPositionForClass spawn = startPosition.GetComponent<NetworkStartPositionForClass>();
            if (spawn != null &&
                spawn.playerPrefab != null &&
                spawn.playerPrefab.name == className)
                return spawn.transform;
        }
        return GetStartPosition();
    } 

Player CreateCharacter(GameObject classPrefab, string characterName, string account, bool gameMaster)
{
    Player player = Instantiate(classPrefab).GetComponent<Player>();
    player.name = characterName;
    player.account = account;
    player.className = classPrefab.name;
    player.transform.position = GetStartPositionFor(player.className).position;

    // init inventory
    for (int i = 0; i < player.inventory.size; ++i)
    {
        player.inventory.slots.Add(i < player.inventory.defaultItems.Length
            ? new ItemSlot(new Item(player.inventory.defaultItems[i].item), player.inventory.defaultItems[i].amount)
            : new ItemSlot());
    }

    // init equipment
    for (int i = 0; i < ((PlayerEquipment)player.equipment).slotInfo.Length; ++i)
    {
        EquipmentInfo info = ((PlayerEquipment)player.equipment).slotInfo[i];
        player.equipment.slots.Add(info.defaultItem.item != null
            ? new ItemSlot(new Item(info.defaultItem.item), info.defaultItem.amount)
            : new ItemSlot());
    }

    // init warehouse slots for new character
    player.EnsureWarehouseInitialized();

    player.health.current = player.health.max;
    player.mana.current = player.mana.max;
    player.isGameMaster = gameMaster;
    return player;
}


    public override void OnServerAddPlayer(NetworkConnectionToClient conn) { Debug.LogWarning("Use the CharacterSelectMsg instead"); } // ref. :contentReference[oaicite:21]{index=21}

    void OnServerCharacterCreate(NetworkConnection conn, CharacterCreateMsg message)
    {
        if (lobby.ContainsKey(conn))
        {
            if (IsAllowedCharacterName(message.name))
            {
                string account = lobby[conn];
                if (!Database.singleton.CharacterExists(message.name))
                {
                    if (Database.singleton.CharactersForAccount(account).Count < characterLimit)
                    {
                        if (0 <= message.classIndex && message.classIndex < playerClasses.Count)
                        {
                            if (message.gameMaster == false || conn == NetworkServer.localConnection)
                            {
                                Player player = CreateCharacter(playerClasses[message.classIndex].gameObject, message.name, account, message.gameMaster);
                                onServerCharacterCreate.Invoke(message, player);
                                Database.singleton.CharacterSave(player, false);
                                Destroy(player.gameObject);
                                conn.Send(MakeCharactersAvailableMessage(account));
                            }
                            else ServerSendError(conn, "insufficient permissions", false);
                        }
                        else ServerSendError(conn, "character invalid class", false);
                    }
                    else ServerSendError(conn, "character limit reached", false);
                }
                else ServerSendError(conn, "name already exists", false);
            }
            else ServerSendError(conn, "character name not allowed", false);
        }
        else ServerSendError(conn, "CharacterCreate: not in lobby", true);
    } 

void OnServerCharacterSelect(NetworkConnectionToClient conn, CharacterSelectMsg message)
{

    // connection must be in lobby
    if (!lobby.ContainsKey(conn))
    {
        ServerSendError(conn, "CharacterSelect: not in lobby", true);
        return;
    }

    string account = lobby[conn];
    List<string> characters = Database.singleton.CharactersForAccount(account);

    // index sanity check
    if (message.index < 0 || message.index >= characters.Count)
    {
        ServerSendError(conn, "invalid character index", false);
        return;
    }

    string characterName = characters[message.index];

    try
    {
        Debug.Log($"[OnServerCharacterSelect] account='{account}' index={message.index} name='{characterName}'");

        GameObject go = Database.singleton.CharacterLoad(characterName, playerClasses, false);
        if (go == null)
        {
            Debug.LogError($"OnServerCharacterSelect: CharacterLoad returned null for '{characterName}' (account '{account}').");
            ServerSendError(conn, "CharacterLoad failed.", true);
            conn.Disconnect();
            return;
        }

        NetworkServer.AddPlayerForConnection(conn, go);
        onServerCharacterSelect.Invoke(account, go, conn, message);
        lobby.Remove(conn);

        
    }
    catch (Exception ex)
    {
        Debug.LogError($"OnServerCharacterSelect exception for account '{account}', character '{characterName}': {ex}");
        ServerSendError(conn, "CharacterSelect failed server-side. See server logs.", true);
        conn.Disconnect();
    }
    
}

    void OnServerCharacterDelete(NetworkConnection conn, CharacterDeleteMsg message)
    {
        if (lobby.ContainsKey(conn))
        {
            string account = lobby[conn];
            List<string> characters = Database.singleton.CharactersForAccount(account);
            if (0 <= message.index && message.index < characters.Count)
            {
                Debug.Log("delete character: " + characters[message.index]);
                Database.singleton.CharacterDelete(characters[message.index]);
                onServerCharacterDelete.Invoke(message);
                conn.Send(MakeCharactersAvailableMessage(account));
            }
            else
            {
                Debug.Log("invalid character index: " + account + " " + message.index);
                ServerSendError(conn, "invalid character index", false);
            }
        }
        else
        {
            //Debug.Log("CharacterDelete: not in lobby: " + conn);
            ServerSendError(conn, "CharacterDelete: not in lobby", true);
        }
    } 
    // ───── ZONE TRANSFER (SERVER DEBUG) ──────────────────────────────────────
public void ServerDebugSendZoneTransfer(NetworkConnectionToClient conn, string targetPortalId = "")
{
    if (!NetworkServer.active)
    {
        Debug.LogWarning("ServerDebugSendZoneTransfer: server not active.");
        return;
    }
    if (conn == null)
    {
        Debug.LogWarning("ServerDebugSendZoneTransfer: null connection.");
        return;
    }

    string token = Guid.NewGuid().ToString("N");

    conn.Send(new ZoneTransferMsg
    {
        targetZoneId    = "DebugZone",
        targetHost      = "",
        targetPort      = 0,
        transferToken   = token,
        targetPortalId  = targetPortalId
    });
    if (conn == NetworkServer.localConnection)
        StartCoroutine(DelayedDisconnect(conn));
    else
        StartCoroutine(DelayedDisconnect(conn));
}

IEnumerator DelayedDisconnect(NetworkConnectionToClient conn)
{
    // give Mirror/KCP one frame to flush the ZoneTransferMsg
    yield return null;

    if (conn != null && conn.connectionId != -1)
        conn.Disconnect();
}

#if UNITY_EDITOR
    [ContextMenu("DEBUG: Zone Transfer local player")]
    void Debug_ZoneTransferLocalPlayer()
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("Debug_ZoneTransferLocalPlayer: server not active.");
            return;
        }
        if (NetworkClient.localPlayer == null)
        {
            Debug.LogWarning("Debug_ZoneTransferLocalPlayer: no local player.");
            return;
        }

        // On host, the *server* connection for this player is on the NetworkIdentity.connectionToClient
        NetworkIdentity identity = NetworkClient.localPlayer.GetComponent<NetworkIdentity>();
        if (identity == null || identity.connectionToClient == null)
        {
            Debug.LogWarning("Debug_ZoneTransferLocalPlayer: local player has no server-side connectionToClient.");
            return;
        }

        NetworkConnectionToClient conn = identity.connectionToClient as NetworkConnectionToClient;
        if (conn == null)
        {
            Debug.LogWarning("Debug_ZoneTransferLocalPlayer: connectionToClient is not a NetworkConnectionToClient.");
            return;
        }

        ServerDebugSendZoneTransfer(conn);
    }
#endif


    // ──────────────────────────────────────────────────────────────────────────

    void SavePlayers()
    {
        Database.singleton.CharacterSaveMany(Player.onlinePlayers.Values);
        if (Player.onlinePlayers.Count > 0)
            Debug.Log("saved " + Player.onlinePlayers.Count + " player(s)");
    } 

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        float delay = 0;
        if (conn.identity != null)
        {
            Player player = conn.identity.GetComponent<Player>();
            delay = (float)player.remainingLogoutTime;
        }
        StartCoroutine(DoServerDisconnect(conn, delay));
    } 

    IEnumerator DoServerDisconnect(NetworkConnectionToClient conn, float delay)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        if (conn.identity != null)
        {
            Database.singleton.CharacterSave(conn.identity.GetComponent<Player>(), false);
            Debug.Log("saved:" + conn.identity.name);
        }

        onServerDisconnect.Invoke(conn);
        lobby.Remove(conn);
        base.OnServerDisconnect(conn);
    } 

public override void OnClientDisconnect()
{
    Debug.Log("OnClientDisconnect");

#if !UNITY_SERVER || UNITY_EDITOR
    Camera mainCamera = Camera.main;
    if (mainCamera != null && mainCamera.transform.parent != null)
        mainCamera.transform.SetParent(null);

    if (uiPopup != null)
    {
        if (state == NetworkState.ZoneTransfer)
        {
            uiPopup.Show("Traveling to new zone...");
        }
        else
        {
            uiPopup.Show("Disconnected.");
        }
    }
#endif

    base.OnClientDisconnect();

    if (state == NetworkState.ZoneTransfer)
    {
        // Planned hop: do NOT raise the normal disconnect event,
        // just start the reconnect to the new zone.
        StartCoroutine(ClientZoneTransferReconnect());
    }
    else
    {
        // Normal disconnect path
        state = NetworkState.Offline;
        onClientDisconnect.Invoke(NetworkClient.connection);
    }
}


    public static void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    } 
    
    public override void ConfigureHeadlessFrameRate()
    {
        if (IsHeadless())
            Application.targetFrameRate = sendRate;
    } 

#if !UNITY_SERVER || UNITY_EDITOR
    void ApplyClientFpsPolicy()
    {
        if (!clientFpsOverrideEnabled) return;

        // optional CLI overrides
        if (clientAllowCommandLineOverride)
        {
            if (TryGetArgInt("-client.maxfps", out int cliFps)) clientFpsMode = ClientFpsMode.TargetFps; // enforce target mode
            if (TryGetArgInt("-client.vsync", out int cliV)) clientVSyncCount = Mathf.Clamp(cliV, 0, 2);
            if (TryGetArgInt("-client.maxfps", out cliFps)) clientTargetFps = Mathf.Clamp(cliFps, -1, 1000);
        }

        switch (clientFpsMode)
        {
            case ClientFpsMode.Unlimited:
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1; // platform default / uncapped
                break;
            case ClientFpsMode.VSync:
                QualitySettings.vSyncCount = Mathf.Clamp(clientVSyncCount, 0, 2);
                Application.targetFrameRate = -1;
                break;
            case ClientFpsMode.TargetFps:
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = Mathf.Clamp(clientTargetFps, -1, 1000);
                break;
        }
    }

    static bool TryGetArgInt(string key, out int value)
    {
        value = 0;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; ++i)
        {
            if (args[i].StartsWith(key, StringComparison.OrdinalIgnoreCase))
            {
                string raw = args[i].Contains("=") ? args[i].Substring(args[i].IndexOf('=') + 1) :
                              i + 1 < args.Length ? args[i + 1] : null;
                if (int.TryParse(raw, out value))
                    return true;
            }
        }
        return false;
    }
#endif
    static bool IsHeadless()
    {
#if UNITY_SERVER
        return true; 
#else
        return Application.isBatchMode ||
               SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
#endif
    }


    public override void OnValidate()
    {
        base.OnValidate();

        if (!Application.isPlaying && networkAddress != "")
            networkAddress = "Use the Server List below!";
        if (selectionLocations.Length != characterLimit)
        {
            Transform[] newArray = new Transform[characterLimit];
            for (int i = 0; i < Mathf.Min(characterLimit, selectionLocations.Length); ++i)
                newArray[i] = selectionLocations[i];
            selectionLocations = newArray;
        }
    } 
}
