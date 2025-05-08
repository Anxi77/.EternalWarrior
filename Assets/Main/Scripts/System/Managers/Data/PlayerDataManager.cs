using System;
using System.Collections;
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

    public PlayerData CurrentPlayerData =>
        new PlayerData
        {
            stats = currentPlayerStatData,
            inventory = currentInventoryData,
            levelData = currentLevelData,
        };

    public IEnumerator Initialize()
    {
        float progress = 0f;
        yield return progress;
        yield return new WaitForSeconds(0.5f);

        LoadingManager.Instance.SetLoadingText("Loading Player Data...");

        yield return new WaitForSeconds(0.5f);

        LoadPlayerData();

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

    public void LoadPlayerData()
    {
        var data = JSONIO<PlayerData>.LoadData(SAVE_PATH, DEFAULT_SAVE_SLOT);

        if (data != null)
        {
            currentLevelData = data.levelData;
            currentPlayerStatData = data.stats;
            currentInventoryData = data.inventory;
        }
        else
        {
            CreateDefaultFiles();
        }
    }

    public void SaveInventoryData(InventoryData data)
    {
        currentInventoryData = data;
        try
        {
            EnsureDirectoryExists();
            JSONIO<InventoryData>.SaveData(SAVE_PATH, DEFAULT_SAVE_SLOT, currentInventoryData);
            Logger.Log(
                typeof(PlayerDataManager),
                $"Successfully saved inventory data to: {DEFAULT_SAVE_SLOT}"
            );
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(PlayerDataManager), $"Error saving inventory data: {e.Message}");
        }
    }

    private void EnsureDirectoryExists()
    {
        string savePath = Path.Combine(Application.persistentDataPath, SAVE_PATH);
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            Logger.Log(typeof(PlayerDataManager), $"Created directory: {savePath}");
        }
    }

    public bool HasSaveData()
    {
        return File.Exists(DEFAULT_SAVE_SLOT);
    }
}
