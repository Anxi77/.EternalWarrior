using System.Collections;
using UnityEngine;

public class StageStateHandler : BaseStateHandler
{
    private const float STAGE_DURATION = 600f;
    private bool isBossPhase = false;
    private bool isInitialized = false;

    public override void OnEnter()
    {
        base.OnEnter();
        isInitialized = false;

        UIManager.Instance.ClosePanel(PanelType.Inventory);

        if (Game.PlayerSystem.Player == null)
        {
            Vector3 spawnPos = PlayerSystem.GetSpawnPosition(SceneType.Main_Stage);
            PlayerSystem.SpawnPlayer(spawnPos);
        }
        else
        {
            InitializeStage();
        }
    }

    private void InitializeStage()
    {
        if (Game.HasSaveData())
        {
            PlayerSystem.LoadGameState();
        }

        GameManager.Instance.CameraSystem.SetupCamera(SceneType.Main_Stage);

        if (GameManager.Instance.PathFindingSystem != null)
        {
            GameManager.Instance.PathFindingSystem.gameObject.SetActive(true);
            GameManager.Instance.PathFindingSystem.InitializeWithNewCamera();
        }

        if (UIManager.Instance.GetPanel(PanelType.PlayerInfo) != null)
        {
            UIManager.Instance.GetPanel(PanelType.PlayerInfo).gameObject.SetActive(true);
            PlayerInfoPanel playerPanel =
                UIManager.Instance.GetPanel(PanelType.PlayerInfo) as PlayerInfoPanel;
            playerPanel.InitializePlayerUI(Game.PlayerSystem.Player);
        }

        Game.StartLevelCheck();

        GameManager.Instance.StageTimer.StartStageTimer(STAGE_DURATION);

        UIManager.Instance.OpenPanel(PanelType.StageTime);
        isInitialized = true;

        StartMonsterSpawningWhenReady();
    }

    private IEnumerator StartMonsterSpawningWhenReady()
    {
        yield return new WaitForSeconds(0.5f);

        GameManager.Instance.MonsterSystem.StartSpawning();
    }

    public override void OnExit()
    {
        isInitialized = false;

        base.OnExit();

        GameManager.Instance.MonsterSystem.StopSpawning();
        GameManager.Instance.StageTimer?.PauseTimer();
        GameManager.Instance.StageTimer?.ResetTimer();
        GameManager.Instance.CameraSystem?.ClearCamera();

        if (GameManager.Instance.PathFindingSystem != null)
        {
            GameManager.Instance.PathFindingSystem.gameObject.SetActive(false);
        }
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
