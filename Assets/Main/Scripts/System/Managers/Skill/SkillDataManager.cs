using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class SkillDataManager : DataManager<SkillDataManager>, IInitializable
{
    #region Constants
    private const string PREFAB_PATH = "SkillData/Prefabs";
    private const string ICON_PATH = "SkillData/Icons";
    private const string STAT_PATH = "SkillData/Stats";
    private const string JSON_PATH = "SkillData/Json";
    #endregion

    #region Fields
    private Dictionary<SkillID, SkillData> skillDatabase = new Dictionary<SkillID, SkillData>();
    private Dictionary<SkillID, Dictionary<int, SkillStatData>> statDatabase =
        new Dictionary<SkillID, Dictionary<int, SkillStatData>>();
    private Dictionary<SkillID, Dictionary<int, GameObject>> levelPrefabDatabase =
        new Dictionary<SkillID, Dictionary<int, GameObject>>();
    #endregion

    public override void Initialize()
    {
        try
        {
            LoadAllSkillData();
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing SkillDataManager: {e.Message}");
            isInitialized = false;
        }
    }

    private void LoadAllSkillData()
    {
        try
        {
            var jsonFiles = Resources.LoadAll<TextAsset>(JSON_PATH);

            foreach (var jsonAsset in jsonFiles)
            {
                try
                {
                    string fileName = jsonAsset.name;
                    string skillIdStr = fileName.Replace("_Data", "");

                    if (Enum.TryParse(skillIdStr, out SkillID skillId))
                    {
                        var skillData = JsonConvert.DeserializeObject<SkillData>(jsonAsset.text);
                        if (skillData != null)
                        {
                            LoadSkillResources(skillId, skillData);

                            LoadSkillStats(skillId, skillData.Type);

                            LoadLevelPrefabs(skillId, skillData);

                            skillDatabase[skillId] = skillData;
                            Debug.Log(
                                $"Successfully loaded skill: {skillData.Name} (ID: {skillId})"
                            );
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing JSON file {jsonAsset.name}: {e.Message}");
                }
            }

            Debug.Log($"[SkillDataManager] Total loaded skill data count : {skillDatabase.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SkillDataManager] Error in LoadAllSkillData: {e.Message}");
        }
    }

    private void LoadSkillResources(SkillID skillId, SkillData skillData)
    {
        string iconPath = $"{ICON_PATH}/{skillId}_Icon";
        string prefabPath = $"{PREFAB_PATH}/{skillId}_Prefab";

        skillData.Icon = Resources.Load<Sprite>(iconPath);
        skillData.Prefab = Resources.Load<GameObject>(prefabPath);

        if (skillData.Type == SkillType.Projectile)
        {
            string projectilePath = $"{PREFAB_PATH}/{skillId}_Projectile";
            skillData.ProjectilePrefab = Resources.Load<GameObject>(projectilePath);
        }
    }

    private void LoadSkillStats(SkillID skillId, SkillType type)
    {
        try
        {
            string statsFileName = type switch
            {
                SkillType.Projectile => "ProjectileSkillStats",
                SkillType.Area => "AreaSkillStats",
                SkillType.Passive => "PassiveSkillStats",
                _ => null,
            };

            if (statsFileName == null)
            {
                Debug.LogError($"Invalid skill type: {type}");
                return;
            }

            var loadedStats = CSVIO<SkillStatData>.LoadBulkData(statsFileName);
            var stats = loadedStats
                .Where(stat => stat.SkillID == skillId)
                .ToDictionary(stat => stat.Level, stat => stat);

            if (stats.Any())
            {
                statDatabase[skillId] = stats;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading stats for skill {skillId}: {e.Message}");
        }
    }

    private void LoadLevelPrefabs(SkillID skillId, SkillData skillData)
    {
        try
        {
            var stats = GetSkillStats(skillId, 1);
            if (stats == null)
                return;

            int maxLevel = stats.MaxSkillLevel;
            skillData.PrefabsByLevel = new GameObject[maxLevel];
            var levelPrefabs = new Dictionary<int, GameObject>();

            for (int i = 0; i < maxLevel; i++)
            {
                string levelPrefabName = $"{skillId}_Level_{i + 1}";
                var levelPrefab = Resources.Load<GameObject>($"{PREFAB_PATH}/{levelPrefabName}");

                if (levelPrefab != null)
                {
                    skillData.PrefabsByLevel[i] = levelPrefab;
                    levelPrefabs[i + 1] = levelPrefab;
                }
            }

            if (levelPrefabs.Any())
            {
                levelPrefabDatabase[skillId] = levelPrefabs;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading level prefabs for skill {skillId}: {e.Message}");
        }
    }

    private void SetStatValue(SkillStatData statData, string fieldName, string value)
    {
        try
        {
            var property = typeof(SkillStatData).GetProperty(
                char.ToUpper(fieldName[0]) + fieldName.Substring(1)
            );

            if (property != null)
            {
                object convertedValue;
                if (property.PropertyType == typeof(bool))
                {
                    convertedValue = value.ToLower() == "true" || value == "1";
                }
                else if (property.PropertyType == typeof(SkillID))
                {
                    if (!Enum.TryParse(value, out SkillID skillId))
                        throw new Exception($"Failed to parse SkillID: {value}");
                    convertedValue = skillId;
                }
                else if (property.PropertyType == typeof(ElementType))
                {
                    if (!Enum.TryParse(value, out ElementType elementType))
                        throw new Exception($"Failed to parse ElementType: {value}");
                    convertedValue = elementType;
                }
                else
                {
                    convertedValue = Convert.ChangeType(value, property.PropertyType);
                }

                property.SetValue(statData, convertedValue);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in SetStatValue for field '{fieldName}': {e.Message}");
        }
    }

    #region Public Methods
    public SkillData GetSkillData(SkillID id)
    {
        if (skillDatabase.TryGetValue(id, out var data))
            return data;
        return null;
    }

    public List<SkillData> GetAllSkillData()
    {
        return new List<SkillData>(skillDatabase.Values);
    }

    public SkillStatData GetSkillStats(SkillID id, int level)
    {
        if (
            statDatabase.TryGetValue(id, out var levelStats)
            && levelStats.TryGetValue(level, out var statData)
        )
        {
            return statData;
        }
        return null;
    }

    public GameObject GetLevelPrefab(SkillID skillId, int level)
    {
        if (
            levelPrefabDatabase.TryGetValue(skillId, out var levelPrefabs)
            && levelPrefabs.TryGetValue(level, out var prefab)
        )
        {
            return prefab;
        }
        return null;
    }

    public ISkillStat GetSkillStatsForLevel(SkillID id, int level, SkillType type)
    {
        var statData = GetSkillStats(id, level);
        if (statData != null)
        {
            return statData.CreateSkillStat(type);
        }
        return null;
    }
    #endregion
}
