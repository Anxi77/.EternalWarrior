using System.Collections;
using UnityEngine;

public class TownStateHandler : BaseStateHandler
{
    public override void OnEnter()
    {
        base.OnEnter();

        if (Game != null && Game.player != null && Game.player.playerStatus == Player.Status.Dead)
        {
            RespawnPlayer();
        }
        else if (Game != null && Game.player == null)
        {
            Vector3 spawnPos = PlayerUnit.GetSpawnPosition(SceneType.Main_Town);
            PlayerUnit.SpawnPlayer(spawnPos);
        }

        StartCoroutine(InitializeTownAfterPlayerSpawn());
    }

    private void RespawnPlayer()
    {
        if (Game.player != null)
        {
            GameObject.Destroy(Game.player.gameObject);
            Game.player = null;
        }

        Vector3 spawnPos = PlayerUnit.GetSpawnPosition(SceneType.Main_Town);
        PlayerUnit.SpawnPlayer(spawnPos);

        if (Game.player != null)
        {
            Game.player.playerStatus = Player.Status.Alive;
            PlayerUnit.LoadGameState();
        }
    }

    private IEnumerator InitializeTownAfterPlayerSpawn()
    {
        while (Game != null && Game.player == null || !Game.player.IsInitialized)
        {
            yield return null;
        }

        InitializeTown();
    }

    private void InitializeTown()
    {
        if (Game != null && Game.player == null)
        {
            Debug.LogError("Cannot initialize town: Player is null");
            return;
        }

        GameManager.Instance.CameraSystem.SetupCamera(SceneType.Main_Town);

        if (GameManager.Instance.PathFindingSystem != null)
        {
            GameManager.Instance.PathFindingSystem.gameObject.SetActive(false);
        }

        if (UI != null)
        {
            if (UI.playerUIPanel != null)
            {
                UI.playerUIPanel.gameObject.SetActive(true);
                UI.playerUIPanel.InitializePlayerUI(Game.player);
            }

            Game.player.TryGetComponent(out Inventory inventory);

            if (inventory != null)
            {
                DataSystem.ItemDataSystem.LoadRuntimeData();
            }

            UI.InitializeInventoryUI();
            UI.SetInventoryAccessible(true);
            UI.UpdateInventoryUI();
        }

        LoadingManager.Instance.SpawnGameStagePortal();
    }

    public override void OnExit()
    {
        base.OnExit();

        if (UI != null)
        {
            UI.SetInventoryAccessible(false);
            UI.HideInventory();
        }
    }
}
