using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerDataManager : Singleton<PlayerDataManager>
{
    private const string SAVE_FOLDER = "PlayerData";
    private string SAVE_PATH => Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
    private const string DEFAULT_SAVE_SLOT = "DefaultSave";

    private StatData currentPlayerStatData;
    private InventoryData currentInventoryData;
    private LevelData currentLevelData = new LevelData { level = 1, exp = 0f };
    public StatData CurrentPlayerStatData => currentPlayerStatData;
    public InventoryData CurrentInventoryData => currentInventoryData;

    public IEnumerator Initialize()
    {
        float progress = 0f;
        yield return progress;
        yield return new WaitForSeconds(0.5f);

        LoadingManager.Instance.SetLoadingText("Loading Player Data...");

        yield return new WaitForSeconds(0.5f);

        var data = JSONIO<PlayerData>.LoadData(SAVE_PATH, DEFAULT_SAVE_SLOT);
        if (data != null)
        {
            currentPlayerStatData = data.stats;
            currentInventoryData = data.inventory;
            currentLevelData = data.levelData;
        }
        else
        {
            CreateDefaultFiles();
        }

        progress += 1f;
        yield return progress;
    }

    public void CreateDefaultFiles()
    {
        currentPlayerStatData = new StatData();
        currentInventoryData = new InventoryData();
        currentLevelData = new LevelData { level = 1, exp = 0f };
        JSONIO<PlayerData>.SaveData(
            SAVE_PATH,
            DEFAULT_SAVE_SLOT,
            new PlayerData
            {
                stats = currentPlayerStatData,
                inventory = currentInventoryData,
                levelData = currentLevelData,
            }
        );
    }

    public void ClearAllRuntimeData()
    {
        currentPlayerStatData = new StatData();
        currentInventoryData = new InventoryData();
        currentLevelData = new LevelData { level = 1, exp = 0f };
        JSONIO<PlayerData>.DeleteData(SAVE_PATH, DEFAULT_SAVE_SLOT);
    }

    public void SavePlayerData(PlayerData data)
    {
        JSONIO<PlayerData>.SaveData(SAVE_PATH, DEFAULT_SAVE_SLOT, data);
    }

    public PlayerData LoadPlayerData()
    {
        return JSONIO<PlayerData>.LoadData(SAVE_PATH, DEFAULT_SAVE_SLOT);
    }

    public void SaveInventoryData(InventoryData data)
    {
        currentInventoryData = data;
        try
        {
            EnsureDirectoryExists();
            JSONIO<InventoryData>.SaveData(SAVE_PATH, DEFAULT_SAVE_SLOT, currentInventoryData);
            Debug.Log($"Successfully saved inventory data to: {DEFAULT_SAVE_SLOT}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving inventory data: {e.Message}");
        }
    }

    private void EnsureDirectoryExists()
    {
        string savePath = Path.Combine(Application.persistentDataPath, SAVE_PATH);
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            Debug.Log($"Created directory: {savePath}");
        }
    }

    public bool HasSaveData()
    {
        return File.Exists(DEFAULT_SAVE_SLOT);
    }

    public void SaveRuntimeData()
    {
        SavePlayerData(
            new PlayerData
            {
                stats = currentPlayerStatData,
                inventory = currentInventoryData,
                levelData = currentLevelData,
            }
        );
    }
}
