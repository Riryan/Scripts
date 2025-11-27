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
        // quests: remove old entries first, then add all new ones
        connection.Execute("DELETE FROM character_customization WHERE character=?", player.name);

        string _values = "";
        for (int i = 0; i < player.customization.values.Count; i++)
        {
            _values += player.customization.values[i] + ";";
        }

        // note: .Insert causes a 'Constraint' exception. use Replace.
        connection.Insert(new character_customization
        {
            character = player.name,
            values = _values,
            scale = player.customization.scale
        });
    }

    public void CharacterLoad_Customization(Player player)
    {
        character_customization info = connection.FindWithQuery<character_customization>("Select * FROM character_customization WHERE character=?", player.name);

        if (info != null)
        {
            string temp = info.values;
            //We are looping through all instances of the letter in the given string
            while (temp.IndexOf(";") != -1)
            {
                player.customization.values.Add(int.Parse(temp.Substring(0, temp.IndexOf(";"))));
                temp = temp.Remove(0, temp.IndexOf(";") + 1);
            }

            player.customization.scale = info.scale;
        }
    }
}
