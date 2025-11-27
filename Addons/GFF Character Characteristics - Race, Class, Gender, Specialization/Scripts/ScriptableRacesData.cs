using System;
using System.Collections.Generic;
using UnityEngine;

public enum RaceList { none, Humans, Orcs, Undead, All, HumansAndOrcs, HumansAndUndead, OrcsAndUndead };

[Serializable]public class Race
{
    public string name;
    public RaceList race;
    public Color nameColor;
    public List<Classes> classes;
}

[Serializable]public class Classes
{
    public string name;
    public int hp;
    public int fp;
    public int sp;
    public int damage;
    public int defense;
    public string weapon;
    public string armor;

    public bool men;
    public bool girl;
}

[CreateAssetMenu(menuName = "uMMORPG Races", order = 999)]
public class ScriptableRacesData : ScriptableObject
{
    public Race[] races;

    public Color RaceColor(RaceList race)
    {
        for (int i = 0; i < races.Length; i++)
        {
            if (races[i].race == race) return races[i].nameColor;
        }
        return Color.white;
    }

    public int RaceIndex(RaceList race)
    {
        for (int i = 0; i < races.Length; i++)
        {
            if (races[i].race == race) return i;
        }
        return -1;
    }
}
