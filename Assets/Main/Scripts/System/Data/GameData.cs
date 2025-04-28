using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

#region Player Data

[Serializable]
public class PlayerData
{
    public LevelData levelData;
    public StatData stats;
    public InventoryData inventory;
}

[Serializable]
public class LevelData
{
    public int level = 1;
    public float exp = 0f;
}

[Serializable]
public class StatData
{
    public Dictionary<StatType, float> baseStats = new();
    public List<StatModifier> permanentModifiers = new();

    public StatData()
    {
        InitializeDefaultStats();
    }

    private void InitializeDefaultStats()
    {
        baseStats[StatType.MaxHp] = 100f;
        baseStats[StatType.CurrentHp] = baseStats[StatType.MaxHp];
        baseStats[StatType.Damage] = 5f;
        baseStats[StatType.Defense] = 2f;
        baseStats[StatType.MoveSpeed] = 5f;
        baseStats[StatType.AttackSpeed] = 1f;
        baseStats[StatType.AttackRange] = 2f;
        baseStats[StatType.ExpCollectionRadius] = 3f;
        baseStats[StatType.HpRegenRate] = 1f;
        baseStats[StatType.AttackRadius] = 1f;
        baseStats[StatType.CriticalChance] = 10f;
        baseStats[StatType.CriticalDamage] = 2f;
        baseStats[StatType.DodgeChance] = 10f;
        baseStats[StatType.Luck] = 5f;
        baseStats[StatType.LifeSteal] = 5f;
    }
}

[Serializable]
public class StatModifier
{
    private StatType type;
    private SourceType source;
    private IncreaseType increaseType;
    private float value;

    [JsonIgnore]
    public StatType Type
    {
        get => type;
    }

    [JsonIgnore]
    public SourceType Source
    {
        get => source;
    }

    [JsonIgnore]
    public IncreaseType IncreaseType
    {
        get => increaseType;
    }

    [JsonIgnore]
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

[Serializable]
public class InventoryData
{
    public List<InventorySlot> slots = new();
    public int gold;
}

[Serializable]
public class InventorySlot
{
    public SlotType slotType = SlotType.Storage;
    public AccessoryType accessoryType = AccessoryType.None;
    public Item item = null;
    public int amount = 0;
    public bool isEquipmentSlot = false;
    public bool isEquipped = false;

    public bool AddItem(Item item, int amount = 1)
    {
        if (this.item == null && amount < this.item.GetItemData().MaxStack)
        {
            this.item = item;
            this.amount += amount;
            if (isEquipmentSlot)
            {
                isEquipped = true;
            }
            return true;
        }
        else
        {
            if (this.item.GetItemData().MaxStack > amount)
            {
                this.amount += amount;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool RemoveItem(int amount = 1)
    {
        if (this.amount > amount)
        {
            this.amount -= amount;
            if (isEquipmentSlot)
            {
                isEquipped = false;
            }
            if (this.amount <= 0)
            {
                this.item = null;
            }
            return true;
        }
        else
        {
            return false;
        }
    }
}

#endregion

#region Item Data

public static class ItemDataExtensions
{
    public const string ICON_PATH = "Items/Icons/";
    public const string DATABASE_PATH = "Items/Database/";
    public const string DROP_TABLES_PATH = "Items/DropTables/";
}

[Serializable]
public class SerializableItemList
{
    public List<ItemData> items = new();
}

[Serializable]
public class DropTablesWrapper
{
    public List<DropTableData> dropTables = new();
}

[Serializable]
public class ItemData
{
    public Guid ID = Guid.NewGuid();
    public string Name;
    public string Description;
    public int MaxStack;
    public ItemType Type;
    public ItemRarity Rarity;
    public ElementType Element;
    public AccessoryType AccessoryType = AccessoryType.None;
    public ItemStatRangeData StatRanges = new();
    public ItemEffectRangeData EffectRanges = new();

    public ItemData()
    {
        ID = Guid.NewGuid();
        Name = "New Item";
        Type = ItemType.None;
        Rarity = ItemRarity.Common;
        Element = ElementType.None;
        AccessoryType = AccessoryType.None;
        MaxStack = 1;
        StatRanges = new ItemStatRangeData();
        EffectRanges = new ItemEffectRangeData();
    }

    [JsonIgnore]
    public List<StatModifier> Stats = new();

    [JsonIgnore]
    public List<ItemEffectData> Effects = new();

    [JsonIgnore]
    public Sprite Icon;

    [JsonIgnore]
    public int amount = 1;

    #region Stats Management
    public void AddStat(StatModifier stat)
    {
        if (stat == null)
            return;
        Stats.RemoveAll(s =>
            s.Type == stat.Type
            && s.Source == stat.Source
            && s.IncreaseType == stat.IncreaseType
            && Math.Abs(s.Value - stat.Value) < float.Epsilon
        );
        Stats.Add(stat);
    }

    public StatModifier GetStat(StatType statType) => Stats.FirstOrDefault(s => s.Type == statType);

    public float GetStatValue(StatType statType) => GetStat(statType)?.Value ?? 0f;

    public void ClearStats() => Stats.Clear();
    #endregion

    #region Effects Management
    public void AddEffect(ItemEffectData effect)
    {
        if (effect == null)
            return;
        Effects.Add(effect);
    }

    public void RemoveEffect(Guid effectId) => Effects.RemoveAll(e => e.effectId == effectId);

    public ItemEffectData GetEffect(Guid effectId) =>
        Effects.FirstOrDefault(e => e.effectId == effectId);

    public List<ItemEffectData> GetEffectsByType(EffectType type) =>
        Effects.Where(e => e.effectType == type).ToList();

    public List<ItemEffectData> GetEffectsForSkill(SkillType skillType) =>
        Effects.Where(e => e.applicableSkills?.Contains(skillType) ?? false).ToList();

    public List<ItemEffectData> GetEffectsForElement(ElementType elementType) =>
        Effects.Where(e => e.applicableElements?.Contains(elementType) ?? false).ToList();

    #endregion

    #region Cloning
    public ItemData Clone()
    {
        return JsonConvert.DeserializeObject<ItemData>(JsonConvert.SerializeObject(this));
    }
    #endregion

    public override bool Equals(object obj)
    {
        if (obj is ItemData other)
            return ID.Equals(other.ID);
        return false;
    }

    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }
}

#region Stat & Effects
[Serializable]
public class ItemStatRange
{
    public StatType statType;
    public float minValue;
    public float maxValue;
    public float weight = 1f;
    public IncreaseType increaseType = IncreaseType.Flat;
}

[Serializable]
public class ItemStatRangeData
{
    public List<ItemStatRange> possibleStats = new();
    public int minStatCount = 1;
    public int maxStatCount = 4;
}

[Serializable]
public class ItemEffectRange
{
    public Guid effectId = Guid.NewGuid();
    public string effectName;
    public string description;
    public EffectType effectType;
    public float minValue;
    public float maxValue;
    public float weight = 1f;
    public SkillType[] applicableSkills;
    public ElementType[] applicableElements;
}

[Serializable]
public class ItemEffectRangeData
{
    public List<ItemEffectRange> possibleEffects = new List<ItemEffectRange>();
    public int minEffectCount = 1;
    public int maxEffectCount = 3;
}

[Serializable]
public class ItemEffectData
{
    public Guid effectId = Guid.NewGuid();
    public string effectName;
    public EffectType effectType;
    public float value;
    public ItemRarity minRarity;
    public ItemType[] applicableTypes;
    public SkillType[] applicableSkills;
    public ElementType[] applicableElements;

    public bool CanApplyTo(
        ItemData item,
        SkillType skillType = SkillType.None,
        ElementType element = ElementType.None
    )
    {
        if (item.Rarity < minRarity)
            return false;
        if (applicableTypes != null && !applicableTypes.Contains(item.Type))
            return false;
        if (
            skillType != SkillType.None
            && applicableSkills != null
            && !applicableSkills.Contains(skillType)
        )
            return false;
        if (
            element != ElementType.None
            && applicableElements != null
            && !applicableElements.Contains(element)
        )
            return false;
        return true;
    }
}
#endregion

#region Drop Table

[Serializable]
public class DropTableEntry
{
    [SerializeField]
    public Guid itemId;

    [SerializeField]
    public float dropRate;

    [SerializeField]
    public ItemRarity rarity;

    [SerializeField]
    public int minAmount = 1;

    [SerializeField]
    public int maxAmount = 1;
}

[Serializable]
public class DropTableData
{
    [SerializeField]
    public MonsterType enemyType;

    [SerializeField]
    public List<DropTableEntry> dropEntries = new();

    [SerializeField]
    public float guaranteedDropRate = 0.1f;

    [SerializeField]
    public int maxDrops = 3;
}

#endregion

#endregion

#region Skill Data

[Serializable]
public class SkillLevelData
{
    public int level;

    public BaseSkillStat baseSkillStat;

    [SerializeReference]
    public ISkillStat skillStat;

    public ISkillStat GetStats(SkillType type)
    {
        switch (type)
        {
            case SkillType.Projectile:
                baseSkillStat = skillStat.baseStat;
                return skillStat as ProjectileSkillStat;
            case SkillType.Area:
                baseSkillStat = skillStat.baseStat;
                return skillStat as AreaSkillStat;
            case SkillType.Passive:
                baseSkillStat = skillStat.baseStat;
                return skillStat as PassiveSkillStat;
            default:
                return null;
        }
    }

    public void SetStats(ISkillStat stats)
    {
        if (stats == null)
            return;

        switch (stats)
        {
            case ProjectileSkillStat projectile:
                skillStat = new ProjectileSkillStat(projectile);
                baseSkillStat = skillStat.baseStat;
                break;
            case AreaSkillStat area:
                skillStat = new AreaSkillStat(area);
                baseSkillStat = skillStat.baseStat;
                break;
            case PassiveSkillStat passive:
                skillStat = new PassiveSkillStat(passive);
                baseSkillStat = skillStat.baseStat;
                break;
        }
    }
}

[Serializable]
public class SkillData : ICloneable
{
    #region Properties
    public SkillID ID;
    public string Name;
    public string Description;
    public SkillType Type;
    public ElementType Element;
    public int Tier;
    public string[] Tags;

    [JsonIgnore]
    public GameObject BasePrefab;

    [JsonIgnore]
    public List<SkillLevelData> levelDataList = new();

    [JsonIgnore]
    public Sprite Icon;

    [JsonIgnore]
    public GameObject ProjectilePrefab;

    [JsonIgnore]
    public GameObject[] PrefabsByLevel;

    [Header("Current Stat")]
    public BaseSkillStat currentBaseSkillStat;

    [SerializeReference]
    public ISkillStat currentSkillStat;

    #endregion

    public SkillData()
    {
        ID = SkillID.None;
        Name = "None";
        Description = "None";
        Type = SkillType.None;
        Element = ElementType.None;
        Tier = 0;
        Tags = new string[0];
        levelDataList = new List<SkillLevelData>();
        PrefabsByLevel = new GameObject[0];

        InitializeSkillStat();
    }

    private void InitializeSkillStat()
    {
        currentSkillStat = CreateDefaultStats();
    }

    public ISkillStat GetStatsForLevel(int level)
    {
        var stats = levelDataList.FirstOrDefault(x => x.level == level)?.GetStats(Type);

        if (stats != null)
            return stats;

        var defaultStats = CreateDefaultStats();
        SetStatsForLevel(level, defaultStats);
        return defaultStats;
    }

    public void SetStatsForLevel(int level, ISkillStat stats)
    {
        if (stats?.baseStat == null)
        {
            Debug.LogError("Attempting to set null stats");
            return;
        }

        try
        {
            UpdateCurrentStat(stats);

            var existingData = levelDataList.FirstOrDefault(x => x.level == level);

            if (existingData != null)
            {
                existingData.SetStats(stats);
            }
            else
            {
                var newLevelData = new SkillLevelData { level = level };
                newLevelData.SetStats(stats);
                levelDataList.Add(newLevelData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error setting stats: {e.Message}");
        }
    }

    private void UpdateCurrentStat(ISkillStat stats)
    {
        switch (stats)
        {
            case ProjectileSkillStat projectileStats:
                currentSkillStat = new ProjectileSkillStat(projectileStats);
                break;
            case AreaSkillStat areaStats:
                currentSkillStat = new AreaSkillStat(areaStats);
                break;
            case PassiveSkillStat passiveStats:
                currentSkillStat = new PassiveSkillStat(passiveStats);
                break;
            default:
                Debug.LogWarning($"Unknown skill stat type: {stats.GetType()}");
                break;
        }
    }

    public ISkillStat GetSkillStats()
    {
        return currentSkillStat;
    }

    private ISkillStat CreateDefaultStats()
    {
        switch (Type)
        {
            case SkillType.Projectile:
                return new ProjectileSkillStat();
            case SkillType.Area:
                return new AreaSkillStat();
            case SkillType.Passive:
                return new PassiveSkillStat();
            default:
                return null;
        }
    }

    //ICloneable
    public object Clone()
    {
        return new SkillData
        {
            ID = this.ID,
            Name = this.Name,
            Description = this.Description,
            Type = this.Type,
            Element = this.Element,
            Tier = this.Tier,
            Tags = (string[])this.Tags?.Clone(),
        };
    }
}

[Serializable]
public class BaseSkillStat
{
    public float damage;
    public int skillLevel;
    public int maxSkillLevel;
    public ElementType element;
    public float elementalPower;

    public BaseSkillStat()
    {
        damage = 10f;
        skillLevel = 1;
        maxSkillLevel = 5;
        element = ElementType.None;
        elementalPower = 1f;
    }

    public BaseSkillStat(BaseSkillStat source)
    {
        damage = source.damage;
        skillLevel = source.skillLevel;
        maxSkillLevel = source.maxSkillLevel;
        element = source.element;
        elementalPower = source.elementalPower;
    }
}

[Serializable]
public class ProjectileSkillStat : ISkillStat
{
    public BaseSkillStat baseStat { get; set; }
    public SkillType skillType => SkillType.Projectile;

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

    public ProjectileSkillStat() { }

    public ProjectileSkillStat(ProjectileSkillStat source)
    {
        baseStat = new BaseSkillStat(source.baseStat);
        projectileSpeed = source.projectileSpeed;
        projectileScale = source.projectileScale;
        shotInterval = source.shotInterval;
        pierceCount = source.pierceCount;
        attackRange = source.attackRange;
        homingRange = source.homingRange;
        isHoming = source.isHoming;
        explosionRad = source.explosionRad;
        projectileCount = source.projectileCount;
        innerInterval = source.innerInterval;
    }
}

[Serializable]
public class AreaSkillStat : ISkillStat
{
    public BaseSkillStat baseStat { get; set; }
    public SkillType skillType => SkillType.Area;

    public float radius;
    public float duration;
    public float tickRate;
    public bool isPersistent;
    public float moveSpeed;

    public AreaSkillStat() { }

    public AreaSkillStat(AreaSkillStat source)
    {
        baseStat = new BaseSkillStat(source.baseStat);
        radius = source.radius;
        duration = source.duration;
        tickRate = source.tickRate;
        isPersistent = source.isPersistent;
        moveSpeed = source.moveSpeed;
    }
}

[Serializable]
public class PassiveSkillStat : ISkillStat
{
    public BaseSkillStat baseStat { get; set; }
    public SkillType skillType => SkillType.Passive;

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
    public bool isPermanent;

    public PassiveSkillStat()
    {
        baseStat = new BaseSkillStat();
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

    public PassiveSkillStat(PassiveSkillStat source)
    {
        baseStat = new BaseSkillStat(source.baseStat);
        effectDuration = source.effectDuration;
        cooldown = source.cooldown;
        triggerChance = source.triggerChance;
        damageIncrease = source.damageIncrease;
        defenseIncrease = source.defenseIncrease;
        expAreaIncrease = source.expAreaIncrease;
        homingActivate = source.homingActivate;
        hpIncrease = source.hpIncrease;
        moveSpeedIncrease = source.moveSpeedIncrease;
        attackSpeedIncrease = source.attackSpeedIncrease;
        attackRangeIncrease = source.attackRangeIncrease;
        hpRegenIncrease = source.hpRegenIncrease;
    }
}

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

#endregion
