using System.Diagnostics;
using UnityEngine;

public static class ElementalEffects
{
    private const float DARK_EFFECT_DURATION = 5f;
    private const float WATER_EFFECT_DURATION = 3f;
    private const float FIRE_EFFECT_DURATION = 3f;
    private const float FIRE_TICK_RATE = 0.5f;

    public static void ApplyElementalEffect(
        ElementType element,
        float elementalPower,
        object source,
        Unit target
    )
    {
        if (target == null || elementalPower <= 0)
            return;

        float power = CalculateEffectPower(elementalPower, 0.1f);

        switch (element)
        {
            case ElementType.Dark:
                target.ApplyDebuff(power, StatType.Defense, DARK_EFFECT_DURATION, source);
                break;
            case ElementType.Water:
                target.ApplyDebuff(power, StatType.MoveSpeed, WATER_EFFECT_DURATION, source);
                break;
            case ElementType.Fire:
                target.ApplyDotDamage(power, FIRE_EFFECT_DURATION, FIRE_TICK_RATE, source);
                break;
            case ElementType.Earth:
                target.ApplyStun(power);
                break;
            case ElementType.None:
                break;
            default:
                Logger.LogWarning(typeof(ElementalEffects), $"Unknown element type: {element}");
                break;
        }
    }

    private static float CalculateEffectPower(float basePower, float scaling)
    {
        return Mathf.Clamp(basePower * scaling, 0f, 100f);
    }
}
