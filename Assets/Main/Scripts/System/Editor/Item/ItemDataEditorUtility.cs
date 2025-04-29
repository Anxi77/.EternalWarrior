using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class ItemDataEditorUtility
{
    #region Constants
    private const string RESOURCE_ROOT = "Assets/Resources";
    private const string ITEM_DB_PATH = "Items/Database";
    private const string ITEM_ICON_PATH = "Items/Icons";
    private const string DROP_TABLES_PATH = "Items/DropTables";
    #endregion

    #region Data Management
    private static Dictionary<Guid, ItemData> itemDatabase = new();
    private static Dictionary<MonsterType, DropTableData> dropTables = new();

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

            SaveResource(itemData);
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
        ResourceIO<Sprite>.SaveData(resourcePath, itemData.Icon);
        Logger.Log(typeof(ItemDataEditorUtility), $"Icon saved to Resources path: {resourcePath}");
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
            Logger.Log(
                typeof(ItemDataEditorUtility),
                $"Item {itemId} and its resources deleted successfully"
            );
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
            var wrapper = new SerializableItemList { items = itemDatabase.Values.ToList() };
            JSONIO<SerializableItemList>.SaveData(ITEM_DB_PATH, "ItemDatabase", wrapper);
            Logger.Log(typeof(ItemDataEditorUtility), "Database saved successfully");
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

            var wrapper = new DropTablesWrapper { dropTables = dropTables.Values.ToList() };
            JSONIO<DropTablesWrapper>.SaveData(DROP_TABLES_PATH, "DropTables", wrapper);
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
        if (itemData == null || index < 0 || index >= itemData.EffectRanges.possibleEffects.Count)
            return;
        itemData.EffectRanges.possibleEffects.RemoveAt(index);
        SaveItemData(itemData);
    }

    public static void AddEffectRange(ItemData itemData)
    {
        if (itemData == null)
            return;
        itemData.EffectRanges.possibleEffects.Add(new ItemEffectRange());
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

    #endregion

    #region Private Methods
    private static void LoadItemDatabase()
    {
        try
        {
            var data = JSONIO<SerializableItemList>.LoadData("Items/Database", "ItemDatabase");
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
            var data = JSONIO<DropTablesWrapper>.LoadData("Items/DropTables", "DropTables");
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
            else
            {
                Logger.LogWarning(
                    typeof(ItemDataEditorUtility),
                    $"Failed to load icon for item {item.ID}. Paths tried:\n1. {ItemDataExtensions.ICON_PATH + item.ID + "_Icon.png"}\n2. {path}"
                );
            }
        }
    }

    private static void CreateDefaultDropTables()
    {
        dropTables = new Dictionary<MonsterType, DropTableData>
        {
            {
                MonsterType.Normal,
                new DropTableData
                {
                    enemyType = MonsterType.Normal,
                    guaranteedDropRate = 0.1f,
                    maxDrops = 2,
                    dropEntries = new List<DropTableEntry>(),
                }
            },
            {
                MonsterType.Elite,
                new DropTableData
                {
                    enemyType = MonsterType.Elite,
                    guaranteedDropRate = 0.3f,
                    maxDrops = 3,
                    dropEntries = new List<DropTableEntry>(),
                }
            },
            {
                MonsterType.Boss,
                new DropTableData
                {
                    enemyType = MonsterType.Boss,
                    guaranteedDropRate = 1f,
                    maxDrops = 5,
                    dropEntries = new List<DropTableEntry>(),
                }
            },
        };
        SaveDropTables();
    }

    public static void InitializeDefaultData()
    {
        try
        {
            EnsureDirectoryStructure();

            foreach (var item in itemDatabase.Values)
            {
                if (File.Exists(ItemDataExtensions.ICON_PATH + item.ID + ".png"))
                {
                    string iconPath =
                        $"Assets/Resources/{ItemDataExtensions.ICON_PATH + item.ID + ".png"}";
                    if (File.Exists(iconPath))
                    {
                        AssetDatabase.DeleteAsset(iconPath);
                        Logger.Log(typeof(ItemDataEditorUtility), $"Deleted icon file: {iconPath}");
                    }
                }
            }

            itemDatabase.Clear();
            SaveDatabase();

            CreateDefaultDropTables();

            AssetDatabase.Refresh();
            Logger.Log(typeof(ItemDataEditorUtility), "Data reset successfully");
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

    #endregion
}
