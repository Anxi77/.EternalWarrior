using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 대용량 정수를 효율적으로 처리하기 위한 스케일드 정수 구조체
/// value * (1000 ^ scale)로 표현됩니다.
/// 예: 1,500,000 = value: 1500, scale: 1 (1500 * 1000^1)
/// Burst 컴파일 호환 (개별 메서드에 적용)
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 8)]
[JsonConverter(typeof(ScaledIntJsonConverter))]
public readonly struct ScaledInt : IEquatable<ScaledInt>, IComparable<ScaledInt>
{
    private readonly long value;
    private readonly byte scale;

    private const long SCALE_FACTOR = 1000L;
    private const long MAX_VALUE_PER_SCALE = 999_999L;
    private const byte MAX_SCALE = 20;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long GetScaleMultiplier(byte scale)
    {
        return scale switch
        {
            0 => 1L,
            1 => 1_000L,
            2 => 1_000_000L,
            3 => 1_000_000_000L,
            4 => 1_000_000_000_000L,
            5 => 1_000_000_000_000_000L,
            6 => 1_000_000_000_000_000_000L,
            _ => long.MaxValue,
        };
    }

    /// <summary>
    /// 스케일에 따른 접미사를 반환 (Burst 호환)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetScaleSuffix(byte scale)
    {
        return scale switch
        {
            0 => "",
            1 => "K",
            2 => "M",
            3 => "B",
            4 => "T",
            5 => "Qa",
            6 => "Qi",
            7 => "Sx",
            8 => "Sp",
            9 => "Oc",
            10 => "No",
            11 => "Dc",
            12 => "Ud",
            13 => "Dd",
            14 => "Td",
            15 => "Qad",
            16 => "Qid",
            17 => "Sxd",
            18 => "Spd",
            19 => "Ocd",
            20 => "Nod",
            _ => "",
        };
    }

    public ScaledInt(long value, byte scale = 0)
    {
        this.value = value;
        this.scale = scale;
    }

    public long Value => value;
    public byte Scale => scale;
    public bool IsZero => value == 0;
    public bool IsPositive => value > 0;
    public bool IsNegative => value < 0;

    /// <summary>
    /// 값을 정규화하여 적절한 스케일로 조정
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ScaledInt Normalize()
    {
        long val = value;
        byte sc = scale;

        // 상향 정규화 (값이 너무 클 때)
        while (math.abs(val) > MAX_VALUE_PER_SCALE && sc < MAX_SCALE)
        {
            val /= SCALE_FACTOR;
            sc++;
        }

        // 하향 정규화 (값이 너무 작을 때)
        while (val != 0 && math.abs(val) < 1 && sc > 0)
        {
            val *= SCALE_FACTOR;
            sc--;
        }

        return new ScaledInt(val, sc);
    }

    public static ScaledInt operator +(ScaledInt a, ScaledInt b)
    {
        if (a.IsZero)
            return b;
        if (b.IsZero)
            return a; // 같은 스케일이면 빠른 연산
        if (a.scale == b.scale)
        {
            return new ScaledInt(a.value + b.value, a.scale).Normalize();
        }

        // 스케일 맞춤
        ScaledInt normalizedA,
            normalizedB;
        AlignScales(a, b, out normalizedA, out normalizedB);
        return new ScaledInt(normalizedA.value + normalizedB.value, normalizedA.scale).Normalize();
    }

    public static ScaledInt operator -(ScaledInt a, ScaledInt b)
    {
        if (b.IsZero)
            return a;
        if (a.scale == b.scale)
        {
            return new ScaledInt(a.value - b.value, a.scale).Normalize();
        }

        ScaledInt normalizedA,
            normalizedB;
        AlignScales(a, b, out normalizedA, out normalizedB);
        return new ScaledInt(normalizedA.value - normalizedB.value, normalizedA.scale).Normalize();
    }

    public static ScaledInt operator *(ScaledInt a, ScaledInt b)
    {
        if (a.IsZero || b.IsZero)
            return Zero;

        long newValue = a.value * b.value;
        byte newScale = (byte)math.min(a.scale + b.scale, MAX_SCALE);

        return new ScaledInt(newValue, newScale).Normalize();
    }

    public static ScaledInt operator /(ScaledInt a, ScaledInt b)
    {
        if (b.IsZero)
            return Zero;
        if (a.IsZero)
            return Zero;

        long newValue = a.value / b.value;
        byte newScale = (byte)math.max(0, a.scale - b.scale);

        return new ScaledInt(newValue, newScale).Normalize();
    }

    public static bool operator ==(ScaledInt a, ScaledInt b) => a.Equals(b);

    public static bool operator !=(ScaledInt a, ScaledInt b) => !a.Equals(b);

    public static bool operator <(ScaledInt a, ScaledInt b) => a.CompareTo(b) < 0;

    public static bool operator >(ScaledInt a, ScaledInt b) => a.CompareTo(b) > 0;

    public static bool operator <=(ScaledInt a, ScaledInt b) => a.CompareTo(b) <= 0;

    public static bool operator >=(ScaledInt a, ScaledInt b) => a.CompareTo(b) >= 0;

    public static implicit operator ScaledInt(int value) => new(value);

    public static implicit operator ScaledInt(long value) => new(value);

    public static implicit operator ScaledInt(float value) => new((long)value);

    public static implicit operator ScaledInt(double value) => new((long)value);

    public static implicit operator int(ScaledInt scaledInt) => (int)scaledInt.ToLong();

    public static implicit operator float(ScaledInt scaledInt) => scaledInt.ToFloat();

    /// <summary>
    /// long으로 변환 (오버플로우 주의)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ToLong()
    {
        if (scale == 0)
            return value;
        if (scale <= 6)
        {
            return value * GetScaleMultiplier(scale);
        }

        double result = value * math.pow(SCALE_FACTOR, scale);
        return result > long.MaxValue ? long.MaxValue : (long)result;
    }

    /// <summary>
    /// double로 변환 (정밀도 손실 가능)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ToDouble()
    {
        return value * math.pow(SCALE_FACTOR, scale);
    }

    /// <summary>
    /// float로 변환 (정밀도 손실 가능)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ToFloat()
    {
        return (float)(value * math.pow(SCALE_FACTOR, scale));
    }

    /// <summary>
    /// 지정된 스케일로 정규화
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ScaledInt NormalizeToScale(byte targetScale)
    {
        if (scale == targetScale)
            return this;
        if (scale > targetScale)
        {
            // 스케일을 낮춤 (값이 커짐)
            byte scaleDiff = (byte)(scale - targetScale);
            long multiplier =
                scaleDiff <= 6
                    ? GetScaleMultiplier(scaleDiff)
                    : (long)math.pow(SCALE_FACTOR, scaleDiff);

            return new ScaledInt(value * multiplier, targetScale);
        }
        else
        {
            // 스케일을 높임 (값이 작아짐)
            byte scaleDiff = (byte)(targetScale - scale);
            long divisor =
                scaleDiff <= 6
                    ? GetScaleMultiplier(scaleDiff)
                    : (long)math.pow(SCALE_FACTOR, scaleDiff);

            return new ScaledInt(value / divisor, targetScale);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AlignScales(
        ScaledInt a,
        ScaledInt b,
        out ScaledInt alignedA,
        out ScaledInt alignedB
    )
    {
        if (a.scale == b.scale)
        {
            alignedA = a;
            alignedB = b;
            return;
        }
        if (a.scale > b.scale)
        {
            byte scaleDiff = (byte)(a.scale - b.scale);
            long multiplier =
                scaleDiff <= 6
                    ? GetScaleMultiplier(scaleDiff)
                    : (long)math.pow(SCALE_FACTOR, scaleDiff);

            alignedA = a;
            alignedB = new ScaledInt(b.value * multiplier, a.scale);
        }
        else
        {
            byte scaleDiff = (byte)(b.scale - a.scale);
            long multiplier =
                scaleDiff <= 6
                    ? GetScaleMultiplier(scaleDiff)
                    : (long)math.pow(SCALE_FACTOR, scaleDiff);

            alignedA = new ScaledInt(a.value * multiplier, b.scale);
            alignedB = b;
        }
    }

    public bool Equals(ScaledInt other)
    {
        if (IsZero && other.IsZero)
            return true;
        if (scale == other.scale)
            return value == other.value;

        ScaledInt normalizedA,
            normalizedB;
        AlignScales(this, other, out normalizedA, out normalizedB);
        return normalizedA.value == normalizedB.value;
    }

    public int CompareTo(ScaledInt other)
    {
        if (IsZero && other.IsZero)
            return 0;
        if (scale == other.scale)
            return value.CompareTo(other.value);

        ScaledInt normalizedA,
            normalizedB;
        AlignScales(this, other, out normalizedA, out normalizedB);
        return normalizedA.value.CompareTo(normalizedB.value);
    }

    public override bool Equals(object obj)
    {
        return obj is ScaledInt other && Equals(other);
    }

    public override int GetHashCode()
    {
        var normalized = Normalize();
        return HashCode.Combine(normalized.value, normalized.scale);
    }

    public override string ToString()
    {
        if (IsZero)
            return "0";

        var normalized = Normalize();
        if (normalized.scale == 0 || normalized.scale > MAX_SCALE)
        {
            return normalized.value.ToString();
        }

        string suffix = GetScaleSuffix(normalized.scale);

        if (normalized.value >= 100)
        {
            return $"{normalized.value}{suffix}";
        }
        else if (normalized.value >= 10)
        {
            return $"{normalized.value:F1}{suffix}";
        }
        else
        {
            return $"{normalized.value:F2}{suffix}";
        }
    }

    public string ToString(string format)
    {
        var normalized = Normalize();
        if (normalized.scale == 0 || normalized.scale > MAX_SCALE)
        {
            return normalized.value.ToString(format);
        }

        string suffix = GetScaleSuffix(normalized.scale);
        return $"{normalized.value.ToString(format)}{suffix}";
    }

    public static readonly ScaledInt Zero = new(0, 0);
    public static readonly ScaledInt One = new(1, 0);

    public static ScaledInt Min(ScaledInt a, ScaledInt b)
    {
        return a.CompareTo(b) < 0 ? a : b;
    }

    public static ScaledInt Max(ScaledInt a, ScaledInt b)
    {
        return a.CompareTo(b) > 0 ? a : b;
    }
}
