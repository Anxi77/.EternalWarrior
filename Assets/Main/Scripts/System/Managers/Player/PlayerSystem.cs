using System;
using UnityEngine;

public class PlayerSystem : MonoBehaviour, IInitializable
{
    private Player playerPrefab;
    private Vector3 defaultSpawnPosition = Vector3.zero;
    private Player player;
    public Player Player => player;
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        try
        {
            playerPrefab = Resources.Load<Player>("Units/Player");
            IsInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing PlayerSystem: {e.Message}");
            IsInitialized = false;
        }
    }

    public void SpawnPlayer(Vector3 position)
    {
        try
        {
            Player player = Instantiate(playerPrefab, position, Quaternion.identity);

            player.Initialize();

            this.player = player;

            if (player.playerStat != null)
            {
                if (PlayerDataManager.Instance.HasSaveData())
                {
                    LoadGameState();
                }
            }

            if (player.characterControl != null)
            {
                player.characterControl.Initialize();
            }

            player.playerStatus = Player.Status.Alive;

            player.StartCombatSystems();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error spawning player: {e.Message}");
        }
    }

    public void DespawnPlayer()
    {
        if (player != null)
        {
            Destroy(player.gameObject);
            player = null;
        }
    }

    public Vector3 GetSpawnPosition(SceneType sceneType)
    {
        switch (sceneType)
        {
            case SceneType.Main_Town:
                return new Vector3(0, 0, 0);
            case SceneType.Main_Stage:
            case SceneType.Test:
                return defaultSpawnPosition;
            default:
                return Vector3.zero;
        }
    }

    public void SaveGameState()
    {
        if (player == null)
        {
            Debug.LogWarning("Player is not initialized");
            return;
        }

        var playerStat = player.GetComponent<PlayerStatSystem>();
        var inventory = player.GetComponent<Inventory>();

        if (playerStat != null && inventory != null)
        {
            PlayerData data = new PlayerData();
            data.stats = playerStat.CreateSaveData();
            data.inventory = inventory.GetInventoryData();
            PlayerDataManager.Instance.SavePlayerData(data);
        }
    }

    public void LoadGameState()
    {
        if (player == null)
        {
            Debug.LogWarning("Player is not initialized");
            return;
        }

        var playerStat = player.GetComponent<PlayerStatSystem>();
        var inventory = player.GetComponent<Inventory>();

        if (playerStat != null && inventory != null)
        {
            var savedData = PlayerDataManager.Instance.LoadPlayerData();
            if (savedData != null)
            {
                playerStat.LoadFromSaveData(savedData.stats);
                inventory.LoadInventoryData(savedData.inventory);
            }
        }
    }

    public void ClearTemporaryEffects()
    {
        if (player.playerStat != null)
        {
            player.playerStat.RemoveStatsBySource(SourceType.Buff);
            player.playerStat.RemoveStatsBySource(SourceType.Debuff);
            player.playerStat.RemoveStatsBySource(SourceType.Consumable);
        }
        player.ResetPassiveEffects();
    }
}
