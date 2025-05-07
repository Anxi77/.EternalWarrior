using System.Collections;
using System.Drawing.Printing;
using UnityEngine;

public class TownStateHandler : BaseStateHandler
{
    public override void OnEnter()
    {
        base.OnEnter();

        if (
            Game.PlayerSystem.Player != null
            && Game.PlayerSystem.Player.playerStatus == Player.Status.Dead
        )
        {
            GameManager.Instance.RespawnPlayer();
        }
        else if (Game != null && Game.PlayerSystem.Player == null)
        {
            Vector3 spawnPos = PlayerSystem.GetSpawnPosition(SceneType.Main_Town);
            PlayerSystem.SpawnPlayer(spawnPos);
        }

        InitializeTown();
    }

    private void InitializeTown()
    {
        if (Game != null && Game.PlayerSystem.Player == null)
        {
            Logger.LogError(typeof(TownStateHandler), "Cannot initialize town: Player is null");
            return;
        }

        GameManager.Instance.CameraSystem.SetupCamera(SceneType.Main_Town);

        if (UIManager.Instance.GetPanel(PanelType.PlayerInfo) != null)
        {
            UIManager.Instance.GetPanel(PanelType.PlayerInfo).gameObject.SetActive(true);
            PlayerInfoPanel playerPanel =
                UIManager.Instance.GetPanel(PanelType.PlayerInfo) as PlayerInfoPanel;
            playerPanel.InitializePlayerUI(Game.PlayerSystem.Player);
        }

        Game.PlayerSystem.Player.TryGetComponent(out Inventory inventory);

        if (inventory != null)
        {
            ItemDataManager.Instance.Initialize();
        }

        GameManager.Instance.SpawnPortal(
            new Vector3(5f, 0f, 5f),
            SceneType.Main_Stage,
            OnStagePortalEnter
        );

        Game.PlayerSystem.Player.Initialize();

        Game.ItemSystem.Initialize();

        Item item = GameManager.Instance.ItemSystem.TestItem();

        Logger.Log(
            typeof(TownStateHandler),
            $"Item Generated \n"
                + $"ItemName : {item.GetItemData().Name}\n"
                + $"ItemType : {item.GetItemData().Type}\n"
                + $"ItemRarity : {item.GetItemData().Rarity}\n"
                + $"ItemDescription : {item.GetItemData().Description}\n"
                + $"ItemStat : {item.GetItemData().StatRanges}\n"
                + $"ItemEffect : {item.GetItemData().Effects}\n"
        );

        GameManager.Instance.PlayerSystem.Player.inventory.AddItem(item);
    }

    private void OnStagePortalEnter(SceneType sceneType)
    {
        Game.ChangeState(GameState.Stage);
    }

    public override void OnExit()
    {
        base.OnExit();
    }
}
