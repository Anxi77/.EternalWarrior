using System;
using System.Collections;
using UnityEngine;

public class StageTimer : MonoBehaviour, IInitializable
{
    public bool IsInitialized { get; private set; }

    private float elapsedTime;
    private float stageDuration;
    private bool isTimerRunning;

    public float StageDuration => stageDuration;

    public void Initialize()
    {
        try
        {
            ResetTimer();
            IsInitialized = true;
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(StageTimer),
                $"Error initializing StageTimeManager: {e.Message}"
            );
            IsInitialized = false;
        }
    }

    public void StartStageTimer(float duration)
    {
        StartCoroutine(StageTimerRoutine(duration));
    }

    private IEnumerator StageTimerRoutine(float duration)
    {
        stageDuration = duration;
        elapsedTime = 0f;
        isTimerRunning = true;

        while (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    public void PauseTimer()
    {
        isTimerRunning = false;
    }

    public void ResumeTimer()
    {
        isTimerRunning = true;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        stageDuration = 0f;
        isTimerRunning = false;
    }

    public bool IsStageTimeUp()
    {
        return elapsedTime >= stageDuration;
    }

    public float GetElapsedTime()
    {
        return elapsedTime;
    }

    public float GetRemainingTime()
    {
        return Mathf.Max(0f, stageDuration - elapsedTime);
    }

    public float GetTimeProgress()
    {
        return stageDuration > 0f ? elapsedTime / stageDuration : 0f;
    }
}
