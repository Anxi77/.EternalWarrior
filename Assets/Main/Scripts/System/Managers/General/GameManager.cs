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
    private StageTimer stageTimer;
    private CameraSystem cameraSystem;
    private ItemSystem itemSystem;
    private SkillSystem skillSystem;
    private PlayerSystem playerSystem;
    private MonsterSystem monsterSystem;
    private List<Monster> monsters = new();

    public StageTimer StageTimer => stageTimer;
    public CameraSystem CameraSystem => cameraSystem;
    public ItemSystem ItemSystem => itemSystem;
    public SkillSystem SkillSystem => skillSystem;
    private PathFindingSystem pathFindingSystem;
    public PathFindingSystem PathFindingSystem => pathFindingSystem;
    public PlayerSystem PlayerSystem => playerSystem;
    public MonsterSystem MonsterSystem => monsterSystem;
    public List<Monster> Monsters => monsters;

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
            PoolManager.Instance.Initialize,
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
        skillSystem.transform.SetParent(transform);
        skillSystem.Initialize();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;

        LoadingManager.Instance.SetLoadingText("Initializing Item System...");
        itemSystem = new GameObject("ItemSystem").AddComponent<ItemSystem>();
        itemSystem.transform.SetParent(transform);
        itemSystem.Initialize();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;

        playerSystem = new GameObject("PlayerSystem").AddComponent<PlayerSystem>();
        playerSystem.transform.SetParent(transform);
        playerSystem.Initialize();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;

        stageTimer = new GameObject("StageTimer").AddComponent<StageTimer>();
        stageTimer.transform.SetParent(transform);
        stageTimer.Initialize();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;

        LoadingManager.Instance.SetLoadingText("Initializing Camera System...");
        cameraSystem = new GameObject("CameraSystem").AddComponent<CameraSystem>();
        cameraSystem.transform.SetParent(transform);
        cameraSystem.Initialize();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;

        LoadingManager.Instance.SetLoadingText("Initializing Path Finding System...");
        pathFindingSystem = new GameObject("PathFindingSystem").AddComponent<PathFindingSystem>();
        pathFindingSystem.transform.SetParent(transform);
        pathFindingSystem.Initialize();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;

        LoadingManager.Instance.SetLoadingText("Initializing Monster System...");
        monsterSystem = new GameObject("MonsterSystem").AddComponent<MonsterSystem>();
        monsterSystem.transform.SetParent(transform);
        monsterSystem.Initialize();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;

        LoadingManager.Instance.SetLoadingText("Creating State Handlers...");
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
            $"GameState transition from [{currentState}] to [{newState}]"
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
        if (PlayerSystem != null)
        {
            if (PlayerSystem.Player != null)
            {
                PlayerSystem.SavePlayerData();
            }
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
