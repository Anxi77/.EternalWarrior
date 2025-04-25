using System;

public class StatModifier
{
    private StatType type;
    private SourceType source;
    private IncreaseType increaseType;
    private float value;
    public StatType Type
    {
        get => type;
    }
    public SourceType Source
    {
        get => source;
    }
    public IncreaseType IncreaseType
    {
        get => increaseType;
    }
    public float Value
    {
        get => value;
    }

    public StatModifier(StatType type, SourceType source, IncreaseType increaseType, float value)
    {
        this.type = type;
        this.source = source;
        this.increaseType = increaseType;
        this.value = value;
    }

    public override bool Equals(object obj)
    {
        if (obj is StatModifier other)
        {
            return Type == other.Type
                && Source == other.Source
                && IncreaseType == other.IncreaseType
                && Math.Abs(Value - other.Value) < float.Epsilon;
        }
        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + Type.GetHashCode();
            hash = hash * 23 + Source.GetHashCode();
            hash = hash * 23 + IncreaseType.GetHashCode();
            hash = hash * 23 + Value.GetHashCode();
            return hash;
        }
    }

    public override string ToString()
    {
        return $"[{Source}] {Type} {(IncreaseType == IncreaseType.Flat ? "+" : "x")} {Value}";
    }
}
