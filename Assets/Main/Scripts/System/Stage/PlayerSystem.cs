using System;
using UnityEngine;

public class PlayerSystem : MonoBehaviour, IInitializable
{
    private Player playerPrefab;
    private Vector3 defaultSpawnPosition = Vector3.zero;
    private Player player;
    public Player Player => player;
    public PlayerInfoPanel playerInfoPanel;
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        playerPrefab = Resources.Load<Player>("Prefabs/Units/Player");
        IsInitialized = true;
    }

    public void SpawnPlayer(Vector3 position, PlayerInfoPanel playerInfoPanel = null)
    {
        Player player = Instantiate(playerPrefab, position, Quaternion.identity)
            .GetComponent<Player>();

        var playerData = PlayerDataManager.Instance.CurrentPlayerData;
        var inventoryData = playerData.inventory;
        var saveData = playerData.stats;

        this.player = player;

        player.Initialize(saveData, inventoryData);

        if (player.playerStat != null)
        {
            if (PlayerDataManager.Instance.HasSaveData())
            {
                LoadGameState();
            }
        }

        if (player.animationController != null)
        {
            player.animationController.Initialize();
        }

        player.playerStatus = Player.Status.Alive;
        player.StartCombatSystems();
        if (playerInfoPanel != null)
        {
            playerInfoPanel.InitializePlayerUI(player);
        }
    }

    public void DespawnPlayer()
    {
        if (player != null)
        {
            Destroy(player.gameObject);
            player = null;
        }

        if (playerInfoPanel != null)
        {
            playerInfoPanel.Close();
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
            Logger.LogWarning(typeof(PlayerSystem), "Player is not initialized");
            return;
        }

        var playerStat = player.GetComponent<PlayerStat>();
        var inventory = player.GetComponent<Inventory>();

        if (playerStat != null && inventory != null)
        {
            PlayerData data = new PlayerData();
            data.stats = playerStat.CreateSaveData();
            data.inventory = inventory.GetInventoryData();
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.SavePlayerData(data);
            }
        }
    }

    public void LoadGameState()
    {
        if (player == null)
        {
            Logger.LogWarning(typeof(PlayerSystem), "Player is not initialized");
            return;
        }

        var playerStat = player.GetComponent<PlayerStat>();
        var inventory = player.GetComponent<Inventory>();

        if (playerStat != null && inventory != null)
        {
            var savedData = PlayerDataManager.Instance.LoadPlayerData();
            if (savedData != null)
            {
                playerStat.Initialize(savedData.stats);
                inventory.LoadInventoryData(savedData.inventory);
            }
        }
    }
}
