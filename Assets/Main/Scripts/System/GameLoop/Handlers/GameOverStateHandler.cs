using UnityEngine;

public class GameOverStateHandler : BaseStateHandler
{
    private bool portalSpawned = false;

    public override void OnEnter()
    {
        base.OnEnter();
        Debug.Log("Entering Game Over state");

        UIManager.Instance.OpenPanel(PanelType.GameOver);

        if (Game != null && Game.Player != null)
        {
            PlayerSystem.SpawnPlayer(Vector3.zero);
            PlayerSystem.LoadGameState();
            Debug.Log("Player respawned at death location");

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
        if (Game != null && Game.Player != null)
        {
            Vector3 playerPos = Game.Player.transform.position;
            Vector3 portalPosition = playerPos + new Vector3(2f, 0f, 0f);
            GameManager.Instance.SpawnPortal(portalPosition, SceneType.Main_Town);
            Debug.Log("Town portal spawned near player's death location");
        }
    }
}
