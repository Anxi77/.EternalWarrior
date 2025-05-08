using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverStateHandler : BaseStateHandler
{
    private bool portalSpawned = false;

    public override void OnEnter()
    {
        base.OnEnter();

        UIManager.Instance.OpenPanel(PanelType.GameOver);

        if (Game.PlayerSystem.Player != null)
        {
            PlayerSystem.SpawnPlayer(Vector3.zero);

            PlayerSystem.Player.Initialize(
                PlayerDataManager.Instance.CurrentPlayerData.stats,
                PlayerDataManager.Instance.CurrentPlayerData.inventory
            );

            GameManager.Instance.CameraSystem.SetupCamera(SceneType.Main_Stage);

            if (!portalSpawned)
            {
                SpawnTownPortal();
                portalSpawned = true;
            }
        }
    }

    public override void OnExit()
    {
        UIManager.Instance.ClosePanel(PanelType.GameOver);
        portalSpawned = false;

        //Todo: Call LoadingManager to clear all things

        base.OnExit();
    }

    private void SpawnTownPortal()
    {
        if (Game != null && Game.PlayerSystem.Player != null)
        {
            Vector3 playerPos = Game.PlayerSystem.Player.transform.position;
            Vector3 portalPosition = playerPos + new Vector3(2f, 0f, 0f);
            GameManager.Instance.SpawnPortal(
                portalPosition,
                SceneType.Main_Town,
                OnTownPortalEnter
            );
        }
    }

    private void OnTownPortalEnter(SceneType sceneType)
    {
        List<Func<IEnumerator>> loadSceneRoutines = new List<Func<IEnumerator>> { LoadTownScene };

        PlayerDataManager.Instance.SavePlayerData(PlayerDataManager.Instance.CurrentPlayerData);

        LoadingManager.Instance.LoadScene(
            sceneType,
            loadSceneRoutines,
            () =>
            {
                PlayerSystem.SpawnPlayer(Vector3.zero);
            }
        );
    }

    public IEnumerator LoadTownScene()
    {
        Game.ChangeState(GameState.Town);
        float progress = 0f;
        yield return new WaitForSeconds(1f);
        yield return progress;
        progress += 0.2f;
        yield return new WaitForSeconds(1f);
        yield return progress;
        progress += 0.2f;
        yield return new WaitForSeconds(1f);
        yield return progress;
        progress += 0.2f;
        yield return new WaitForSeconds(1f);
        yield return progress;
        progress += 0.2f;
        yield return new WaitForSeconds(1f);
        yield return progress;
        progress += 0.2f;
        yield return new WaitForSeconds(1f);
        yield return progress;
    }
}
