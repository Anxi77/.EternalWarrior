using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class ItemDataManager : Singleton<ItemDataManager>
{
    #region Constants
    private const string ITEM_DB_PATH = "Items/Database";
    private const string DROP_TABLES_PATH = "Items/DropTables";
    #endregion

    #region Fields
    private static Dictionary<string, ItemData> itemDatabase = new();
    private static Dictionary<MonsterType, DropTableData> dropTables = new();
    #endregion

    #region Data Loading
    public IEnumerator Initialize()
    {
        float progress = 0f;
        int steps = 0;

        yield return progress;
        yield return new WaitForSeconds(0.5f);
        LoadingManager.Instance.SetLoadingText("Loading Item Data...");

        var itemJSON = Resources.Load<TextAsset>($"{ITEM_DB_PATH}/ItemDatabase");
        SerializableItemList itemData = null;

        var dropTableJSON = Resources.Load<TextAsset>($"{DROP_TABLES_PATH}/DropTables");
        DropTablesWrapper dropTableData = null;

        if (itemJSON != null)
        {
            itemData = JsonConvert.DeserializeObject<SerializableItemList>(itemJSON.text);
            steps += itemData.items.Count;
        }
        else
        {
            Debug.LogError(
                $"[ItemDataManager] ItemDatabase.json not found at path: Resources/{ITEM_DB_PATH}/ItemDatabase"
            );
        }

        if (dropTableJSON != null)
        {
            dropTableData = JsonConvert.DeserializeObject<DropTablesWrapper>(dropTableJSON.text);
            steps += dropTableData.dropTables.Count;
        }
        else
        {
            Debug.LogError(
                $"[ItemDataManager] DropTables.json not found at path: Resources/{DROP_TABLES_PATH}/DropTables"
            );
        }

        if (itemData?.items != null)
        {
            itemDatabase = itemData.items.ToDictionary(item => item.ID);
            foreach (var item in itemDatabase)
            {
                Debug.Log(
                    $"[ItemDataManager] Loaded item [Name : {item.Value.Name}] [ID : {item.Value.ID}]"
                );

                progress += 1f / steps;
                yield return progress;
                yield return new WaitForSeconds(0.1f);
                LoadingManager.Instance.SetLoadingText(
                    $"Loading Item Data... {progress * 100f:F0}%"
                );
            }
        }
        else
        {
            Debug.LogError(
                "[ItemDataManager] Failed to deserialize item data or items list is null"
            );
        }
        Debug.Log($"[ItemDataManager] Total loaded item data count : {itemDatabase.Count}");

        yield return new WaitForSeconds(0.5f);
        LoadingManager.Instance.SetLoadingText("Loading Drop Tables...");

        if (dropTableJSON != null)
        {
            var wrapper = JsonConvert.DeserializeObject<DropTablesWrapper>(dropTableJSON.text);
            dropTables = wrapper.dropTables.ToDictionary(dt => dt.enemyType);
            foreach (var dropTable in dropTables)
            {
                progress += 1f / steps;
                yield return progress;
                yield return new WaitForSeconds(0.1f);
                LoadingManager.Instance.SetLoadingText(
                    $"Loading Drop Tables... {progress * 100f:F0}%"
                );
            }
        }
        else
        {
            Debug.LogError("No drop tables found.");
        }
    }

    public List<ItemData> GetAllData()
    {
        return new List<ItemData>(itemDatabase.Values);
    }

    private void LoadDropTables()
    {
        try { }
        catch (Exception e)
        {
            Debug.LogError($"Error loading drop tables: {e.Message}");
        }
    }
    #endregion

    #region Data Access

    public ItemData GetData(string itemId)
    {
        if (itemDatabase.TryGetValue(itemId, out var itemData))
        {
            return itemData.Clone();
        }
        Debug.LogWarning($"Item not found: {itemId}");
        return null;
    }

    public bool HasData(string itemId)
    {
        return itemDatabase.ContainsKey(itemId);
    }

    public Dictionary<string, ItemData> GetDatabase()
    {
        return new Dictionary<string, ItemData>(itemDatabase);
    }

    public Dictionary<MonsterType, DropTableData> GetDropTables()
    {
        return new Dictionary<MonsterType, DropTableData>(dropTables);
    }

    #endregion
}
