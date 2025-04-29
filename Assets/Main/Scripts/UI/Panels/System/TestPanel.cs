using System;
using System.Collections;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;

public class TestPanel : Panel
{
    public override PanelType PanelType => PanelType.Test;

    [Header("UI Elements")]
    [SerializeField]
    private Dropdown skillDropdown;

    [SerializeField]
    private ButtonManager addSkillButton;

    [SerializeField]
    private Dropdown itemDropdown;

    [SerializeField]
    private ButtonManager addItemButton;

    private bool isInitialized = false;

    public override void Open()
    {
        Initialize();
        base.Open();
    }

    public void Initialize()
    {
        InitializeDropdown();
        SetupButton();
        isInitialized = true;
    }

    private void InitializeDropdown()
    {
        var skillDatas = SkillDataManager.Instance.GetAllData();

        foreach (var skillData in skillDatas)
        {
            skillDropdown.CreateNewItem($"{skillData.Name} ({skillData.Type})", true);
        }

        var items = ItemDataManager.Instance.GetAllData();

        foreach (var itemData in items)
        {
            itemDropdown.CreateNewItem($"{itemData.Name} ({itemData.Type})", true);
        }

        Logger.Log(
            typeof(TestPanel),
            $"[Test] Initialized dropdown with {skillDatas.Count} skills"
        );
    }

    private void SetupButton()
    {
        addSkillButton.onClick.AddListener(AddSelectedSkill);
        addItemButton.onClick.AddListener(AddSelectedItem);
    }

    private void AddSelectedSkill()
    {
        if (!isInitialized)
        {
            Logger.LogWarning(typeof(TestPanel), "[Test] Not yet initialized!");
            return;
        }

        if (GameManager.Instance?.PlayerSystem?.Player == null)
        {
            Logger.LogWarning(typeof(TestPanel), "[Test] Player not found!");
            return;
        }

        var skillDatas = SkillDataManager.Instance.GetAllData();
        if (skillDropdown.selectedItemIndex < skillDatas.Count)
        {
            var selectedSkill = skillDatas[skillDropdown.selectedItemIndex];
            GameManager.Instance.PlayerSystem.Player.AddOrUpgradeSkill(selectedSkill);
            Logger.Log(
                typeof(TestPanel),
                $"SkillTester: Added/Upgraded skill: {selectedSkill.Name}"
            );
        }
    }

    private void AddSelectedItem()
    {
        var items = ItemDataManager.Instance.GetAllData();
        if (itemDropdown.selectedItemIndex < items.Count)
        {
            var selectedItem = items[itemDropdown.selectedItemIndex];

            var item = GameManager.Instance.ItemSystem.GetItem(selectedItem.ID);

            GameManager.Instance.PlayerSystem.Player.inventory.AddItem(item);

            Logger.Log(typeof(TestPanel), $"[Test] Added item: {selectedItem.Name}");
        }
    }

    private void OnDisable()
    {
        addSkillButton.onClick.RemoveAllListeners();
        addItemButton.onClick.RemoveAllListeners();
        isInitialized = false;
    }
}
