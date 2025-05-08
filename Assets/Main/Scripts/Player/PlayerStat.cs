using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    private Dictionary<StatType, float> baseStats = new();
    private Dictionary<StatType, float> currentStats = new();
    private List<StatModifier> activeModifiers = new();
    public event Action<StatType, float> OnStatChanged;

    public void Initialize(StatData saveData)
    {
        if (saveData == null)
        {
            Logger.LogWarning(typeof(PlayerStat), "Save Data is null");
            return;
        }

        baseStats = new Dictionary<StatType, float>(saveData.baseStats);
        currentStats = new Dictionary<StatType, float>(baseStats);

        float maxHp = baseStats.GetValueOrDefault(StatType.MaxHp);
        currentStats[StatType.CurrentHp] = maxHp;
    }

    public StatData CreateSaveData()
    {
        var saveData = new StatData();

        foreach (var stat in currentStats)
        {
            saveData.baseStats[stat.Key] = GetBaseValue(stat.Key);
        }

        return saveData;
    }

    public void AddModifier(StatModifier modifier)
    {
        activeModifiers.Add(modifier);
        RecalculateStats(modifier.Type);
    }

    public void RemoveModifier(StatModifier modifier)
    {
        activeModifiers.Remove(modifier);
        RecalculateStats(modifier.Type);
    }

    private void RecalculateStats(StatType statType)
    {
        float baseValue = GetBaseValue(statType);
        float addValue = 0;
        float mulValue = 1f;

        foreach (var modifier in activeModifiers.Where(m => m.Type == statType))
        {
            switch (modifier.IncreaseType)
            {
                case IncreaseType.Flat:
                    addValue += modifier.Value;
                    break;
                case IncreaseType.Multiply:
                    mulValue *= (1 + modifier.Value);
                    break;
            }
        }

        float oldValue = currentStats.ContainsKey(statType) ? currentStats[statType] : 0f;
        float newValue = (baseValue + addValue) * mulValue;
        if (!Mathf.Approximately(oldValue, newValue))
        {
            currentStats[statType] = newValue;
            OnStatChanged?.Invoke(statType, newValue);
        }
    }

    private void RecalculateAllStats()
    {
        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            RecalculateStats(statType);
        }
    }

    public float GetStat(StatType type)
    {
        return currentStats.TryGetValue(type, out float value) ? value : 0f;
    }

    private float GetBaseValue(StatType type)
    {
        return baseStats.TryGetValue(type, out float value) ? value : 0f;
    }

    public void SetCurrentHp(float value)
    {
        float maxHp = GetStat(StatType.MaxHp);
        float newHp = Mathf.Clamp(value, 0, maxHp);

        if (!Mathf.Approximately(currentStats[StatType.CurrentHp], newHp))
        {
            currentStats[StatType.CurrentHp] = newHp;
            OnStatChanged?.Invoke(StatType.CurrentHp, newHp);
        }
    }

    public void UpdateStatsForLevel(int level)
    {
        // update stats for level
    }
}
