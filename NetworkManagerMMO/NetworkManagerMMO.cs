using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace uMMORPG
{
    public enum NetworkState { Offline, Handshake, Lobby, World }

    [Serializable] public class UnityEventCharactersAvailableMsg : UnityEvent<CharactersAvailableMsg> {}
    [Serializable] public class UnityEventCharacterCreateMsgPlayer : UnityEvent<CharacterCreateMsg, Player> {}
    [Serializable] public class UnityEventStringGameObjectNetworkConnectionCharacterSelectMsg : UnityEvent<string, GameObject, NetworkConnection, CharacterSelectMsg> {}
    [Serializable] public class UnityEventCharacterDeleteMsg : UnityEvent<CharacterDeleteMsg> {}
    [Serializable] public class UnityEventNetworkConnection : UnityEvent<NetworkConnection> {}

    [RequireComponent(typeof(Database))]
    [DisallowMultipleComponent]
    public partial class NetworkManagerMMO : NetworkManager
    {
        public NetworkState state = NetworkState.Offline;

        public Dictionary<NetworkConnection, string> lobby = new Dictionary<NetworkConnection, string>();

        [Header("UI")]
        public UIPopup uiPopup;

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
        public float combatLogoutDelay = 5;

        [Header("Character Selection")]
        public int selection = -1;
        public Transform[] selectionLocations;
        public Transform selectionCameraLocation;
        [HideInInspector] public List<Player> playerClasses = new List<Player>(); // cached in Awake

        [Header("Database")]
        public int characterLimit = 4;
        public int characterNameMaxLength = 16;
        public float saveInterval = 60f; // in seconds

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

        public virtual bool IsAllowedCharacterName(string characterName)
        {
            return characterName.Length <= characterNameMaxLength &&
                   Regex.IsMatch(characterName, @"^[a-zA-Z0-9_]+$");
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
            conn.Send(new ErrorMsg{text=error, causesDisconnect=disconnect});
        }

        void OnClientError(ErrorMsg message)
        {
            Debug.Log("OnClientError: " + message.text);

            uiPopup.Show(message.text);
            if (message.causesDisconnect)
            {
                NetworkClient.connection.Disconnect();
                if (NetworkServer.active) StopHost();
            }
        }

        public override void OnStartClient()
        {
            NetworkClient.RegisterHandler<ErrorMsg>(OnClientError, false); // allowed before auth!
            NetworkClient.RegisterHandler<CharactersAvailableMsg>(OnClientCharactersAvailable);
            onStartClient.Invoke();
        }

        public override void OnStartServer()
        {
            Database.singleton.Connect();
            NetworkServer.RegisterHandler<CharacterCreateMsg>(OnServerCharacterCreate);
            NetworkServer.RegisterHandler<CharacterSelectMsg>(OnServerCharacterSelect);
            NetworkServer.RegisterHandler<CharacterDeleteMsg>(OnServerCharacterDelete);
            InvokeRepeating(nameof(SavePlayers), saveInterval, saveInterval);

            // addon system hooks
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
            string account = lobby[conn];
            conn.Send(MakeCharactersAvailableMessage(account));
            // addon system hooks
            onServerConnect.Invoke(conn);
        }

        public override void OnClientSceneChanged() {}

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

        void LoadPreview(GameObject prefab, Transform location, int selectionIndex, CharactersAvailableMsg.CharacterPreview character)
        {
            GameObject preview = Instantiate(prefab.gameObject, location.position, location.rotation);
            preview.transform.parent = location;
            Player player = preview.GetComponent<Player>();
            player.name = character.name;
            player.isGameMaster = character.isGameMaster;
            for (int i = 0; i < character.equipment.Length; ++i)
            {
                ItemSlot slot = character.equipment[i];
                player.equipment.slots.Add(slot);
                if (slot.amount > 0)
                {
                    ((PlayerEquipment)player.equipment).RefreshLocation(i);
                }
            }
            preview.AddComponent<SelectableCharacter>();
            preview.GetComponent<SelectableCharacter>().index = selectionIndex;
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
            Debug.Log("characters available:" + charactersAvailableMsg.characters.Length);
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

            // setup camera
            Camera.main.transform.position = selectionCameraLocation.position;
            Camera.main.transform.rotation = selectionCameraLocation.rotation;

            // addon system hooks
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
            for (int i = 0; i < player.inventory.size; ++i)
            {
                player.inventory.slots.Add(i < player.inventory.defaultItems.Length ? new ItemSlot(new Item(player.inventory.defaultItems[i].item), player.inventory.defaultItems[i].amount) : new ItemSlot());
            }
            for (int i = 0; i < ((PlayerEquipment)player.equipment).slotInfo.Length; ++i)
            {
                EquipmentInfo info = ((PlayerEquipment)player.equipment).slotInfo[i];
                player.equipment.slots.Add(info.defaultItem.item != null ? new ItemSlot(new Item(info.defaultItem.item), info.defaultItem.amount) : new ItemSlot());
            }
            player.health.current = player.health.max; // after equipment in case of boni
            player.mana.current = player.mana.max; // after equipment in case of boni
            player.isGameMaster = gameMaster;

            return player;
        }

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
                                if (message.gameMaster == false ||
                                    conn == NetworkServer.localConnection)
                                {
                                    Player player = CreateCharacter(playerClasses[message.classIndex].gameObject, message.name, account, message.gameMaster);
                                    player.customization = ValidateCustomization( message.customization, playerClasses[message.classIndex] );
                                    onServerCharacterCreate.Invoke(message, player);
                                    Database.singleton.CharacterSave(player, false);
                                    Destroy(player.gameObject);
                                    conn.Send(MakeCharactersAvailableMessage(account));
                                }
                                else
                                {
                                    ServerSendError(conn, "insufficient permissions", false);
                                }
                            }
                            else
                            {
                                ServerSendError(conn, "character invalid class", false);
                            }
                        }
                        else
                        {
                            ServerSendError(conn, "character limit reached", false);
                        }
                    }
                    else
                    {
                        ServerSendError(conn, "name already exists", false);
                    }
                }
                else
                {
                    ServerSendError(conn, "character name not allowed", false);
                }
            }
            else
            {
                ServerSendError(conn, "CharacterCreate: not in lobby", true);
            }
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn) { Debug.LogWarning("Use the CharacterSelectMsg instead"); }

        void OnServerCharacterSelect(NetworkConnectionToClient conn, CharacterSelectMsg message)
        {
            if (lobby.ContainsKey(conn))
            {
                string account = lobby[conn];
                List<string> characters = Database.singleton.CharactersForAccount(account);
                if (0 <= message.index && message.index < characters.Count)
                {
                    GameObject go = Database.singleton.CharacterLoad(characters[message.index], playerClasses, false);
                    Player player = go.GetComponent<Player>();
                    if (player != null)    { }
                    NetworkServer.AddPlayerForConnection(conn, go);
                    onServerCharacterSelect.Invoke(account, go, conn, message);
                    lobby.Remove(conn);
                }
                else
                {
                    Debug.Log("invalid character index: " + account + " " + message.index);
                    ServerSendError(conn, "invalid character index", false);
                }
            }
            else
            {
                Debug.Log("CharacterSelect: not in lobby" + conn);
                ServerSendError(conn, "CharacterSelect: not in lobby", true);
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
                Debug.Log("CharacterDelete: not in lobby: " + conn);
                ServerSendError(conn, "CharacterDelete: not in lobby", true);
            }
        }

        // player saving ///////////////////////////////////////////////////////////
        // we have to save all players at once to make sure that item trading is
        // perfectly save. if we would invoke a save function every few minutes on
        // each player seperately then it could happen that two players trade items
        // and only one of them is saved before a server crash - hence causing item
        // duplicates.
        //void SavePlayers()
        //{
        //    Database.singleton.CharacterSaveMany(Player.onlinePlayers.Values);
        //    if (Player.onlinePlayers.Count > 0)
        //        Debug.Log("saved " + Player.onlinePlayers.Count + " player(s)");
        //}
void SavePlayers()
{
    if (Player.onlinePlayers.Count == 0)
        return;

    StartCoroutine(SavePlayersStaggered());
}

IEnumerator SavePlayersStaggered()
{
    var players = Player.onlinePlayers.Values;

    Database.singleton.connection.BeginTransaction();

    foreach (Player player in players)
    {
        if (player != null)
        {
            Database.singleton.CharacterSave(
                player,
                online: true,
                useTransaction: false
            );
        }

        yield return null;
    }

    Database.singleton.connection.Commit();

    Debug.Log($"[SAVE] Staggered save complete ({players.Count} players)");
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

        IEnumerator<WaitForSeconds> DoServerDisconnect(NetworkConnectionToClient conn, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (conn.identity != null)
            {
                Database.singleton.CharacterSave(conn.identity.GetComponent<Player>(), false);
                Debug.Log("saved:" + conn.identity.name);
            }

            onServerDisconnect.Invoke(conn);

            lobby.Remove(conn); // just returns false if not found

            base.OnServerDisconnect(conn);
        }

        public override void OnClientDisconnect()
        {
            Debug.Log("OnClientDisconnect");

            Camera mainCamera = Camera.main;
            if (mainCamera.transform.parent != null)
                mainCamera.transform.SetParent(null);

            uiPopup.Show("Disconnected.");
            base.OnClientDisconnect();
            state = NetworkState.Offline;
            onClientDisconnect.Invoke(NetworkClient.connection);
        }

        public static void Quit()
        {
    #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
    #else
            Application.Quit();
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
        PlayerCustomizationData ValidateCustomization( PlayerCustomizationData input, Player prefab)
        {
            PlayerCustomizationData result = default;

            PlayerCustomizationVisuals visuals = prefab.GetComponent<PlayerCustomizationVisuals>();

            if (visuals == null || visuals.slots == null)
                return result;

            int SlotCount() => visuals.slots.Length;

            int Clamp(int value, int slot)
            {
                if (slot < 0 || slot >= visuals.slots.Length)
                    return 0;
                var meshes = visuals.slots[slot].meshes;
                if (meshes == null || meshes.Length == 0)
                    return 0;

                return Mathf.Clamp(value, 0, meshes.Length - 1);
            }

            if (SlotCount() > 0) result.hair  = Clamp(input.hair,  0);
            if (SlotCount() > 1) result.beard = Clamp(input.beard, 1);
            if (SlotCount() > 2) result.face  = Clamp(input.face,  2);
            if (SlotCount() > 3) result.brows = Clamp(input.brows, 3);
            if (SlotCount() > 4) result.ears  = Clamp(input.ears,  4);

            return result;
        }
    }
}