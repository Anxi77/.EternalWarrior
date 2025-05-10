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
        availableSkills = SkillDataManager.Instance.GetAllData();
        Logger.Log(
            typeof(SkillSystem),
            $"Loaded {availableSkills.Count} skills from SkillDataManager"
        );
    }

    public Skill GetPlayerSkill(SkillID skillId)
    {
        Logger.Log(typeof(SkillSystem), $"Looking for skill with ID: {skillId}");

        if (GameManager.Instance.PlayerSystem.Player == null)
        {
            Logger.LogError(typeof(SkillSystem), "Player is null");
            return null;
        }

        if (GameManager.Instance.PlayerSystem.Player.skills == null)
        {
            Logger.LogError(typeof(SkillSystem), "Player skills list is null");
            return null;
        }

        Logger.Log(
            typeof(SkillSystem),
            $"Player has {GameManager.Instance.PlayerSystem.Player.skills.Count} skills"
        );

        foreach (var skill in GameManager.Instance.PlayerSystem.Player.skills)
        {
            Logger.Log(
                typeof(SkillSystem),
                $"Checking skill: {skill.skillData.Name} (ID: {skill.skillData.ID})"
            );
        }

        var foundSkill = GameManager.Instance.PlayerSystem.Player.skills.Find(s =>
        {
            Logger.Log(typeof(SkillSystem), $"Comparing {s.skillData.ID} with {skillId}");
            return s.skillData.ID == skillId;
        });

        Logger.Log(
            typeof(SkillSystem),
            $"Found skill: {(foundSkill != null ? foundSkill.skillData.Name : "null")}"
        );
        return foundSkill;
    }

    private bool UpdateSkillStats(Skill skill, int targetLevel, out ISkillStat newStats)
    {
        Logger.Log(
            typeof(SkillSystem),
            $"Updating stats for skill {skill.skillData.Name} to level {targetLevel}"
        );

        newStats = skill.skillData.GetStatsForLevel(targetLevel);

        if (newStats == null)
        {
            Logger.LogError(typeof(SkillSystem), $"Failed to get stats for level {targetLevel}");
            return false;
        }

        Logger.Log(typeof(SkillSystem), $"Got new stats for level {targetLevel}");
        skill.GetSkillData().SetStatsForLevel(targetLevel, newStats);

        bool result = skill.SkillLevelUpdate(targetLevel);
        Logger.Log(typeof(SkillSystem), $"SkillLevelUpdate result: {result}");

        skill.currentLevel = targetLevel;

        return result;
    }

    public void AddOrUpgradeSkill(SkillData skillData)
    {
        if (GameManager.Instance?.PlayerSystem?.Player == null || skillData == null)
            return;

        try
        {
            Logger.Log(
                typeof(SkillSystem),
                $"Adding/Upgrading skill: {skillData.Name} (ID: {skillData.ID})"
            );

            var playerStat = GameManager.Instance.PlayerSystem.Player.GetComponent<StatSystem>();
            float currentHpRatio = 1f;

            if (playerStat != null)
            {
                currentHpRatio =
                    playerStat.GetStat(StatType.CurrentHp) / playerStat.GetStat(StatType.MaxHp);
                Logger.Log(
                    typeof(SkillSystem),
                    $"Before AddOrUpgradeSkill - HP: {playerStat.GetStat(StatType.CurrentHp)}/{playerStat.GetStat(StatType.MaxHp)} ({currentHpRatio:F2})"
                );
            }

            var existingSkill = GetPlayerSkill(skillData.ID);
            Logger.Log(
                typeof(SkillSystem),
                $"Existing skill check - Found: {existingSkill != null}"
            );

            if (existingSkill != null)
            {
                int nextLevel = existingSkill.currentLevel + 1;
                Logger.Log(
                    typeof(SkillSystem),
                    $"Current level: {existingSkill.currentLevel}, Attempting upgrade to level: {nextLevel}"
                );

                if (UpdateSkillStats(existingSkill, nextLevel, out _))
                {
                    Logger.Log(
                        typeof(SkillSystem),
                        $"Successfully upgraded skill to level {nextLevel}"
                    );
                }
            }
            else
            {
                GameObject prefab = skillData.BasePrefab;

                Logger.Log(typeof(SkillSystem), $"Prefab: {prefab}");

                if (prefab != null)
                {
                    var tempObj = Instantiate(
                        prefab,
                        GameManager.Instance.PlayerSystem.Player.transform.position,
                        Quaternion.identity
                    );
                    tempObj.SetActive(false);

                    if (tempObj.TryGetComponent<Skill>(out var skillComponent))
                    {
                        skillComponent.SetSkillData(skillData);
                        skillComponent.Initialize();

                        tempObj.transform.SetParent(
                            GameManager.Instance.PlayerSystem.Player.transform
                        );
                        tempObj.transform.localPosition = Vector3.zero;
                        tempObj.transform.localRotation = Quaternion.identity;
                        tempObj.transform.localScale = Vector3.one;

                        tempObj.SetActive(true);
                        GameManager.Instance.PlayerSystem.Player.skills.Add(skillComponent);
                        Logger.Log(
                            typeof(SkillSystem),
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
                Logger.Log(
                    typeof(SkillSystem),
                    $"After AddOrUpgradeSkill - HP: {newCurrentHp}/{newMaxHp} ({currentHpRatio:F2})"
                );
            }
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(SkillSystem),
                $"Error in AddOrUpgradeSkill: {e.Message}\n{e.StackTrace}"
            );
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

        var playerStat = GameManager.Instance.PlayerSystem.Player.GetComponent<StatSystem>();
        float currentHpRatio = 1f;
        float currentHp = 0f;
        float maxHp = 0f;

        if (playerStat != null)
        {
            currentHp = playerStat.GetStat(StatType.CurrentHp);
            maxHp = playerStat.GetStat(StatType.MaxHp);
            currentHpRatio = currentHp / maxHp;
            Logger.Log(
                typeof(SkillSystem),
                $"[SkillManager] Before replace - HP: {currentHp}/{maxHp} ({currentHpRatio:F2})"
            );
        }

        if (existingSkill is PassiveSkill passiveSkill)
        {
            passiveSkill.RemoveEffectFromPlayer(GameManager.Instance.PlayerSystem.Player);
        }

        GameManager.Instance.PlayerSystem.Player.skills.Remove(existingSkill);
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
            GameManager.Instance.PlayerSystem.Player.skills.Add(newSkill);
            Logger.Log(
                typeof(SkillSystem),
                $"Successfully replaced skill with level {targetLevel} prefab"
            );

            if (playerStat != null)
            {
                float finalMaxHp = playerStat.GetStat(StatType.MaxHp);
                float finalCurrentHp = Mathf.Max(currentHp, finalMaxHp * currentHpRatio);
                playerStat.SetCurrentHp(finalCurrentHp);
                Logger.Log(
                    typeof(SkillSystem),
                    $"[SkillManager] After replace - HP: {finalCurrentHp}/{finalMaxHp} ({currentHpRatio:F2})"
                );
            }
        }
    }

    public void RemoveSkill(SkillID skillID)
    {
        if (GameManager.Instance.PlayerSystem.Player == null)
            return;

        Player player = GameManager.Instance.PlayerSystem.Player;
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
            Logger.LogError(
                typeof(SkillSystem),
                $"No skills available in SkillManager. Available skills count: {availableSkills?.Count ?? 0}"
            );
            return new List<SkillData>();
        }

        Logger.Log(
            typeof(SkillSystem),
            $"Total available skills before filtering: {availableSkills.Count}"
        );
        foreach (var skill in availableSkills)
        {
            Logger.Log(
                typeof(SkillSystem),
                $"Available skill: {skill.Name}, ID: {skill.ID}, Element: {skill.Element}"
            );
        }

        var selectedSkills = new List<SkillData>();
        var filteredSkills = availableSkills
            .Where(skill =>
            {
                if (skill == null)
                {
                    Logger.LogError(typeof(SkillSystem), "Found null skill");
                    return false;
                }

                var stats = skill.GetStatsForLevel(1);
                bool hasStats = stats != null;
                bool matchesElement = elementType == null || skill.Element == elementType;

                Logger.Log(
                    typeof(SkillSystem),
                    $"Checking skill {skill.Name} \n"
                        + $"  - ID: {skill.ID} \n"
                        + $"  - Element: {skill.Element} \n"
                        + $"  - HasStats: {hasStats} \n"
                        + $"  - MatchesElement: {matchesElement}"
                );
                if (!hasStats)
                {
                    Logger.LogWarning(typeof(SkillSystem), $"  - No stats found for level 1");
                }

                return hasStats && matchesElement;
            })
            .ToList();

        if (!filteredSkills.Any())
        {
            Logger.LogWarning(typeof(SkillSystem), "No skills match the criteria");
            return selectedSkills;
        }

        Logger.Log(typeof(SkillSystem), $"Found {filteredSkills.Count} skills matching criteria");

        if (elementType == null)
        {
            var availableElements = filteredSkills.Select(s => s.Element).Distinct().ToList();

            elementType = availableElements[Random.Range(0, availableElements.Count)];
            filteredSkills = filteredSkills.Where(s => s.Element == elementType).ToList();
            Logger.Log(
                typeof(SkillSystem),
                $"Selected element type: {elementType}, remaining skills: {filteredSkills.Count}"
            );
        }

        int possibleCount = Mathf.Min(count, filteredSkills.Count);
        Logger.Log(
            typeof(SkillSystem),
            $"Requested {count} skills, possible to select {possibleCount} skills"
        );

        while (selectedSkills.Count < possibleCount && filteredSkills.Any())
        {
            int index = Random.Range(0, filteredSkills.Count);
            selectedSkills.Add(filteredSkills[index]);
            Logger.Log(typeof(SkillSystem), $"Selected skill: {filteredSkills[index].Name}");
            filteredSkills.RemoveAt(index);
        }

        if (selectedSkills.Count < count)
        {
            Logger.Log(
                typeof(SkillSystem),
                $"Returning {selectedSkills.Count} skills instead of requested {count} due to availability"
            );
        }

        return selectedSkills;
    }
}
