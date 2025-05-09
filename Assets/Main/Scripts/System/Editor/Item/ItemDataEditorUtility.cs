using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ItemDataEditorUtility
{
    #region Constants
    private const string RESOURCE_ROOT = "Assets/Resources";
    private const string ITEM_DB_PATH = "Items/Database";
    private const string ITEM_ICON_PATH = "Items/Icons";
    private const string DROP_TABLES_PATH = "Items/DropTables";
    private const string EFFECT_RANGES_PATH = "Items/EffectRanges";
    #endregion

    #region Data Management
    private static Dictionary<Guid, ItemData> itemDatabase = new();
    private static Dictionary<MonsterType, DropTableData> dropTables = new();
    private static ItemEffectRangeDatabase effectRangeDatabase = new();

    public static Dictionary<Guid, ItemData> GetItemDatabase()
    {
        if (!itemDatabase.Any())
        {
            LoadItemDatabase();
        }
        return new Dictionary<Guid, ItemData>(itemDatabase);
    }

    public static Dictionary<MonsterType, DropTableData> GetDropTables()
    {
        if (!dropTables.Any())
        {
            LoadDropTables();
        }
        return new Dictionary<MonsterType, DropTableData>(dropTables);
    }

    public static void SaveItemData(ItemData itemData)
    {
        if (itemData == null)
            return;

        try
        {
            var clonedData = itemData.Clone();
            itemDatabase[itemData.ID] = clonedData;
            if (itemData.Icon != null)
            {
                SaveResource(itemData);
            }
            SaveDatabase();
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(ItemDataEditorUtility),
                $"Error saving item data: {e.Message}\n{e.StackTrace}"
            );
        }
    }

    public static void SaveResource(ItemData itemData)
    {
        string resourcePath = $"Items/Icons/{itemData.ID}_Icon";
        if (itemData.Icon != null)
        {
            ResourceIO<Sprite>.SaveData(resourcePath, itemData.Icon);
            Logger.Log(
                typeof(ItemDataEditorUtility),
                $"Icon saved to Resources path: {resourcePath}"
            );
        }
    }

    public static void DeleteItemData(Guid itemId)
    {
        if (itemId == Guid.Empty)
            return;

        if (itemDatabase.TryGetValue(itemId, out var item) && itemDatabase.Remove(itemId))
        {
            ResourceIO<Sprite>.DeleteData(ItemDataExtensions.ICON_PATH + item.ID + "_Icon.png");

            SaveDatabase();

            foreach (var dropTable in dropTables.Values)
            {
                dropTable.dropEntries.RemoveAll(entry => entry.itemId == itemId);
            }
            SaveDropTables();

            AssetDatabase.Refresh();
        }
    }

    public static void ClearItemDatabase()
    {
        itemDatabase.Clear();
        SaveDatabase();
    }

    public static void ClearDropTables()
    {
        dropTables.Clear();
        SaveDropTables();
    }

    public static void SaveDatabase()
    {
        try
        {
            var wrapper = new ItemList { items = itemDatabase.Values.ToList() };
            JSONIO<ItemList>.SaveData(ITEM_DB_PATH, "ItemDatabase", wrapper);
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(ItemDataEditorUtility),
                $"Error saving database: {e.Message}\n{e.StackTrace}"
            );
        }
    }

    public static void SaveDropTables()
    {
        try
        {
            if (dropTables == null || !dropTables.Any())
            {
                CreateDefaultDropTables();
                return;
            }

            var wrapper = new DropTableList { dropTables = dropTables.Values.ToList() };
            JSONIO<DropTableList>.SaveData(DROP_TABLES_PATH, "DropTables", wrapper);
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(ItemDataEditorUtility),
                $"Error saving drop tables: {e.Message}\n{e.StackTrace}"
            );
        }
    }

    public static void SaveStatRanges(ItemData itemData)
    {
        if (itemData == null)
            return;
        SaveItemData(itemData);
    }

    public static void SaveEffects(ItemData itemData)
    {
        if (itemData == null)
            return;
        SaveItemData(itemData);
    }

    public static void RemoveStatRange(ItemData itemData, int index)
    {
        if (itemData == null || index < 0 || index >= itemData.StatRanges.possibleStats.Count)
            return;
        itemData.StatRanges.possibleStats.RemoveAt(index);
        SaveItemData(itemData);
    }

    public static void AddStatRange(ItemData itemData)
    {
        if (itemData == null)
            return;
        itemData.StatRanges.possibleStats.Add(new ItemStatRange());
        SaveItemData(itemData);
    }

    public static void RemoveEffectRange(ItemData itemData, int index)
    {
        if (itemData == null || index < 0 || index >= itemData.EffectRanges.effectIDs.Count)
            return;
        itemData.EffectRanges.effectIDs.RemoveAt(index);
        SaveItemData(itemData);
    }

    public static void AddEffectRange(ItemData itemData)
    {
        if (itemData == null)
            return;
        itemData.EffectRanges.effectIDs.Add(Guid.NewGuid());
        SaveItemData(itemData);
    }

    public static void UpdateSkillTypes(
        ItemEffectRange effectRange,
        SkillType skillType,
        bool isSelected
    )
    {
        if (effectRange == null)
            return;
        var list = new List<SkillType>(effectRange.applicableSkills ?? new SkillType[0]);
        if (isSelected)
            list.Add(skillType);
        else
            list.Remove(skillType);
        effectRange.applicableSkills = list.ToArray();
    }

    public static void UpdateElementTypes(
        ItemEffectRange effectRange,
        ElementType elementType,
        bool isSelected
    )
    {
        if (effectRange == null)
            return;
        var list = new List<ElementType>(effectRange.applicableElements ?? new ElementType[0]);
        if (isSelected)
            list.Add(elementType);
        else
            list.Remove(elementType);
        effectRange.applicableElements = list.ToArray();
    }

    public static ItemEffectRangeDatabase GetEffectRangeDatabase()
    {
        if (effectRangeDatabase.effectRanges.Count == 0)
        {
            LoadEffectRangeDatabase();
        }
        return effectRangeDatabase;
    }

    public static void SaveEffectRange(ItemEffectRange range)
    {
        if (range == null)
            return;

        try
        {
            effectRangeDatabase.AddEffectRange(range);
            SaveEffectRangeDatabase();
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(ItemDataEditorUtility),
                $"Error saving effect range: {e.Message}\n{e.StackTrace}"
            );
        }
    }

    public static void DeleteEffectRange(Guid rangeId)
    {
        if (rangeId == Guid.Empty)
            return;

        effectRangeDatabase.RemoveEffectRange(rangeId);
        SaveEffectRangeDatabase();

        foreach (var item in itemDatabase.Values)
        {
            item.EffectRanges.effectIDs.RemoveAll(id => id == rangeId);
        }
        SaveDatabase();
    }

    public static void SaveEffectRangeDatabase()
    {
        try
        {
            JSONIO<ItemEffectRangeDatabase>.SaveData(
                EFFECT_RANGES_PATH,
                "EffectRangeDatabase",
                effectRangeDatabase
            );
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(ItemDataEditorUtility),
                $"Error saving effect range database: {e.Message}\n{e.StackTrace}"
            );
        }
    }

    #endregion

    #region Private Methods
    private static void LoadItemDatabase()
    {
        try
        {
            var data = JSONIO<ItemList>.LoadData("Items/Database", "ItemDatabase");
            if (data != null && data.items != null)
            {
                itemDatabase = data.items.ToDictionary(item => item.ID);
                LoadItemResources();
            }
            else
            {
                Logger.LogWarning(
                    typeof(ItemDataEditorUtility),
                    "No item database found or empty database"
                );
                itemDatabase = new Dictionary<Guid, ItemData>();
            }
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(ItemDataEditorUtility),
                $"Error loading item database: {e.Message}\n{e.StackTrace}"
            );
            itemDatabase = new Dictionary<Guid, ItemData>();
        }
    }

    private static void LoadDropTables()
    {
        try
        {
            var data = JSONIO<DropTableList>.LoadData("Items/DropTables", "DropTables");
            if (data != null && data.dropTables != null)
            {
                dropTables = data.dropTables.ToDictionary(dt => dt.enemyType);
            }
            else
            {
                Logger.LogWarning(typeof(ItemDataEditorUtility), "No drop tables found");
                dropTables = new Dictionary<MonsterType, DropTableData>();
            }
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(ItemDataEditorUtility),
                $"Error loading drop tables: {e.Message}"
            );
            dropTables = new Dictionary<MonsterType, DropTableData>();
        }
    }

    private static void LoadItemResources()
    {
        foreach (var item in itemDatabase.Values)
        {
            string path = $"Items/Icons/{item.ID}_Icon";
            var icon = ResourceIO<Sprite>.LoadData(path);
            if (icon != null)
            {
                item.Icon = icon;
            }
        }
    }

    private static void CreateDefaultDropTables()
    {
        dropTables = new Dictionary<MonsterType, DropTableData>
        {
            {
                MonsterType.Wasp,
                new DropTableData
                {
                    enemyType = MonsterType.Wasp,
                    guaranteedDropRate = 0.1f,
                    maxDrops = 2,
                    dropEntries = new List<DropTableEntry>(),
                }
            },
            {
                MonsterType.Bat,
                new DropTableData
                {
                    enemyType = MonsterType.Bat,
                    guaranteedDropRate = 0.3f,
                    maxDrops = 3,
                    dropEntries = new List<DropTableEntry>(),
                }
            },
            {
                MonsterType.Ogre,
                new DropTableData
                {
                    enemyType = MonsterType.Ogre,
                    guaranteedDropRate = 1f,
                    maxDrops = 5,
                    dropEntries = new List<DropTableEntry>(),
                }
            },
        };
        SaveDropTables();
    }

    public static void InitializeDefaultEffectRanges()
    {
        EnsureDirectoryStructure();
        effectRangeDatabase = new ItemEffectRangeDatabase();
        SaveEffectRangeDatabase();
    }

    public static void InitializeDefaultDropTables()
    {
        EnsureDirectoryStructure();
        dropTables.Clear();
        CreateDefaultDropTables();
        SaveDropTables();
    }

    public static void InitializeDefaultItemData()
    {
        try
        {
            EnsureDirectoryStructure();

            foreach (var item in itemDatabase.Values)
            {
                string iconPath = $"{ItemDataExtensions.ICON_PATH}{item.ID}_Icon.png";
                ResourceIO<Sprite>.DeleteData(iconPath);
            }

            itemDatabase.Clear();
            SaveDatabase();

            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(ItemDataEditorUtility), $"Error resetting data: {e.Message}");
            throw;
        }
    }

    private static void EnsureDirectoryStructure()
    {
        var paths = new[]
        {
            Path.Combine(RESOURCE_ROOT, ITEM_DB_PATH),
            Path.Combine(RESOURCE_ROOT, ITEM_ICON_PATH),
        };

        foreach (var path in paths)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Logger.Log(typeof(ItemDataEditorUtility), $"Created directory: {path}");
            }
        }
        AssetDatabase.Refresh();
    }

    private static void LoadEffectRangeDatabase()
    {
        try
        {
            var data = JSONIO<ItemEffectRangeDatabase>.LoadData(
                "Items/EffectRanges",
                "EffectRangeDatabase"
            );
            if (data != null)
            {
                effectRangeDatabase = data;
            }
            else
            {
                Logger.LogWarning(typeof(ItemDataEditorUtility), "No effect range database found");
                effectRangeDatabase = new ItemEffectRangeDatabase();
            }
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(ItemDataEditorUtility),
                $"Error loading effect range database: {e.Message}\n{e.StackTrace}"
            );
            effectRangeDatabase = new ItemEffectRangeDatabase();
        }
    }

    #endregion
}
