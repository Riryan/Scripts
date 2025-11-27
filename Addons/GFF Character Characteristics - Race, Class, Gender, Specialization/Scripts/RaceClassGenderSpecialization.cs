using Mirror;
using UnityEngine;


public partial class UsableItem
{
    [Header("GFF Races and classes")]
    public RaceList race;
    public int clases;
}

public partial class Player
{
    [Header("GFF Races and classes")]
    public ScriptableRacesData raceData;
    [SyncVar] public RaceList race;
    [SyncVar] public string gender;
    [SyncVar] public string specialisation_1 = "";
    [SyncVar] public string specialisation_2 = "";

}
