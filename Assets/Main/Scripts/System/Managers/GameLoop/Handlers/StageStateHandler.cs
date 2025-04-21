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

        UI.SetInventoryAccessible(false);
        UI.HideInventory();

        if (Game != null && Game.player == null)
        {
            Vector3 spawnPos = PlayerUnit.GetSpawnPosition(SceneType.Main_Stage);
            PlayerUnit.SpawnPlayer(spawnPos);
            StartCoroutine(InitializeStageAfterPlayerSpawn());
        }
        else
        {
            InitializeStage();
        }
    }

    private IEnumerator InitializeStageAfterPlayerSpawn()
    {
        while (UI.IsLoadingScreenVisible())
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);
        InitializeStage();
    }

    private void InitializeStage()
    {
        if (Game.HasSaveData())
        {
            PlayerUnit.LoadGameState();
        }

        GameManager.Instance.CameraSystem.SetupCamera(SceneType.Main_Stage);

        if (GameManager.Instance.PathFindingSystem != null)
        {
            GameManager.Instance.PathFindingSystem.gameObject.SetActive(true);
            GameManager.Instance.PathFindingSystem.InitializeWithNewCamera();
        }

        if (UI != null && UI.playerUIPanel != null)
        {
            UI.playerUIPanel.gameObject.SetActive(true);
            UI.playerUIPanel.InitializePlayerUI(Game.player);
        }

        Game.StartLevelCheck();

        StartCoroutine(GameManager.Instance.StageTimer.StartStageTimer(STAGE_DURATION));

        UI.stageTimeUI.gameObject.SetActive(true);
        isInitialized = true;

        StartCoroutine(StartMonsterSpawningWhenReady());
    }

    private IEnumerator StartMonsterSpawningWhenReady()
    {
        yield return new WaitForSeconds(0.5f);

        if (MonsterManager.Instance != null)
        {
            MonsterManager.Instance.StartSpawning();
        }
    }

    public override void OnExit()
    {
        isInitialized = false;

        base.OnExit();

        MonsterManager.Instance?.StopSpawning();
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
        UI?.ShowBossWarning();
        MonsterManager.Instance?.SpawnStageBoss();
    }

    public void OnBossDefeated(Vector3 position)
    {
        LoadingManager.Instance?.SpawnTownPortal(position);
    }
}
