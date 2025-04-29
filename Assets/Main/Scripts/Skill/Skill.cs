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
            Logger.Log(typeof(Skill), $"Skill data is null or invalid for {gameObject.name}");
        }
    }

    protected bool IsValidSkillData(SkillData data)
    {
        if (data.Name == null)
        {
            Logger.LogError(typeof(Skill), $"Skill data is null for {gameObject.name}");
            return false;
        }
        if (data.Type == SkillType.None)
        {
            Logger.LogError(typeof(Skill), $"Skill type is None for {gameObject.name}");
            return false;
        }
        if (string.IsNullOrEmpty(data.Name))
        {
            Logger.LogError(typeof(Skill), $"Skill name is null or empty for {gameObject.name}");
            return false;
        }
        if (data.ID == SkillID.None)
        {
            Logger.LogError(typeof(Skill), $"Skill ID is None for {gameObject.name}");
            return false;
        }

        var currentStats = data.GetSkillStats();
        print(currentStats);
        if (currentStats == null)
        {
            Logger.LogError(typeof(Skill), $"Current stats are null for {gameObject.name}");
            return false;
        }
        if (currentStats.baseStat == null)
        {
            Logger.LogError(typeof(Skill), $"Base stat is null for {gameObject.name}");
            return false;
        }

        return true;
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
        Logger.Log(typeof(Skill), $"=== Starting SkillLevelUpdate for {skillData.Name} ===");
        Logger.Log(
            typeof(Skill),
            $"Current Level: {skillData.GetSkillStats().baseStat.skillLevel}, Attempting to upgrade to: {newLevel}"
        );

        if (newLevel <= 0)
        {
            Logger.LogError(typeof(Skill), $"Invalid level: {newLevel}");
            return false;
        }

        if (newLevel > skillData.GetSkillStats().baseStat.maxSkillLevel)
        {
            Logger.LogError(
                typeof(Skill),
                $"Attempted to upgrade {skillData.Name} beyond max level ({skillData.GetSkillStats().baseStat.maxSkillLevel})"
            );
            return false;
        }

        if (newLevel < skillData.GetSkillStats().baseStat.skillLevel)
        {
            Logger.LogError(
                typeof(Skill),
                $"Cannot downgrade skill level. Current: {skillData.GetSkillStats().baseStat.skillLevel}, Attempted: {newLevel}"
            );
            return false;
        }

        try
        {
            var currentStats = GetSkillData()?.GetSkillStats();
            Logger.Log(
                typeof(Skill),
                $"Current stats - Level: {currentStats?.baseStat?.skillLevel}, Damage: {currentStats?.baseStat?.damage}"
            );

            var newStats = skillData.GetStatsForLevel(newLevel);

            if (newStats == null)
            {
                Logger.LogError(typeof(Skill), "Failed to get new stats");
                return false;
            }

            Logger.Log(
                typeof(Skill),
                $"New stats received - Level: {newStats.baseStat?.skillLevel}, Damage: {newStats.baseStat?.damage}"
            );

            newStats.baseStat.skillLevel = newLevel;
            skillData.GetSkillStats().baseStat.skillLevel = newLevel;

            Logger.Log(typeof(Skill), "Setting new stats...");
            skillData.SetStatsForLevel(newLevel, newStats);

            Logger.Log(typeof(Skill), "Updating skill type stats...");
            UpdateSkillTypeStats(newStats);

            Logger.Log(
                typeof(Skill),
                $"=== Successfully completed SkillLevelUpdate for {skillData.Name} ==="
            );
            return true;
        }
        catch (System.Exception e)
        {
            Logger.LogError(
                typeof(Skill),
                $"Error in SkillLevelUpdate: {e.Message}\n{e.StackTrace}"
            );
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
            Logger.LogError(typeof(Skill), $"Skill data is missing for {GetType().Name}");
            return;
        }

        if (!IsValidSkillData(skillData))
        {
            Logger.LogError(typeof(Skill), $"Invalid skill data for {GetType().Name}");

            return;
        }

        Logger.Log(typeof(Skill), $"Validated skill data for {skillData.Name}");
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
