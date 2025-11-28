using SQLite; // from https://github.com/praeclarum/sqlite-net

public partial class Database
{
    public class character_customization
    {
        [PrimaryKey] // important for performance: O(log n) instead of O(n)
        public string character { get; set; }
        public string values { get; set; }
        public float scale { get; set; }
    }

    public void Connect_Customization()
    {
        // create tables if they don't exist yet or were deleted
        connection.CreateTable<character_customization>();
    }

    public void CharacterSave_Customization(Player player)
    {
        // remove old entry first
        connection.Execute("DELETE FROM character_customization WHERE character=?", player.name);

        // only persist as many entries as we have customization categories
        int maxCount = player.customization.values.Count;
        if (player.customization.customization != null &&
            player.customization.customization.Length < maxCount)
        {
            maxCount = player.customization.customization.Length;
        }

        string _values = "";
        for (int i = 0; i < maxCount; i++)
        {
            _values += player.customization.values[i] + ";";
        }

        // note: .Insert causes a 'Constraint' exception. use Replace.
        connection.Insert(new character_customization
        {
            character = player.name,
            values    = _values,
            scale     = player.customization.scale
        });
    }

    public void CharacterLoad_Customization(Player player)
    {
        character_customization info =
            connection.FindWithQuery<character_customization>(
                "Select * FROM character_customization WHERE character=?",
                player.name);

        // always start from a clean list to avoid runaway growth
        player.customization.values.Clear();

        if (info != null && !string.IsNullOrEmpty(info.values))
        {
            // parse all entries from the stored string
            var parsed = new System.Collections.Generic.List<int>();
            string temp = info.values;

            // loop through all ";"-separated ints
            while (temp.IndexOf(";") != -1)
            {
                int sep = temp.IndexOf(";");
                string part = temp.Substring(0, sep);
                if (!string.IsNullOrEmpty(part))
                {
                    if (int.TryParse(part, out int val))
                        parsed.Add(val);
                }
                temp = temp.Remove(0, sep + 1);
            }

            // clamp to the number of customization categories on this prefab
            int maxCount = parsed.Count;
            if (player.customization.customization != null &&
                player.customization.customization.Length < maxCount)
            {
                maxCount = player.customization.customization.Length;
            }

            for (int i = 0; i < maxCount; ++i)
                player.customization.values.Add(parsed[i]);

            player.customization.scale = info.scale;
        }
    }
}
