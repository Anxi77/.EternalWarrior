using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class SkillLevelData
{
    public int level;

    public BaseSkillStat baseSkillStat;

    [SerializeReference]
    public ISkillStat skillStat;

    public ISkillStat GetStats(SkillType type)
    {
        switch (type)
        {
            case SkillType.Projectile:
                baseSkillStat = skillStat.baseStat;
                return skillStat as ProjectileSkillStat;
            case SkillType.Area:
                baseSkillStat = skillStat.baseStat;
                return skillStat as AreaSkillStat;
            case SkillType.Passive:
                baseSkillStat = skillStat.baseStat;
                return skillStat as PassiveSkillStat;
            default:
                return null;
        }
    }

    public void SetStats(ISkillStat stats)
    {
        if (stats == null)
            return;

        switch (stats)
        {
            case ProjectileSkillStat projectile:
                skillStat = new ProjectileSkillStat(projectile);
                baseSkillStat = skillStat.baseStat;
                break;
            case AreaSkillStat area:
                skillStat = new AreaSkillStat(area);
                baseSkillStat = skillStat.baseStat;
                break;
            case PassiveSkillStat passive:
                skillStat = new PassiveSkillStat(passive);
                baseSkillStat = skillStat.baseStat;
                break;
        }
    }
}

[Serializable]
public class SkillData : ICloneable
{
    #region Properties
    public SkillID ID;
    public string Name;
    public string Description;
    public SkillType Type;
    public ElementType Element;
    public int Tier;
    public string[] Tags;

    [JsonIgnore]
    public GameObject BasePrefab;

    [JsonIgnore]
    public List<SkillLevelData> levelDataList = new();

    [JsonIgnore]
    public Sprite Icon;

    [JsonIgnore]
    public GameObject ProjectilePrefab;

    [JsonIgnore]
    public GameObject[] PrefabsByLevel;

    [Header("Current Stat")]
    public BaseSkillStat currentBaseSkillStat;

    [SerializeReference]
    public ISkillStat currentSkillStat;

    #endregion

    public SkillData()
    {
        ID = SkillID.None;
        Name = "None";
        Description = "None";
        Type = SkillType.None;
        Element = ElementType.None;
        Tier = 0;
        Tags = new string[0];
        levelDataList = new List<SkillLevelData>();
        PrefabsByLevel = new GameObject[0];

        InitializeSkillStat();
    }

    private void InitializeSkillStat()
    {
        currentSkillStat = CreateDefaultStats();
    }

    public ISkillStat GetStatsForLevel(int level)
    {
        var stats = levelDataList.FirstOrDefault(x => x.level == level)?.GetStats(Type);

        if (stats != null)
            return stats;

        var defaultStats = CreateDefaultStats();
        SetStatsForLevel(level, defaultStats);
        return defaultStats;
    }

    public void SetStatsForLevel(int level, ISkillStat stats)
    {
        if (stats?.baseStat == null)
        {
            Debug.LogError("Attempting to set null stats");
            return;
        }

        try
        {
            UpdateCurrentStat(stats);

            var existingData = levelDataList.FirstOrDefault(x => x.level == level);

            if (existingData != null)
            {
                existingData.SetStats(stats);
            }
            else
            {
                var newLevelData = new SkillLevelData { level = level };
                newLevelData.SetStats(stats);
                levelDataList.Add(newLevelData);
            }

            Debug.Log($"Successfully set stats for level {level}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error setting stats: {e.Message}");
        }
    }

    private void UpdateCurrentStat(ISkillStat stats)
    {
        switch (stats)
        {
            case ProjectileSkillStat projectileStats:
                currentSkillStat = new ProjectileSkillStat(projectileStats);
                break;
            case AreaSkillStat areaStats:
                currentSkillStat = new AreaSkillStat(areaStats);
                break;
            case PassiveSkillStat passiveStats:
                currentSkillStat = new PassiveSkillStat(passiveStats);
                break;
            default:
                Debug.LogWarning($"Unknown skill stat type: {stats.GetType()}");
                break;
        }
    }

    public ISkillStat GetSkillStats()
    {
        return currentSkillStat;
    }

    private ISkillStat CreateDefaultStats()
    {
        switch (Type)
        {
            case SkillType.Projectile:
                return new ProjectileSkillStat();
            case SkillType.Area:
                return new AreaSkillStat();
            case SkillType.Passive:
                return new PassiveSkillStat();
            default:
                return null;
        }
    }

    #region ICloneable
    public object Clone()
    {
        return new SkillData
        {
            ID = this.ID,
            Name = this.Name,
            Description = this.Description,
            Type = this.Type,
            Element = this.Element,
            Tier = this.Tier,
            Tags = (string[])this.Tags?.Clone(),
        };
    }
    #endregion
}
