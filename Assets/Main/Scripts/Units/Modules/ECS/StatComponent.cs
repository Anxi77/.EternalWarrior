using System;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// 스파스 세트 기반 스탯 시스템
/// - 동적 스탯 타입 지원
/// - O(1) 접근 성능
/// - SIMD 친화적 메모리 레이아웃
/// </summary>
public struct StatComponent : IComponentData
{
    // 스파스 세트 패턴
    public FixedList128Bytes<StatType> dense; // 실제 존재하는 스탯 타입들 (조밀 배열)
    public FixedList512Bytes<short> sparse; // 스탯 타입 → dense 인덱스 매핑 (희소 배열)
    public FixedList128Bytes<ScaledInt> values; // dense와 같은 순서로 값들 저장

    public byte globalScale;
    public uint dirtyMask;

    public int ActiveStatCount => dense.Length;

    public ScaledInt MemoryEfficiency
    {
        get
        {
            int sparseSize = sparse.Length * sizeof(short);
            int denseSize = dense.Length * sizeof(StatType);
            int valuesSize = values.Length * sizeof(long);
            return sparseSize + denseSize + valuesSize;
        }
    }

    private const short INVALID_INDEX = -1;
    private const int MAX_STAT_TYPES = 128; // StatType enum의 최대 개수

    /// <summary>
    /// 스탯 조회 - O(1) 성능
    /// </summary>
    public ScaledInt GetStat(StatType statType)
    {
        int statTypeIndex = (int)statType;
        if (statTypeIndex >= sparse.Length)
            return ScaledInt.Zero;

        short denseIndex = sparse[statTypeIndex];
        if (denseIndex == INVALID_INDEX || denseIndex >= values.Length)
            return ScaledInt.Zero;

        return values[denseIndex].NormalizeToScale(globalScale);
    }

    /// <summary>
    /// 스탯 설정 - O(1) 성능
    /// </summary>
    public void SetStat(StatType statType, ScaledInt value)
    {
        var normalized = value.NormalizeToScale(globalScale);
        int statTypeIndex = (int)statType;

        // sparse 배열 확장 (필요시)
        EnsureSparseCapacity(statTypeIndex);

        short denseIndex = sparse[statTypeIndex];

        if (denseIndex == INVALID_INDEX)
        {
            // 새 스탯 추가
            AddNewStat(statType, normalized.Value);
        }
        else
        {
            // 기존 스탯 수정
            values[denseIndex] = normalized.Value;
            SetDirty(denseIndex);
        }
    }

    /// <summary>
    /// 스탯 존재 여부 확인 - O(1)
    /// </summary>
    public bool HasStat(StatType statType)
    {
        int statTypeIndex = (int)statType;
        if (statTypeIndex >= sparse.Length)
            return false;

        short denseIndex = sparse[statTypeIndex];
        return denseIndex != INVALID_INDEX && denseIndex < dense.Length;
    }

    /// <summary>
    /// 스탯의 dense 인덱스 직접 조회 - O(1) 성능
    /// </summary>
    public short GetDenseIndex(StatType statType)
    {
        int statTypeIndex = (int)statType;
        if (statTypeIndex >= sparse.Length)
            return INVALID_INDEX;

        return sparse[statTypeIndex];
    }

    /// <summary>
    /// 새 스탯 추가
    /// </summary>
    private void AddNewStat(StatType statType, long value)
    {
        if (dense.Length >= dense.Capacity || values.Length >= values.Capacity)
            return; // 용량 초과

        int statTypeIndex = (int)statType;
        short newDenseIndex = (short)dense.Length;

        // dense 배열에 추가
        dense.Add(statType);
        values.Add(value);

        // sparse 배열 업데이트
        sparse[statTypeIndex] = newDenseIndex;

        SetDirty(newDenseIndex);
    }

    /// <summary>
    /// 스탯 제거 - O(1) 스왑 제거
    /// </summary>
    public bool RemoveStat(StatType statType)
    {
        int statTypeIndex = (int)statType;
        if (statTypeIndex >= sparse.Length)
            return false;

        short denseIndex = sparse[statTypeIndex];
        if (denseIndex == INVALID_INDEX || denseIndex >= dense.Length)
            return false;

        // 마지막 요소와 스왑
        int lastIndex = dense.Length - 1;
        if (denseIndex != lastIndex)
        {
            // 마지막 요소를 현재 위치로 이동
            dense[denseIndex] = dense[lastIndex];
            values[denseIndex] = values[lastIndex];

            // 이동된 요소의 sparse 인덱스 업데이트
            StatType movedStatType = dense[denseIndex];
            sparse[(int)movedStatType] = denseIndex;
        }

        // 마지막 요소 제거
        dense.RemoveAt(lastIndex);
        values.RemoveAt(lastIndex);

        // sparse 배열에서 제거
        sparse[statTypeIndex] = INVALID_INDEX;

        ClearDirty(denseIndex);
        return true;
    }

    /// <summary>
    /// sparse 배열 용량 보장
    /// </summary>
    private void EnsureSparseCapacity(int requiredIndex)
    {
        while (sparse.Length <= requiredIndex && sparse.Length < sparse.Capacity)
        {
            sparse.Add(INVALID_INDEX);
        }
    }

    /// <summary>
    /// 초기화
    /// </summary>
    public static StatComponent Create(byte scale = 0)
    {
        var component = new StatComponent
        {
            dense = new FixedList128Bytes<StatType>(),
            sparse = new FixedList512Bytes<short>(),
            values = new FixedList128Bytes<ScaledInt>(),
            globalScale = scale,
            dirtyMask = 0,
        };

        // sparse 배열을 INVALID_INDEX로 초기화
        for (int i = 0; i < Math.Min(MAX_STAT_TYPES, component.sparse.Capacity); i++)
        {
            component.sparse.Add(INVALID_INDEX);
        }

        return component;
    }

    #region 더티 플래그 관리

    public void SetDirty(int index)
    {
        if (index >= 0 && index < 32)
        {
            dirtyMask |= (uint)(1 << index);
        }
    }

    public void ClearDirty(int index)
    {
        if (index >= 0 && index < 32)
        {
            dirtyMask &= ~(uint)(1 << index);
        }
    }

    public bool IsDirty(int index)
    {
        if (index >= 0 && index < 32)
        {
            return (dirtyMask & (1 << index)) != 0;
        }
        return false;
    }

    public void ClearAllDirty()
    {
        dirtyMask = 0;
    }

    #endregion
}
