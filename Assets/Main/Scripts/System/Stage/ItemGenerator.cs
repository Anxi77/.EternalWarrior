using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemGenerator : MonoBehaviour
{
    public Item GenerateItem(Guid itemId, ItemRarity? targetRarity = null)
    {
        var itemData = ItemDataManager.Instance.GetData(itemId);

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

        int statCount = Random.Range(
            item.StatRanges.minStatCount,
            Mathf.Min(item.StatRanges.maxStatCount + 1, item.StatRanges.possibleStats.Count)
        );

        Logger.Log(
            typeof(ItemGenerator),
            $"Max stat count: {item.StatRanges.maxStatCount} , possible stats count: {item.StatRanges.possibleStats.Count}"
        );

        Logger.Log(typeof(ItemGenerator), $"Generating {statCount} stats for item {item.Name}");

        var availableStats = item.StatRanges.possibleStats.ToList();

        if (item.Type == ItemType.Weapon)
        {
            var damageStat = availableStats.Find(s => s.statType == StatType.Damage);
            if (damageStat != null)
            {
                float value = GenerateStatValue(damageStat);
                item.AddStat(
                    new StatModifier(
                        damageStat.statType,
                        SourceType.Weapon,
                        IncreaseType.Flat,
                        value
                    )
                );
            }
            availableStats.Remove(damageStat);
        }
        else if (item.Type == ItemType.Armor)
        {
            var defenseStat = availableStats.Find(s => s.statType == StatType.Defense);
            if (defenseStat != null)
            {
                float value = GenerateStatValue(defenseStat);
                item.AddStat(
                    new StatModifier(
                        defenseStat.statType,
                        SourceType.Armor,
                        IncreaseType.Flat,
                        value
                    )
                );
            }
            availableStats.Remove(defenseStat);
        }

        for (int i = 0; i < statCount && availableStats.Any(); i++)
        {
            var selectedStat = SelectStatByWeight(availableStats);
            if (selectedStat != null)
            {
                float value = GenerateStatValue(selectedStat);
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
        if (item.EffectRanges == null || item.EffectRanges.effectIDs == null)
        {
            Logger.LogWarning(
                typeof(ItemGenerator),
                $"No effect ranges defined for item: {item.Name}"
            );
            return;
        }

        item.Effects.Clear();

        int effectCount = Random.Range(
            item.EffectRanges.minEffectCount,
            Mathf.Min(item.EffectRanges.maxEffectCount + 1, item.EffectRanges.effectIDs.Count)
        );

        Logger.Log(typeof(ItemGenerator), $"Generating {effectCount} effects for item {item.Name}");

        var availableEffects = item
            .EffectRanges.effectIDs.Select(id => ItemDataManager.Instance.GetEffectRange(id))
            .ToList();

        for (int i = 0; i < effectCount && availableEffects.Any(); i++)
        {
            var selectedEffect = SelectEffectByWeight(availableEffects);
            if (selectedEffect != null)
            {
                var effectData = new ItemEffect
                {
                    effectId = selectedEffect.effectId,
                    effectName = selectedEffect.effectName,
                    description = selectedEffect.description,
                    applicableSkills = selectedEffect.applicableSkills,
                    applicableElements = selectedEffect.applicableElements,
                    subEffects = new List<SubEffect>(),
                };

                foreach (var subEffectRange in selectedEffect.subEffectRanges)
                {
                    if (!subEffectRange.isEnabled)
                        continue;

                    float value = GenerateEffectValue(subEffectRange);
                    var subEffect = new SubEffect
                    {
                        effectType = subEffectRange.effectType,
                        value = value,
                        isEnabled = true,
                    };

                    effectData.subEffects.Add(subEffect);
                }

                item.AddEffect(effectData);
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

    private float GenerateStatValue(ItemStatRange statRange)
    {
        float baseValue = (float)(
            Random.value * (statRange.maxValue - statRange.minValue) + statRange.minValue
        );

        float finalValue = baseValue;

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

    private float GenerateEffectValue(SubEffectRange effectRange)
    {
        float baseMin = effectRange.minValue;
        float baseMax = effectRange.maxValue;

        float adjustedMin = baseMin;
        float adjustedMax = baseMax;

        return Random.Range(adjustedMin, adjustedMax);
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
