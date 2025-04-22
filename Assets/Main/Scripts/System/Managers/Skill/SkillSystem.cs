using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SkillSystem : MonoBehaviour, IInitializable
{
    private List<SkillData> availableSkills = new List<SkillData>();
    private List<Skill> activeSkills = new List<Skill>();
    public bool IsInitialized { get; private set; } = false;

    public void Initialize()
    {
        LoadSkillData();
        IsInitialized = true;
    }

    private void LoadSkillData()
    {
        availableSkills = DataSystem.SkillDataSystem.GetAllData();
        Debug.Log($"[SkillManager] Loaded {availableSkills.Count} skills from SkillDataManager");
    }

    public Skill GetPlayerSkill(SkillID skillId)
    {
        Debug.Log($"Looking for skill with ID: {skillId}");

        if (GameManager.Instance.Player == null)
        {
            Debug.LogError("Player is null");
            return null;
        }

        if (GameManager.Instance.Player.skills == null)
        {
            Debug.LogError("Player skills list is null");
            return null;
        }

        Debug.Log($"Player has {GameManager.Instance.Player.skills.Count} skills");

        foreach (var skill in GameManager.Instance.Player.skills)
        {
            Debug.Log($"Checking skill: {skill.skillData.Name} (ID: {skill.skillData.ID})");
        }

        var foundSkill = GameManager.Instance.Player.skills.Find(s =>
        {
            Debug.Log($"Comparing {s.skillData.ID} with {skillId}");
            return s.skillData.ID == skillId;
        });

        Debug.Log($"Found skill: {(foundSkill != null ? foundSkill.skillData.Name : "null")}");
        return foundSkill;
    }

    private bool UpdateSkillStats(Skill skill, int targetLevel, out ISkillStat newStats)
    {
        Debug.Log($"Updating stats for skill {skill.skillData.Name} to level {targetLevel}");

        newStats = DataSystem.SkillDataSystem.GetSkillStatsForLevel(
            skill.skillData.ID,
            targetLevel,
            skill.skillData.Type
        );

        if (newStats == null)
        {
            Debug.LogError($"Failed to get stats for level {targetLevel}");
            return false;
        }

        Debug.Log($"Got new stats for level {targetLevel}");
        skill.GetSkillData().SetStatsForLevel(targetLevel, newStats);

        bool result = skill.SkillLevelUpdate(targetLevel);
        Debug.Log($"SkillLevelUpdate result: {result}");

        skill.currentLevel = targetLevel;

        return result;
    }

    public void AddOrUpgradeSkill(SkillData skillData)
    {
        if (GameManager.Instance?.Player == null || skillData == null)
            return;

        try
        {
            Debug.Log($"Adding/Upgrading skill: {skillData.Name} (ID: {skillData.ID})");

            var playerStat = GameManager.Instance.Player.GetComponent<PlayerStatSystem>();
            float currentHpRatio = 1f;
            if (playerStat != null)
            {
                currentHpRatio =
                    playerStat.GetStat(StatType.CurrentHp) / playerStat.GetStat(StatType.MaxHp);
                Debug.Log(
                    $"Before AddOrUpgradeSkill - HP: {playerStat.GetStat(StatType.CurrentHp)}/{playerStat.GetStat(StatType.MaxHp)} ({currentHpRatio:F2})"
                );
            }

            var existingSkill = GetPlayerSkill(skillData.ID);
            Debug.Log($"Existing skill check - Found: {existingSkill != null}");

            if (existingSkill != null)
            {
                int nextLevel = existingSkill.currentLevel + 1;
                Debug.Log(
                    $"Current level: {existingSkill.currentLevel}, Attempting upgrade to level: {nextLevel}"
                );

                GameObject levelPrefab = DataSystem.SkillDataSystem.GetLevelPrefab(
                    skillData.ID,
                    nextLevel
                );
                if (levelPrefab != null)
                {
                    Debug.Log($"Found level {nextLevel} prefab, replacing skill");
                    ReplaceSkillWithNewPrefab(existingSkill, levelPrefab, skillData, nextLevel);
                }
                else
                {
                    Debug.Log($"No level {nextLevel} prefab found, updating stats");
                    if (UpdateSkillStats(existingSkill, nextLevel, out _))
                    {
                        Debug.Log($"Successfully upgraded skill to level {nextLevel}");
                    }
                }
            }
            else
            {
                GameObject prefab =
                    DataSystem.SkillDataSystem.GetLevelPrefab(skillData.ID, 1)
                    ?? skillData.BasePrefab;

                if (prefab != null)
                {
                    var tempObj = Instantiate(
                        prefab,
                        GameManager.Instance.Player.transform.position,
                        Quaternion.identity
                    );
                    tempObj.SetActive(false);

                    if (tempObj.TryGetComponent<Skill>(out var skillComponent))
                    {
                        skillComponent.SetSkillData(skillData);
                        skillComponent.Initialize();

                        tempObj.transform.SetParent(GameManager.Instance.Player.transform);
                        tempObj.transform.localPosition = Vector3.zero;
                        tempObj.transform.localRotation = Quaternion.identity;
                        tempObj.transform.localScale = Vector3.one;

                        tempObj.SetActive(true);
                        GameManager.Instance.Player.skills.Add(skillComponent);
                        Debug.Log(
                            $"Successfully added new skill: {skillData.Name} at position {tempObj.transform.localPosition}"
                        );
                    }
                }
            }

            if (playerStat != null)
            {
                float newMaxHp = playerStat.GetStat(StatType.MaxHp);
                float newCurrentHp = Mathf.Max(1f, newMaxHp * currentHpRatio);
                playerStat.SetCurrentHp(newCurrentHp);
                Debug.Log(
                    $"After AddOrUpgradeSkill - HP: {newCurrentHp}/{newMaxHp} ({currentHpRatio:F2})"
                );
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in AddOrUpgradeSkill: {e.Message}\n{e.StackTrace}");
        }
    }

    private void ReplaceSkillWithNewPrefab(
        Skill existingSkill,
        GameObject newPrefab,
        SkillData skillData,
        int targetLevel
    )
    {
        Vector3 position = existingSkill.transform.position;
        Quaternion rotation = existingSkill.transform.rotation;
        Transform parent = existingSkill.transform.parent;

        var playerStat = GameManager.Instance.Player.GetComponent<PlayerStatSystem>();
        float currentHpRatio = 1f;
        float currentHp = 0f;
        float maxHp = 0f;

        if (playerStat != null)
        {
            currentHp = playerStat.GetStat(StatType.CurrentHp);
            maxHp = playerStat.GetStat(StatType.MaxHp);
            currentHpRatio = currentHp / maxHp;
            Debug.Log(
                $"[SkillManager] Before replace - HP: {currentHp}/{maxHp} ({currentHpRatio:F2})"
            );
        }

        if (existingSkill is PassiveSkill passiveSkill)
        {
            passiveSkill.RemoveEffectFromPlayer(GameManager.Instance.Player);
        }

        GameManager.Instance.Player.skills.Remove(existingSkill);
        Destroy(existingSkill.gameObject);

        var newObj = Instantiate(newPrefab, position, rotation, parent);
        if (newObj.TryGetComponent<Skill>(out var newSkill))
        {
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;
            newObj.transform.localScale = Vector3.one;

            skillData.GetSkillStats().baseStat.skillLevel = targetLevel;
            newSkill.SetSkillData(skillData);

            if (playerStat != null)
            {
                playerStat.SetCurrentHp(currentHp);
            }

            newSkill.Initialize();
            GameManager.Instance.Player.skills.Add(newSkill);
            Debug.Log($"Successfully replaced skill with level {targetLevel} prefab");

            if (playerStat != null)
            {
                float finalMaxHp = playerStat.GetStat(StatType.MaxHp);
                float finalCurrentHp = Mathf.Max(currentHp, finalMaxHp * currentHpRatio);
                playerStat.SetCurrentHp(finalCurrentHp);
                Debug.Log(
                    $"[SkillManager] After replace - HP: {finalCurrentHp}/{finalMaxHp} ({currentHpRatio:F2})"
                );
            }
        }
    }

    public void RemoveSkill(SkillID skillID)
    {
        if (GameManager.Instance.Player == null)
            return;

        Player player = GameManager.Instance.Player;
        Skill skillToRemove = player.skills.Find(x => x.skillData.ID == skillID);

        if (skillToRemove != null)
        {
            player.skills.Remove(skillToRemove);
            activeSkills.Remove(skillToRemove);
            Destroy(skillToRemove.gameObject);
        }
    }

    public List<SkillData> GetRandomSkills(int count = 3, ElementType? elementType = null)
    {
        if (availableSkills == null || availableSkills.Count == 0)
        {
            Debug.LogError(
                $"No skills available in SkillManager. Available skills count: {availableSkills?.Count ?? 0}"
            );
            return new List<SkillData>();
        }

        Debug.Log($"Total available skills before filtering: {availableSkills.Count}");
        foreach (var skill in availableSkills)
        {
            Debug.Log($"Available skill: {skill.Name}, ID: {skill.ID}, Element: {skill.Element}");
        }

        var selectedSkills = new List<SkillData>();
        var filteredSkills = availableSkills
            .Where(skill =>
            {
                if (skill == null)
                {
                    Debug.LogError("Found null skill");
                    return false;
                }

                var stats = DataSystem.SkillDataSystem.GetSkillStats(skill.ID, 1);
                bool hasStats = stats != null;
                bool matchesElement = elementType == null || skill.Element == elementType;

                Debug.Log($"Checking skill {skill.Name}:");
                Debug.Log($"  - ID: {skill.ID}");
                Debug.Log($"  - Element: {skill.Element}");
                Debug.Log($"  - HasStats: {hasStats}");
                Debug.Log($"  - MatchesElement: {matchesElement}");
                if (!hasStats)
                {
                    Debug.LogWarning($"  - No stats found for level 1");
                }

                return hasStats && matchesElement;
            })
            .ToList();

        if (!filteredSkills.Any())
        {
            Debug.LogWarning("No skills match the criteria");
            return selectedSkills;
        }

        Debug.Log($"Found {filteredSkills.Count} skills matching criteria");

        if (elementType == null)
        {
            var availableElements = filteredSkills.Select(s => s.Element).Distinct().ToList();

            elementType = availableElements[Random.Range(0, availableElements.Count)];
            filteredSkills = filteredSkills.Where(s => s.Element == elementType).ToList();
            Debug.Log(
                $"Selected element type: {elementType}, remaining skills: {filteredSkills.Count}"
            );
        }

        int possibleCount = Mathf.Min(count, filteredSkills.Count);
        Debug.Log($"Requested {count} skills, possible to select {possibleCount} skills");

        while (selectedSkills.Count < possibleCount && filteredSkills.Any())
        {
            int index = Random.Range(0, filteredSkills.Count);
            selectedSkills.Add(filteredSkills[index]);
            Debug.Log($"Selected skill: {filteredSkills[index].Name}");
            filteredSkills.RemoveAt(index);
        }

        if (selectedSkills.Count < count)
        {
            Debug.Log(
                $"Returning {selectedSkills.Count} skills instead of requested {count} due to availability"
            );
        }

        return selectedSkills;
    }
}
