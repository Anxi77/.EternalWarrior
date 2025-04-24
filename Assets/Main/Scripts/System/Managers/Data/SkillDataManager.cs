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
        int steps;
        yield return progress;
        yield return new WaitForSeconds(0.5f);
        LoadingManager.Instance.SetLoadingText("Initializing Skill Data...");

        List<Sprite> icons = Resources.LoadAll<Sprite>(ICON_PATH).ToList();
        List<GameObject> prefabs = Resources.LoadAll<GameObject>(PREFAB_PATH).ToList();
        List<TextAsset> jsonFiles = Resources.LoadAll<TextAsset>(JSON_PATH).ToList();

        steps = icons.Count + prefabs.Count + jsonFiles.Count;

        foreach (var icon in icons)
        {
            iconCache[icon.name] = icon;
            progress += 1f / steps;
            yield return progress;
            LoadingManager.Instance.SetLoadingText($"Loading Assets... {progress * 100f:F0}%");
        }

        List<GameObject> basePrefabs = new();
        foreach (var prefab in prefabs)
        {
            if (!prefab.name.Contains("Level"))
            {
                basePrefabs.Add(prefab);
            }
            else
            {
                prefabCache[prefab.name] = prefab;
            }
        }
        foreach (var prefab in basePrefabs)
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
                    Debug.Log($"[skilldatamanager] Loading SkillData: {skillName}");

                    var skillData = JSONIO<SkillData>.LoadData(JSON_PATH, jsonAsset.name);

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
                    }
                }
                else
                {
                    Debug.LogWarning($"스킬 ID 파싱 실패: {skillName}");
                }
            }
        }

        foreach (var skill in skillDatabase)
        {
            Debug.Log($"스킬 로드 성공: {skill.Value.Name} (ID: {skill.Key})");
            skillList.Add(new SkillList { SkillID = skill.Key, SkillData = skill.Value });
        }

        isInitialized = true;
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
        string statsFileName = skillData.Type switch
        {
            SkillType.Projectile => "ProjectileSkillStats",
            SkillType.Area => "AreaSkillStats",
            SkillType.Passive => "PassiveSkillStats",
            _ => null,
        };

        if (statsFileName == null)
        {
            Debug.LogError($"[SkillDataManager] 유효하지 않은 스킬 타입: {skillData.Type}");
            return;
        }

        var loadedStats = CSVIO<SkillStatData>.LoadBulkData(STAT_PATH, statsFileName);
        foreach (var stat in loadedStats)
        {
            Debug.Log($"[SkillDataManager] {stat.SkillID}, {stat.Level}, {stat.MaxSkillLevel}");
        }

        var stats = loadedStats
            .Where(stat => stat.SkillID == skillId)
            .ToDictionary(stat => stat.Level, stat => stat);

        Debug.Log($"[SkillDataManager] 스킬 {skillId}에 대한 스탯 데이터 수: {stats.Count}");

        if (stats.Count == 0)
        {
            Debug.LogError($"[SkillDataManager] 스킬 {skillId}에 대한 스탯 데이터가 없습니다!");
            return;
        }

        foreach (var levelStat in stats)
        {
            int level = levelStat.Key;
            var statData = levelStat.Value;

            Debug.Log($"[SkillDataManager] 스탯 상세 정보 - SkillID: {skillId}, Level: {level}");
            // statData 필드 내용 출력 (주요 필드들만)

            var skillStat = statData.CreateSkillStat(skillData.Type);

            // 생성된 스킬 스탯 확인
            Debug.Log(
                $"[SkillDataManager] 생성된 스킬 스탯 타입: {skillStat.GetType().Name}, baseStat 존재: {skillStat.baseStat != null}"
            );

            if (skillStat.baseStat == null)
            {
                Debug.LogError(
                    $"[SkillDataManager] CreateSkillStat()에서 baseStat이 null로 생성됨! SkillID: {skillId}, Level: {level}"
                );
            }

            skillData.SetStatsForLevel(level, skillStat);

            // 설정 후 다시 확인
            var checkStat = skillData.GetStatsForLevel(level);
            Debug.Log(
                $"[SkillDataManager] 설정 후 스탯 확인 - null 여부: {checkStat == null}, baseStat 존재: {checkStat?.baseStat != null}"
            );
        }
    }

    private void LoadLevelPrefabs(SkillID skillId, SkillData skillData)
    {
        try
        {
            int maxLevel = 1;
            var stat = skillData.GetStatsForLevel(1);

            Debug.Log(
                $"[SkillDataManager] 스킬 {skillId} 스탯 로드 점검: stat 객체 존재 여부: {stat != null}"
            );

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

            Debug.Log(
                $"[SkillDataManager] 스킬 {skillId} 스탯 로드 점검: baseStat 존재 여부: {stat.baseStat != null}"
            );

            if (stat.baseStat == null)
            {
                Debug.LogError(
                    $"[SkillDataManager] 스킬 {skillId}의 baseStat이 null입니다. 스탯 타입: {stat.GetType().Name}"
                );
                return;
            }

            Debug.Log(
                $"[SkillDataManager] 스킬 {skillId} 스탯 로드 점검: maxSkillLevel 값: {stat.baseStat.maxSkillLevel}"
            );

            if (stat.baseStat.maxSkillLevel <= 0)
            {
                Debug.LogWarning(
                    $"[SkillDataManager] 스킬 {skillId}의 maxSkillLevel이 유효하지 않습니다: {stat.baseStat.maxSkillLevel}"
                );
            }

            if (stat?.baseStat != null)
            {
                Debug.Log(
                    $"[SkillDataManager] 스킬 {skillId} 레벨 최대 값: {stat.baseStat.maxSkillLevel}"
                );
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
