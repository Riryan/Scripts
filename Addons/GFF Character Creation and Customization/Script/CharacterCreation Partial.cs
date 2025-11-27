using GFFAddons;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class Player
{
    [Header("GFF Customization")]
    public PlayerCustomization customization;
}

public partial struct CharactersAvailableMsg
{
    public partial struct CharacterPreview
    {
        public RaceList race;
        public string gender;
        public int level;
        public long gold;

        public string customization;
        public float scale;
    }

    void Load_Customization(List<Player> players)
    {
        for (int i = 0; i < players.Count; ++i)
        {
            characters[i].race = players[i].race;
            characters[i].gender = players[i].gender;
            characters[i].level = players[i].level.current;
            characters[i].gold = players[i].gold;

            //Customization
            string _values = "";
            for (int x = 0; x < players[i].customization.values.Count; x++)
            {
                _values += players[i].customization.values[x] + ";";
            }

            characters[i].customization = _values;
            characters[i].scale = players[i].customization.scale;
        }
    }
}

public partial struct CharacterCreateMsg
{
    public RaceList race;
    public string gender;

    public string customization;
    public float scale;
}

public partial class NetworkManagerMMO
{
    public void OnServerCharacterCreate_Customization(CharacterCreateMsg message, Player player)
    {
        player.race = message.race;
        player.gender = message.gender;
        player.customization.values.Clear();
        string temp = message.customization;
        //We are looping through all instances of the letter in the given string
        while (temp.IndexOf(";") != -1)
        {
            player.customization.values.Add(int.Parse(temp.Substring(0, temp.IndexOf(";"))));
            temp = temp.Remove(0, temp.IndexOf(";") + 1);
        }

        if (player.customization.rescaling || message.scale > 0) player.customization.scale = message.scale;
        else player.customization.scale = 1;
    }

    private void SetCustomization(CharactersAvailableMsg.CharacterPreview character, Player player)
    {
        player.customization.values.Clear();
        string temp = character.customization;
        //We are looping through all instances of the letter in the given string
        while (temp.IndexOf(";") != -1)
        {
            player.customization.values.Add(int.Parse(temp.Substring(0, temp.IndexOf(";"))));
            temp = temp.Remove(0, temp.IndexOf(";") + 1);
        }

        player.customization.scale = character.scale;

        player.customization.SetCustomization();
    }
}

public partial class UICharacterInfo
{
    [Header("GFF Customization Addon")]
    public Text raceText;
    public Text classText;
    public Text genderText;

    void Update_RaceClassGender(Player player)
    {
        raceText.text = player.race.ToString();
        classText.text = player.className;
        genderText.text = player.gender;
    }
}

