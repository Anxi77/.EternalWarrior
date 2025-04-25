using System;
using System.Collections.Generic;

public static class SkillStatFilters
{
    private static readonly List<string> CommonSkillFields = new List<string>
    {
        "skillID",
        "level",
        "damage",
        "maxSkillLevel",
        "element",
        "elementalPower",
    };

    private static readonly List<string> ProjectileSkillFields = new List<string>
    {
        "projectileSpeed",
        "projectileScale",
        "shotInterval",
        "pierceCount",
        "attackRange",
        "homingRange",
        "isHoming",
        "explosionRad",
        "projectileCount",
        "innerInterval",
    };

    private static readonly List<string> AreaSkillFields = new List<string>
    {
        "radius",
        "duration",
        "tickRate",
        "isPersistent",
        "moveSpeed",
    };

    private static readonly List<string> PassiveSkillFields = new List<string>
    {
        "effectDuration",
        "cooldown",
        "triggerChance",
        "damageIncrease",
        "defenseIncrease",
        "expAreaIncrease",
        "homingActivate",
        "hpIncrease",
        "moveSpeedIncrease",
        "attackSpeedIncrease",
        "attackRangeIncrease",
        "hpRegenIncrease",
    };

    public static List<string> GetFieldsForSkillType(SkillType type)
    {
        var skillFields = new List<string>(CommonSkillFields);
        switch (type)
        {
            case SkillType.Projectile:
                skillFields.AddRange(ProjectileSkillFields);
                return skillFields;
            case SkillType.Area:
                skillFields.AddRange(AreaSkillFields);
                return skillFields;
            case SkillType.Passive:
                skillFields.AddRange(PassiveSkillFields);
                return skillFields;
            default:
                return skillFields;
        }
    }
}

[Serializable]
public class SkillStatData
{
    #region Basic Info
    public SkillID skillID;
    public int level;
    public float damage;
    public int maxSkillLevel;
    public ElementType element;
    public float elementalPower;
    #endregion

    #region ProjectileSkill Stats
    public float projectileSpeed;
    public float projectileScale;
    public float shotInterval;
    public int pierceCount;
    public float attackRange;
    public float homingRange;
    public bool isHoming;
    public float explosionRad;
    public int projectileCount;
    public float innerInterval;
    #endregion

    #region AreaSkill Stats
    public float radius;
    public float duration;
    public float tickRate;
    public bool isPersistent;
    public float moveSpeed;
    #endregion

    #region PassiveSkill Stats
    public float effectDuration;
    public float cooldown;
    public float triggerChance;
    public float damageIncrease;
    public float defenseIncrease;
    public float expAreaIncrease;
    public bool homingActivate;
    public float hpIncrease;
    public float moveSpeedIncrease;
    public float attackSpeedIncrease;
    public float attackRangeIncrease;
    public float hpRegenIncrease;
    #endregion

    public SkillStatData()
    {
        skillID = SkillID.None;
        level = 1;
        damage = 10f;
        maxSkillLevel = 5;
        element = ElementType.None;
        elementalPower = 1f;

        projectileSpeed = 10f;
        projectileScale = 1f;
        shotInterval = 1f;
        pierceCount = 1;
        attackRange = 10f;
        homingRange = 5f;
        isHoming = false;
        explosionRad = 0f;
        projectileCount = 1;
        innerInterval = 0.1f;

        radius = 5f;
        duration = 3f;
        tickRate = 1f;
        isPersistent = false;
        moveSpeed = 0f;

        effectDuration = 5f;
        cooldown = 10f;
        triggerChance = 100f;
        damageIncrease = 0f;
        defenseIncrease = 0f;
        expAreaIncrease = 0f;
        homingActivate = false;
        hpIncrease = 0f;
        moveSpeedIncrease = 0f;
        attackSpeedIncrease = 0f;
        attackRangeIncrease = 0f;
        hpRegenIncrease = 0f;
    }

    public ISkillStat CreateSkillStat(SkillType skillType)
    {
        var baseStats = new BaseSkillStat
        {
            damage = this.damage,
            maxSkillLevel = this.maxSkillLevel,
            skillLevel = this.level,
            element = this.element,
            elementalPower = this.elementalPower,
        };

        switch (skillType)
        {
            case SkillType.Projectile:
                return new ProjectileSkillStat
                {
                    baseStat = baseStats,
                    projectileSpeed = projectileSpeed,
                    projectileScale = projectileScale,
                    shotInterval = shotInterval,
                    pierceCount = pierceCount,
                    attackRange = attackRange,
                    homingRange = homingRange,
                    isHoming = isHoming,
                    explosionRad = explosionRad,
                    projectileCount = projectileCount,
                    innerInterval = innerInterval,
                };

            case SkillType.Area:
                return new AreaSkillStat
                {
                    baseStat = baseStats,
                    radius = radius,
                    duration = duration,
                    tickRate = tickRate,
                    isPersistent = isPersistent,
                    moveSpeed = moveSpeed,
                };

            case SkillType.Passive:
                return new PassiveSkillStat
                {
                    baseStat = baseStats,
                    effectDuration = effectDuration,
                    cooldown = cooldown,
                    triggerChance = triggerChance,
                    damageIncrease = damageIncrease,
                    defenseIncrease = defenseIncrease,
                    expAreaIncrease = expAreaIncrease,
                    homingActivate = homingActivate,
                    hpIncrease = hpIncrease,
                    moveSpeedIncrease = moveSpeedIncrease,
                    attackSpeedIncrease = attackSpeedIncrease,
                    attackRangeIncrease = attackRangeIncrease,
                    hpRegenIncrease = hpRegenIncrease,
                };

            default:
                throw new ArgumentException($"Invalid skill type: {skillType}");
        }
    }
}
