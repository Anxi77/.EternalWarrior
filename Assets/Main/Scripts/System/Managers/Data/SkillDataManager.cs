using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class SkillList
{
    [SerializeField]
    public SkillID SkillID;

    [SerializeField]
    public SkillData SkillData;
}

public class SkillDataManager : Singleton<SkillDataManager>
{
    #region Constants
    private const string PREFAB_PATH = "Skills/Prefabs";
    private const string ICON_PATH = "Skills/Icons";
    private const string STAT_PATH = "Skills/Stats";
    private const string JSON_PATH = "Skills/Json";
    #endregion

    #region Field
    [SerializeField]
    private Dictionary<SkillID, SkillData> skillDatabase = new();
    public List<SkillList> skillList = new();

    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    #endregion

    #region Data Loading

    public IEnumerator Initialize()
    {
        float progress = 0f;
        yield return progress;
        yield return new WaitForSeconds(0.5f);
        LoadingManager.Instance.SetLoadingText("Initializing Skill Data...");

        yield return LoadSkillData();

        foreach (var skill in skillDatabase)
        {
            skillList.Add(new SkillList { SkillID = skill.Key, SkillData = skill.Value });
        }

        isInitialized = true;
    }

    private IEnumerator LoadSkillData()
    {
        float progress = 0.6f;

        List<TextAsset> jsonFiles = Resources.LoadAll<TextAsset>(JSON_PATH).ToList();

        Dictionary<SkillID, SkillData> tempDatabase = new Dictionary<SkillID, SkillData>();

        for (int i = 0; i < jsonFiles.Count; i++)
        {
            var jsonAsset = jsonFiles[i];
            string skillName = jsonAsset.name;

            if (Enum.TryParse(skillName, out SkillID skillId))
            {
                var skillData = JSONIO<SkillData>.LoadData(JSON_PATH, skillName);

                if (skillData != null)
                {
                    tempDatabase[skillId] = skillData;
                }
            }
            else
            {
                Logger.LogWarning(
                    typeof(SkillDataManager),
                    $"Failed to parse skill ID: {skillName}"
                );
            }

            progress = 0.6f + (float)i / jsonFiles.Count * 0.2f;
            yield return progress;
            LoadingManager.Instance.SetLoadingText($"Loading Skill Data... {progress * 100f:F0}%");
        }

        Dictionary<SkillType, List<SkillStatData>> statsByType =
            new Dictionary<SkillType, List<SkillStatData>>();

        statsByType[SkillType.Projectile] = LoadSkillStats(
            "ProjectileSkillStats",
            SkillType.Projectile
        );
        statsByType[SkillType.Area] = LoadSkillStats("AreaSkillStats", SkillType.Area);
        statsByType[SkillType.Passive] = LoadSkillStats("PassiveSkillStats", SkillType.Passive);

        int skillCount = tempDatabase.Count;
        int current = 0;

        foreach (var pair in tempDatabase)
        {
            SkillID skillId = pair.Key;
            SkillData skillData = pair.Value;

            LoadSkillResources(skillId, skillData);

            if (statsByType.TryGetValue(skillData.Type, out var statsForType))
            {
                ApplySkillStats(skillId, skillData, statsForType);
            }

            LoadLevelPrefabs(skillId, skillData);

            skillDatabase[skillId] = skillData;

            current++;
            progress = 0.8f + (float)current / skillCount * 0.2f;
            yield return progress;
            LoadingManager.Instance.SetLoadingText(
                $"Processing Skill Data... {progress * 100f:F0}%"
            );
        }
    }

    private List<SkillStatData> LoadSkillStats(string statsFileName, SkillType skillType)
    {
        List<string> skillFields = SkillStatFilters.GetFieldsForSkillType(skillType);
        return CSVIO<SkillStatData>.LoadBulkData(STAT_PATH, statsFileName, skillFields);
    }

    private void ApplySkillStats(SkillID skillId, SkillData skillData, List<SkillStatData> allStats)
    {
        var statsForSkill = allStats
            .Where(stat =>
                string.Equals(
                    stat.skillID.ToString(),
                    skillId.ToString(),
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .GroupBy(stat => stat.level)
            .ToDictionary(group => group.Key, group => group.First());

        if (statsForSkill.Count == 0)
        {
            Logger.LogWarning(
                typeof(SkillDataManager),
                $"[SkillDataManager] No stats data for skill {skillId}!"
            );
            return;
        }

        foreach (var levelStat in statsForSkill)
        {
            int level = levelStat.Key;
            var statData = levelStat.Value;

            var skillStat = statData.CreateSkillStat(skillData.Type);
            skillData.SetStatsForLevel(level, skillStat);
        }
    }

    private void LoadSkillResources(SkillID skillId, SkillData skillData)
    {
        string iconName = $"{skillId}_Icon";
        var iconPath = $"{ICON_PATH}/{skillId}/{iconName}";
        Logger.Log(typeof(SkillDataManager), $"IconPath: {iconPath}");
        skillData.Icon = Resources.Load<Sprite>(iconPath);
        if (skillData.Icon == null)
        {
            Logger.LogError(
                typeof(SkillDataManager),
                $"Failed to load icon for skill {skillId}: {iconPath}"
            );
        }

        string prefabName = $"{skillId}_Prefab";
        var prefabPath = $"{PREFAB_PATH}/{skillId}/{prefabName}";
        Logger.Log(typeof(SkillDataManager), $"PrefabPath: {prefabPath}");
        skillData.BasePrefab = Resources.Load<GameObject>(prefabPath);
        if (skillData.BasePrefab == null)
        {
            Logger.LogError(
                typeof(SkillDataManager),
                $"Failed to load prefab for skill {skillId}: {prefabPath}"
            );
        }

        if (skillData.Type == SkillType.Projectile)
        {
            string projectileName = $"{skillId}_Projectile";
            var projectilePath = $"{PREFAB_PATH}/{skillId}/{projectileName}";
            Logger.Log(typeof(SkillDataManager), $"ProjectilePath: {projectilePath}");
            skillData.ProjectilePrefab = Resources.Load<GameObject>(projectilePath);
            if (skillData.ProjectilePrefab == null)
            {
                Logger.LogError(
                    typeof(SkillDataManager),
                    $"Failed to load projectile prefab for skill {skillId}: {projectilePath}"
                );
            }
        }
    }

    private void LoadLevelPrefabs(SkillID skillId, SkillData skillData)
    {
        int maxLevel = 1;
        var stat = skillData.GetStatsForLevel(1);

        if (stat == null)
        {
            Logger.LogWarning(
                typeof(SkillDataManager),
                $"Skill {skillId} has null stats. skillData.Name: {skillData.Name}, Type: {skillData.Type}\n"
                    + $"skillData.ID: {skillData.ID}, skillData.Name: {skillData.Name}, skillData.Type: {skillData.Type}, skillData.Description: {skillData.Description}"
            );
            return;
        }

        if (stat.baseStat == null)
        {
            return;
        }

        if (stat.baseStat.maxSkillLevel <= 0)
        {
            Logger.LogWarning(
                typeof(SkillDataManager),
                $"[SkillDataManager] Skill {skillId} has invalid maxSkillLevel: {stat.baseStat.maxSkillLevel}"
            );
        }

        if (stat?.baseStat != null)
        {
            maxLevel = stat.baseStat.maxSkillLevel;
        }

        skillData.PrefabsByLevel = new GameObject[maxLevel];

        for (int i = 0; i < maxLevel; i++)
        {
            int level = i + 1;
            string prefabName = $"{skillId}_Level_{level}";
            var prefabPath = $"{PREFAB_PATH}/{skillId}/{prefabName}";

            skillData.PrefabsByLevel[i] = Resources.Load<GameObject>(prefabPath);
        }
    }
    #endregion

    #region Public Methods
    public SkillData GetData(SkillID id)
    {
        if (skillDatabase.TryGetValue(id, out var data))
            return data;
        return null;
    }

    public List<SkillData> GetAllData()
    {
        return new List<SkillData>(skillDatabase.Values);
    }

    public bool HasData(SkillID id)
    {
        return skillDatabase.ContainsKey(id);
    }

    public Dictionary<SkillID, SkillData> GetDatabase()
    {
        return new Dictionary<SkillID, SkillData>(skillDatabase);
    }

    public ISkillStat GetSkillStatsForLevel(SkillID id, int level)
    {
        if (skillDatabase.TryGetValue(id, out var skillData))
        {
            return skillData.GetStatsForLevel(level);
        }
        return null;
    }

    public GameObject GetLevelPrefab(SkillID skillId, int level)
    {
        if (
            skillDatabase.TryGetValue(skillId, out var skillData)
            && skillData.PrefabsByLevel != null
            && level > 0
            && level <= skillData.PrefabsByLevel.Length
        )
        {
            return skillData.PrefabsByLevel[level - 1];
        }
        return null;
    }
    #endregion
}
