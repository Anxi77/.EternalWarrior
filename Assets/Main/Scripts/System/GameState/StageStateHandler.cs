using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StageStateHandler : BaseStateHandler
{
    private const float STAGE_DURATION = 600f;
    private bool isBossPhase = false;
    private bool isInitialized = false;
    private Tilemap obstacleTilemap;
    private Tilemap terrainTilemap;

    public override void OnEnter()
    {
        base.OnEnter();
        LoadingManager.Instance.LoadScene(
            SceneType.Main_Stage,
            () =>
            {
                Game.PlayerSystem.SpawnPlayer(Vector3.zero);
                obstacleTilemap = GameObject.FindWithTag("Obstacle").GetComponent<Tilemap>();
                terrainTilemap = GameObject.FindWithTag("Terrain").GetComponent<Tilemap>();
                InitializeStage();
                isInitialized = true;
            }
        );
    }

    private void InitializeStage()
    {
        PlayerInfoPanel playerInfoPanel =
            UIManager.Instance.OpenPanel(PanelType.PlayerInfo) as PlayerInfoPanel;

        playerInfoPanel.InitializePlayerUI(GameManager.Instance.PlayerSystem.Player);

        GameManager.Instance.CameraSystem.SetupCamera(SceneType.Main_Stage);

        if (GameManager.Instance.PathFindingSystem != null)
        {
            GameManager.Instance.PathFindingSystem.gameObject.SetActive(true);
            GameManager.Instance.PathFindingSystem.InitializeGrid(terrainTilemap, obstacleTilemap);
        }

        GameManager.Instance.StageTimer.StartStageTimer(STAGE_DURATION);

        UIManager.Instance.OpenPanel(PanelType.StageTime);
        isInitialized = true;

        StartSpawn();
    }

    private void StartSpawn()
    {
        GameManager.Instance.MonsterSystem.StartSpawning();
    }

    public override void OnExit()
    {
        isInitialized = false;

        GameManager.Instance.PathFindingSystem.ResetRuntimeData();

        base.OnExit();

        GameManager.Instance.MonsterSystem.StopSpawning();
        GameManager.Instance.StageTimer?.PauseTimer();
        GameManager.Instance.StageTimer?.ResetTimer();
        GameManager.Instance.CameraSystem?.ClearCamera();

        if (GameManager.Instance.PathFindingSystem != null)
        {
            GameManager.Instance.PathFindingSystem.gameObject.SetActive(false);
        }

        UIManager.Instance.ClosePanel(PanelType.PlayerInfo);
    }

    public override void OnUpdate()
    {
        if (!isInitialized)
            return;

        if (!isBossPhase && GameManager.Instance.StageTimer.IsStageTimeUp())
        {
            StartBossPhase();
        }
    }

    private void StartBossPhase()
    {
        isBossPhase = true;
        UIManager.Instance.OpenPanel(PanelType.BossWarning);
        GameManager.Instance.MonsterSystem.SpawnStageBoss();
    }

    public void OnBossDefeated(Vector3 position)
    {
        GameManager.Instance.SpawnPortal(position, SceneType.Main_Town, OnTownPortalEnter);
    }

    private void OnTownPortalEnter(SceneType sceneType)
    {
        Game.ChangeState(GameState.Town);
    }
}
