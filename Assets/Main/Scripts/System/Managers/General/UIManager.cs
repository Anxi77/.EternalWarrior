using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.Heat;
using UnityEngine;

public partial class UIManager : Singleton<UIManager>, IInitializable
{
    public bool IsInitialized { get; private set; }

    private const string PANEL_PATH = "Prefabs/UI/Panels/";
    private List<Panel> panelPrefabs = new List<Panel>();
    private List<Panel> panels = new List<Panel>();
    public List<Panel> Panels => panels;

    [Header("UI Panels")]
    public Canvas mainCanvas;

    [SerializeField]
    private ModalWindowManager popUpWindow;

    public void Initialize()
    {
        try
        {
            LoadResources();
            IsInitialized = true;
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(UIManager), $"Error initializing UIManager: {e.Message}");
            IsInitialized = false;
        }
    }

    private void LoadResources()
    {
        panelPrefabs = Resources.LoadAll<Panel>(PANEL_PATH).ToList();
    }

    public Panel OpenPanel(PanelType panelType)
    {
        Panel panel = Panels.Find(p => p.PanelType == panelType);
        if (panel != null)
        {
            panel.Open();
            return panel;
        }
        else
        {
            Panel prefab = panelPrefabs.Find(p => p.PanelType == panelType);
            if (prefab != null)
            {
                Panel instance = Instantiate(prefab, mainCanvas.transform);
                instance.name = prefab.name;
                Panels.Add(instance);
                instance.Open();
                return instance;
            }
        }
        Logger.LogWarning(typeof(UIManager), $"Panel {panelType} not found");
        return null;
    }

    public void ClosePanel(PanelType panelType, bool objActive = true)
    {
        Panel panel = Panels.Find(p => p.PanelType == panelType);
        if (panel == null)
        {
            Logger.LogError(typeof(UIManager), $"PanelType {panelType} not found");
            return;
        }
        panel.Close(objActive);
    }

    public void OpenPopUp(string title, string description, Action OnConfirm = null)
    {
        popUpWindow.windowTitle.text = title;
        popUpWindow.windowDescription.text = description;
        popUpWindow.showCancelButton = false;
        popUpWindow.closeOnConfirm = true;
        popUpWindow.transform.SetAsLastSibling();
        popUpWindow.OpenWindow();
        popUpWindow.onConfirm.AddListener(() => OnConfirm?.Invoke());
    }

    public Panel GetPanel(PanelType panelType)
    {
        return Panels.Find(p => p.PanelType == panelType);
    }

    public void CloseAllPanels()
    {
        foreach (var panel in Panels)
        {
            panel.Close(false);
        }
        if (popUpWindow != null && popUpWindow.isOn)
        {
            popUpWindow.CloseWindow();
        }
    }
}
