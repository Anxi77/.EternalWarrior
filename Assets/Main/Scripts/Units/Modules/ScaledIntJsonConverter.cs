using System;
using System.Text;
using Newtonsoft.Json;

/// <summary>
/// ScaledInt를 "1500K" 등 문자열로 직렬화/역직렬화하는 커스텀 JsonConverter
/// 성능 최적화 버전
/// </summary>
public class ScaledIntJsonConverter : JsonConverter<ScaledInt>
{
    [ThreadStatic]
    private static StringBuilder _numberBuilder;

    [ThreadStatic]
    private static StringBuilder _suffixBuilder;

    public override void WriteJson(JsonWriter writer, ScaledInt value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }

    public override ScaledInt ReadJson(
        JsonReader reader,
        Type objectType,
        ScaledInt existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        var str = reader.Value as string;
        if (string.IsNullOrEmpty(str))
            return ScaledInt.Zero;
        return ParseScaledInt(str);
    }

    private ScaledInt ParseScaledInt(string str)
    {
        if (string.IsNullOrEmpty(str) || str == "0")
            return ScaledInt.Zero;

        _numberBuilder ??= new StringBuilder(16);
        _suffixBuilder ??= new StringBuilder(4);

        _numberBuilder.Clear();
        _suffixBuilder.Clear();

        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            if (char.IsLetter(c))
                _suffixBuilder.Append(c);
            else if (char.IsDigit(c) || c == '.' || c == '-')
                _numberBuilder.Append(c);
        }

        if (_numberBuilder.Length == 0)
            return ScaledInt.Zero;

        if (!double.TryParse(_numberBuilder.ToString(), out double number))
            return ScaledInt.Zero;

        long value = (long)Math.Round(number);
        byte scale = 0;
        if (_suffixBuilder.Length > 0)
        {
            var suffix = _suffixBuilder.ToString();
            scale = GetScaleFromSuffix(suffix);
        }

        return new ScaledInt(value, scale);
    }

    private static byte GetScaleFromSuffix(string suffix)
    {
        return suffix.ToUpperInvariant() switch
        {
            "" => 0,
            "K" => 1,
            "M" => 2,
            "B" => 3,
            "T" => 4,
            "QA" => 5,
            "QI" => 6,
            "SX" => 7,
            "SP" => 8,
            "OC" => 9,
            "NO" => 10,
            "DC" => 11,
            "UD" => 12,
            "DD" => 13,
            "TD" => 14,
            "QAD" => 15,
            "QID" => 16,
            "SXD" => 17,
            "SPD" => 18,
            "OCD" => 19,
            "NOD" => 20,
            _ => 0,
        };
    }
}
