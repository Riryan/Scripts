using UnityEngine;
using Mirror;
using System;
using System.IO;
using System.Collections.Generic;
using SQLite; 
using UnityEngine.Events;
using GFFAddons;

public partial class Database : MonoBehaviour
{
    
    public static Database singleton;
    public string databaseFile = "Database.sqlite";   
    public SQLiteConnection connection;
    class accounts
    {
        [PrimaryKey] 
        public string name { get; set; }
        public string password { get; set; }
        public DateTime created { get; set; }
        public DateTime lastlogin { get; set; }
        public bool banned { get; set; }
    }
    class characters
    {
        [PrimaryKey]
        [Collation("NOCASE")]
        public string name { get; set; }
        [Indexed]
        public string account { get; set; }
        public string classname { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public int level { get; set; }
        public int health { get; set; }
        public int mana { get; set; }
        public int strength { get; set; }
        public int intelligence { get; set; }
        public long experience { get; set; }
        public long skillExperience { get; set; }
        public long gold { get; set; }
        public long coins { get; set; }
        public bool gamemaster { get; set; }
        public bool online { get; set; }
        public DateTime lastsaved { get; set; }
        public bool deleted { get; set; }
         public RaceList race { get; set; }
        public string gender { get; set; }
        public string specialisation1 { get; set; }
        public string specialisation2 { get; set; }
        public int graveyardTombstoneId { get; set; }

    }
    class character_inventory
    {
        public string character { get; set; }
        public int slot { get; set; }
        public string name { get; set; }
        public int amount { get; set; }
        public int durability { get; set; }
        public int summonedHealth { get; set; }
        public int summonedLevel { get; set; }
        public long summonedExperience { get; set; } 
        
    }
    class character_equipment : character_inventory   {    }
    
    class character_itemcooldowns
    {
        [PrimaryKey]
        public string character { get; set; }
        public string category { get; set; }
        public float cooldownEnd { get; set; }
    }
    class character_skills
    {
        public string character { get; set; }
        public string name { get; set; }
        public int level { get; set; }
        public float castTimeEnd { get; set; }
        public float cooldownEnd { get; set; }
        
    }
    class character_buffs
    {
        public string character { get; set; }
        public string name { get; set; }
        public int level { get; set; }
        public float buffTimeEnd { get; set; }
        
    }
    class character_quests
    {
        public string character { get; set; }
        public string name { get; set; }
        public int progress { get; set; }
        public bool completed { get; set; }
        
    }
    class character_orders
    {        
        [PrimaryKey] 
        public int orderid { get; set; }
        public string character { get; set; }
        public long coins { get; set; }
        public bool processed { get; set; }
    }
    class character_guild
    {
        [PrimaryKey] 
        public string character { get; set; }
        [Indexed]
        public string guild { get; set; }
        public int rank { get; set; }
    }
    class guild_info
    {
        [PrimaryKey] 
        public string name { get; set; }
        public string notice { get; set; }
    }
    // Account-wide warehouse, 50 slots shared by all characters on the account
    class account_warehouse
    {
        public string account { get; set; }     // account name (key)
        public int slot { get; set; }           // 0..49
        public string name { get; set; }        // item template name
        public int amount { get; set; }
        public int durability { get; set; }
        public int summonedHealth { get; set; }
        public int summonedLevel { get; set; }
        public long summonedExperience { get; set; }
    }
    [Header("Events")]
    public UnityEvent onConnected;
    public UnityEventPlayer onCharacterLoad;
    public UnityEventPlayer onCharacterSave;

    void Awake()
    {
        if (singleton == null) singleton = this;
    }
    
    public void Connect()
    {        
#if UNITY_EDITOR
        string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, databaseFile);
#elif UNITY_ANDROID
        string path = Path.Combine(Application.persistentDataPath, databaseFile);
#elif UNITY_IOS
        string path = Path.Combine(Application.persistentDataPath, databaseFile);
#else
        string path = Path.Combine(Application.dataPath, databaseFile);
#endif
        connection = new SQLiteConnection(path);
        connection.CreateTable<accounts>();
        connection.CreateTable<characters>();
        connection.CreateTable<character_inventory>();
        connection.CreateIndex(nameof(character_inventory), new []{"character", "slot"});
        connection.CreateTable<character_equipment>();
        connection.CreateIndex(nameof(character_equipment), new []{"character", "slot"});
        // --- Warehouse ---
        connection.CreateTable<account_warehouse>();
        connection.CreateIndex(nameof(account_warehouse), new []{"account", "slot"});
        // --- /Warehouse ---
        connection.CreateTable<character_itemcooldowns>();
        connection.CreateTable<character_skills>();
        connection.CreateIndex(nameof(character_skills), new []{"character", "name"});
        connection.CreateTable<character_buffs>();
        connection.CreateIndex(nameof(character_buffs), new []{"character", "name"});
        connection.CreateTable<character_quests>();
        connection.CreateIndex(nameof(character_quests), new []{"character", "name"});
        connection.CreateTable<character_orders>();
        connection.CreateTable<character_guild>();
        connection.CreateTable<guild_info>();
        // --- CombatSkills: ensure table exists and hook runtime load/save ---
        try
        {
            Connect_CombatSkills();
        }
        catch (System.Exception e)
        {
           Debug.LogError("Connect_CombatSkills failed: " + e.Message);
        }
#if UNITY_SERVER || UNITY_EDITOR
        // Register runtime listeners so headless/server builds load & save combat skills
        onCharacterLoad.AddListener(CharacterLoad_CombatSkills);
        onCharacterSave.AddListener(CharacterSave_CombatSkills);
#endif
        // --- /CombatSkills ---
        onConnected.Invoke();        
    }

    
    void OnApplicationQuit()
    {
        connection?.Close();
    }
    
    public bool TryLogin(string account, string password)
    {
        if (!string.IsNullOrWhiteSpace(account) && !string.IsNullOrWhiteSpace(password))
        {
            if (connection.FindWithQuery<accounts>("SELECT * FROM accounts WHERE name=?", account) == null)
                connection.Insert(new accounts{ name=account, password=password, created=DateTime.UtcNow, lastlogin=DateTime.Now, banned=false});
            if (connection.FindWithQuery<accounts>("SELECT * FROM accounts WHERE name=? AND password=? and banned=0", account, password) != null)
            {
                connection.Execute("UPDATE accounts SET lastlogin=? WHERE name=?", DateTime.UtcNow, account);
                return true;
            }
        }
        return false;
    }

    
    public bool CharacterExists(string characterName)
    {
        return connection.FindWithQuery<characters>("SELECT * FROM characters WHERE name=?", characterName) != null;
    }

    public void CharacterDelete(string characterName)
    {
        connection.Execute("UPDATE characters SET deleted=1 WHERE name=?", characterName);
    }

    public List<string> CharactersForAccount(string account)
    {
        List<string> result = new List<string>();
        foreach (characters character in connection.Query<characters>("SELECT * FROM characters WHERE account=? AND deleted=0", account))
            result.Add(character.name);
        return result;
    }

void LoadInventory(PlayerInventory inventory)
{
    // determine which character this inventory belongs to
    Player player = inventory.GetComponent<Player>();
    string characterName = player != null ? player.name : inventory.name;

    // ensure slots exist
    for (int i = 0; i < inventory.size; ++i)
        inventory.slots.Add(new ItemSlot());

    foreach (character_inventory row in connection.Query<character_inventory>(
                 "SELECT * FROM character_inventory WHERE character=?", characterName))
    {
        if (row.slot < inventory.size)
        {
            if (ScriptableItem.All.TryGetValue(row.name.GetStableHashCode(), out ScriptableItem itemData))
            {
                Item item = new Item(itemData);
                item.durability         = Mathf.Min(row.durability, item.maxDurability);
                item.summonedHealth     = row.summonedHealth;
                item.summonedLevel      = row.summonedLevel;
                item.summonedExperience = row.summonedExperience;
                inventory.slots[row.slot] = new ItemSlot(item, row.amount);
            }
            else Debug.LogWarning("LoadInventory: skipped item " + row.name + " for " + characterName + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
        }
        else Debug.LogWarning("LoadInventory: skipped slot " + row.slot + " for " + characterName + " because it's bigger than size " + inventory.size);
    }
}

    void LoadWarehouse(Player player)
    {
        const int WarehouseSize = 50;

        // ensure 50 empty slots first
        player.warehouseSlots.Clear();
        for (int i = 0; i < WarehouseSize; ++i)
            player.warehouseSlots.Add(new ItemSlot());

        // load existing items for this account
        foreach (account_warehouse row in connection.Query<account_warehouse>(
                 "SELECT * FROM account_warehouse WHERE account=?", player.account))
        {
            if (row.slot >= 0 && row.slot < WarehouseSize)
            {
                if (ScriptableItem.All.TryGetValue(row.name.GetStableHashCode(), out ScriptableItem itemData))
                {
                    Item item = new Item(itemData);
                    item.durability         = Mathf.Min(row.durability, item.maxDurability);
                    item.summonedHealth     = row.summonedHealth;
                    item.summonedLevel      = row.summonedLevel;
                    item.summonedExperience = row.summonedExperience;
                    player.warehouseSlots[row.slot] = new ItemSlot(item, row.amount);
                }
                else
                {
                    Debug.LogWarning($"[DB][Warehouse] Skipped unknown item {row.name} for {player.account}.");
                }
            }
            else
            {
                Debug.LogWarning($"[DB][Warehouse] Skipped invalid slot {row.slot} for {player.account}.");
            }
        }
    }

void LoadEquipment(PlayerEquipment equipment)
{
    Player player = equipment.GetComponent<Player>();
    string characterName = player != null ? player.name : equipment.name;

    for (int i = 0; i < equipment.slotInfo.Length; ++i)
        equipment.slots.Add(new ItemSlot());

    foreach (character_equipment row in connection.Query<character_equipment>(
                 "SELECT * FROM character_equipment WHERE character=?", characterName))
    {
        if (row.slot < equipment.slotInfo.Length)
        {
            if (ScriptableItem.All.TryGetValue(row.name.GetStableHashCode(), out ScriptableItem itemData))
            {
                Item item = new Item(itemData);
                item.durability         = Mathf.Min(row.durability, item.maxDurability);
                item.summonedHealth     = row.summonedHealth;
                item.summonedLevel      = row.summonedLevel;
                item.summonedExperience = row.summonedExperience;
                equipment.slots[row.slot] = new ItemSlot(item, row.amount);
            }
            else Debug.LogWarning("LoadEquipment: skipped item " + row.name + " for " + characterName + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
        }
        else Debug.LogWarning("LoadEquipment: skipped slot " + row.slot + " for " + characterName + " because it's bigger than size " + equipment.slotInfo.Length);
    }
}


    void LoadItemCooldowns(Player player)
    {        
        foreach (character_itemcooldowns row in connection.Query<character_itemcooldowns>("SELECT * FROM character_itemcooldowns WHERE character=?", player.name))
        {
            player.itemCooldowns.Add(row.category, row.cooldownEnd + NetworkTime.time);
        }
    }

    void LoadSkills(PlayerSkills skills)
    {
        foreach (ScriptableSkill skillData in skills.skillTemplates)
            skills.skills.Add(new Skill(skillData));
        
        foreach (character_skills row in connection.Query<character_skills>("SELECT * FROM character_skills WHERE character=?", skills.name))
        {
            int index = skills.GetSkillIndexByName(row.name);
            if (index != -1)
            {
                Skill skill = skills.skills[index];
                skill.level = Mathf.Clamp(row.level, 1, skill.maxLevel);
                skill.castTimeEnd = row.castTimeEnd + NetworkTime.time;
                skill.cooldownEnd = row.cooldownEnd + NetworkTime.time;
                skills.skills[index] = skill;
            }
        }
    }

    void LoadBuffs(PlayerSkills skills)
    {
        foreach (character_buffs row in connection.Query<character_buffs>("SELECT * FROM character_buffs WHERE character=?", skills.name))
        {
            if (ScriptableSkill.All.TryGetValue(row.name.GetStableHashCode(), out ScriptableSkill skillData))
            {
                int level = Mathf.Clamp(row.level, 1, skillData.maxLevel);
                Buff buff = new Buff((BuffSkill)skillData, level);                
                buff.buffTimeEnd = row.buffTimeEnd + NetworkTime.time;
                skills.buffs.Add(buff);
            }
            else Debug.LogWarning("LoadBuffs: skipped buff " + row.name + " for " + skills.name + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
        }
    }

    void LoadQuests(PlayerQuests quests)
    {
        foreach (character_quests row in connection.Query<character_quests>("SELECT * FROM character_quests WHERE character=?", quests.name))
        {
            ScriptableQuest questData;
            if (ScriptableQuest.All.TryGetValue(row.name.GetStableHashCode(), out questData))
            {
                Quest quest = new Quest(questData);
                quest.progress = row.progress;
                quest.completed = row.completed;
                quests.quests.Add(quest);
            }
            else Debug.LogWarning("LoadQuests: skipped quest " + row.name + " for " + quests.name + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
        }
    }
    
    void LoadGuildOnDemand(PlayerGuild playerGuild)
    {
        string guildName = connection.ExecuteScalar<string>("SELECT guild FROM character_guild WHERE character=?", playerGuild.name);
        if (guildName != null)
        {
            if (!GuildSystem.guilds.ContainsKey(guildName))
            {
                Guild guild = LoadGuild(guildName);
                GuildSystem.guilds[guild.name] = guild;
                playerGuild.guild = guild;
            }
            else playerGuild.guild = GuildSystem.guilds[guildName];
        }
    }

public GameObject CharacterLoad(string characterName, List<Player> prefabs, bool isPreview)
{
    characters row = connection.FindWithQuery<characters>(
        "SELECT * FROM characters WHERE name=? AND deleted=0",
        characterName);

    if (row != null)
    {
        Player prefab = prefabs.Find(p => p.name == row.classname);
        if (prefab != null)
        {
            GameObject go = Instantiate(prefab.gameObject);
            Player player = go.GetComponent<Player>();

            // basic identity / stats
            player.name                                   = row.name;
            player.account                                = row.account;
            player.className                              = row.classname;
            Vector3 position                              = new Vector3(row.x, row.y, row.z);
            player.level.current                          = Mathf.Min(row.level, player.level.max);
            player.strength.value                         = row.strength;
            player.intelligence.value                     = row.intelligence;
            player.experience.current                     = row.experience;
            ((PlayerSkills)player.skills).skillExperience = row.skillExperience;
            player.gold                                   = row.gold;
            player.isGameMaster                           = row.gamemaster;
            player.itemMall.coins                         = row.coins;

            // spawn position
            if (player.movement.IsValidSpawnPoint(position))
            {
                player.movement.Warp(position);
            }
            else
            {
                Transform start = NetworkManagerMMO.GetNearestStartPosition(position);
                player.movement.Warp(start.position);
            }

            // DB loads
            try
            {
                LoadInventory(player.inventory);
                LoadEquipment((PlayerEquipment)player.equipment);
                LoadItemCooldowns(player);
                LoadSkills((PlayerSkills)player.skills);
                LoadBuffs((PlayerSkills)player.skills);
                LoadQuests(player.quests);
                LoadGuildOnDemand(player.guild);
                LoadWarehouse(player);
            }
            catch (Exception dbEx)
            {
                Debug.LogError($"[CharacterLoad] DB load error for '{characterName}' (preview={isPreview}): {dbEx}");
                // continue spawning even if some DB parts failed
            }

            // DEBUG: see what equipment we actually loaded
            PlayerEquipment eq = (PlayerEquipment)player.equipment;
            int equippedCount = 0;
            for (int i = 0; i < eq.slots.Count; ++i)
            {
                if (eq.slots[i].amount > 0)
                {
                    equippedCount++;
                }
            }

            // OPTIONAL: if you ever want server-side visuals for preview instances
            // (not strictly needed for the network data, since the client handles visuals),
            // you could refresh locations here for preview-only instances:
            if (isPreview)
            {
                PlayerCustomization custom = player.GetComponent<PlayerCustomization>();
                if (custom != null)
                    custom.allowOfflinePreview = true;

                for (int i = 0; i < eq.slots.Count; ++i)
                {
                    if (eq.slots[i].amount > 0)
                        eq.RefreshLocation(i);
                }
            }

            // vitals
            player.health.current = row.health;
            player.mana.current   = row.mana;

            if (!isPreview)
            {
                connection.Execute(
                    "UPDATE characters SET online=1, lastsaved=? WHERE name=?",
                    DateTime.UtcNow, characterName);
            }

#if UNITY_SERVER || UNITY_EDITOR
            player.graveyardTombstoneId = row.graveyardTombstoneId;
#endif

            onCharacterLoad.Invoke(player);

            return go;
        }
        else
        {
            Debug.LogError("no prefab found for class: " + row.classname);
        }
    }

    return null;
}


void SaveInventory(PlayerInventory inventory)
{
    Player player = inventory.GetComponent<Player>();
    string characterName = player != null ? player.name : inventory.name;

    connection.Execute("DELETE FROM character_inventory WHERE character=?", characterName);
    for (int i = 0; i < inventory.slots.Count; ++i)
    {
        ItemSlot slot = inventory.slots[i];
        if (slot.amount > 0)
        {
            connection.InsertOrReplace(new character_inventory{
                character        = characterName,
                slot             = i,
                name             = slot.item.name,
                amount           = slot.amount,
                durability       = slot.item.durability,
                summonedHealth   = slot.item.summonedHealth,
                summonedLevel    = slot.item.summonedLevel,
                summonedExperience = slot.item.summonedExperience
            });
        }
    }
}


void SaveEquipment(PlayerEquipment equipment)
{
    Player player = equipment.GetComponent<Player>();
    string characterName = player != null ? player.name : equipment.name;

    connection.Execute("DELETE FROM character_equipment WHERE character=?", characterName);
    for (int i = 0; i < equipment.slots.Count; ++i)
    {
        ItemSlot slot = equipment.slots[i];
        if (slot.amount > 0)
        {
            connection.InsertOrReplace(new character_equipment{
                character        = characterName,
                slot             = i,
                name             = slot.item.name,
                amount           = slot.amount,
                durability       = slot.item.durability,
                summonedHealth   = slot.item.summonedHealth,
                summonedLevel    = slot.item.summonedLevel,
                summonedExperience = slot.item.summonedExperience
            });
        }
    }
}

    void SaveWarehouse(Player player)
    {
        const int WarehouseSize = 50;

        connection.Execute("DELETE FROM account_warehouse WHERE account=?", player.account);

        int slotCount = player.warehouseSlots != null ? player.warehouseSlots.Count : 0;

        for (int i = 0; i < slotCount; ++i)
        {
            ItemSlot slot = player.warehouseSlots[i];
            if (slot.amount > 0)
            {
                connection.InsertOrReplace(new account_warehouse
                {
                    account            = player.account,
                    slot               = i,
                    name               = slot.item.name,
                    amount             = slot.amount,
                    durability         = slot.item.durability,
                    summonedHealth     = slot.item.summonedHealth,
                    summonedLevel      = slot.item.summonedLevel,
                    summonedExperience = slot.item.summonedExperience
                });
            }
        }
    }

    void SaveItemCooldowns(Player player)
    {
        connection.Execute("DELETE FROM character_itemcooldowns WHERE character=?", player.name);
        foreach (KeyValuePair<string, double> kvp in player.itemCooldowns)
        {
            float cooldown = player.GetItemCooldown(kvp.Key);
            if (cooldown > 0)
            {
                connection.InsertOrReplace(new character_itemcooldowns{
                    character = player.name,
                    category = kvp.Key,
                    cooldownEnd = cooldown
                });
            }
        }
    }

    void SaveSkills(PlayerSkills skills)
    {
        
        connection.Execute("DELETE FROM character_skills WHERE character=?", skills.name);
        foreach (Skill skill in skills.skills)
            if (skill.level > 0) 
            {
                connection.InsertOrReplace(new character_skills{
                    character = skills.name,
                    name = skill.name,
                    level = skill.level,
                    castTimeEnd = skill.CastTimeRemaining(),
                    cooldownEnd = skill.CooldownRemaining()
                });
            }
    }

    void SaveBuffs(PlayerSkills skills)
    {
        connection.Execute("DELETE FROM character_buffs WHERE character=?", skills.name);
        foreach (Buff buff in skills.buffs)
        {
            connection.InsertOrReplace(new character_buffs{
                character = skills.name,
                name = buff.name,
                level = buff.level,
                buffTimeEnd = buff.BuffTimeRemaining()
            });
        }
    }

    void SaveQuests(PlayerQuests quests)
    {
        connection.Execute("DELETE FROM character_quests WHERE character=?", quests.name);
        foreach (Quest quest in quests.quests)
        {
            connection.InsertOrReplace(new character_quests{
                character = quests.name,
                name = quest.name,
                progress = quest.progress,
                completed = quest.completed
            });
        }
    }
    
    public void CharacterSave(Player player, bool online, bool useTransaction = true)
    {
        if (useTransaction) connection.BeginTransaction();
        connection.InsertOrReplace(new characters{
            name = player.name,
            account = player.account,
            classname = player.className,
            x = player.transform.position.x,
            y = player.transform.position.y,
            z = player.transform.position.z,
            level = player.level.current,
            health = player.health.current,
            mana = player.mana.current,
            strength = player.strength.value,
            intelligence = player.intelligence.value,
            experience = player.experience.current,
            skillExperience = ((PlayerSkills)player.skills).skillExperience,
            gold = player.gold,
            coins = player.itemMall.coins,
            gamemaster = player.isGameMaster,
            online = online,
            lastsaved = DateTime.UtcNow,
            graveyardTombstoneId = player.graveyardTombstoneId
        });
        SaveInventory(player.inventory);
        SaveEquipment((PlayerEquipment)player.equipment);
        SaveItemCooldowns(player);
        SaveSkills((PlayerSkills)player.skills);
        SaveBuffs((PlayerSkills)player.skills);
        SaveQuests(player.quests);
        if (player.guild.InGuild())
            SaveGuild(player.guild.guild, false);
        SaveWarehouse(player); 
        onCharacterSave.Invoke(player);

        if (useTransaction) connection.Commit();
    }

    
    public void CharacterSaveMany(IEnumerable<Player> players, bool online = true)
    {
        connection.BeginTransaction(); 
        foreach (Player player in players)
            CharacterSave(player, online, false);
        connection.Commit(); 
    }
    
    public bool GuildExists(string guild)
    {
        return connection.FindWithQuery<guild_info>("SELECT * FROM guild_info WHERE name=?", guild) != null;
    }

    Guild LoadGuild(string guildName)
    {
        Guild guild = new Guild();
        guild.name = guildName;
        guild_info info = connection.FindWithQuery<guild_info>("SELECT * FROM guild_info WHERE name=?", guildName);
        if (info != null)
        {
            guild.notice = info.notice;
        }
        
        List<character_guild> rows = connection.Query<character_guild>("SELECT * FROM character_guild WHERE guild=?", guildName);
        GuildMember[] members = new GuildMember[rows.Count]; 
        for (int i = 0; i < rows.Count; ++i)
        {
            character_guild row = rows[i];
            GuildMember member = new GuildMember();
            member.name = row.character;
            member.rank = (GuildRank)row.rank;
            
            if (Player.onlinePlayers.TryGetValue(member.name, out Player player))
            {
                member.online = true;
                member.level = player.level.current;
            }
            else
            {
                member.online = false;
                characters character = connection.FindWithQuery<characters>("SELECT * FROM characters WHERE name=?", member.name);
                member.level = character != null ? character.level : 1;
            }
            members[i] = member;
        }
        guild.members = members;
        return guild;
    }

    public void SaveGuild(Guild guild, bool useTransaction = true)
    {
        if (useTransaction) connection.BeginTransaction(); 

        
        connection.InsertOrReplace(new guild_info{
            name = guild.name,
            notice = guild.notice
        });

        connection.Execute("DELETE FROM character_guild WHERE guild=?", guild.name);
        foreach (GuildMember member in guild.members)
        {
            connection.InsertOrReplace(new character_guild{
                character = member.name,
                guild = guild.name,
                rank = (int)member.rank
            });
        }

        if (useTransaction) connection.Commit(); 
    }

    public void RemoveGuild(string guild)
    {
        connection.BeginTransaction(); 
        connection.Execute("DELETE FROM guild_info WHERE name=?", guild);
        connection.Execute("DELETE FROM character_guild WHERE guild=?", guild);
        connection.Commit(); 
    }
    
    public List<long> GrabCharacterOrders(string characterName)
    {
        
        List<long> result = new List<long>();
        List<character_orders> rows = connection.Query<character_orders>("SELECT * FROM character_orders WHERE character=? AND processed=0", characterName);
        foreach (character_orders row in rows)
        {
            result.Add(row.coins);
            connection.Execute("UPDATE character_orders SET processed=1 WHERE orderid=?", row.orderid);
        }
        return result;
    }
}