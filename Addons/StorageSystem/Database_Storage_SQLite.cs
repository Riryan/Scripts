using System.Data;

namespace uMMORPG
{
    public partial class Database
    {
        bool storageSchemaChecked = false;

        // --------------------------------------------------
        // Ensure SQLite schema (runs once)
        // --------------------------------------------------
        void EnsureStorageSchema(IDbConnection connection)
        {
            if (storageSchemaChecked)
                return;

            storageSchemaChecked = true;

            using (var cmd = connection.CreateCommand())
            {
                // Check if column exists
                cmd.CommandText =
                    "PRAGMA table_info(characters);";

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    bool hasStorageGold = false;

                    while (reader.Read())
                    {
                        if (reader["name"].ToString() == "storageGold")
                        {
                            hasStorageGold = true;
                            break;
                        }
                    }

                    if (!hasStorageGold)
                    {
                        reader.Close();

                        using (var alter = connection.CreateCommand())
                        {
                            alter.CommandText =
                                "ALTER TABLE characters " +
                                "ADD COLUMN storageGold INTEGER NOT NULL DEFAULT 0;";
                            alter.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        // --------------------------------------------------
        // Storage: Save (SQLite)
        // --------------------------------------------------
        void SaveStorage(Player player, IDbCommand cmd)
        {
            if (player?.Storage == null)
                return;

            cmd.CommandText += ", storageGold=@storageGold";

            var param = cmd.CreateParameter();
            param.ParameterName = "@storageGold";
            param.Value = player.Storage.gold;
            cmd.Parameters.Add(param);
        }

        // --------------------------------------------------
        // Storage: Load (SQLite)
        // --------------------------------------------------
        void LoadStorage(Player player, IDataReader reader)
        {
            if (player == null)
                return;

            long gold = 0;
            int ordinal = reader.GetOrdinal("storageGold");
            if (ordinal >= 0 && !reader.IsDBNull(ordinal))
                gold = reader.GetInt64(ordinal);

            player.InitializeStorage(
                maxSlots: 0,
                maxGold: 100000
            );

            player.Storage.gold = gold;
        }
    }
}
