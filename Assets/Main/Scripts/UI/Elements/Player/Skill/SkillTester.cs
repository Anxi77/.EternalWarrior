using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTester : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField]
    private TMP_Dropdown skillDropdown;

    [SerializeField]
    private Button addSkillButton;

    private bool isInitialized = false;

    private void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }

    private IEnumerator InitializeWhenReady()
    {
        yield return new WaitUntil(() => GameManager.Instance != null);

        if (!ValidateComponents())
        {
            Debug.LogError("SkillTester: Required UI components are missing!");
            yield break;
        }

        InitializeDropdown();
        SetupButton();
        isInitialized = true;
        Debug.Log("SkillTester initialized successfully");
    }

    private bool ValidateComponents()
    {
        if (skillDropdown == null)
        {
            Debug.LogError("SkillTester: Skill Dropdown is not assigned!");
            return false;
        }

        if (addSkillButton == null)
        {
            Debug.LogError("SkillTester: Add Skill Button is not assigned!");
            return false;
        }

        return true;
    }

    private void InitializeDropdown()
    {
        skillDropdown.ClearOptions();
        var skillDatas = SkillDataManager.Instance.GetAllData();

        foreach (var skillData in skillDatas)
        {
            skillDropdown.options.Add(
                new TMP_Dropdown.OptionData($"{skillData.Name} ({skillData.Type})")
            );
        }

        skillDropdown.RefreshShownValue();
        Debug.Log($"SkillTester: Initialized dropdown with {skillDatas.Count} skills");
    }

    private void SetupButton()
    {
        addSkillButton.onClick.RemoveAllListeners();
        addSkillButton.onClick.AddListener(AddSelectedSkill);
        Debug.Log("SkillTester: Button setup completed");
    }

    private void AddSelectedSkill()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("SkillTester: Not yet initialized!");
            return;
        }

        if (GameManager.Instance?.PlayerSystem?.Player == null)
        {
            Debug.LogWarning("SkillTester: Player not found!");
            return;
        }

        var skillDatas = SkillDataManager.Instance.GetAllData();
        if (skillDropdown.value < skillDatas.Count)
        {
            var selectedSkill = skillDatas[skillDropdown.value];
            GameManager.Instance.PlayerSystem.Player.AddOrUpgradeSkill(selectedSkill);
            Debug.Log($"SkillTester: Added/Upgraded skill: {selectedSkill.Name}");
        }
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        if (Input.GetKeyDown(KeyCode.T))
        {
            AddSelectedSkill();
        }
    }

    private void OnDisable()
    {
        isInitialized = false;
        if (addSkillButton != null)
        {
            addSkillButton.onClick.RemoveAllListeners();
        }
    }
}
