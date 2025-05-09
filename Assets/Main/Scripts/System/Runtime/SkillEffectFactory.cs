using System.Collections.Generic;

public static class SkillEffectFactory
{
    public static List<SkillIneractionEffect> CreateEffects(ItemData itemData)
    {
        var effects = new List<SkillIneractionEffect>();

        foreach (var effectData in itemData.Effects)
        {
            var effect = CreateEffectFromData(effectData);
            if (effect != null)
            {
                effects.AddRange(effect);
            }
        }

        return effects;
    }

    private static List<SkillIneractionEffect> CreateEffectFromData(ItemEffect effectData)
    {
        var effects = new List<SkillIneractionEffect>();
        foreach (var subEffect in effectData.subEffects)
        {
            effects.Add(
                subEffect.effectType switch
                {
                    EffectType.DamageBonus => new SkillIneractionEffect(effectData),
                    EffectType.CooldownReduction => new SkillIneractionEffect(effectData),
                    EffectType.ProjectileSpeed => new SkillIneractionEffect(effectData),
                    EffectType.ProjectileRange => new SkillIneractionEffect(effectData),
                    EffectType.HomingEffect => new SkillIneractionEffect(effectData),
                    EffectType.AreaRadius => new SkillIneractionEffect(effectData),
                    EffectType.AreaDuration => new SkillIneractionEffect(effectData),
                    EffectType.ElementalPower => new SkillIneractionEffect(effectData),
                    _ => null,
                }
            );
        }
        return effects;
    }
}
