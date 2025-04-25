using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
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
    private const string PREFAB_PATH = "SkillData/Prefabs";
    private const string ICON_PATH = "SkillData/Icons";
    private const string STAT_PATH = "SkillData/Stats";
    private const string JSON_PATH = "SkillData/Json";
    #endregion

    #region Field
    [SerializeField]
    private Dictionary<SkillID, SkillData> skillDatabase = new();
    public List<SkillList> skillList = new();

    private Dictionary<string, Sprite> iconCache = new();
    private Dictionary<string, GameObject> prefabCache = new();

    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    #endregion

    #region Data Loading

    public IEnumerator Initialize()
    {
        float progress = 0f;
        yield return progress;
        yield return new WaitForSeconds(0.5f);
        LoadingManager.Instance.SetLoadingText("스킬 데이터 초기화 중...");

        yield return LoadResources();

        yield return LoadSkillData();

        foreach (var skill in skillDatabase)
        {
            Debug.Log($"스킬 로드 성공: {skill.Value.Name} (ID: {skill.Key})");
            skillList.Add(new SkillList { SkillID = skill.Key, SkillData = skill.Value });
        }

        isInitialized = true;
    }

    private IEnumerator LoadResources()
    {
        float progress = 0f;

        List<Sprite> icons = Resources.LoadAll<Sprite>(ICON_PATH).ToList();
        for (int i = 0; i < icons.Count; i++)
        {
            iconCache[icons[i].name] = icons[i];
            progress = (float)i / icons.Count * 0.3f;
            yield return progress;
            LoadingManager.Instance.SetLoadingText($"Loading Icon... {progress * 100f:F0}%");
        }

        List<GameObject> prefabs = Resources.LoadAll<GameObject>(PREFAB_PATH).ToList();
        for (int i = 0; i < prefabs.Count; i++)
        {
            var prefab = prefabs[i];
            prefabCache[prefab.name] = prefab;
            progress = 0.3f + (float)i / prefabs.Count * 0.3f;
            yield return progress;
            LoadingManager.Instance.SetLoadingText($"Loading Prefab... {progress * 100f:F0}%");
        }
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
                Debug.Log($"[SkillDataManager] Loading Skill Data: {skillName}");
                var skillData = JSONIO<SkillData>.LoadData(JSON_PATH, skillName);

                if (skillData != null)
                {
                    tempDatabase[skillId] = skillData;
                }
            }
            else
            {
                Debug.LogWarning($"스킬 ID 파싱 실패: {skillName}");
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

        Debug.Log(
            $"[SkillDataManager] 스킬 {skillId}에 대한 스탯 데이터 수: {statsForSkill.Count}"
        );

        if (statsForSkill.Count == 0)
        {
            Debug.LogWarning($"[SkillDataManager] 스킬 {skillId}에 대한 스탯 데이터가 없습니다!");
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

    private void LoadLevelPrefabs(SkillID skillId, SkillData skillData)
    {
        try
        {
            int maxLevel = 1;
            var stat = skillData.GetStatsForLevel(1);

            if (stat == null)
            {
                Debug.LogWarning(
                    $"[SkillDataManager] 스킬 {skillId}의 스탯이 null입니다. skillData.Name: {skillData.Name}, Type: {skillData.Type}"
                );
                Debug.LogWarning(
                    $"[SkillDataManager] 스킬 기본 정보: ID: {skillData.ID}, Name: {skillData.Name}, Type: {skillData.Type}, Description: {skillData.Description}"
                );
                return;
            }

            if (stat.baseStat == null)
            {
                return;
            }

            if (stat.baseStat.maxSkillLevel <= 0)
            {
                Debug.LogWarning(
                    $"[SkillDataManager] 스킬 {skillId}의 maxSkillLevel이 유효하지 않습니다: {stat.baseStat.maxSkillLevel}"
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
