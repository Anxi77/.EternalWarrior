using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 완전히 ECS 통합된 StatSystem - Source mapping까지 ECS에서 처리
/// 이제 MonoBehaviour는 순수하게 ECS의 얇은 래퍼 역할만 수행
/// </summary>
public class StatSystem : MonoBehaviour
{
    #region Fields
    private Entity statEntity;
    private EntityManager entityManager;
    private World ecsWorld;

    [SerializeField]
    private bool enableDetailedLogs = false;
    #endregion

    #region Lifecycle
    public void Initialize(StatData statData)
    {
        ecsWorld = World.DefaultGameObjectInjectionWorld;
        if (ecsWorld == null)
        {
            Logger.LogError(typeof(StatSystem), "ECS World not found!");
            return;
        }

        entityManager = ecsWorld.EntityManager;
        statEntity = entityManager.CreateEntity();

        var stats = StatComponent.Create();
        var modifiers = StatModifierComponent.Create();

        entityManager.AddComponentData(statEntity, stats);
        entityManager.AddComponentData(statEntity, modifiers);

        if (statData?.baseStats != null)
        {
            foreach (var stat in statData.baseStats)
            {
                stats.SetStat(stat.Key, stat.Value);
            }
            entityManager.SetComponentData(statEntity, stats);
        }

        LogOperation("StatSystem initialized with fully integrated ECS backend");
    }

    public void Cleanup()
    {
        if (entityManager != null && entityManager.Exists(statEntity))
        {
            entityManager.DestroyEntity(statEntity);
        }
    }
    #endregion

    #region Core Stat Operations
    /// <summary>
    /// Get calculated stat value with automatic recalculation if dirty
    /// </summary>
    public ScaledInt GetStat(StatType type)
    {
        if (!IsValidEntity())
            return ScaledInt.Zero;

        var stats = entityManager.GetComponentData<StatComponent>(statEntity);

        // Auto-recalculate if dirty - handled efficiently by Job system
        if (stats.dirtyMask != 0)
        {
            StatCalculationHelperEnhanced.RecalculateStatsImmediate(entityManager, statEntity);
            stats = entityManager.GetComponentData<StatComponent>(statEntity);
        }

        return stats.GetStat(type);
    }

    /// <summary>
    /// Update base stat value
    /// </summary>
    public void UpdateBaseStat(StatType type, ScaledInt newValue)
    {
        if (!IsValidEntity())
            return;

        var stats = entityManager.GetComponentData<StatComponent>(statEntity);
        var oldValue = stats.GetStat(type);

        if (oldValue != newValue)
        {
            stats.SetStat(type, newValue);
            stats.dirtyMask |= (1u << (int)type); // Mark dirty for recalculation
            entityManager.SetComponentData(statEntity, stats);

            LogOperation($"Base stat updated: {type} = {newValue}");
        }
    }

    /// <summary>
    /// Add stat modifier from source - 완전히 ECS에서 처리 (source tracking 포함)
    /// </summary>
    public void AddStat(StatType type, CalcType calcType, ScaledInt value, object source)
    {
        if (!IsValidEntity())
            return;

        // 완전히 ECS 시스템에 위임 (source tracking 자동)
        uint modifierId = StatCalculationHelperEnhanced.AddStat(
            entityManager,
            statEntity,
            type,
            calcType,
            value,
            source
        );

        if (modifierId > 0)
        {
            LogOperation(
                $"Added modifier: {type} {calcType} {value} from {source?.GetType().Name} (ID: {modifierId})"
            );
        }
    }

    /// <summary>
    /// Remove specific stat modifier from source - 완전히 ECS에서 처리
    /// </summary>
    public void RemoveStat(StatType type, CalcType calcType, ScaledInt value, object source)
    {
        if (!IsValidEntity() || source == null)
            return;

        // 완전히 ECS 시스템에 위임
        uint removedCount = StatCalculationHelperEnhanced.RemoveStat(
            entityManager,
            statEntity,
            type,
            calcType,
            value,
            source
        );

        if (removedCount > 0)
        {
            LogOperation(
                $"Removed {removedCount} modifiers: {type} {calcType} from {source?.GetType().Name}"
            );
        }
    }

    /// <summary>
    /// Remove all modifiers from a source
    /// </summary>
    public void RemoveStatFromSource(object source)
    {
        if (!IsValidEntity() || source == null)
            return;

        uint removedCount = StatCalculationHelperEnhanced.RemoveAllModifiersFromSource(
            entityManager,
            statEntity,
            source
        );

        if (removedCount > 0)
        {
            LogOperation($"Removed {removedCount} modifiers from source: {source?.GetType().Name}");
        }
    }

    /// <summary>
    /// Remove all modifiers from all sources
    /// </summary>
    public void RemoveAllModifiers()
    {
        if (!IsValidEntity())
            return;

        // Reset modifier component
        var modifiers = StatModifierComponent.Create();
        entityManager.SetComponentData(statEntity, modifiers);

        // Mark all stats dirty
        var stats = entityManager.GetComponentData<StatComponent>(statEntity);
        stats.dirtyMask = uint.MaxValue;
        entityManager.SetComponentData(statEntity, stats);

        LogOperation("Removed all modifiers");
    }
    #endregion

    #region Data Management
    public StatData GetSaveData()
    {
        if (!IsValidEntity())
            return new StatData();

        var saveData = new StatData();
        var stats = entityManager.GetComponentData<StatComponent>(statEntity);

        for (int i = 0; i < stats.dense.Length; i++)
        {
            var statType = stats.dense[i];
            var value = stats.GetStat(statType);
            saveData.baseStats.Add(statType, value);
        }

        return saveData;
    }
    #endregion

    #region Validation & Logging
    private bool IsValidEntity()
    {
        return entityManager != null && entityManager.Exists(statEntity);
    }

    private void LogOperation(string message)
    {
        if (enableDetailedLogs)
        {
            Logger.Log(typeof(StatSystem), message);
        }
    }

    #endregion
}
