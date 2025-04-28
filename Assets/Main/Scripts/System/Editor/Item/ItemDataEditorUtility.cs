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
            Debug.LogError($"Error saving item data: {e.Message}\n{e.StackTrace}");
        }
    }

    public static void SaveResource(ItemData itemData)
    {
        string resourcePath = $"Items/Icons/{itemData.ID}_Icon";
        ResourceIO<Sprite>.SaveData(resourcePath, itemData.Icon);
        Debug.Log($"Icon saved to Resources path: {resourcePath}");
    }

    public static void DeleteItemData(Guid itemId)
    {
        if (itemId == Guid.Empty)
            return;

        try
        {
            if (itemDatabase.TryGetValue(itemId, out var item) && itemDatabase.Remove(itemId))
            {
                if (File.Exists(ItemDataExtensions.ICON_PATH + item.ID + ".png"))
                {
                    string iconPath =
                        $"Assets/Resources/{ItemDataExtensions.ICON_PATH + item.ID + ".png"}";
                    if (File.Exists(iconPath))
                    {
                        AssetDatabase.DeleteAsset(iconPath);
                        Debug.Log($"Deleted icon file: {iconPath}");
                    }
                }

                SaveDatabase();

                foreach (var dropTable in dropTables.Values)
                {
                    dropTable.dropEntries.RemoveAll(entry => entry.itemId == itemId);
                }
                SaveDropTables();

                AssetDatabase.Refresh();
                Debug.Log($"Item {itemId} and its resources deleted successfully");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting item {itemId}: {e.Message}\n{e.StackTrace}");
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
            Debug.Log($"Database saved successfully");
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving database: {e.Message}\n{e.StackTrace}");
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
            Debug.LogError($"Error saving drop tables: {e.Message}\n{e.StackTrace}");
        }
    }

    public static void SaveWithBackup()
    {
        string backupPath = Path.Combine(RESOURCE_ROOT, ITEM_DB_PATH, "Backups");
        Directory.CreateDirectory(backupPath);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupFile = Path.Combine(backupPath, $"ItemDatabase_Backup_{timestamp}.json");

        var wrapper = new SerializableItemList { items = itemDatabase.Values.ToList() };
        File.WriteAllText(backupFile, JsonConvert.SerializeObject(wrapper));

        Debug.Log($"Backup created at: {backupFile}");
        AssetDatabase.Refresh();
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
                Debug.LogWarning("No item database found or empty database");
                itemDatabase = new Dictionary<Guid, ItemData>();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading item database: {e.Message}\n{e.StackTrace}");
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
                Debug.LogWarning("No drop tables found");
                dropTables = new Dictionary<MonsterType, DropTableData>();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading drop tables: {e.Message}");
            dropTables = new Dictionary<MonsterType, DropTableData>();
        }
    }

    private static void LoadItemResources()
    {
        foreach (var item in itemDatabase.Values)
        {
            try
            {
                if (File.Exists(ItemDataExtensions.ICON_PATH + item.ID + ".png"))
                {
                    var icon = ResourceIO<Sprite>.LoadData(
                        ItemDataExtensions.ICON_PATH + item.ID + ".png"
                    );
                    if (icon != null)
                    {
                        Debug.Log($"Successfully loaded icon for item {item.ID}");
                    }
                    else
                    {
                        string alternativePath = $"Items/Icons/{item.ID}_Icon";
                        Debug.Log($"Trying alternative path for item {item.ID}: {alternativePath}");
                        icon = ResourceIO<Sprite>.LoadData(alternativePath);

                        if (icon != null)
                        {
                            Debug.Log(
                                $"Successfully loaded icon from alternative path for item {item.ID}"
                            );
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"Failed to load icon for item {item.ID}. Paths tried:\n1. {ItemDataExtensions.ICON_PATH + item.ID + ".png"}\n2. {alternativePath}"
                            );
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"No icon path specified for item {item.ID}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Error loading resources for item {item.ID}: {e.Message}\n{e.StackTrace}"
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

            // 기존 아이템들의 리소스 정리
            foreach (var item in itemDatabase.Values)
            {
                if (File.Exists(ItemDataExtensions.ICON_PATH + item.ID + ".png"))
                {
                    string iconPath =
                        $"Assets/Resources/{ItemDataExtensions.ICON_PATH + item.ID + ".png"}";
                    if (File.Exists(iconPath))
                    {
                        AssetDatabase.DeleteAsset(iconPath);
                        Debug.Log($"Deleted icon file: {iconPath}");
                    }
                }
            }

            // 데이터베이스 초기화
            itemDatabase.Clear();
            SaveDatabase();

            // 드롭 테이블 초기화
            CreateDefaultDropTables();

            AssetDatabase.Refresh();
            Debug.Log("Data reset successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error resetting data: {e.Message}");
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
                Debug.Log($"Created directory: {path}");
            }
        }
        AssetDatabase.Refresh();
    }

    public static void DrawDropTableEntry(DropTableData dropTable, int index, out bool shouldRemove)
    {
        shouldRemove = false;
        var entry = dropTable.dropEntries[index];

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField($"Entry {index + 1}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                shouldRemove = true;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (!shouldRemove)
        {
            EditorGUILayout.Space(2);
            // 아이템 선택
            var items = itemDatabase.Values.Select(item => item.Name).ToArray();
            int selectedIndex = Array.FindIndex(
                items,
                name =>
                    itemDatabase.Values.FirstOrDefault(item => item.Name == name)?.ID
                    == entry.itemId
            );
            EditorGUI.indentLevel++;
            int newIndex = EditorGUILayout.Popup("Item", selectedIndex, items);
            if (newIndex != selectedIndex && newIndex >= 0)
            {
                entry.itemId = itemDatabase.Values.ElementAt(newIndex).ID;
                GUI.changed = true;
            }

            float newDropRate = EditorGUILayout.Slider("Drop Rate", entry.dropRate, 0f, 1f);
            if (newDropRate != entry.dropRate)
            {
                entry.dropRate = newDropRate;
                GUI.changed = true;
            }

            ItemRarity newRarity = (ItemRarity)
                EditorGUILayout.EnumPopup("Min Rarity", entry.rarity);
            if (newRarity != entry.rarity)
            {
                entry.rarity = newRarity;
                GUI.changed = true;
            }

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Amount Range", GUILayout.Width(100));
                int newMinAmount = EditorGUILayout.IntField(entry.minAmount, GUILayout.Width(50));
                if (newMinAmount != entry.minAmount)
                {
                    entry.minAmount = newMinAmount;
                    GUI.changed = true;
                }

                EditorGUILayout.LabelField("to", GUILayout.Width(20));
                int newMaxAmount = EditorGUILayout.IntField(entry.maxAmount, GUILayout.Width(50));
                if (newMaxAmount != entry.maxAmount)
                {
                    entry.maxAmount = newMaxAmount;
                    GUI.changed = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }
    }
    #endregion
}
