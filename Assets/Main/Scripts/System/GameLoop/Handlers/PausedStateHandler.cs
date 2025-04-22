using UnityEngine;

public class PausedStateHandler : BaseStateHandler
{
    public override void OnEnter()
    {
        Time.timeScale = 0f;
        UIManager.Instance.OpenPanel(PanelType.Pause);
    }

    public override void OnExit()
    {
        Time.timeScale = 1f;
        UIManager.Instance.ClosePanel(PanelType.Pause);
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameManager.Instance.ChangeState(GameState.Stage);
        }
    }
}
