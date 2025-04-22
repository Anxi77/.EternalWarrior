using UnityEngine;

public class MainMenuStateHandler : BaseStateHandler
{
    public override void OnEnter()
    {
        base.OnEnter();
        UIManager.Instance.OpenPanel(PanelType.Title);
        Time.timeScale = 1f;
    }

    public override void OnExit()
    {
        UIManager.Instance.ClosePanel(PanelType.Title);
        base.OnExit();
    }
}
