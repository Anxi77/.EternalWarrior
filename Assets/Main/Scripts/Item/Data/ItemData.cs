using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

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
    public ItemType Type;
    public ItemRarity Rarity;
    public ElementType Element;
    public AccessoryType AccessoryType = AccessoryType.None;
    public int MaxStack;
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
    private Sprite _icon;

    [JsonIgnore]
    public Sprite Icon
    {
        get
        {
            if (_icon == null && !string.IsNullOrEmpty(IconResourceName))
            {
                _icon = Resources.Load<Sprite>(IconResourceName);
            }
            return _icon;
        }
    }

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
