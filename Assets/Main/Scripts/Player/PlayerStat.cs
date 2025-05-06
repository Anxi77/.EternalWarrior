using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    private Dictionary<StatType, float> baseStats = new();
    private Dictionary<StatType, float> currentStats = new();
    private Dictionary<SourceType, List<StatModifier>> activeModifiers = new();

    public event Action<StatType, float> OnStatChanged;

    public void Initialize()
    {
        InitializeStats();
    }

    private void InitializeStats()
    {
        var defaultData = PlayerDataManager.Instance.CurrentPlayerStatData;
        baseStats = new Dictionary<StatType, float>(defaultData.baseStats);
        currentStats = new Dictionary<StatType, float>(baseStats);
        LoadFromSaveData(defaultData);
    }

    public void LoadFromSaveData(StatData saveData)
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

        activeModifiers.Clear();

        foreach (var modifierData in saveData.permanentModifiers)
        {
            AddModifier(
                new StatModifier(
                    modifierData.Type,
                    modifierData.Source,
                    modifierData.IncreaseType,
                    modifierData.Value
                )
            );
        }

        RecalculateAllStats();
    }

    public StatData CreateSaveData()
    {
        var saveData = new StatData();

        foreach (var stat in currentStats)
        {
            saveData.baseStats[stat.Key] = GetBaseValue(stat.Key);
        }

        foreach (var modifierList in activeModifiers.Values)
        {
            foreach (var modifier in modifierList.Where(m => IsPermanentSource(m.Source)))
            {
                saveData.permanentModifiers.Add(
                    new StatModifier(
                        modifier.Type,
                        modifier.Source,
                        modifier.IncreaseType,
                        modifier.Value
                    )
                );
            }
        }

        return saveData;
    }

    public void AddModifier(StatModifier modifier)
    {
        if (!activeModifiers.ContainsKey(modifier.Source))
        {
            activeModifiers[modifier.Source] = new List<StatModifier>();
        }

        activeModifiers[modifier.Source].Add(modifier);
        RecalculateStats(modifier.Type);
    }

    public void RemoveModifier(StatModifier modifier)
    {
        if (activeModifiers.ContainsKey(modifier.Source))
        {
            var list = activeModifiers[modifier.Source];
            int removed = list.RemoveAll(m =>
                m.Type == modifier.Type
                && m.Source == modifier.Source
                && m.IncreaseType == modifier.IncreaseType
            );
            if (removed > 0)
            {
                RecalculateStats(modifier.Type);
            }
            else
            {
                Logger.LogWarning(
                    typeof(PlayerStat),
                    $"Modifier not found for removal: {modifier.Type} {modifier.Value}"
                );
            }
        }
    }

    private void RecalculateStats(StatType statType)
    {
        float baseValue = GetBaseValue(statType);
        float addValue = 0;
        float mulValue = 1f;

        foreach (var modifierList in activeModifiers.Values)
        {
            foreach (var modifier in modifierList.Where(m => m.Type == statType))
            {
                if (modifier.IncreaseType == IncreaseType.Flat)
                    addValue += modifier.Value;
                else
                    mulValue *= (1 + modifier.Value);
            }
        }

        float oldValue = currentStats.ContainsKey(statType) ? currentStats[statType] : 0f;
        float newValue = (baseValue + addValue) * mulValue;
        if (
            currentStats.ContainsKey(statType)
            && !Mathf.Approximately(currentStats[statType], newValue)
        )
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
        var stat = currentStats.TryGetValue(type, out float value) ? value : 0f;
        return stat;
    }

    private float GetBaseValue(StatType type)
    {
        return baseStats.TryGetValue(type, out float value) ? value : 0f;
    }

    private bool IsPermanentSource(SourceType source)
    {
        return source == SourceType.Weapon
            || source == SourceType.Armor
            || source == SourceType.Accessory
            || source == SourceType.Special;
    }

    public void SetCurrentHp(float value)
    {
        float maxHp = GetStat(StatType.MaxHp);
        float newHp = Mathf.Clamp(value, 0, maxHp);

        if (
            !currentStats.ContainsKey(StatType.CurrentHp)
            || !Mathf.Approximately(currentStats[StatType.CurrentHp], newHp)
        )
        {
            currentStats[StatType.CurrentHp] = newHp;
            OnStatChanged?.Invoke(StatType.CurrentHp, newHp);
        }
    }

    public void RemoveStatsBySource(SourceType source)
    {
        if (activeModifiers.ContainsKey(source))
        {
            var modifiers = new List<StatModifier>(activeModifiers[source]);
            foreach (var modifier in modifiers)
            {
                RemoveModifier(modifier);
            }
        }
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Debug"))
        {
            GUILayout.Label("Current Stats");
            foreach (var stat in currentStats)
            {
                GUILayout.Label($"{stat.Key}: {stat.Value}");
            }

            GUILayout.Label("Active Modifiers");
            foreach (var modifier in activeModifiers)
            {
                GUILayout.Label($"{modifier.Key}: {modifier.Value}");
            }
        }
    }

    public void UpdateStatsForLevel(int level)
    {
        // update stats for level
    }
}
