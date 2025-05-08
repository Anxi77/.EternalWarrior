using System;
using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneType
{
    Main_Title,
    Main_Town,
    Main_Stage,
    Main_Loading,
    Test,
}

public class LoadingManager : Singleton<LoadingManager>
{
    private const string LOADING_SCENE_NAME = "Main_Loading";
    private const float MINIMUM_LOADING_TIME = 3f;

    private LoadingPanel loadingUI;
    public LoadingPanel LoadingUI => loadingUI;
    private ProgressBar progressBar;

    private bool isLoading = false;

    public event Action<float> OnProgressUpdated;

    public void Initialize()
    {
        if (loadingUI == null)
        {
            loadingUI = UIManager.Instance.OpenPanel(PanelType.Loading) as LoadingPanel;
        }

        loadingUI.Close(false);
    }

    /// <summary>
    /// 씬을 로딩합니다.
    /// </summary>
    /// <param name="sceneName">로드할 씬 이름</param>
    /// <param name="onComplete">로딩 완료 후 실행할 콜백</param>
    public void LoadScene(SceneType sceneType, Action onComplete = null)
    {
        if (isLoading)
            return;

        isLoading = true;

        string targetSceneName = sceneType.ToString();

        UIManager.Instance.CloseAllPanels();

        StartCoroutine(LoadSceneRoutine(targetSceneName, onComplete));
    }

    /// <summary>
    /// 비동기 작업을 로딩합니다.
    /// </summary>
    /// <param name="asyncOperation">비동기 작업</param>
    /// <param name="loadingText">로딩 텍스트</param>
    /// <param name="onComplete">로딩 완료 후 실행할 콜백</param>
    public void LoadScene(
        SceneType sceneType,
        Func<IEnumerator> asyncOperation,
        Action onComplete = null
    )
    {
        if (isLoading)
            return;

        isLoading = true;

        string targetSceneName = sceneType.ToString();

        UIManager.Instance.CloseAllPanels();

        StartCoroutine(LoadOpertion(targetSceneName, asyncOperation, onComplete));
    }

    /// <summary>
    /// 여러 비동기 작업을 로딩합니다.
    /// </summary>
    /// <param name="operations">비동기 작업 목록</param>
    /// <param name="onComplete">로딩 완료 후 실행할 콜백</param>
    public void LoadScene(
        SceneType sceneType,
        List<Func<IEnumerator>> operations,
        Action onComplete = null
    )
    {
        if (isLoading)
            return;

        isLoading = true;

        string targetSceneName = sceneType.ToString();

        UIManager.Instance.CloseAllPanels();

        StartCoroutine(LoadOperations(targetSceneName, operations, onComplete));
    }

    private IEnumerator LoadSceneRoutine(string targetSceneName, Action onComplete)
    {
        if (GameManager.Instance.PlayerSystem != null)
        {
            if (GameManager.Instance.PlayerSystem.Player != null)
            {
                GameManager.Instance.PlayerSystem.DespawnPlayer();
            }
            yield return new WaitUntil(() => GameManager.Instance.PlayerSystem.Player == null);
        }

        AsyncOperation loadLoadingScene = SceneManager.LoadSceneAsync(LOADING_SCENE_NAME);

        yield return new WaitUntil(() => loadLoadingScene.isDone);

        UIManager.Instance.OpenPanel(PanelType.Loading);

        GameManager.Instance.PlayerSystem.SpawnPlayer(Vector3.zero);

        yield return new WaitUntil(() => GameManager.Instance.PlayerSystem.Player != null);

        AsyncOperation loadTargetScene = SceneManager.LoadSceneAsync(targetSceneName);

        loadTargetScene.allowSceneActivation = false;

        loadingUI.Open();

        float startTime = Time.time;
        float progress = 0;

        while (!loadTargetScene.isDone)
        {
            UpdateProgress(0);

            progress = Mathf.Clamp01(loadTargetScene.progress / 0.9f);

            UpdateProgress(progress);

            if (progress >= 1.0f && (Time.time - startTime) >= MINIMUM_LOADING_TIME)
            {
                loadTargetScene.allowSceneActivation = true;
            }

            yield return null;
        }

        isLoading = false;

        loadTargetScene.allowSceneActivation = true;

        yield return new WaitUntil(() => loadTargetScene.isDone);

        loadingUI.Close(false);

        onComplete?.Invoke();
    }

    private IEnumerator LoadOpertion(
        string targetSceneName,
        Func<IEnumerator> asyncOperation,
        Action onComplete = null
    )
    {
        if (GameManager.Instance.PlayerSystem != null)
        {
            if (GameManager.Instance.PlayerSystem.Player != null)
            {
                GameManager.Instance.PlayerSystem.DespawnPlayer();
            }
            yield return new WaitUntil(() => GameManager.Instance.PlayerSystem.Player == null);
        }

        UIManager.Instance.OpenPanel(PanelType.Loading);

        AsyncOperation loadLoadingScene = SceneManager.LoadSceneAsync(LOADING_SCENE_NAME);

        yield return new WaitUntil(() => loadLoadingScene.isDone);

        AsyncOperation loadTargetScene = SceneManager.LoadSceneAsync(targetSceneName);

        loadTargetScene.allowSceneActivation = false;

        float startTime = Time.time;

        var operationCoroutine = asyncOperation();

        while (operationCoroutine.MoveNext())
        {
            if (operationCoroutine.Current is float progressValue)
            {
                UpdateProgress(progressValue);
            }
            yield return operationCoroutine.Current;
        }

        float elapsedTime = Time.time - startTime;
        if (elapsedTime < MINIMUM_LOADING_TIME)
        {
            yield return new WaitForSeconds(MINIMUM_LOADING_TIME - elapsedTime);
        }

        loadTargetScene.allowSceneActivation = true;

        yield return new WaitUntil(() => loadTargetScene.isDone);

        loadingUI.Close(false);

        isLoading = false;

        onComplete?.Invoke();
    }

    private IEnumerator LoadOperations(
        string targetSceneName,
        List<Func<IEnumerator>> operations,
        Action onComplete
    )
    {
        if (GameManager.Instance.PlayerSystem != null)
        {
            if (GameManager.Instance.PlayerSystem.Player != null)
            {
                GameManager.Instance.PlayerSystem.DespawnPlayer();
            }
            yield return new WaitUntil(() => GameManager.Instance.PlayerSystem.Player == null);
        }

        AsyncOperation loadLoadingScene = SceneManager.LoadSceneAsync(LOADING_SCENE_NAME);

        yield return new WaitUntil(() => loadLoadingScene.isDone);

        UIManager.Instance.OpenPanel(PanelType.Loading);

        AsyncOperation loadTargetSceneAsync = SceneManager.LoadSceneAsync(targetSceneName);
        loadTargetSceneAsync.allowSceneActivation = false;

        loadingUI.Open();

        float startTime = Time.time;

        float totalOperations = operations.Count;
        float accumulatedProgress = 0f;

        for (int i = 0; i < operations.Count; i++)
        {
            var operation = operations[i];
            var operationCoroutine = operation();

            float baseProgress = i / totalOperations;
            float operationWeight = 1f / totalOperations;

            while (operationCoroutine.MoveNext())
            {
                if (operationCoroutine.Current is float progressValue)
                {
                    float scaledProgress = baseProgress + (progressValue * operationWeight);
                    UpdateProgress(scaledProgress);
                }
                yield return operationCoroutine.Current;
            }

            accumulatedProgress = (i + 1) / totalOperations;
            UpdateProgress(accumulatedProgress);
        }

        float elapsedTime = Time.time - startTime;
        if (elapsedTime < MINIMUM_LOADING_TIME)
        {
            yield return new WaitForSeconds(MINIMUM_LOADING_TIME - elapsedTime);
        }

        UpdateProgress(1.0f);

        loadTargetSceneAsync.allowSceneActivation = true;

        yield return new WaitUntil(() => loadTargetSceneAsync.isDone);

        isLoading = false;

        loadingUI.Close(false);

        onComplete?.Invoke();
    }

    public void UpdateProgress(float progress)
    {
        if (loadingUI != null)
        {
            loadingUI.UpdateProgress(progress);
        }
        else if (progressBar != null)
        {
            progressBar.currentValue = progress * 100f;
            progressBar.UpdateUI();
        }

        OnProgressUpdated?.Invoke(progress);
    }

    public void SetLoadingText(string text)
    {
        if (loadingUI != null)
        {
            loadingUI.SetLoadingText(text);
        }
    }
}
