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

public class GameManager : Singleton<GameManager>, IInitializable
{
    public bool IsInitialized { get; private set; }
    internal List<Enemy> enemies = new();
    internal Player player;
    private bool hasInitializedGame = false;
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

    private int lastPlayerLevel = 1;
    private Coroutine levelCheckCoroutine;
    private GameState currentState = GameState.Title;

    private Dictionary<GameState, IGameState> stateHandlers;

    private bool isStateTransitioning = false;

    private readonly Queue<GameState> stateTransitionQueue = new();

    public void Initialize()
    {
        try
        {
            IsInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing GameManager: {e.Message}");
            IsInitialized = false;
        }
    }

    private bool CreateStateHandlers()
    {
        stateHandlers = new Dictionary<GameState, IGameState>();

        try
        {
            stateHandlers[GameState.Title] = new MainMenuStateHandler();
            stateHandlers[GameState.Town] = new TownStateHandler();
            stateHandlers[GameState.Stage] = new StageStateHandler();
            stateHandlers[GameState.Paused] = new PausedStateHandler();
            stateHandlers[GameState.GameOver] = new GameOverStateHandler();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating state handlers: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    public void ChangeState(GameState newState)
    {
        if (!IsInitialized || stateHandlers == null)
            return;

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
            Debug.LogError($"Error during state change: {e.Message}\n{e.StackTrace}");
            isStateTransitioning = false;
        }
    }

    private void Update()
    {
        if (!IsInitialized || stateHandlers == null)
            return;

        try
        {
            stateHandlers[currentState]?.OnUpdate();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in state update: {e.Message}");
        }
    }

    private void FixedUpdate()
    {
        if (!IsInitialized || stateHandlers == null)
            return;

        try
        {
            stateHandlers[currentState]?.OnFixedUpdate();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in state fixed update: {e.Message}");
        }
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

        if (player != null && player.playerStatus != Player.Status.Dead)
        {
            levelCheckCoroutine = StartCoroutine(CheckLevelUp());
        }
    }

    private IEnumerator CheckLevelUp()
    {
        if (player == null)
        {
            Debug.LogError("Player reference is null in GameManager");
            yield break;
        }

        lastPlayerLevel = player.level;

        while (true)
        {
            if (player == null || player.playerStatus == Player.Status.Dead)
            {
                levelCheckCoroutine = null;
                yield break;
            }

            if (player.level > lastPlayerLevel)
            {
                lastPlayerLevel = player.level;
                OnPlayerLevelUp();
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void OnPlayerLevelUp()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLevelUpPanel();
        }
    }

    #region Game State Management
    public void InitializeNewGame()
    {
        if (!hasInitializedGame)
        {
            DataSystem.PlayerDataSystem.LoadPlayerData();
            hasInitializedGame = true;
        }
        else
        {
            ClearGameData();
            DataSystem.PlayerDataSystem.LoadPlayerData();
        }
    }

    public void SaveGameData()
    {
        if (player != null)
        {
            PlayerUnitManager.Instance.SaveGameState();
        }
    }

    public void LoadGameData()
    {
        if (player != null)
        {
            PlayerUnitManager.Instance.LoadGameState();
        }
    }

    public void ClearGameData()
    {
        DataSystem.PlayerDataSystem.ClearAllRuntimeData();
    }

    public bool HasSaveData()
    {
        return DataSystem.PlayerDataSystem.HasSaveData();
    }
    #endregion

    protected override void OnApplicationQuit()
    {
        try
        {
            SaveGameData();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during application quit: {e.Message}");
        }

        base.OnApplicationQuit();
    }
}
