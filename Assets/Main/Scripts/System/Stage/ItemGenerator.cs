using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemGenerator : MonoBehaviour
{
    public Dictionary<ItemRarity, int> additionalEffectsByRarity = new Dictionary<ItemRarity, int>
    {
        { ItemRarity.Common, 0 },
        { ItemRarity.Uncommon, 1 },
        { ItemRarity.Rare, 2 },
        { ItemRarity.Epic, 3 },
        { ItemRarity.Legendary, 4 },
    };

    public Dictionary<ItemRarity, int> additionalStatsByRarity = new()
    {
        { ItemRarity.Common, 0 },
        { ItemRarity.Uncommon, 1 },
        { ItemRarity.Rare, 2 },
        { ItemRarity.Epic, 3 },
        { ItemRarity.Legendary, 4 },
    };

    public Item GenerateItem(Guid itemId, ItemRarity? targetRarity = null)
    {
        var itemData = ItemDataManager.Instance.GetData(itemId).Clone();

        if (targetRarity.HasValue)
        {
            itemData.Rarity = targetRarity.Value;
        }

        GenerateStats(itemData);

        GenerateEffects(itemData);

        switch (itemData.Type)
        {
            case ItemType.Weapon:
                return new WeaponItem(itemData);
            case ItemType.Armor:
                return new ArmorItem(itemData);
            case ItemType.Accessory:
                return new AccessoryItem(itemData);
            default:
                return null;
        }
    }

    private void GenerateStats(ItemData item)
    {
        if (item.StatRanges == null || item.StatRanges.possibleStats == null)
        {
            Logger.LogWarning(
                typeof(ItemGenerator),
                $"No stat ranges defined for item: {item.Name}"
            );
            return;
        }

        item.Stats.Clear();

        int additionalStats = additionalStatsByRarity.GetValueOrDefault(item.Rarity, 0);
        int statCount = Random.Range(
            item.StatRanges.minStatCount,
            Mathf.Min(
                item.StatRanges.maxStatCount + additionalStats + 1,
                item.StatRanges.possibleStats.Count
            )
        );

        Logger.Log(typeof(ItemGenerator), $"Generating {statCount} stats for item {item.Name}");

        var availableStats = item.StatRanges.possibleStats.ToList();

        for (int i = 0; i < statCount && availableStats.Any(); i++)
        {
            var selectedStat = SelectStatByWeight(availableStats);
            if (selectedStat != null)
            {
                float value = GenerateStatValue(selectedStat, item.Rarity);
                SourceType sourceType = (SourceType)
                    Enum.Parse(typeof(SourceType), item.Type.ToString());
                item.AddStat(
                    new StatModifier(selectedStat.statType, sourceType, IncreaseType.Flat, value)
                );

                Logger.Log(typeof(ItemGenerator), $"Added stat: {selectedStat.statType} = {value}");
                availableStats.Remove(selectedStat);
            }
        }
    }

    private void GenerateEffects(ItemData item)
    {
        if (item.EffectRanges == null || item.EffectRanges.possibleEffects == null)
        {
            Logger.LogWarning(
                typeof(ItemGenerator),
                $"No effect ranges defined for item: {item.Name}"
            );
            return;
        }

        item.Effects.Clear();

        int additionalEffects = additionalEffectsByRarity.GetValueOrDefault(item.Rarity, 0);
        int effectCount = Random.Range(
            item.EffectRanges.minEffectCount,
            Mathf.Min(
                item.EffectRanges.maxEffectCount + additionalEffects + 1,
                item.EffectRanges.possibleEffects.Count
            )
        );

        Logger.Log(typeof(ItemGenerator), $"Generating {effectCount} effects for item {item.Name}");

        var availableEffects = item.EffectRanges.possibleEffects.ToList();

        for (int i = 0; i < effectCount && availableEffects.Any(); i++)
        {
            var selectedEffect = SelectEffectByWeight(availableEffects);
            if (selectedEffect != null)
            {
                float value = GenerateEffectValue(selectedEffect, item.Rarity);
                var effectData = new ItemEffectData
                {
                    effectId = selectedEffect.effectId,
                    effectName = selectedEffect.effectName,
                    effectType = selectedEffect.effectType,
                    value = value,
                    applicableSkills = selectedEffect.applicableSkills,
                    applicableElements = selectedEffect.applicableElements,
                };

                item.AddEffect(effectData);
                Logger.Log(
                    typeof(ItemGenerator),
                    $"Added effect: {effectData.effectName} with value {value}"
                );
                availableEffects.Remove(selectedEffect);
            }
        }
    }

    private ItemStatRange SelectStatByWeight(List<ItemStatRange> stats)
    {
        float totalWeight = stats.Sum(s => s.weight);
        float randomValue = (float)(Random.value * totalWeight);

        float currentWeight = 0;
        foreach (var stat in stats)
        {
            currentWeight += stat.weight;
            if (randomValue <= currentWeight)
            {
                return stat;
            }
        }

        return stats.LastOrDefault();
    }

    private ItemEffectRange SelectEffectByWeight(List<ItemEffectRange> effects)
    {
        float totalWeight = effects.Sum(e => e.weight);
        float randomValue = (float)(Random.value * totalWeight);

        float currentWeight = 0;
        foreach (var effect in effects)
        {
            currentWeight += effect.weight;
            if (randomValue <= currentWeight)
            {
                return effect;
            }
        }

        return effects.LastOrDefault();
    }

    private float GenerateStatValue(ItemStatRange statRange, ItemRarity rarity)
    {
        float baseValue = (float)(
            Random.value * (statRange.maxValue - statRange.minValue) + statRange.minValue
        );

        float rarityMultiplier = 1 + ((int)rarity * 0.2f);
        float finalValue = baseValue * rarityMultiplier;

        switch (statRange.increaseType)
        {
            case IncreaseType.Flat:
                finalValue = Mathf.Round(finalValue);
                break;
            case IncreaseType.Multiply:
                finalValue = Mathf.Round(finalValue * 100) / 100;
                break;
        }

        return finalValue;
    }

    private float GenerateEffectValue(ItemEffectRange effectRange, ItemRarity rarity)
    {
        float baseValue = (float)(
            Random.value * (effectRange.maxValue - effectRange.minValue) + effectRange.minValue
        );
        float rarityMultiplier = 1 + ((int)rarity * 0.2f);
        return baseValue * rarityMultiplier;
    }

    public List<Item> GenerateDrops(DropTableData dropTable, float luckMultiplier = 1f)
    {
        if (dropTable == null || dropTable.dropEntries == null)
        {
            Logger.LogWarning(typeof(ItemGenerator), "Invalid drop table");
            return new List<Item>();
        }

        var drops = new List<Item>();
        int dropCount = 0;

        if (Random.value < dropTable.guaranteedDropRate)
        {
            var guaranteedDrop = GenerateGuaranteedDrop(dropTable);
            if (guaranteedDrop != null)
            {
                drops.Add(guaranteedDrop);
                dropCount++;
            }
        }

        foreach (var entry in dropTable.dropEntries)
        {
            if (dropCount >= dropTable.maxDrops)
                break;

            float adjustedDropRate = entry.dropRate * luckMultiplier;
            if (Random.value < adjustedDropRate)
            {
                var item = GenerateItem(entry.itemId, entry.rarity);
                if (item != null)
                {
                    drops.Add(item);
                    dropCount++;
                    Logger.Log(
                        typeof(ItemGenerator),
                        $"Generated drop: {item.GetItemData().Name} x{dropCount}"
                    );
                }
            }
        }

        return drops;
    }

    private Item GenerateGuaranteedDrop(DropTableData dropTable)
    {
        float totalWeight = dropTable.dropEntries.Sum(entry => entry.dropRate);
        float randomValue = Random.value * totalWeight;

        float currentWeight = 0;
        foreach (var entry in dropTable.dropEntries)
        {
            currentWeight += entry.dropRate;
            if (randomValue <= currentWeight)
            {
                var item = GenerateItem(entry.itemId, entry.rarity);
                if (item != null)
                {
                    Logger.Log(
                        typeof(ItemGenerator),
                        $"Generated guaranteed drop: {item.GetItemData().Name} x 1"
                    );
                    return item;
                }
            }
        }

        return null;
    }
}
