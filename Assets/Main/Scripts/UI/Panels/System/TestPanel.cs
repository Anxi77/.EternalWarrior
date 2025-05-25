using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestPanel : Panel
{
    public override PanelType PanelType => PanelType.Test;

    [Header("UI Elements")]
    [SerializeField]
    private RectTransform skillParent;

    [SerializeField]
    private Button skillButtonPrefab;

    private bool isInitialized = false;

    public override void Open()
    {
        Initialize();
        base.Open();
    }

    public void Initialize()
    {
        InitializeDropdown();
        isInitialized = true;
    }

    private void InitializeDropdown()
    {
        var skillDatas = SkillDataManager.Instance.GetAllData();

        foreach (var skillData in skillDatas)
        {
            var skillButton = Instantiate(skillButtonPrefab, skillParent);
            skillButton.GetComponent<Button>().onClick.AddListener(() => AddSelectedSkill(skillData));
            skillButton.GetComponentInChildren<TextMeshProUGUI>().text = skillData.Name;
        }

        Logger.Log(
            typeof(TestPanel),
            $"[Test] Initialized dropdown with {skillDatas.Count} skills"
        );
    }

    private void AddSelectedSkill(SkillData skillData)
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

        GameManager.Instance.PlayerSystem.Player.AddOrUpgradeSkill(skillData);

        Logger.Log(
            typeof(TestPanel),
            $"SkillTester: Added/Upgraded skill: {skillData.Name}"
        );
    }
}
