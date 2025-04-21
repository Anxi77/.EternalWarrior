using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneType
{
    Main_Title,
    Main_Town,
    Main_Stage,
    Test,
}

public class LoadingManager : Singleton<LoadingManager>
{
    [Header("Portal Settings")]
    [SerializeField]
    private GameObject portalPrefab;

    [SerializeField]
    private Vector3 townPortalPosition = new(10, 0, 0);

    #region Scene Loading
    public void LoadMainMenu()
    {
        StartCoroutine(LoadSceneCoroutine(SceneType.Main_Title));
    }

    public void LoadTownScene()
    {
        StartCoroutine(LoadSceneCoroutine(SceneType.Main_Town));
    }

    public void LoadGameScene()
    {
        StartCoroutine(LoadSceneCoroutine(SceneType.Main_Stage));
    }

    public void LoadTestScene()
    {
        StartCoroutine(LoadSceneCoroutine(SceneType.Test));
    }

    private IEnumerator LoadSceneCoroutine(SceneType sceneType)
    {
        UIManager.Instance.ShowLoadingScreen();
        UIManager.Instance.UpdateLoadingProgress(0f);
        Time.timeScale = 0f;

        float progress = 0f;
        while (progress < 10f)
        {
            progress += Time.unscaledDeltaTime * 50f;
            UIManager.Instance.UpdateLoadingProgress(progress);
            yield return null;
        }

        CleanupCurrentScene();

        if (sceneType.ToString().Contains("Test"))
        {
            progress = 10f;
            while (progress < 70f)
            {
                progress += Time.unscaledDeltaTime * 100f;
                UIManager.Instance.UpdateLoadingProgress(progress);
                yield return null;
            }

            SceneManager.LoadScene(sceneType.ToString());

            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneType.ToString());
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                progress = Mathf.Lerp(10f, 70f, asyncLoad.progress / 0.9f);
                UIManager.Instance.UpdateLoadingProgress(progress);
                yield return null;
            }

            asyncLoad.allowSceneActivation = true;
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        switch (sceneType)
        {
            case SceneType.Main_Title:
                UIManager.Instance.SetupMainMenuUI();
                break;
            default:
                UIManager.Instance.SetupGameUI();
                break;
        }

        switch (sceneType)
        {
            case SceneType.Main_Title:
                GameManager.Instance.ChangeState(GameState.Title);
                break;
            case SceneType.Main_Town:
                GameManager.Instance.ChangeState(GameState.Town);
                break;
            case SceneType.Main_Stage:
            case SceneType.Test:
                GameManager.Instance.ChangeState(GameState.Stage);
                break;
        }

        while (!IsSceneReady(sceneType))
        {
            progress = Mathf.Lerp(80f, 95f, Time.unscaledDeltaTime);
            UIManager.Instance.UpdateLoadingProgress(progress);
            yield return null;
        }

        while (progress < 100f)
        {
            progress += Time.unscaledDeltaTime * 50f;
            UIManager.Instance.UpdateLoadingProgress(Mathf.Min(100f, progress));
            yield return null;
        }

        UIManager.Instance.HideLoadingScreen();
        Time.timeScale = 1f;
    }

    private bool IsSceneReady(SceneType sceneType)
    {
        switch (sceneType)
        {
            case SceneType.Main_Title:
                return UIManager.Instance != null && UIManager.Instance.IsMainMenuActive();

            case SceneType.Main_Town:
                return GameManager.Instance?.player != null
                    && GameManager.Instance.CameraSystem?.IsInitialized == true
                    && UIManager.Instance?.playerUIPanel != null
                    && UIManager.Instance.IsGameUIReady();

            case SceneType.Main_Stage:
            case SceneType.Test:
                bool isReady =
                    GameManager.Instance?.player != null
                    && GameManager.Instance.CameraSystem?.IsInitialized == true
                    && UIManager.Instance?.playerUIPanel != null
                    && UIManager.Instance.IsGameUIReady()
                    && MonsterManager.Instance?.IsInitialized == true;

                if (!isReady)
                {
                    Debug.Log(
                        $"Test Scene not ready: Player={GameManager.Instance?.player != null}, "
                            + $"Camera={GameManager.Instance.CameraSystem?.IsInitialized}, "
                            + $"UI={UIManager.Instance?.playerUIPanel != null}, "
                            + $"GameUI={UIManager.Instance?.IsGameUIReady()}, "
                            + $"Monster={MonsterManager.Instance?.IsInitialized}"
                    );
                }

                return isReady;

            default:
                return true;
        }
    }

    private void CleanupCurrentScene()
    {
        var existingPortals = FindObjectsOfType<Portal>();
        foreach (var portal in existingPortals)
        {
            Destroy(portal.gameObject);
        }
        PoolManager.Instance?.ClearAllPools();
    }
    #endregion

    #region Portal Management
    public void SpawnGameStagePortal()
    {
        SpawnPortal(townPortalPosition, SceneType.Main_Stage);
    }

    public void SpawnTownPortal(Vector3 position)
    {
        SpawnPortal(position, SceneType.Main_Town);
    }

    private void SpawnPortal(Vector3 position, SceneType destinationType)
    {
        if (portalPrefab != null)
        {
            GameObject portalObj = Instantiate(portalPrefab, position, Quaternion.identity);
            DontDestroyOnLoad(portalObj);

            if (portalObj.TryGetComponent<Portal>(out var portal))
            {
                portal.Initialize(destinationType);
            }
        }
    }

    public void OnPortalEnter(SceneType destinationType)
    {
        switch (destinationType)
        {
            case SceneType.Main_Town:
                PlayerUnitManager.Instance.SaveGameState();
                LoadTownScene();
                break;
            case SceneType.Main_Stage:
                LoadGameScene();
                break;
        }
    }
    #endregion
}
