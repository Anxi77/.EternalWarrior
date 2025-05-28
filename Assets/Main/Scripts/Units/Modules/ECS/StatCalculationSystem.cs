using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 향상된 스탯 재계산 시스템 - Source Mapping 통합
/// </summary>
[BurstCompile]
public partial struct StatCalculationSystem : ISystem
{
    private EntityQuery statQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        statQuery = SystemAPI
            .QueryBuilder()
            .WithAll<StatComponent, StatModifierComponent>()
            .Build();

        state.RequireForUpdate(statQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var calculateJob = new StatCalculationJob();
        state.Dependency = calculateJob.ScheduleParallel(statQuery, state.Dependency);
    }
}

/// <summary>
/// 향상된 스탯 계산 Job - Source mapping 지원
/// </summary>
[BurstCompile]
public partial struct StatCalculationJob : IJobEntity
{
    public void Execute(ref StatComponent stats, ref StatModifierComponent modifiers)
    {
        if (stats.dirtyMask == 0)
            return;

        for (int i = 0; i < stats.dense.Length; i++)
        {
            if (!stats.IsDirty(i))
                continue;

            var statType = stats.dense[i];
            var baseValue = stats.GetStat(statType);

            var statModifiers = new FixedList128Bytes<StatModifierComponent.ModifierEntry>();
            modifiers.GetModifiersForStat(statType, ref statModifiers);

            var result = CalculateStat(baseValue, statModifiers, stats.globalScale);

            var normalized = result.NormalizeToScale(stats.globalScale);
            stats.values[i] = normalized.Value;

            stats.ClearDirty(i);
        }

        CalculateHP(ref stats, ref modifiers);
    }

    [BurstCompile]
    private ScaledInt CalculateStat(
        ScaledInt baseValue,
        FixedList128Bytes<StatModifierComponent.ModifierEntry> modifiers,
        byte globalScale
    )
    {
        ScaledInt flatBonus = ScaledInt.Zero;
        ScaledInt multiplyBonus = 1;

        for (int i = 0; i < modifiers.Length; i++)
        {
            var modifier = modifiers[i];
            var modifierValue = new ScaledInt(modifier.value, globalScale);

            switch (modifier.calcType)
            {
                case CalcType.Plus:
                    flatBonus += modifierValue;
                    break;
                case CalcType.Minus:
                    flatBonus -= modifierValue;
                    break;
                case CalcType.Percent:
                    multiplyBonus = multiplyBonus * (ScaledInt.One + modifierValue);
                    break;
            }
        }

        return (baseValue + flatBonus) * multiplyBonus;
    }

    [BurstCompile]
    private void CalculateHP(ref StatComponent stats, ref StatModifierComponent modifiers)
    {
        short maxHpIndex = stats.GetDenseIndex(StatType.MaxHp);
        short currentHpIndex = stats.GetDenseIndex(StatType.CurrentHp);

        if (maxHpIndex >= 0 && currentHpIndex >= 0 && stats.IsDirty(maxHpIndex))
        {
            var oldMaxHp = stats.GetStat(StatType.MaxHp);
            if (oldMaxHp.IsZero)
            {
                stats.values[currentHpIndex] = 0;
                return;
            }
            var currentHp = stats.GetStat(StatType.CurrentHp);

            var maxHpModifiers = new FixedList128Bytes<StatModifierComponent.ModifierEntry>();
            modifiers.GetModifiersForStat(StatType.MaxHp, ref maxHpModifiers);
            var newMaxHp = CalculateStat(oldMaxHp, maxHpModifiers, stats.globalScale);

            if (!oldMaxHp.IsZero)
            {
                float ratio = (currentHp / oldMaxHp).ToFloat();
                var newCurrentHp = newMaxHp * ratio;
                if (newCurrentHp < ScaledInt.Zero)
                {
                    newCurrentHp = ScaledInt.Zero;
                }

                stats.values[currentHpIndex] = newCurrentHp;
            }
            else
            {
                stats.values[currentHpIndex] = 0;
            }

            var normalizedMaxHp = newMaxHp.NormalizeToScale(stats.globalScale);
            stats.values[maxHpIndex] = normalizedMaxHp.Value;
        }
    }
}

/// <summary>
/// MonoBehaviour에서 ECS 시스템과 상호작용하는 헬퍼 (Source Mapping 통합 버전)
/// </summary>
public static class StatCalculationHelperEnhanced
{
    /// <summary>
    /// 특정 엔티티의 스탯을 즉시 재계산 (동기식)
    /// </summary>
    public static void RecalculateStatsImmediate(EntityManager entityManager, Entity entity)
    {
        if (
            !entityManager.HasComponent<StatComponent>(entity)
            || !entityManager.HasComponent<StatModifierComponent>(entity)
        )
            return;

        var stats = entityManager.GetComponentData<StatComponent>(entity);
        var modifiers = entityManager.GetComponentData<StatModifierComponent>(entity);

        // 수동으로 계산 실행
        var job = new StatCalculationJob();
        job.Execute(ref stats, ref modifiers);

        // 결과 저장
        entityManager.SetComponentData(entity, stats);
        entityManager.SetComponentData(entity, modifiers);
    }

    /// <summary>
    /// 스탯 수정자 추가 (ECS에서 직접, source tracking 포함)
    /// </summary>
    public static uint AddStat(
        EntityManager entityManager,
        Entity entity,
        StatType statType,
        CalcType calcType,
        ScaledInt value,
        object source = null
    )
    {
        if (
            !entityManager.HasComponent<StatComponent>(entity)
            || !entityManager.HasComponent<StatModifierComponent>(entity)
        )
            return 0;

        var stats = entityManager.GetComponentData<StatComponent>(entity);
        var modifiers = entityManager.GetComponentData<StatModifierComponent>(entity);

        // 스탯이 없으면 추가
        if (!stats.HasStat(statType))
        {
            stats.SetStat(statType, ScaledInt.Zero);
        }

        // 수정자 추가 (source tracking 자동)
        uint modifierId = modifiers.AddModifier(
            statType,
            calcType,
            value,
            stats.globalScale,
            source
        );

        if (modifierId > 0)
        {
            // 더티 플래그 설정
            short index = stats.GetDenseIndex(statType);
            if (index >= 0)
            {
                stats.SetDirty(index);
            }

            // 컴포넌트 데이터 저장
            entityManager.SetComponentData(entity, stats);
            entityManager.SetComponentData(entity, modifiers);
        }

        return modifierId;
    }

    /// <summary>
    /// 특정 source의 특정 stat modifier 제거
    /// </summary>
    public static uint RemoveStat(
        EntityManager entityManager,
        Entity entity,
        StatType statType,
        CalcType calcType,
        ScaledInt value,
        object source
    )
    {
        if (
            !entityManager.HasComponent<StatComponent>(entity)
            || !entityManager.HasComponent<StatModifierComponent>(entity)
            || source == null
        )
            return 0;
        var stats = entityManager.GetComponentData<StatComponent>(entity);
        var modifiers = entityManager.GetComponentData<StatModifierComponent>(entity); // 정확히 일치하는 modifier IDs 찾기 (value까지 포함)
        var idsToRemove = new FixedList64Bytes<uint>();
        modifiers.GetModifierIDs(
            source,
            statType,
            calcType,
            value,
            stats.globalScale,
            ref idsToRemove
        );

        if (idsToRemove.Length == 0)
            return 0;

        // 일괄 제거
        var nativeIds = new NativeArray<uint>(idsToRemove.Length, Allocator.Temp);
        try
        {
            for (int i = 0; i < idsToRemove.Length; i++)
            {
                nativeIds[i] = idsToRemove[i];
            }

            uint removedCount = modifiers.RemoveModifiersByIds(nativeIds, out var affectedStats);

            if (removedCount > 0)
            {
                // 영향받은 스탯들만 더티 마킹
                for (int i = 0; i < affectedStats.Length; i++)
                {
                    short index = stats.GetDenseIndex(affectedStats[i]);
                    if (index >= 0)
                    {
                        stats.SetDirty(index);
                    }
                }

                entityManager.SetComponentData(entity, stats);
                entityManager.SetComponentData(entity, modifiers);
            }

            return removedCount;
        }
        finally
        {
            nativeIds.Dispose();
        }
    }

    /// <summary>
    /// 특정 source의 모든 modifiers 제거
    /// </summary>
    public static uint RemoveAllModifiersFromSource(
        EntityManager entityManager,
        Entity entity,
        object source
    )
    {
        if (
            !entityManager.HasComponent<StatComponent>(entity)
            || !entityManager.HasComponent<StatModifierComponent>(entity)
            || source == null
        )
            return 0;

        var stats = entityManager.GetComponentData<StatComponent>(entity);
        var modifiers = entityManager.GetComponentData<StatModifierComponent>(entity);

        uint removedCount = modifiers.RemoveAllModifiersFromSource(source);

        if (removedCount > 0)
        {
            // 모든 스탯을 더티로 마킹 (어떤 스탯이 영향받았는지 모르므로)
            for (int i = 0; i < stats.dense.Length; i++)
            {
                stats.SetDirty(i);
            }

            entityManager.SetComponentData(entity, stats);
            entityManager.SetComponentData(entity, modifiers);
        }

        return removedCount;
    }

    /// <summary>
    /// 스탯 수정자 제거 (ID로)
    /// </summary>
    public static bool RemoveStatById(EntityManager entityManager, Entity entity, uint modifierId)
    {
        if (
            !entityManager.HasComponent<StatComponent>(entity)
            || !entityManager.HasComponent<StatModifierComponent>(entity)
        )
            return false;

        var stats = entityManager.GetComponentData<StatComponent>(entity);
        var modifiers = entityManager.GetComponentData<StatModifierComponent>(entity);

        bool removed = modifiers.RemoveModifierById(modifierId);

        if (removed)
        {
            // 모든 스탯을 더티로 마킹 (어떤 스탯이 영향받았는지 모르므로)
            for (int i = 0; i < stats.dense.Length; i++)
            {
                stats.SetDirty(i);
            }

            entityManager.SetComponentData(entity, stats);
            entityManager.SetComponentData(entity, modifiers);
        }

        return removed;
    }

    /// <summary>
    /// 여러 스탯 수정자를 ID로 일괄 제거
    /// </summary>
    public static uint RemoveStatsByIds(
        EntityManager entityManager,
        Entity entity,
        NativeArray<uint> modifierIds
    )
    {
        if (
            !entityManager.HasComponent<StatComponent>(entity)
            || !entityManager.HasComponent<StatModifierComponent>(entity)
        )
            return 0;

        var stats = entityManager.GetComponentData<StatComponent>(entity);
        var modifiers = entityManager.GetComponentData<StatModifierComponent>(entity);

        uint removedCount = modifiers.RemoveModifiersByIds(modifierIds, out var affectedStats);

        if (removedCount > 0)
        {
            // 영향받은 스탯들만 더티 마킹
            for (int i = 0; i < affectedStats.Length; i++)
            {
                short index = stats.GetDenseIndex(affectedStats[i]);
                if (index >= 0)
                {
                    stats.SetDirty(index);
                }
            }

            entityManager.SetComponentData(entity, stats);
            entityManager.SetComponentData(entity, modifiers);
        }

        return removedCount;
    }

    /// <summary>
    /// 특정 source의 정확히 일치하는 첫 번째 stat modifier 하나만 제거
    /// (같은 값이 여러 개 있을 때 하나씩 제거하고 싶을 때 사용)
    /// </summary>
    public static bool RemoveStatExactOne(
        EntityManager entityManager,
        Entity entity,
        StatType statType,
        CalcType calcType,
        ScaledInt value,
        object source
    )
    {
        if (
            !entityManager.HasComponent<StatComponent>(entity)
            || !entityManager.HasComponent<StatModifierComponent>(entity)
            || source == null
        )
            return false;

        var stats = entityManager.GetComponentData<StatComponent>(entity);
        var modifiers = entityManager.GetComponentData<StatModifierComponent>(entity); // 정확히 일치하는 첫 번째 modifier 제거
        bool removed = modifiers.RemoveFirstExactMatch(
            source,
            statType,
            calcType,
            value,
            stats.globalScale,
            out var affectedStat
        );

        if (removed)
        {
            // 영향받은 스탯 더티 마킹
            short index = stats.GetDenseIndex(affectedStat);
            if (index >= 0)
            {
                stats.SetDirty(index);
            }

            entityManager.SetComponentData(entity, stats);
            entityManager.SetComponentData(entity, modifiers);
        }

        return removed;
    }
}
