using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillIneractionEffect : ISkillInteractionEffect
{
    protected ItemEffect effectData;
    protected float procChance;
    protected float cooldown;
    protected float lastProcTime;
    public float damageMultiplier { get; private set; }
    public float cooldownReduction { get; private set; }
    public float rangeMultiplier { get; private set; }
    public float durationMultiplier { get; private set; }
    public float projectileSpeedMultiplier { get; private set; }
    public float elementalPowerBonus { get; private set; }
    public float homingRange { get; private set; }
    public List<SkillType> applicableSkillTypes { get; private set; }
    public List<ElementType> applicableElements { get; private set; }

    public SkillIneractionEffect(ItemEffect effect, float chance = 1f, float cd = 0)
    {
        effectData = effect;
        procChance = chance;
        cooldown = cd;
        lastProcTime = 0f;

        foreach (var subEffect in effect.subEffects)
        {
            switch (subEffect.effectType)
            {
                case EffectType.DamageBonus:
                    damageMultiplier = 1f + subEffect.value;
                    break;
                case EffectType.CooldownReduction:
                    cooldownReduction = subEffect.value;
                    break;
                case EffectType.ProjectileSpeed:
                    projectileSpeedMultiplier = 1f + subEffect.value;
                    break;
                case EffectType.ProjectileRange:
                    rangeMultiplier = 1f + subEffect.value;
                    break;
                case EffectType.AreaRadius:
                    rangeMultiplier = 1f + subEffect.value;
                    break;
                case EffectType.AreaDuration:
                    durationMultiplier = 1f + subEffect.value;
                    break;
                case EffectType.ElementalPower:
                    elementalPowerBonus = subEffect.value;
                    break;
                case EffectType.HomingEffect:
                    homingRange = subEffect.value;
                    break;
                default:
                    break;
            }
        }

        applicableSkillTypes = effect.applicableSkills?.ToList() ?? new List<SkillType>();
        applicableElements = effect.applicableElements?.ToList() ?? new List<ElementType>();
    }

    public bool CanApplyTo(SkillType skillType, ElementType elementType = ElementType.None)
    {
        if (applicableSkillTypes.Any() && !applicableSkillTypes.Contains(skillType))
            return false;
        if (
            elementType != ElementType.None
            && applicableElements.Any()
            && !applicableElements.Contains(elementType)
        )
            return false;
        return true;
    }

    protected bool CanTriggerEffect()
    {
        if (Time.time < lastProcTime + cooldown)
            return false;
        if (Random.value > procChance)
            return false;

        lastProcTime = Time.time;
        return true;
    }

    public virtual void OnSkillCast(Skill skill) { }

    public virtual void OnSkillHit(Skill skill, Monster target) { }

    public virtual void OnSkillKill(Skill skill, Player player, Monster target) { }

    public virtual void ModifySkillStats(Skill skill)
    {
        if (!CanApplyTo(skill.skillData.Type, skill.skillData.Element))
            return;

        var stats = skill.skillData.GetSkillStats();

        if (stats == null)
        {
            return;
        }
        if (damageMultiplier > 0)
        {
            stats.baseStat.damage *= damageMultiplier;
        }

        switch (stats)
        {
            case ProjectileSkillStat projectileStats:
                projectileStats.projectileSpeed *= projectileSpeedMultiplier;
                projectileStats.attackRange *= rangeMultiplier;
                if (homingRange > 0)
                {
                    projectileStats.isHoming = true;
                    projectileStats.homingRange = homingRange;
                }
                break;
            case AreaSkillStat areaStats:
                areaStats.radius *= rangeMultiplier;
                areaStats.duration *= durationMultiplier;
                break;
            case PassiveSkillStat passiveStats:
                passiveStats.cooldown *= (1f - cooldownReduction);
                break;
        }
    }
}
