using UnityEngine;

public static class ElementalEffects
{
    private const float DARK_EFFECT_DURATION = 5f;
    private const float WATER_EFFECT_DURATION = 3f;
    private const float FIRE_EFFECT_DURATION = 3f;
    private const float FIRE_TICK_RATE = 0.5f;
    private const float EARTH_EFFECT_DURATION = 2f;

    public static void ApplyElementalEffect(
        ElementType element,
        float elementalPower,
        GameObject target
    )
    {
        if (target == null || elementalPower <= 0)
            return;

        if (!target.TryGetComponent<Monster>(out Monster enemy))
        {
            Logger.LogWarning(
                typeof(ElementalEffects),
                $"Failed to apply elemental effect: Target {target.name} is not an enemy"
            );
            return;
        }

        switch (element)
        {
            case ElementType.Dark:
                ApplyDarkEffect(elementalPower, enemy);
                break;
            case ElementType.Water:
                ApplyWaterEffect(elementalPower, enemy);
                break;
            case ElementType.Fire:
                ApplyFireEffect(elementalPower, enemy);
                break;
            case ElementType.Earth:
                ApplyEarthEffect(elementalPower, enemy);
                break;
            case ElementType.None:
                break;
            default:
                Logger.LogWarning(typeof(ElementalEffects), $"Unknown element type: {element}");
                break;
        }
    }

    private static void ApplyDarkEffect(float power, Monster enemy)
    {
        float defenseReduction = Mathf.Clamp(power * 0.2f, 0.1f, 0.5f);
        enemy.ApplyDefenseDebuff(defenseReduction, DARK_EFFECT_DURATION);

        Logger.Log(
            typeof(ElementalEffects),
            $"Applied Dark effect to {enemy.name}: {defenseReduction * 100}% defense reduction for {DARK_EFFECT_DURATION}s"
        );
    }

    private static void ApplyWaterEffect(float power, Monster enemy)
    {
        float slowAmount = Mathf.Clamp(power * 0.3f, 0.2f, 0.6f);
        enemy.ApplySlowEffect(slowAmount, WATER_EFFECT_DURATION);

        Logger.Log(
            typeof(ElementalEffects),
            $"Applied Water effect to {enemy.name}: {slowAmount * 100}% slow for {WATER_EFFECT_DURATION}s"
        );
    }

    private static void ApplyFireEffect(float power, Monster enemy)
    {
        float dotDamage = power * 0.15f;
        enemy.ApplyDotDamage(dotDamage, FIRE_TICK_RATE, FIRE_EFFECT_DURATION);

        Logger.Log(
            typeof(ElementalEffects),
            $"Applied Fire effect to {enemy.name}: {dotDamage} damage every {FIRE_TICK_RATE}s for {FIRE_EFFECT_DURATION}s"
        );
    }

    private static void ApplyEarthEffect(float power, Monster enemy)
    {
        float stunDuration = Mathf.Clamp(power * 0.1f, 0.5f, EARTH_EFFECT_DURATION);
        enemy.ApplyStun(power, stunDuration);

        Logger.Log(
            typeof(ElementalEffects),
            $"Applied Earth effect to {enemy.name}: Stunned for {stunDuration}s"
        );
    }

    private static float CalculateEffectPower(float basePower, float scaling)
    {
        return Mathf.Clamp(basePower * scaling, 0f, 100f);
    }
}
