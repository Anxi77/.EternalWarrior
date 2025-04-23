using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillDataManager : Singleton<SkillDataManager>
{
    #region Constants
    private const string PREFAB_PATH = "SkillData/Prefabs";
    private const string ICON_PATH = "SkillData/Icons";
    private const string STAT_PATH = "SkillData/Stats";
    private const string JSON_PATH = "SkillData/Json";
    #endregion

    #region Fields
    private Dictionary<SkillID, SkillData> skillDatabase = new();
    private Dictionary<string, Sprite> iconCache = new();
    private Dictionary<string, GameObject> prefabCache = new();
    #endregion

    #region Data Loading

    public IEnumerator Initialize()
    {
        float progress = 0f;
        int steps;
        yield return progress;
        yield return new WaitForSeconds(0.5f);
        LoadingManager.Instance.SetLoadingText("Initializing Skill Data...");

        var icons = Resources.LoadAll<Sprite>(ICON_PATH);
        var prefabs = Resources.LoadAll<GameObject>(PREFAB_PATH);
        var jsonFiles = Resources.LoadAll<TextAsset>(JSON_PATH);

        steps = icons.Length + prefabs.Length + jsonFiles.Length;

        foreach (var icon in icons)
        {
            iconCache[icon.name] = icon;
            progress += 1f / steps;
            yield return progress;
            LoadingManager.Instance.SetLoadingText($"Loading Assets... {progress * 100f:F0}%");
        }

        foreach (var prefab in prefabs)
        {
            prefabCache[prefab.name] = prefab;

            progress += 1f / steps;
            yield return progress;
            LoadingManager.Instance.SetLoadingText($"Loading Assets... {progress * 100f:F0}%");

            foreach (var jsonAsset in jsonFiles)
            {
                string skillName = jsonAsset.name;

                if (Enum.TryParse(skillName, out SkillID skillId))
                {
                    var skillData = JSONIO<SkillData>.LoadData(jsonAsset.name);
                    if (skillData != null)
                    {
                        LoadSkillResources(skillId, skillData);

                        LoadSkillStats(skillId, skillData);

                        LoadLevelPrefabs(skillId, skillData);

                        skillDatabase[skillId] = skillData;

                        progress += 1f / steps;
                        yield return progress;
                        LoadingManager.Instance.SetLoadingText(
                            $"Loading Assets... {progress * 100f:F0}%"
                        );

                        Debug.Log($"스킬 로드 성공: {skillData.Name} (ID: {skillId})");
                    }
                }
                else
                {
                    Debug.LogWarning($"스킬 ID 파싱 실패: {skillName}");
                }
            }
        }
    }

    private void LoadSkillResources(SkillID skillId, SkillData skillData)
    {
        string iconName = $"{skillId}_Icon";
        if (iconCache.TryGetValue(iconName, out var icon))
        {
            skillData.Icon = icon;
        }

        string prefabName = $"{skillId}_Prefab";
        if (prefabCache.TryGetValue(prefabName, out var prefab))
        {
            skillData.BasePrefab = prefab;
        }

        if (skillData.Type == SkillType.Projectile)
        {
            string projectileName = $"{skillId}_Projectile";
            if (prefabCache.TryGetValue(projectileName, out var projectile))
            {
                skillData.ProjectilePrefab = projectile;
            }
        }
    }

    private void LoadSkillStats(SkillID skillId, SkillData skillData)
    {
        try
        {
            string statsFileName = skillData.Type switch
            {
                SkillType.Projectile => "ProjectileSkillStats",
                SkillType.Area => "AreaSkillStats",
                SkillType.Passive => "PassiveSkillStats",
                _ => null,
            };

            if (statsFileName == null)
            {
                Debug.LogError($"유효하지 않은 스킬 타입: {skillData.Type}");
                return;
            }

            var loadedStats = CSVIO<SkillStatData>.LoadBulkData(statsFileName);

            var stats = loadedStats
                .Where(stat => stat.SkillID == skillId)
                .ToDictionary(stat => stat.Level, stat => stat);

            foreach (var levelStat in stats)
            {
                int level = levelStat.Key;
                var statData = levelStat.Value;

                var skillStat = statData.CreateSkillStat(skillData.Type);

                skillData.SetStatsForLevel(level, skillStat);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"스킬 {skillId}의 스탯 로드 중 오류: {e.Message}");
        }
    }

    private void LoadLevelPrefabs(SkillID skillId, SkillData skillData)
    {
        try
        {
            int maxLevel = 1;
            var stat = skillData.GetStatsForLevel(1);
            if (stat?.baseStat != null)
            {
                maxLevel = stat.baseStat.maxSkillLevel;
            }

            if (maxLevel <= 0)
                return;

            skillData.PrefabsByLevel = new GameObject[maxLevel];

            for (int i = 0; i < maxLevel; i++)
            {
                int level = i + 1;
                string prefabName = $"{skillId}_Level_{level}";

                if (prefabCache.TryGetValue(prefabName, out var prefab))
                {
                    skillData.PrefabsByLevel[i] = prefab;
                }
                else if (i == 0)
                {
                    skillData.PrefabsByLevel[i] = skillData.BasePrefab;
                }
                else
                {
                    skillData.PrefabsByLevel[i] = skillData.PrefabsByLevel[i - 1];
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"스킬 {skillId}의 레벨 프리팹 로드 중 오류: {e.Message}");
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
