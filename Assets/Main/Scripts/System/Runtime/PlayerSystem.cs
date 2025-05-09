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

    public void SpawnPlayer(Vector3 position)
    {
        Player player = Instantiate(playerPrefab, position, Quaternion.identity)
            .GetComponent<Player>();

        var playerData = PlayerDataManager.Instance.CurrentPlayerData;
        var inventoryData = playerData.inventory;
        var saveData = playerData.stats;

        this.player = player;

        player.Initialize(saveData, inventoryData);

        if (player.animationController != null)
        {
            player.animationController.Initialize();
        }

        player.playerStatus = Player.Status.Alive;

        player.StartCombatSystems();
    }

    public void DespawnPlayer()
    {
        SavePlayerData();

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
                return new Vector3(0, 0, 0);
            case SceneType.Test:
                return new Vector3(0, 0, 0);
            default:
                return Vector3.zero;
        }
    }

    public void SavePlayerData()
    {
        if (player == null)
        {
            Logger.LogWarning(typeof(PlayerSystem), "Player is not initialized");
            return;
        }

        var playerStat = player.GetComponent<StatSystem>();
        var inventory = player.GetComponent<Inventory>();

        if (playerStat != null && inventory != null)
        {
            PlayerData data = new PlayerData();
            data.stats = playerStat.GetSaveData();
            data.inventory = inventory.GetSaveData();
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.SavePlayerData(data);
            }
        }
    }
}
