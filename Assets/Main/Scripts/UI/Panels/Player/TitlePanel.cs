using UnityEngine;
using UnityEngine.UI;

public class TitlePanel : Panel
{
    public override PanelType PanelType => PanelType.Title;

    [SerializeField]
    private Button startGameButton;

    [SerializeField]
    private Button loadGameButton;

    [SerializeField]
    private Button exitButton;

    public override void Open()
    {
        InitializeButtons();
        base.Open();
    }

    private void InitializeButtons()
    {
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartNewGame);

        if (loadGameButton != null)
            loadGameButton.onClick.AddListener(OnLoadGame);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitGame);
    }

    public void OnStartNewGame()
    {
        GameManager.Instance.InitializeNewGame();
    }

    public void OnLoadGame()
    {
        GameManager.Instance.LoadGameData();
    }

    public void OnExitGame()
    {
        Application.Quit();
    }

    public void UpdateButtons(bool hasSaveData)
    {
        if (loadGameButton != null)
            loadGameButton.interactable = hasSaveData;
    }

    public override void Close(bool objActive = true)
    {
        if (startGameButton != null)
            startGameButton.onClick.RemoveAllListeners();
        if (loadGameButton != null)
            loadGameButton.onClick.RemoveAllListeners();
        if (exitButton != null)
            exitButton.onClick.RemoveAllListeners();
        base.Close(objActive);
    }
}
