using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSetting", menuName = "ScriptableObjects/DefaultPlayer")]
public class PlayerSetting : ScriptableObject
{
    public DefaultPlayerStat DefaultPlayerStat;

    public Dictionary<StatType, float> GetDefaultStat()
    {
        var stat = new Dictionary<StatType, float>()
        {
            { StatType.MaxHp, DefaultPlayerStat.MaxHp },
            { StatType.Damage, DefaultPlayerStat.Damage },
            { StatType.Defense, DefaultPlayerStat.Defense },
            { StatType.MoveSpeed, DefaultPlayerStat.MoveSpeed },
            { StatType.AttackSpeed, DefaultPlayerStat.AttackSpeed },
            { StatType.AttackRange, DefaultPlayerStat.AttackRange },
            { StatType.ExpCollectionRadius, DefaultPlayerStat.ExpCollectionRadius },
            { StatType.HpRegenRate, DefaultPlayerStat.HpRegenRate },
            { StatType.AttackRadius, DefaultPlayerStat.AttackRadius },
            { StatType.CriticalChance, DefaultPlayerStat.CriticalChance },
            { StatType.CriticalDamage, DefaultPlayerStat.CriticalDamage },
            { StatType.DodgeChance, DefaultPlayerStat.DodgeChance },
            { StatType.Luck, DefaultPlayerStat.Luck },
            { StatType.CurrentHp, DefaultPlayerStat.MaxHp },
            { StatType.LifeSteal, DefaultPlayerStat.LifeSteal },
        };
        return stat;
    }
}
