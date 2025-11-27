

using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Intelligence : PlayerAttribute, IManaBonus
{
    
    public float manaBonusPercentPerPoint = 0.01f;

    public int GetManaBonus(int baseMana) =>
        Convert.ToInt32(baseMana * (value * manaBonusPercentPerPoint));

    public int GetManaRecoveryBonus() => 0;
}
