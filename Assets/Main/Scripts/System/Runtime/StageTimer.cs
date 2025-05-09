using System;
using System.Collections;
using UnityEngine;

public class StageTimer : MonoBehaviour, IInitializable
{
    public bool IsInitialized { get; private set; }

    private float stageTimer;
    private float stageDuration;
    private bool isTimerRunning;

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
        stageTimer = 0f;
        isTimerRunning = true;

        while (isTimerRunning)
        {
            stageTimer += Time.deltaTime;
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
        stageTimer = 0f;
        stageDuration = 0f;
        isTimerRunning = false;
    }

    public bool IsStageTimeUp()
    {
        return stageTimer >= stageDuration;
    }

    public float GetElapsedTime()
    {
        return stageTimer;
    }

    public float GetRemainingTime()
    {
        return Mathf.Max(0f, stageDuration - stageTimer);
    }

    public float GetTimeProgress()
    {
        return stageDuration > 0f ? stageTimer / stageDuration : 0f;
    }
}
