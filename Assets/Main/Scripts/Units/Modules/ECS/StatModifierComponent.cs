using System;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// 스탯 수정자 관리 컴포넌트 (Source Mapping 통합)
/// Source tracking을 위해 기존 컴포넌트를 확장하여 더 효율적인 구조로 개선
/// </summary>
public struct StatModifierComponent : IComponentData
{
    // 4096바이트로 약 160개 수정자 가능 (ModifierEntry는 약 25바이트)
    public FixedList4096Bytes<ModifierEntry> modifiers;
    public uint activeMask; // 32비트로 32개 수정자 활성 상태 (첫 번째 32개만)
    public uint nextId; // 수정자 ID 생성용

    // Capacity management
    public byte capacityWarningThreshold; // 용량 경고 임계값 (기본값: 85%)
    public byte autoCompactThreshold; // 자동 정리 임계값 (기본값: 90%)
    public byte isOverflowMode; // 0 = 정상, 1 = 오버플로우 모드

    // Source mapping: source hash -> modifier IDs
    public FixedList512Bytes<SourceMappingEntry> sourceMappings;

    [Serializable]
    public struct ModifierEntry
    {
        public uint id; // 고유 ID
        public StatType statType;
        public CalcType calcType;
        public long value; // ScaledInt의 value 부분
        public byte isActive; // 0 = 비활성, 1 = 활성
        public int sourceHash; // source object의 hash code

        public bool IsActive => isActive == 1;
    }

    [Serializable]
    public struct SourceMappingEntry
    {
        public int sourceHash;
        public FixedList64Bytes<uint> modifierIds; // 해당 source의 modifier ID들
        public byte isActive; // 0 = 비활성, 1 = 활성

        public bool IsActive => isActive == 1;
    }

    /// <summary>
    /// 새 수정자 추가 (source tracking 포함)
    /// </summary>
    public uint AddModifier(
        StatType statType,
        CalcType calcType,
        ScaledInt value,
        byte globalScale,
        object source = null
    )
    {
        var normalized = value.NormalizeToScale(globalScale);
        uint id = GenerateNewId();
        int sourceHash = source?.GetHashCode() ?? 0;

        var modifier = new ModifierEntry
        {
            id = id,
            statType = statType,
            calcType = calcType,
            value = normalized.Value,
            isActive = 1,
            sourceHash = sourceHash,
        };

        // 빈 슬롯 찾기
        for (int i = 0; i < modifiers.Length; i++)
        {
            if (!modifiers[i].IsActive)
            {
                modifiers[i] = modifier;
                SetActive(i);

                // Source mapping 업데이트
                if (source != null)
                {
                    AddToSourceMapping(sourceHash, id);
                }

                return id;
            }
        } // 새 슬롯 추가
        if (modifiers.Length < modifiers.Capacity)
        {
            modifiers.Add(modifier);
            SetActive(modifiers.Length - 1);

            // Source mapping 업데이트
            if (source != null)
            {
                AddToSourceMapping(sourceHash, id);
            }

            // Capacity 체크 및 오버플로우 처리
            CheckAndHandleCapacity();

            return id;
        }

        // Capacity 초과 시 자동 정리 시도
        if (TryAutoCompact())
        {
            // 정리 후 다시 시도
            return AddModifier(statType, calcType, value, globalScale, source);
        }

        // 여전히 공간이 없으면 오버플로우 모드 활성화
        isOverflowMode = 1;
        return 0; // 실패 - 오버플로우 컴포넌트 사용 필요
    }

    /// <summary>
    /// 수정자 제거 (ID로) - source mapping도 자동 업데이트
    /// </summary>
    public bool RemoveModifierById(uint id)
    {
        for (int i = 0; i < modifiers.Length; i++)
        {
            var modifier = modifiers[i];
            if (modifier.IsActive && modifier.id == id)
            {
                // Source mapping에서 제거
                if (modifier.sourceHash != 0)
                {
                    RemoveFromSourceMapping(modifier.sourceHash, id);
                }

                modifier.isActive = 0;
                modifiers[i] = modifier;
                SetInactive(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 특정 source의 모든 modifier ID 가져오기
    /// </summary>
    public void GetModifierIdsBySource(object source, ref FixedList64Bytes<uint> result)
    {
        result.Clear();
        if (source == null)
            return;

        int sourceHash = source.GetHashCode();

        for (int i = 0; i < sourceMappings.Length; i++)
        {
            var mapping = sourceMappings[i];
            if (mapping.IsActive && mapping.sourceHash == sourceHash)
            {
                // 모든 modifier IDs 복사
                for (int j = 0; j < mapping.modifierIds.Length; j++)
                {
                    if (result.Length < result.Capacity)
                    {
                        result.Add(mapping.modifierIds[j]);
                    }
                }
                break;
            }
        }
    }

    /// <summary>
    /// 특정 source의 특정 stat/calc type/value와 정확히 일치하는 modifier IDs 가져오기
    /// </summary>
    public void GetModifierIDs(
        object source,
        StatType statType,
        CalcType calcType,
        ScaledInt value,
        byte globalScale,
        ref FixedList64Bytes<uint> result
    )
    {
        result.Clear();
        if (source == null)
            return;

        var normalizedValue = value.NormalizeToScale(globalScale);
        int sourceHash = source.GetHashCode();

        // 직접 순회로 O(n) 복잡도로 최적화
        for (int i = 0; i < modifiers.Length; i++)
        {
            var modifier = modifiers[i];
            if (
                modifier.IsActive
                && modifier.sourceHash == sourceHash
                && modifier.statType == statType
                && modifier.calcType == calcType
                && modifier.value == normalizedValue.Value
            )
            {
                if (result.Length < result.Capacity)
                {
                    result.Add(modifier.id);
                }
            }
        }
    }

    /// <summary>
    /// 특정 source의 모든 modifiers 제거
    /// </summary>
    public uint RemoveAllModifiersFromSource(object source)
    {
        if (source == null)
            return 0;

        var sourceIds = new FixedList64Bytes<uint>();
        GetModifierIdsBySource(source, ref sourceIds);

        if (sourceIds.Length == 0)
            return 0;

        NativeArray<uint> nativeIds = new NativeArray<uint>(sourceIds.Length, Allocator.Temp);
        for (int i = 0; i < sourceIds.Length; i++)
        {
            nativeIds[i] = sourceIds[i];
        }

        uint removedCount;
        try
        {
            removedCount = RemoveModifiersByIds(nativeIds, out _);
        }
        finally
        {
            nativeIds.Dispose();
        }

        return removedCount;
    }

    /// <summary>
    /// 여러 수정자를 ID로 일괄 제거하고 영향받은 스탯 타입들 반환
    /// </summary>
    public uint RemoveModifiersByIds(
        NativeArray<uint> idsToRemove,
        out FixedList64Bytes<StatType> affectedStats
    )
    {
        affectedStats = new FixedList64Bytes<StatType>();
        uint removedCount = 0;

        for (int i = 0; i < modifiers.Length; i++)
        {
            var modifier = modifiers[i];
            if (!modifier.IsActive)
                continue;

            // ID가 제거 목록에 있는지 확인
            for (int j = 0; j < idsToRemove.Length; j++)
            {
                if (modifier.id == idsToRemove[j])
                {
                    // 영향받은 스탯 타입 추가 (중복 방지)
                    bool alreadyAdded = false;
                    for (int k = 0; k < affectedStats.Length; k++)
                    {
                        if (affectedStats[k] == modifier.statType)
                        {
                            alreadyAdded = true;
                            break;
                        }
                    }

                    if (!alreadyAdded && affectedStats.Length < affectedStats.Capacity)
                    {
                        affectedStats.Add(modifier.statType);
                    }

                    // Source mapping에서 제거
                    if (modifier.sourceHash != 0)
                    {
                        RemoveFromSourceMapping(modifier.sourceHash, modifier.id);
                    }

                    // 수정자 제거
                    modifier.isActive = 0;
                    modifiers[i] = modifier;
                    SetInactive(i);
                    removedCount++;
                    break;
                }
            }
        }

        return removedCount;
    }

    /// <summary>
    /// 특정 스탯 타입의 모든 수정자 가져오기
    /// </summary>
    public void GetModifiersForStat(StatType statType, ref FixedList128Bytes<ModifierEntry> result)
    {
        result.Clear();

        for (int i = 0; i < modifiers.Length; i++)
        {
            var modifier = modifiers[i];
            if (modifier.IsActive && modifier.statType == statType)
            {
                if (result.Length < result.Capacity)
                {
                    result.Add(modifier);
                }
            }
        }
    }

    /// <summary>
    /// 활성 수정자 수 조회
    /// </summary>
    public int GetActiveModifierCount()
    {
        int count = 0;
        for (int i = 0; i < modifiers.Length; i++)
        {
            if (modifiers[i].IsActive)
                count++;
        }
        return count;
    }

    /// <summary>
    /// 메모리 정리 (비활성 수정자 실제 제거)
    /// </summary>
    public void Compact()
    {
        var compactedModifiers = new FixedList4096Bytes<ModifierEntry>();
        var compactedSourceMappings = new FixedList512Bytes<SourceMappingEntry>();

        // 활성 modifiers만 복사
        for (int i = 0; i < modifiers.Length; i++)
        {
            var modifier = modifiers[i];
            if (modifier.IsActive)
            {
                compactedModifiers.Add(modifier);
            }
        }

        // 활성 source mappings만 복사
        for (int i = 0; i < sourceMappings.Length; i++)
        {
            var mapping = sourceMappings[i];
            if (mapping.IsActive && mapping.modifierIds.Length > 0)
            {
                compactedSourceMappings.Add(mapping);
            }
        }

        modifiers = compactedModifiers;
        sourceMappings = compactedSourceMappings;
        activeMask = 0; // 재설정 필요

        // 새로운 활성 마스크 설정
        for (int i = 0; i < Math.Min(modifiers.Length, 32); i++)
        {
            if (modifiers[i].IsActive)
            {
                activeMask |= (uint)(1 << i);
            }
        }
    }

    #region Source Mapping 헬퍼 메서드

    private void AddToSourceMapping(int sourceHash, uint modifierId)
    {
        // 기존 mapping 찾기
        for (int i = 0; i < sourceMappings.Length; i++)
        {
            var mapping = sourceMappings[i];
            if (mapping.IsActive && mapping.sourceHash == sourceHash)
            {
                // 기존 mapping에 ID 추가
                if (mapping.modifierIds.Length < mapping.modifierIds.Capacity)
                {
                    mapping.modifierIds.Add(modifierId);
                    sourceMappings[i] = mapping;
                }
                return;
            }
        }

        // 새 mapping 생성
        var newMapping = new SourceMappingEntry
        {
            sourceHash = sourceHash,
            modifierIds = new FixedList64Bytes<uint>(),
            isActive = 1,
        };
        newMapping.modifierIds.Add(modifierId);

        // 빈 슬롯 찾기
        for (int i = 0; i < sourceMappings.Length; i++)
        {
            if (!sourceMappings[i].IsActive)
            {
                sourceMappings[i] = newMapping;
                return;
            }
        }

        // 새 슬롯 추가
        if (sourceMappings.Length < sourceMappings.Capacity)
        {
            sourceMappings.Add(newMapping);
        }
    }

    private void RemoveFromSourceMapping(int sourceHash, uint modifierId)
    {
        for (int i = 0; i < sourceMappings.Length; i++)
        {
            var mapping = sourceMappings[i];
            if (mapping.IsActive && mapping.sourceHash == sourceHash)
            {
                // modifier ID 제거
                for (int j = 0; j < mapping.modifierIds.Length; j++)
                {
                    if (mapping.modifierIds[j] == modifierId)
                    {
                        mapping.modifierIds.RemoveAtSwapBack(j);
                        break;
                    }
                }

                // 빈 mapping이면 비활성화
                if (mapping.modifierIds.Length == 0)
                {
                    mapping.isActive = 0;
                }

                sourceMappings[i] = mapping;
                return;
            }
        }
    }

    #endregion

    #region 내부 헬퍼 메서드

    private uint GenerateNewId()
    {
        return ++nextId;
    }

    private void SetActive(int index)
    {
        if (index >= 0 && index < 32)
        {
            activeMask |= (uint)(1 << index);
        }
    }

    private void SetInactive(int index)
    {
        if (index >= 0 && index < 32)
        {
            activeMask &= ~(uint)(1 << index);
        }
    }

    public bool IsActive(int index)
    {
        if (index >= 0 && index < 32)
        {
            return (activeMask & (1 << index)) != 0;
        }
        return index < modifiers.Length ? modifiers[index].IsActive : false;
    }

    #endregion    /// <summary>
    /// 초기화
    /// </summary>
    public static StatModifierComponent Create()
    {
        return new StatModifierComponent
        {
            modifiers = new FixedList4096Bytes<ModifierEntry>(),
            activeMask = 0,
            nextId = 0,
            sourceMappings = new FixedList512Bytes<SourceMappingEntry>(),
            capacityWarningThreshold = 85, // 85%
            autoCompactThreshold = 90, // 90%
            isOverflowMode = 0,
        };
    }

    #region Capacity Management

    /// <summary>
    /// Capacity 상태 체크 및 필요시 처리
    /// </summary>
    private void CheckAndHandleCapacity()
    {
        int activeCount = GetActiveModifierCount();
        int totalCapacity = modifiers.Capacity;

        float usagePercentage = (float)activeCount / totalCapacity * 100f;

        // 경고 임계값 도달
        if (usagePercentage >= capacityWarningThreshold)
        {
            // 자동 정리 임계값 도달 시 정리 시도
            if (usagePercentage >= autoCompactThreshold)
            {
                TryAutoCompact();
            }
        }
    }

    /// <summary>
    /// 자동 정리 시도 - 비활성 modifier 제거
    /// </summary>
    private bool TryAutoCompact()
    {
        int beforeCount = GetActiveModifierCount();
        Compact();
        int afterCount = GetActiveModifierCount();

        // 공간이 확보되었는지 확인
        return afterCount < beforeCount || modifiers.Length < modifiers.Capacity;
    }

    /// <summary>
    /// Capacity 사용률 반환 (0-100)
    /// </summary>
    public float GetCapacityUsage()
    {
        int activeCount = GetActiveModifierCount();
        return (float)activeCount / modifiers.Capacity * 100f;
    }

    /// <summary>
    /// 오버플로우 상태인지 확인
    /// </summary>
    public bool IsOverflowMode()
    {
        return isOverflowMode == 1;
    }

    /// <summary>
    /// 오버플로우 모드 해제 시도
    /// </summary>
    public bool TryExitOverflowMode()
    {
        if (isOverflowMode == 0)
            return true;

        // 정리 후 여유 공간이 있으면 오버플로우 모드 해제
        TryAutoCompact();
        float usage = GetCapacityUsage();

        if (usage < autoCompactThreshold)
        {
            isOverflowMode = 0;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 우선순위가 낮은 modifier 제거하여 공간 확보
    /// priority: 낮을수록 우선순위 높음 (0 = 제거 불가)
    /// </summary>
    public bool TryEvictLowPriorityModifiers(int minPriority, int targetSlots = 5)
    {
        var evictionCandidates = new FixedList128Bytes<int>();

        // 우선순위가 낮은 modifier 찾기 (간단한 예시: 가장 오래된 것들)
        for (int i = 0; i < modifiers.Length && evictionCandidates.Length < targetSlots; i++)
        {
            var modifier = modifiers[i];
            if (modifier.IsActive)
            {
                // 여기서는 간단히 ID가 낮을수록 오래된 것으로 가정
                // 실제로는 더 복잡한 우선순위 로직 필요
                if (modifier.id % 10 >= minPriority) // 간단한 우선순위 계산
                {
                    evictionCandidates.Add(i);
                }
            }
        }

        // 선택된 modifier들 제거
        for (int i = 0; i < evictionCandidates.Length; i++)
        {
            int index = evictionCandidates[i];
            var modifier = modifiers[index];

            // Source mapping에서 제거
            if (modifier.sourceHash != 0)
            {
                RemoveFromSourceMapping(modifier.sourceHash, modifier.id);
            }

            // modifier 제거
            modifier.isActive = 0;
            modifiers[index] = modifier;
            SetInactive(index);
        }

        return evictionCandidates.Length > 0;
    }

    #endregion    /// <summary>
    /// 정확히 일치하는 첫 번째 modifier 하나만 제거 (같은 값이 여러 개일 때 유용)
    /// </summary>
    public bool RemoveFirstExactMatch(
        object source,
        StatType statType,
        CalcType calcType,
        ScaledInt value,
        byte globalScale,
        out StatType affectedStat
    )
    {
        affectedStat = statType;
        if (source == null)
            return false;

        var normalizedValue = value.NormalizeToScale(globalScale);
        int sourceHash = source.GetHashCode();

        // 직접 순회로 O(n) 복잡도로 최적화
        for (int i = 0; i < modifiers.Length; i++)
        {
            var modifier = modifiers[i];
            if (
                modifier.IsActive
                && modifier.sourceHash == sourceHash
                && modifier.statType == statType
                && modifier.calcType == calcType
                && modifier.value == normalizedValue.Value
            )
            {
                // Source mapping에서 제거
                if (modifier.sourceHash != 0)
                {
                    RemoveFromSourceMapping(modifier.sourceHash, modifier.id);
                }

                // modifier 제거
                modifier.isActive = 0;
                modifiers[i] = modifier;
                SetInactive(i);

                return true;
            }
        }

        return false;
    }
}
