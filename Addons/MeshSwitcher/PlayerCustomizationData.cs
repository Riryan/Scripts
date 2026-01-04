using System;
using UnityEngine;

[Serializable]
public struct PlayerCustomizationData
{
    public int hair;
    public int beard;
    public int face;
    public int brows;
    public int ears;

    // ---------------------------------------------
    // Index-based access (runtime)
    // ---------------------------------------------
    public int GetByIndex(int index)
    {
        return index switch
        {
            0 => hair,
            1 => beard,
            2 => face,
            3 => brows,
            4 => ears,
            _ => 0
        };
    }
    public void SetByIndex(int index, int value)
    {
        switch (index)
        {
            case 0: hair = value; break;
            case 1: beard = value; break;
            case 2: face = value; break;
            case 3: brows = value; break;
            case 4: ears = value; break;
        }
    }

    // ---------------------------------------------
    // Serialization helpers (DB-safe)
    // ---------------------------------------------
    public static string Serialize(PlayerCustomizationData data)
    {
        return JsonUtility.ToJson(data);
    }

    public static PlayerCustomizationData Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonUtility.FromJson<PlayerCustomizationData>(json);
        }
        catch
        {
            // safety fallback for corrupted / legacy data
            return default;
        }
    }
}
