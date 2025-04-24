using System.Collections;
using UnityEngine;

public abstract class Skill : MonoBehaviour
{
    [SerializeField]
    public SkillData skillData;
    public MonoBehaviour Owner { get; private set; }
    protected bool isInitialized = false;
    public int currentLevel = 1;

    public virtual void Initialize()
    {
        InitializeSkillData();
    }

    protected virtual void InitializeSkillData()
    {
        if (skillData == null || !IsValidSkillData(skillData))
        {
            skillData = new SkillData();
            Debug.Log($"Skill data is null or invalid for {gameObject.name}");
        }
    }

    protected bool IsValidSkillData(SkillData data)
    {
        if (data.Name == null)
        {
            Debug.LogError($"Skill data is null for {gameObject.name}");
            return false;
        }
        if (data.Type == SkillType.None)
        {
            Debug.LogError($"Skill type is None for {gameObject.name}");
            return false;
        }
        if (string.IsNullOrEmpty(data.Name))
        {
            Debug.LogError($"Skill name is null or empty for {gameObject.name}");
            return false;
        }
        if (data.ID == SkillID.None)
        {
            Debug.LogError($"Skill ID is None for {gameObject.name}");
            return false;
        }

        var currentStats = data.GetSkillStats();
        print(currentStats);
        if (currentStats == null)
        {
            Debug.LogError($"Current stats are null for {gameObject.name}");
            return false;
        }
        if (currentStats.baseStat == null)
        {
            Debug.LogError($"Base stat is null for {gameObject.name}");
            return false;
        }

        return true;
    }

    protected T GetTypeStats<T>()
        where T : ISkillStat
    {
        if (skillData == null)
            return default(T);

        var currentStats = skillData.GetSkillStats();
        if (currentStats == null)
            return default(T);

        if (currentStats is T typedStats)
        {
            return typedStats;
        }
        Debug.LogWarning($"Current skill is not of type {typeof(T)}");
        return default(T);
    }

    public virtual void SetSkillData(SkillData data)
    {
        skillData = data;
    }

    public virtual SkillData GetSkillData()
    {
        return skillData;
    }

    public virtual bool SkillLevelUpdate(int newLevel)
    {
        Debug.Log($"=== Starting SkillLevelUpdate for {skillData.Name} ===");
        Debug.Log(
            $"Current Level: {skillData.GetSkillStats().baseStat.skillLevel}, Attempting to upgrade to: {newLevel}"
        );

        if (newLevel <= 0)
        {
            Debug.LogError($"Invalid level: {newLevel}");
            return false;
        }

        if (newLevel > skillData.GetSkillStats().baseStat.maxSkillLevel)
        {
            Debug.LogError(
                $"Attempted to upgrade {skillData.Name} beyond max level ({skillData.GetSkillStats().baseStat.maxSkillLevel})"
            );
            return false;
        }

        if (newLevel < skillData.GetSkillStats().baseStat.skillLevel)
        {
            Debug.LogError(
                $"Cannot downgrade skill level. Current: {skillData.GetSkillStats().baseStat.skillLevel}, Attempted: {newLevel}"
            );
            return false;
        }

        try
        {
            var currentStats = GetSkillData()?.GetSkillStats();
            Debug.Log(
                $"Current stats - Level: {currentStats?.baseStat?.skillLevel}, Damage: {currentStats?.baseStat?.damage}"
            );

            var newStats = skillData.GetStatsForLevel(newLevel);

            if (newStats == null)
            {
                Debug.LogError("Failed to get new stats");
                return false;
            }

            Debug.Log(
                $"New stats received - Level: {newStats.baseStat?.skillLevel}, Damage: {newStats.baseStat?.damage}"
            );

            newStats.baseStat.skillLevel = newLevel;
            skillData.GetSkillStats().baseStat.skillLevel = newLevel;

            Debug.Log("Setting new stats...");
            skillData.SetStatsForLevel(newLevel, newStats);

            Debug.Log("Updating skill type stats...");
            UpdateSkillTypeStats(newStats);

            Debug.Log($"=== Successfully completed SkillLevelUpdate for {skillData.Name} ===");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in SkillLevelUpdate: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    protected virtual void UpdateSkillTypeStats(ISkillStat newStats) { }

    public virtual string GetDetailedDescription()
    {
        return skillData?.Description ?? "No description available";
    }

    protected virtual void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        if (!SkillDataManager.Instance.IsInitialized)
            return;

        if (skillData == null)
        {
            Debug.LogError($"Skill data is missing for {GetType().Name}");
            return;
        }

        if (!IsValidSkillData(skillData))
        {
            Debug.LogError($"Invalid skill data for {GetType().Name}");

            return;
        }

        Debug.Log($"Validated skill data for {skillData.Name}");
    }

    public virtual void ApplyItemEffect(ISkillInteractionEffect effect)
    {
        effect.ModifySkillStats(this);
    }

    public virtual void RemoveItemEffect(ISkillInteractionEffect effect)
    {
        effect.ModifySkillStats(this);
    }

    public virtual void ModifyDamage(float multiplier)
    {
        if (skillData?.GetSkillStats()?.baseStat != null)
        {
            skillData.GetSkillStats().baseStat.damage *= multiplier;
        }
    }

    public virtual void ModifyCooldown(float multiplier) { }
}
