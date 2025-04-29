using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameState
{
    void OnEnter();
    void OnUpdate();
    void OnFixedUpdate();
    void OnExit();
}

public class GameManager : Singleton<GameManager>
{
    private List<Monster> monsters = new();
    public List<Monster> Monsters => monsters;

    private StageTimer stageTimer;
    public StageTimer StageTimer => stageTimer;
    private CameraSystem cameraSystem;
    public CameraSystem CameraSystem => cameraSystem;
    private ItemSystem itemSystem;
    public ItemSystem ItemSystem => itemSystem;
    private SkillSystem skillSystem;
    public SkillSystem SkillSystem => skillSystem;
    private PathFindingSystem pathFindingSystem;
    public PathFindingSystem PathFindingSystem => pathFindingSystem;
    private PlayerSystem playerSystem;
    public PlayerSystem PlayerSystem => playerSystem;
    private MonsterSystem monsterSystem;
    public MonsterSystem MonsterSystem => monsterSystem;

    private int lastPlayerLevel = 1;
    private Coroutine levelCheckCoroutine;
    private GameState currentState = GameState.Initialize;

    private Dictionary<GameState, IGameState> stateHandlers;

    private bool isStateTransitioning = false;

    private readonly Queue<GameState> stateTransitionQueue = new();

    private readonly WaitForSeconds LOADING_TIME = new WaitForSeconds(0.3f);

    public bool IsInitialized { get; private set; }

    [SerializeField]
    private Portal portalPrefab;

    private void Start()
    {
        UIManager.Instance.Initialize();
        LoadingManager.Instance.Initialize();

        List<Func<IEnumerator>> operations = new()
        {
            SkillDataManager.Instance.Initialize,
            ItemDataManager.Instance.Initialize,
            PlayerDataManager.Instance.Initialize,
            LoadSystems,
        };

        LoadingManager.Instance.LoadScene(
            SceneType.Main_Title,
            operations,
            () =>
            {
                ChangeState(GameState.Title);
            }
        );
    }

    public IEnumerator LoadSystems()
    {
        float progress = 0f;
        int steps = 8;
        yield return progress;
        yield return LOADING_TIME;
        LoadingManager.Instance.SetLoadingText("Initializing Skill System...");

        skillSystem = new GameObject("SkillSystem").AddComponent<SkillSystem>();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;
        itemSystem = new GameObject("ItemSystem").AddComponent<ItemSystem>();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;
        playerSystem = new GameObject("PlayerSystem").AddComponent<PlayerSystem>();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;
        stageTimer = new GameObject("StageTimer").AddComponent<StageTimer>();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;
        cameraSystem = new GameObject("CameraSystem").AddComponent<CameraSystem>();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;
        pathFindingSystem = new GameObject("PathFindingSystem").AddComponent<PathFindingSystem>();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;
        monsterSystem = new GameObject("MonsterSystem").AddComponent<MonsterSystem>();
        yield return LOADING_TIME;
        CreateStateHandlers();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;

        progress = 1f;
        yield return progress;
    }

    private void CreateStateHandlers()
    {
        stateHandlers = new Dictionary<GameState, IGameState>();

        stateHandlers[GameState.Title] = new TitleStateHandler();
        stateHandlers[GameState.Town] = new TownStateHandler();
        stateHandlers[GameState.Stage] = new StageStateHandler();
        stateHandlers[GameState.Paused] = new PausedStateHandler();
        stateHandlers[GameState.GameOver] = new GameOverStateHandler();
    }

    public void ChangeState(GameState newState)
    {
        if (stateHandlers == null)
            return;

        Logger.Log(
            typeof(GameManager),
            $"[GameManager] Gamesate transition from [{currentState}] to [{newState}]"
        );

        stateTransitionQueue.Enqueue(newState);

        if (!isStateTransitioning)
        {
            ProcessStateQueue();
        }
    }

    private void ProcessStateQueue()
    {
        if (stateTransitionQueue.Count == 0)
            return;

        try
        {
            isStateTransitioning = true;
            GameState newState = stateTransitionQueue.Dequeue();

            if (currentState == newState)
            {
                isStateTransitioning = false;
                ProcessStateQueue();
                return;
            }

            if (stateHandlers.ContainsKey(currentState))
            {
                stateHandlers[currentState].OnExit();
            }

            currentState = newState;

            if (stateHandlers.ContainsKey(currentState))
            {
                stateHandlers[currentState].OnEnter();
            }

            isStateTransitioning = false;

            if (stateTransitionQueue.Count > 0)
            {
                ProcessStateQueue();
            }
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(GameManager),
                $"Error during state change: {e.Message}\n{e.StackTrace}"
            );
            isStateTransitioning = false;
        }
    }

    private void Update()
    {
        if (!IsInitialized || stateHandlers == null)
            return;

        stateHandlers[currentState]?.OnUpdate();
    }

    private void FixedUpdate()
    {
        if (!IsInitialized || stateHandlers == null)
            return;

        stateHandlers[currentState]?.OnFixedUpdate();
    }

    public T GetCurrentHandler<T>()
        where T : class, IGameState
    {
        if (!IsInitialized || stateHandlers == null)
            return null;

        if (stateHandlers.TryGetValue(currentState, out var handler))
        {
            return handler as T;
        }
        return null;
    }

    protected override void OnDestroy()
    {
        IsInitialized = false;
        stateHandlers?.Clear();
        StopAllCoroutines();

        base.OnDestroy();
    }

    public void StartLevelCheck()
    {
        if (levelCheckCoroutine != null)
        {
            StopCoroutine(levelCheckCoroutine);
            levelCheckCoroutine = null;
        }

        if (PlayerSystem.Player != null && PlayerSystem.Player.playerStatus != Player.Status.Dead)
        {
            levelCheckCoroutine = StartCoroutine(CheckLevelUp());
        }
    }

    private IEnumerator CheckLevelUp()
    {
        if (PlayerSystem.Player == null)
        {
            Logger.LogError(typeof(GameManager), "Player reference is null in GameManager");
            yield break;
        }

        lastPlayerLevel = PlayerSystem.Player.level;

        while (true)
        {
            if (
                PlayerSystem.Player == null
                || PlayerSystem.Player.playerStatus == Player.Status.Dead
            )
            {
                levelCheckCoroutine = null;
                yield break;
            }

            if (PlayerSystem.Player.level > lastPlayerLevel)
            {
                lastPlayerLevel = PlayerSystem.Player.level;
                OnPlayerLevelUp();
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void OnPlayerLevelUp()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenPanel(PanelType.LevelUp);
        }
    }

    public void SpawnPortal(Vector3 position, SceneType destinationType, Action<SceneType> onEnter)
    {
        if (portalPrefab != null)
        {
            Portal portal = Instantiate(portalPrefab, position, Quaternion.identity);
            portal.Initialize(destinationType, onEnter);
        }
    }

    public void RespawnPlayer()
    {
        if (PlayerSystem.Player != null)
        {
            PlayerSystem.DespawnPlayer();
        }

        Vector3 spawnPos = PlayerSystem.GetSpawnPosition(SceneType.Main_Town);
        PlayerSystem.SpawnPlayer(spawnPos);

        if (PlayerSystem.Player != null)
        {
            PlayerSystem.Player.playerStatus = Player.Status.Alive;
            PlayerSystem.LoadGameState();
        }
    }

    #region Game State Management
    public void InitializeNewGame()
    {
        LoadingManager.Instance.LoadScene(
            SceneType.Main_Town,
            NewGameRoutine,
            () =>
            {
                ChangeState(GameState.Town);
            }
        );
    }

    public IEnumerator NewGameRoutine()
    {
        float progress = 0f;
        yield return progress;
        yield return new WaitForSeconds(0.5f);
        LoadingManager.Instance.SetLoadingText("Initializing New Game...");

        playerSystem.Initialize();
        cameraSystem.Initialize();

        yield return 1.0f;
    }

    public void SaveGameData()
    {
        if (PlayerSystem.Player != null)
        {
            PlayerSystem.SaveGameState();
        }
    }

    public void LoadGameData()
    {
        if (PlayerSystem.Player != null)
        {
            PlayerSystem.LoadGameState();
        }
    }

    public void ClearGameData()
    {
        PlayerDataManager.Instance.ClearAllRuntimeData();
    }

    public bool HasSaveData()
    {
        return PlayerDataManager.Instance.HasSaveData();
    }

    private void OnDisable()
    {
        SaveGameData();
    }

    #endregion
}
