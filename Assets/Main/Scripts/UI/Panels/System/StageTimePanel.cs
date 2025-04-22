using System.Collections;
using TMPro;
using UnityEngine;

public class StageTimePanel : Panel
{
    public override PanelType PanelType => PanelType.StageTime;

    [SerializeField]
    private TextMeshProUGUI remainingTimeText;

    public override void Open()
    {
        base.Open();
        StartCoroutine(UpdateTimerDisplay());
    }

    private IEnumerator UpdateTimerDisplay()
    {
        yield return new WaitUntil(() => GameManager.Instance.StageTimer != null);

        while (true)
        {
            float remainingTime = GameManager.Instance.StageTimer.GetRemainingTime();

            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            remainingTimeText.text = $"Remaining: {minutes:00}:{seconds:00}";

            if (remainingTime <= 0)
            {
                break;
            }

            yield return null;
        }
    }

    public void Clear()
    {
        if (remainingTimeText)
            remainingTimeText.text = "Remaining: 00:00";
    }

    public override void Close(bool objActive = true)
    {
        base.Close(objActive);
        StopCoroutine(UpdateTimerDisplay());
        Clear();
    }
}
