using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SkillDataEditorWindow : EditorWindow
{
    #region Fields
    private Dictionary<SkillID, SkillData> skillDatabase = new();
    private Dictionary<SkillID, Dictionary<int, SkillStatData>> statDatabase = new();
    private string searchText = "";
    private SkillType typeFilter = SkillType.None;
    private ElementType elementFilter = ElementType.None;
    private SkillID selectedSkillId;
    private Vector2 mainScrollPosition;
    private GUIStyle headerStyle;
    private Vector2 skillListScrollPosition;
    private Vector2 skillDetailScrollPosition;
    private Dictionary<string, bool> levelFoldouts = new();
    private bool showBasicInfo = true;
    private bool showResources = true;
    private bool showLevelStats = true;
    private bool wasModified = false;
    #endregion

    private const string SKILL_DB_PATH = "SkillData/Json";
    private const string SKILL_ICON_PATH = "SkillData/Icons";
    private const string SKILL_PREFAB_PATH = "SkillData/Prefabs";
    private const string SKILL_STAT_PATH = "SkillData/Stats";

    #region Properties
    private SkillData CurrentSkill
    {
        get { return skillDatabase.TryGetValue(selectedSkillId, out var skill) ? skill : null; }
    }
    #endregion

    [MenuItem("Tools/Anxi/Skill Data Editor")]
    public static void ShowWindow()
    {
        GetWindow<SkillDataEditorWindow>("Skill Data Editor");
    }

    private void OnEnable()
    {
        RefreshData();
    }

    private void RefreshData()
    {
        skillDatabase = SkillDataEditorUtility.GetSkillDatabase();
        statDatabase = SkillDataEditorUtility.GetStatDatabase();

        ResourceIO<Sprite>.ClearCache();
        ResourceIO<GameObject>.ClearCache();

        foreach (var skill in skillDatabase.Values)
        {
            if (skill.Icon != null)
            {
                skill.Icon = ResourceIO<Sprite>.LoadData(
                    $"{SKILL_ICON_PATH}/{skill.ID}/{skill.ID}_Icon"
                );
            }
            if (File.Exists($"{SKILL_PREFAB_PATH}/{skill.ID}/{skill.ID}_Prefab"))
            {
                skill.BasePrefab = ResourceIO<GameObject>.LoadData(
                    $"{SKILL_PREFAB_PATH}/{skill.ID}/{skill.ID}_Prefab"
                );
            }

            if (File.Exists($"{SKILL_PREFAB_PATH}/{skill.ID}/{skill.ID}_Projectile"))
            {
                skill.ProjectilePrefab = ResourceIO<GameObject>.LoadData(
                    $"{SKILL_PREFAB_PATH}/{skill.ID}/{skill.ID}_Projectile"
                );
            }

            if (skill.Type == SkillType.Projectile)
            {
                if (File.Exists($"{SKILL_PREFAB_PATH}/{skill.ID}/{skill.ID}_Projectile"))
                {
                    skill.ProjectilePrefab = ResourceIO<GameObject>.LoadData(
                        $"{SKILL_PREFAB_PATH}/{skill.ID}/{skill.ID}_Projectile"
                    );
                }
            }

            var prefabs = Resources.LoadAll<GameObject>($"{SKILL_PREFAB_PATH}/{skill.ID}/");

            if (prefabs.Length > 0)
            {
                skill.PrefabsByLevel = new GameObject[prefabs.Length - 1];
                int cnt = 0;
                foreach (var prefab in prefabs)
                {
                    if (prefab.name.Contains("Level_"))
                    {
                        skill.PrefabsByLevel[cnt] = prefab;
                        cnt++;
                    }
                }
            }
        }
    }

    private void OnGUI()
    {
        if (headerStyle == null)
        {
            InitializeStyles();
        }

        EditorGUILayout.BeginVertical();
        {
            EditorGUILayout.Space(10);

            float footerHeight = 25f;
            float contentHeight = position.height - footerHeight - 35f;
            EditorGUILayout.BeginVertical(GUILayout.Height(contentHeight));
            {
                DrawMainContent();
            }
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            DrawFooter();
        }
        EditorGUILayout.EndVertical();
    }

    private void InitializeStyles()
    {
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(5, 5, 10, 10),
        };
    }

    private void DrawMainContent()
    {
        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
        {
            DrawSkillsTab();
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(25));
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save All", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                SaveAllData();
            }
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                RefreshData();
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Create Backup", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                SkillDataEditorUtility.Save();
            }
            GUILayout.Space(10);
            if (
                GUILayout.Button(
                    "Reset to Default",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(100)
                )
            )
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Reset to Default",
                        "Are you sure you want to reset all data to default? This cannot be undone.",
                        "Reset",
                        "Cancel"
                    )
                )
                {
                    SkillDataEditorUtility.InitializeDefaultData();
                    selectedSkillId = SkillID.None;
                    RefreshData();
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillsTab()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            {
                DrawSkillList();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
            DrawVerticalLine(Color.gray);
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical();
            {
                DrawSkillDetails();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Search & Filter", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            searchText = EditorGUILayout.TextField("Search", searchText);
            typeFilter = (SkillType)EditorGUILayout.EnumPopup("Type", typeFilter);
            elementFilter = (ElementType)EditorGUILayout.EnumPopup("Element", elementFilter);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Skills", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            float listHeight = position.height - 300;
            skillListScrollPosition = EditorGUILayout.BeginScrollView(
                skillListScrollPosition,
                GUILayout.Height(listHeight)
            );
            {
                var filteredSkills = FilterSkills();
                foreach (var skill in filteredSkills)
                {
                    bool isSelected = skill.ID == selectedSkillId;
                    GUI.backgroundColor = isSelected ? Color.cyan : Color.white;
                    if (GUILayout.Button(skill.Name, GUILayout.Height(25)))
                    {
                        selectedSkillId = skill.ID;
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        if (GUILayout.Button("Create New Skill", GUILayout.Height(30)))
        {
            CreateNewSkill();
        }
    }

    private void DrawSkillDetails()
    {
        if (CurrentSkill == null)
        {
            EditorGUILayout.LabelField("Select a skill to edit", headerStyle);
            return;
        }

        EditorGUILayout.BeginVertical();
        {
            skillDetailScrollPosition = EditorGUILayout.BeginScrollView(
                skillDetailScrollPosition,
                GUILayout.Height(position.height - 100)
            );
            try
            {
                if (showBasicInfo)
                {
                    DrawBasicInfo();
                }

                if (showResources)
                {
                    EditorGUILayout.Space(10);
                    DrawResources();
                }

                if (showLevelStats)
                {
                    EditorGUILayout.Space(10);
                    DrawLevelStats();
                }

                EditorGUILayout.Space(20);
                DrawDeleteButton();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawBasicInfo()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Basic Information", headerStyle);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            SkillID newId = (SkillID)EditorGUILayout.EnumPopup("Skill ID", CurrentSkill.ID);
            if (EditorGUI.EndChangeCheck() && newId != CurrentSkill.ID)
            {
                if (newId != SkillID.None && !skillDatabase.ContainsKey(newId))
                {
                    var skillData = CurrentSkill.Clone() as SkillData;
                    var oldId = skillData.ID;

                    SkillDataEditorUtility.DeleteSkillData(oldId);

                    skillData.ID = newId;
                    SkillDataEditorUtility.SaveSkillData(skillData);

                    selectedSkillId = newId;

                    RefreshData();
                }
            }

            EditorGUI.BeginChangeCheck();
            CurrentSkill.Name = EditorGUILayout.TextField("Name", CurrentSkill.Name);
            CurrentSkill.Description = EditorGUILayout.TextField(
                "Description",
                CurrentSkill.Description
            );
            CurrentSkill.Type = (SkillType)EditorGUILayout.EnumPopup("Type", CurrentSkill.Type);

            EditorGUI.BeginChangeCheck();
            ElementType newElement = (ElementType)
                EditorGUILayout.EnumPopup("Element", CurrentSkill.Element);
            if (EditorGUI.EndChangeCheck() && newElement != CurrentSkill.Element)
            {
                CurrentSkill.Element = newElement;

                var elementStats = statDatabase.GetValueOrDefault(CurrentSkill.ID);
                if (elementStats != null && elementStats.Any())
                {
                    foreach (var levelStat in elementStats.Values)
                    {
                        levelStat.element = newElement;
                    }

                    SkillDataEditorUtility.SaveStatDatabase();
                    SkillDataEditorUtility.SaveSkillData(CurrentSkill);
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Base Stats", EditorStyles.boldLabel);

            var stats = statDatabase.GetValueOrDefault(CurrentSkill.ID);
            if (stats != null && stats.Any())
            {
                var firstStat = stats.Values.First();
                EditorGUI.BeginChangeCheck();
                int newMaxLevel = EditorGUILayout.IntField(
                    "Max Skill Level",
                    firstStat.maxSkillLevel
                );

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var levelStat in stats.Values)
                    {
                        levelStat.maxSkillLevel = newMaxLevel;
                    }

                    if (
                        CurrentSkill.PrefabsByLevel == null
                        || CurrentSkill.PrefabsByLevel.Length != newMaxLevel
                    )
                    {
                        var prefabs = CurrentSkill.PrefabsByLevel;

                        Array.Resize(ref prefabs, newMaxLevel);
                        CurrentSkill.PrefabsByLevel = prefabs;
                    }

                    if (!statDatabase.ContainsKey(CurrentSkill.ID))
                    {
                        statDatabase[CurrentSkill.ID] = new Dictionary<int, SkillStatData>();
                    }

                    var currentStats = statDatabase[CurrentSkill.ID];
                    var existingLevels = currentStats.Keys.ToList();

                    for (int level = 1; level <= newMaxLevel; level++)
                    {
                        if (!currentStats.ContainsKey(level))
                        {
                            var newStat = new SkillStatData
                            {
                                skillID = CurrentSkill.ID,
                                level = level,
                                maxSkillLevel = newMaxLevel,
                                damage = 10f + (level - 1) * 5f,
                                elementalPower = 1f + (level - 1) * 0.2f,
                                element = CurrentSkill.Element,
                            };
                            currentStats[level] = newStat;
                        }
                    }

                    foreach (var level in existingLevels.Where(l => l > newMaxLevel))
                    {
                        currentStats.Remove(level);
                    }

                    SaveCurrentSkill();
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                SaveCurrentSkill();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawResources()
    {
        if (CurrentSkill == null)
            return;

        wasModified = false;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (CurrentSkill.Icon != null)
            {
                float size = 64f;
                var rect = EditorGUILayout.GetControlRect(
                    GUILayout.Width(size),
                    GUILayout.Height(size)
                );
                EditorGUI.DrawPreviewTexture(rect, CurrentSkill.Icon.texture);
                EditorGUILayout.Space(5);
            }

            Sprite oldIcon = CurrentSkill.Icon;
            Sprite newIcon = (Sprite)
                EditorGUILayout.ObjectField("Icon", oldIcon, typeof(Sprite), false);

            if (newIcon != oldIcon && newIcon != null)
            {
                string resourcePath = $"SkillData/Icons/{CurrentSkill.ID}/{CurrentSkill.ID}_Icon";
                ResourceIO<Sprite>.SaveData(resourcePath, newIcon);
                CurrentSkill.Icon = newIcon;
                wasModified = true;
            }

            EditorGUILayout.Space(5);

            GameObject oldBasePrefab = CurrentSkill.BasePrefab;
            GameObject newBasePrefab = (GameObject)
                EditorGUILayout.ObjectField(
                    "Base Prefab",
                    oldBasePrefab,
                    typeof(GameObject),
                    false
                );

            if (newBasePrefab != oldBasePrefab && newBasePrefab != null)
            {
                if (PrefabUtility.GetPrefabAssetType(newBasePrefab) == PrefabAssetType.NotAPrefab)
                {
                    Logger.LogError(
                        typeof(SkillDataEditorWindow),
                        "Please select a prefab from the Project window, not a scene object."
                    );
                }
                else
                {
                    string resourcePath =
                        $"SkillData/Prefabs/{CurrentSkill.ID}/{CurrentSkill.ID}_Prefab";
                    try
                    {
                        ResourceIO<GameObject>.SaveData(resourcePath, newBasePrefab);
                        CurrentSkill.BasePrefab = newBasePrefab;
                        wasModified = true;
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            typeof(SkillDataEditorWindow),
                            $"Failed to save prefab: {e.Message}"
                        );
                    }
                }
            }

            if (CurrentSkill.Type == SkillType.Projectile)
            {
                GameObject oldProjectilePrefab = CurrentSkill.ProjectilePrefab;
                GameObject newProjectilePrefab = (GameObject)
                    EditorGUILayout.ObjectField(
                        "Projectile Prefab",
                        oldProjectilePrefab,
                        typeof(GameObject),
                        false
                    );

                if (newProjectilePrefab != oldProjectilePrefab && newProjectilePrefab != null)
                {
                    string resourcePath =
                        $"SkillData/Prefabs/{CurrentSkill.ID}/{CurrentSkill.ID}_Projectile";
                    ResourceIO<GameObject>.SaveData(resourcePath, newProjectilePrefab);
                    CurrentSkill.ProjectilePrefab = newProjectilePrefab;
                    wasModified = true;
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Level Prefabs", EditorStyles.boldLabel);

            var stats = statDatabase.GetValueOrDefault(CurrentSkill.ID);
            if (stats != null && stats.Any())
            {
                var maxLevel = stats.Values.First().maxSkillLevel;
                if (
                    CurrentSkill.PrefabsByLevel == null
                    || CurrentSkill.PrefabsByLevel.Length != maxLevel
                )
                {
                    var prefabs = CurrentSkill.PrefabsByLevel;
                    Array.Resize(ref prefabs, maxLevel);
                    CurrentSkill.PrefabsByLevel = prefabs;
                    wasModified = true;
                }

                if (CurrentSkill.PrefabsByLevel != null)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < CurrentSkill.PrefabsByLevel.Length; i++)
                    {
                        GameObject oldLevelPrefab = CurrentSkill.PrefabsByLevel[i];
                        GameObject newLevelPrefab = (GameObject)
                            EditorGUILayout.ObjectField(
                                $"Level {i + 1}",
                                oldLevelPrefab,
                                typeof(GameObject),
                                false
                            );

                        if (newLevelPrefab != oldLevelPrefab && newLevelPrefab != null)
                        {
                            string resourcePath =
                                $"SkillData/Prefabs/{CurrentSkill.ID}/{CurrentSkill.ID}_Level_{i + 1}";
                            ResourceIO<GameObject>.SaveData(resourcePath, newLevelPrefab);
                            CurrentSkill.PrefabsByLevel[i] = newLevelPrefab;
                            wasModified = true;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }
        EditorGUILayout.EndVertical();

        if (wasModified && Event.current.type == EventType.Used)
        {
            var currentId = selectedSkillId;
            var currentIcon = CurrentSkill.Icon;
            var currentBasePrefab = CurrentSkill.BasePrefab;
            var currentProjectilePrefab = CurrentSkill.ProjectilePrefab;
            GameObject[] currentPrefabsByLevel = null;

            if (CurrentSkill.PrefabsByLevel != null)
            {
                currentPrefabsByLevel = new GameObject[CurrentSkill.PrefabsByLevel.Length];
                Array.Copy(
                    CurrentSkill.PrefabsByLevel,
                    currentPrefabsByLevel,
                    CurrentSkill.PrefabsByLevel.Length
                );
            }

            EditorApplication.delayCall += () =>
            {
                SaveCurrentSkill();
                wasModified = false;

                if (skillDatabase.TryGetValue(currentId, out var skill))
                {
                    skill.Icon = currentIcon;
                    skill.BasePrefab = currentBasePrefab;
                    skill.ProjectilePrefab = currentProjectilePrefab;

                    if (
                        currentPrefabsByLevel != null
                        && skill.PrefabsByLevel != null
                        && currentPrefabsByLevel.Length == skill.PrefabsByLevel.Length
                    )
                    {
                        Array.Copy(
                            currentPrefabsByLevel,
                            skill.PrefabsByLevel,
                            currentPrefabsByLevel.Length
                        );
                    }
                }

                Repaint();
            };
        }
    }

    private void DrawLevelStats()
    {
        if (CurrentSkill == null)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Level Stats", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var stats = statDatabase.GetValueOrDefault(CurrentSkill.ID);
            if (stats == null || !stats.Any())
            {
                EditorGUILayout.HelpBox(
                    "No level stats found. Set Max Skill Level in Basic Info to create level stats.",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUI.BeginChangeCheck();

                foreach (var levelStat in stats.Values.OrderBy(s => s.level))
                {
                    string levelKey = $"{CurrentSkill.ID}_{levelStat.level}";

                    if (!levelFoldouts.ContainsKey(levelKey))
                        levelFoldouts[levelKey] = false;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        levelFoldouts[levelKey] = EditorGUILayout.Foldout(
                            levelFoldouts[levelKey],
                            $"Level {levelStat.level}",
                            true
                        );

                        if (levelFoldouts[levelKey])
                        {
                            EditorGUILayout.Space(5);
                            DrawStatFields(levelStat);
                            EditorGUILayout.Space(5);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    SkillDataEditorUtility.SaveStatDatabase();
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawStatFields(SkillStatData stat)
    {
        EditorGUI.BeginChangeCheck();

        float newDamage = EditorGUILayout.FloatField("Damage", stat.damage);
        float newElementalPower = EditorGUILayout.FloatField(
            "Elemental Power",
            stat.elementalPower
        );

        bool statsChanged = false;
        if (newDamage != stat.damage)
        {
            stat.damage = newDamage;
            statsChanged = true;
        }
        if (newElementalPower != stat.elementalPower)
        {
            stat.elementalPower = newElementalPower;
            statsChanged = true;
        }

        switch (CurrentSkill.Type)
        {
            case SkillType.Projectile:
                statsChanged |= DrawProjectileStats(stat);
                break;
            case SkillType.Area:
                statsChanged |= DrawAreaStats(stat);
                break;
            case SkillType.Passive:
                statsChanged |= DrawPassiveStats(stat);
                break;
        }

        if (EditorGUI.EndChangeCheck() && statsChanged)
        {
            SkillDataEditorUtility.SaveStatDatabase();
        }
    }

    private bool DrawProjectileStats(SkillStatData stat)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Projectile Stats", EditorStyles.boldLabel);
        bool changed = false;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            float newSpeed = EditorGUILayout.FloatField("Speed", stat.projectileSpeed);
            if (newSpeed != stat.projectileSpeed)
            {
                stat.projectileSpeed = newSpeed;
                changed = true;
            }

            float newScale = EditorGUILayout.FloatField("Scale", stat.projectileScale);
            if (newScale != stat.projectileScale)
            {
                stat.projectileScale = newScale;
                changed = true;
            }

            float newInterval = EditorGUILayout.FloatField("Shot Interval", stat.shotInterval);
            if (newInterval != stat.shotInterval)
            {
                stat.shotInterval = newInterval;
                changed = true;
            }

            int newPierceCount = EditorGUILayout.IntField("Pierce Count", stat.pierceCount);
            if (newPierceCount != stat.pierceCount)
            {
                stat.pierceCount = newPierceCount;
                changed = true;
            }

            float newRange = EditorGUILayout.FloatField("Attack Range", stat.attackRange);
            if (newRange != stat.attackRange)
            {
                stat.attackRange = newRange;
                changed = true;
            }

            float newHomingRange = EditorGUILayout.FloatField("Homing Range", stat.homingRange);
            if (newHomingRange != stat.homingRange)
            {
                stat.homingRange = newHomingRange;
                changed = true;
            }

            bool newIsHoming = EditorGUILayout.Toggle("Is Homing", stat.isHoming);
            if (newIsHoming != stat.isHoming)
            {
                stat.isHoming = newIsHoming;
                changed = true;
            }

            float newExplosionRad = EditorGUILayout.FloatField(
                "Explosion Radius",
                stat.explosionRad
            );
            if (newExplosionRad != stat.explosionRad)
            {
                stat.explosionRad = newExplosionRad;
                changed = true;
            }

            int newProjectileCount = EditorGUILayout.IntField(
                "Projectile Count",
                stat.projectileCount
            );
            if (newProjectileCount != stat.projectileCount)
            {
                stat.projectileCount = newProjectileCount;
                changed = true;
            }

            float newInnerInterval = EditorGUILayout.FloatField(
                "Inner Interval",
                stat.innerInterval
            );
            if (newInnerInterval != stat.innerInterval)
            {
                stat.innerInterval = newInnerInterval;
                changed = true;
            }
        }
        EditorGUILayout.EndVertical();

        return changed;
    }

    private bool DrawAreaStats(SkillStatData stat)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Area Stats", EditorStyles.boldLabel);
        bool changed = false;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            float newRadius = EditorGUILayout.FloatField("Radius", stat.radius);
            if (newRadius != stat.radius)
            {
                stat.radius = newRadius;
                changed = true;
            }

            float newDuration = EditorGUILayout.FloatField("Duration", stat.duration);
            if (newDuration != stat.duration)
            {
                stat.duration = newDuration;
                changed = true;
            }

            float newTickRate = EditorGUILayout.FloatField("Tick Rate", stat.tickRate);
            if (newTickRate != stat.tickRate)
            {
                stat.tickRate = newTickRate;
                changed = true;
            }

            bool newIsPersistent = EditorGUILayout.Toggle("Is Persistent", stat.isPersistent);
            if (newIsPersistent != stat.isPersistent)
            {
                stat.isPersistent = newIsPersistent;
                changed = true;
            }

            float newMoveSpeed = EditorGUILayout.FloatField("Move Speed", stat.moveSpeed);
            if (newMoveSpeed != stat.moveSpeed)
            {
                stat.moveSpeed = newMoveSpeed;
                changed = true;
            }
        }
        EditorGUILayout.EndVertical();

        return changed;
    }

    private bool DrawPassiveStats(SkillStatData stat)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Passive Stats", EditorStyles.boldLabel);
        bool changed = false;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            float newEffectDuration = EditorGUILayout.FloatField(
                "Effect Duration",
                stat.effectDuration
            );
            if (newEffectDuration != stat.effectDuration)
            {
                stat.effectDuration = newEffectDuration;
                changed = true;
            }

            float newCooldown = EditorGUILayout.FloatField("Cooldown", stat.cooldown);
            if (newCooldown != stat.cooldown)
            {
                stat.cooldown = newCooldown;
                changed = true;
            }

            float newTriggerChance = EditorGUILayout.FloatField(
                "Trigger Chance",
                stat.triggerChance
            );
            if (newTriggerChance != stat.triggerChance)
            {
                stat.triggerChance = newTriggerChance;
                changed = true;
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Passive Effects", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            float newDamageInc = EditorGUILayout.FloatField(
                "Damage Increase (%)",
                stat.damageIncrease
            );
            if (newDamageInc != stat.damageIncrease)
            {
                stat.damageIncrease = newDamageInc;
                changed = true;
            }

            float newDefenseInc = EditorGUILayout.FloatField(
                "Defense Increase (%)",
                stat.defenseIncrease
            );
            if (newDefenseInc != stat.defenseIncrease)
            {
                stat.defenseIncrease = newDefenseInc;
                changed = true;
            }

            float newExpAreaInc = EditorGUILayout.FloatField(
                "Exp Area Increase (%)",
                stat.expAreaIncrease
            );
            if (newExpAreaInc != stat.expAreaIncrease)
            {
                stat.expAreaIncrease = newExpAreaInc;
                changed = true;
            }

            bool newHomingActivate = EditorGUILayout.Toggle("Homing Activate", stat.homingActivate);
            if (newHomingActivate != stat.homingActivate)
            {
                stat.homingActivate = newHomingActivate;
                changed = true;
            }

            float newHpInc = EditorGUILayout.FloatField("HP Increase (%)", stat.hpIncrease);
            if (newHpInc != stat.hpIncrease)
            {
                stat.hpIncrease = newHpInc;
                changed = true;
            }

            float newMoveSpeedInc = EditorGUILayout.FloatField(
                "Move Speed Increase (%)",
                stat.moveSpeedIncrease
            );
            if (newMoveSpeedInc != stat.moveSpeedIncrease)
            {
                stat.moveSpeedIncrease = newMoveSpeedInc;
                changed = true;
            }

            float newAttackSpeedInc = EditorGUILayout.FloatField(
                "Attack Speed Increase (%)",
                stat.attackSpeedIncrease
            );
            if (newAttackSpeedInc != stat.attackSpeedIncrease)
            {
                stat.attackSpeedIncrease = newAttackSpeedInc;
                changed = true;
            }

            float newAttackRangeInc = EditorGUILayout.FloatField(
                "Attack Range Increase (%)",
                stat.attackRangeIncrease
            );
            if (newAttackRangeInc != stat.attackRangeIncrease)
            {
                stat.attackRangeIncrease = newAttackRangeInc;
                changed = true;
            }

            float newHpRegenInc = EditorGUILayout.FloatField(
                "HP Regen Increase (%)",
                stat.hpRegenIncrease
            );
            if (newHpRegenInc != stat.hpRegenIncrease)
            {
                stat.hpRegenIncrease = newHpRegenInc;
                changed = true;
            }
        }
        EditorGUILayout.EndVertical();

        return changed;
    }

    private List<SkillData> FilterSkills()
    {
        return skillDatabase
            .Values.Where(skill =>
                (
                    string.IsNullOrEmpty(searchText)
                    || skill.Name.ToLower().Contains(searchText.ToLower())
                )
                && (typeFilter == SkillType.None || skill.Type == typeFilter)
                && (elementFilter == ElementType.None || skill.Element == elementFilter)
            )
            .ToList();
    }

    private void CreateNewSkill()
    {
        var window = GetWindow<SkillCreationPopup>("Create New Skill");
        window.Initialize(
            (selectedId, selectedType) =>
            {
                if (selectedId != SkillID.None && selectedType != SkillType.None)
                {
                    var newSkill = new SkillData
                    {
                        ID = selectedId,
                        Name = $"New {selectedType} Skill",
                        Description = $"New {selectedType} skill description",
                        Type = selectedType,
                        Element = ElementType.None,
                    };

                    SkillDataEditorUtility.SaveSkillData(newSkill);
                    selectedSkillId = newSkill.ID;
                    RefreshData();
                }
            }
        );
    }

    public class SkillCreationPopup : EditorWindow
    {
        private SkillID selectedId = SkillID.None;
        private SkillType selectedType = SkillType.None;
        private Action<SkillID, SkillType> onConfirm;
        private Vector2 scrollPosition;

        public void Initialize(Action<SkillID, SkillType> callback)
        {
            onConfirm = callback;
            minSize = new Vector2(300, 400);
            maxSize = new Vector2(300, 400);
            position = new Rect(Screen.width / 2, Screen.height / 2, 300, 400);
            Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Create New Skill", EditorStyles.boldLabel);
                EditorGUILayout.Space(10);

                EditorGUI.BeginChangeCheck();
                selectedType = (SkillType)EditorGUILayout.EnumPopup("Skill Type", selectedType);
                if (EditorGUI.EndChangeCheck())
                {
                    selectedId = SkillID.None;
                }

                EditorGUILayout.Space(10);

                EditorGUILayout.LabelField("Select Skill ID", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                {
                    var skillDatabase = SkillDataEditorUtility.GetSkillDatabase();
                    foreach (SkillID id in Enum.GetValues(typeof(SkillID)))
                    {
                        if (id == SkillID.None)
                            continue;
                        if (skillDatabase.ContainsKey(id))
                            continue;

                        bool isSelected = id == selectedId;
                        GUI.backgroundColor = isSelected ? Color.cyan : Color.white;
                        if (GUILayout.Button(id.ToString(), GUILayout.Height(25)))
                        {
                            selectedId = id;
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(10);

                GUI.enabled = false;
                EditorGUILayout.EnumPopup("Selected Type", selectedType);
                EditorGUILayout.EnumPopup("Selected ID", selectedId);
                GUI.enabled = true;

                EditorGUILayout.Space(10);

                GUI.enabled = selectedId != SkillID.None && selectedType != SkillType.None;
                if (GUILayout.Button("Create", GUILayout.Height(30)))
                {
                    onConfirm?.Invoke(selectedId, selectedType);
                    Close();
                }
                GUI.enabled = true;

                if (GUILayout.Button("Cancel", GUILayout.Height(30)))
                {
                    Close();
                }
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void SaveCurrentSkill()
    {
        if (
            CurrentSkill == null
            || string.IsNullOrEmpty(CurrentSkill.Name)
            || CurrentSkill.ID == SkillID.None
        )
        {
            EditorUtility.DisplayDialog("Error", "Cannot save skill with empty name or ID", "OK");
            return;
        }

        var currentId = selectedSkillId;
        SkillDataEditorUtility.SaveSkillData(CurrentSkill);

        // 데이터베이스 리로드
        skillDatabase = SkillDataEditorUtility.GetSkillDatabase();

        // 현재 스킬의 리소스 명시적 리로드
        if (skillDatabase.TryGetValue(currentId, out var skill))
        {
            skill.Icon = ResourceIO<Sprite>.LoadData(
                $"{SKILL_ICON_PATH}/{currentId}/{currentId}_Icon"
            );
            skill.BasePrefab = ResourceIO<GameObject>.LoadData(
                $"{SKILL_PREFAB_PATH}/{currentId}/{currentId}_Prefab"
            );

            if (skill.Type == SkillType.Projectile)
            {
                skill.ProjectilePrefab = ResourceIO<GameObject>.LoadData(
                    $"{SKILL_PREFAB_PATH}/{currentId}/{currentId}_Projectile"
                );
            }

            var stats = statDatabase.GetValueOrDefault(currentId);
            if (stats != null && stats.Any())
            {
                int maxLevel = stats.Values.First().maxSkillLevel;
                skill.PrefabsByLevel = new GameObject[maxLevel];

                for (int i = 0; i < maxLevel; i++)
                {
                    skill.PrefabsByLevel[i] = ResourceIO<GameObject>.LoadData(
                        $"{SKILL_PREFAB_PATH}/{currentId}/{currentId}_Level_{i + 1}"
                    );
                }
            }
        }

        selectedSkillId = currentId;
        Repaint();
    }

    private void SaveAllData()
    {
        foreach (var skill in skillDatabase.Values)
        {
            SkillDataEditorUtility.SaveSkillData(skill);
        }
        SkillDataEditorUtility.SaveStatDatabase();
        RefreshData();
    }

    private void DrawDeleteButton()
    {
        EditorGUILayout.Space(20);

        if (GUILayout.Button("Delete Skill", GUILayout.Height(30)))
        {
            if (
                EditorUtility.DisplayDialog(
                    "Delete Skill",
                    $"Are you sure you want to delete '{CurrentSkill.Name}'?",
                    "Delete",
                    "Cancel"
                )
            )
            {
                SkillDataEditorUtility.DeleteSkillData(CurrentSkill.ID);
                selectedSkillId = SkillID.None;
                RefreshData();
            }
        }
    }

    private void DrawVerticalLine(Color color)
    {
        var rect = EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(1));
        EditorGUI.DrawRect(rect, color);
    }
}
