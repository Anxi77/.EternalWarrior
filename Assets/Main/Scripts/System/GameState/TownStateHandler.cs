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

        GameManager.Instance.SpawnPortal(
            new Vector3(5f, 0f, 5f),
            SceneType.Main_Stage,
            OnStagePortalEnter
        );
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
