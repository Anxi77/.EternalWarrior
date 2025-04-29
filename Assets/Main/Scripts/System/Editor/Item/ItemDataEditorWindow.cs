using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ItemDataEditorWindow : EditorWindow
{
    private enum EditorTab
    {
        Items,
        DropTables,
        EffectRanges,
    }

    #region Fields
    private Dictionary<Guid, ItemData> itemDatabase = new();
    private Dictionary<MonsterType, DropTableData> dropTables = new();
    private string searchText = "";
    private string dropTableSearchText = "";
    private string effectRangeSearchText = "";
    private ItemType typeFilter = ItemType.None;
    private ItemRarity rarityFilter = ItemRarity.Common;
    private EffectType effectTypeFilter = EffectType.None;
    private Guid selectedItemId = Guid.Empty;
    private MonsterType selectedMonsterType = MonsterType.None;
    private Guid selectedEffectRangeId = Guid.Empty;
    private Vector2 mainScrollPosition;
    private EditorTab currentTab = EditorTab.Items;
    private GUIStyle headerStyle;
    private GUIStyle tabStyle;
    private Vector2 itemListScrollPosition;
    private Vector2 itemDetailScrollPosition;
    private Vector2 dropTableListScrollPosition;
    private Vector2 dropTableDetailScrollPosition;
    private Vector2 effectRangeListScrollPosition;
    private Vector2 effectRangeDetailScrollPosition;

    private bool showStatRanges = true;
    private bool showEffects = true;
    private bool showResources = true;
    #endregion

    #region Properties
    private ItemData CurrentItem
    {
        get
        {
            if (selectedItemId == Guid.Empty)
                return null;
            return itemDatabase.TryGetValue(selectedItemId, out var item) ? item : null;
        }
    }
    #endregion

    [MenuItem("Tools/Anxi/Item Data Editor")]
    public static void ShowWindow()
    {
        GetWindow<ItemDataEditorWindow>("Item Data Editor");
    }

    private void OnEnable()
    {
        RefreshData();
    }

    private void RefreshData()
    {
        RefreshItemDatabase();
        RefreshDropTables();
    }

    private void RefreshItemDatabase()
    {
        itemDatabase = ItemDataEditorUtility.GetItemDatabase();
    }

    private void RefreshDropTables()
    {
        dropTables = ItemDataEditorUtility.GetDropTables();
    }

    private void OnGUI()
    {
        if (headerStyle == null || tabStyle == null)
        {
            InitializeStyles();
        }

        EditorGUILayout.BeginVertical();
        {
            DrawTabs();
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

        tabStyle = new GUIStyle(EditorStyles.toolbarButton)
        {
            fixedHeight = 25,
            fontStyle = FontStyle.Bold,
        };
    }

    private void DrawMainContent()
    {
        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
        {
            switch (currentTab)
            {
                case EditorTab.Items:
                    DrawItemsTab();
                    break;
                case EditorTab.DropTables:
                    DrawDropTablesTab();
                    break;
                case EditorTab.EffectRanges:
                    DrawEffectRangesTab();
                    break;
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(25));
        {
            if (GUILayout.Toggle(currentTab == EditorTab.Items, "Items", tabStyle))
                currentTab = EditorTab.Items;
            if (GUILayout.Toggle(currentTab == EditorTab.DropTables, "Drop Tables", tabStyle))
                currentTab = EditorTab.DropTables;
            if (GUILayout.Toggle(currentTab == EditorTab.EffectRanges, "Effect Ranges", tabStyle))
                currentTab = EditorTab.EffectRanges;
        }
        EditorGUILayout.EndHorizontal();
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
                LoadAllData();
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
                    ItemDataEditorUtility.InitializeDefaultItemData();
                    selectedItemId = Guid.Empty;

                    EditorApplication.delayCall += () =>
                    {
                        RefreshData();
                        Repaint();
                    };

                    EditorUtility.SetDirty(this);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawItemsTab()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            {
                DrawItemList();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
            DrawVerticalLine(Color.gray);
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical();
            {
                DrawItemDetails();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawItemList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Search & Filter", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            searchText = EditorGUILayout.TextField("Search", searchText);
            typeFilter = (ItemType)EditorGUILayout.EnumPopup("Type", typeFilter);
            rarityFilter = (ItemRarity)EditorGUILayout.EnumPopup("Rarity", rarityFilter);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Items", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            float listHeight = position.height - 300;
            itemListScrollPosition = EditorGUILayout.BeginScrollView(
                itemListScrollPosition,
                GUILayout.Height(listHeight)
            );
            {
                var filteredItems = FilterItems();
                foreach (var item in filteredItems)
                {
                    bool isSelected = item.ID == selectedItemId;
                    GUI.backgroundColor = isSelected ? Color.cyan : Color.white;
                    if (GUILayout.Button(item.Name, GUILayout.Height(25)))
                    {
                        selectedItemId = item.ID;
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        if (GUILayout.Button("Create New Item", GUILayout.Height(30)))
        {
            CreateNewItem();
        }
    }

    private void DrawItemDetails()
    {
        if (CurrentItem == null)
        {
            EditorGUILayout.LabelField("Select an item to edit", headerStyle);
            return;
        }

        EditorGUILayout.BeginVertical();
        {
            itemDetailScrollPosition = EditorGUILayout.BeginScrollView(
                itemDetailScrollPosition,
                GUILayout.Height(position.height - 100)
            );
            try
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField("ID", CurrentItem.ID.ToString());
                    EditorGUI.EndDisabledGroup();

                    CurrentItem.Name = EditorGUILayout.TextField("Name", CurrentItem.Name);
                    CurrentItem.Description = EditorGUILayout.TextField(
                        "Description",
                        CurrentItem.Description
                    );
                    CurrentItem.Type = (ItemType)
                        EditorGUILayout.EnumPopup("Type", CurrentItem.Type);

                    if (CurrentItem.Type == ItemType.Accessory)
                    {
                        CurrentItem.AccessoryType = (AccessoryType)
                            EditorGUILayout.EnumPopup("Accessory Type", CurrentItem.AccessoryType);
                    }

                    CurrentItem.Rarity = (ItemRarity)
                        EditorGUILayout.EnumPopup("Rarity", CurrentItem.Rarity);
                    CurrentItem.MaxStack = EditorGUILayout.IntField(
                        "Max Stack",
                        CurrentItem.MaxStack
                    );
                }
                EditorGUILayout.EndVertical();

                if (showStatRanges)
                {
                    EditorGUILayout.Space(10);
                    DrawStatRanges();
                }

                if (showEffects)
                {
                    EditorGUILayout.Space(10);
                    DrawItemEffects();
                }

                if (showResources)
                {
                    EditorGUILayout.Space(10);
                    DrawResources();
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

    private void DrawItemEffects()
    {
        EditorGUILayout.Space();
        DrawEffectRangesSection();
    }

    private void DrawDropTablesTab()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            {
                DrawDropTablesList();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
            DrawVerticalLine(Color.gray);
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical();
            {
                DrawDropTableDetails();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawDropTablesList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Search & Filter", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            dropTableSearchText = EditorGUILayout.TextField("Search", dropTableSearchText);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Drop Tables", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            float listHeight = position.height - 300;
            dropTableListScrollPosition = EditorGUILayout.BeginScrollView(
                dropTableListScrollPosition,
                GUILayout.Height(listHeight)
            );
            {
                var filteredDropTables = FilterDropTables();
                foreach (var kvp in filteredDropTables)
                {
                    bool isSelected = kvp.Key == selectedMonsterType;
                    GUI.backgroundColor = isSelected ? Color.cyan : Color.white;
                    if (GUILayout.Button(kvp.Key.ToString(), GUILayout.Height(25)))
                    {
                        selectedMonsterType = kvp.Key;
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawDropTableDetails()
    {
        if (selectedMonsterType == MonsterType.None)
        {
            EditorGUILayout.LabelField("Select a drop table to edit", headerStyle);
            return;
        }

        dropTables = ItemDataEditorUtility.GetDropTables();

        var dropTable = dropTables.TryGetValue(selectedMonsterType, out var dt) ? dt : null;
        if (dropTable == null)
        {
            dropTable = new DropTableData
            {
                enemyType = selectedMonsterType,
                dropEntries = new List<DropTableEntry>(),
                guaranteedDropRate = 0.1f,
                maxDrops = 3,
            };
            dropTables[selectedMonsterType] = dropTable;
            ItemDataEditorUtility.SaveDropTables();
            AssetDatabase.Refresh();
        }

        DrawDropTableDetails(dropTable);
    }

    private void DrawDropTableDetails(DropTableData dropTable)
    {
        if (dropTable == null)
            return;

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Drop Table Details", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);

        float newDropRate = EditorGUILayout.Slider(
            new GUIContent("Guaranteed Drop Rate", "Chance for a guaranteed drop"),
            dropTable.guaranteedDropRate,
            0f,
            1f
        );
        if (Math.Abs(newDropRate - dropTable.guaranteedDropRate) > float.Epsilon)
        {
            dropTable.guaranteedDropRate = newDropRate;
            GUI.changed = true;
        }

        int newMaxDrops = EditorGUILayout.IntSlider(
            new GUIContent("Max Drops", "Maximum number of items that can drop"),
            dropTable.maxDrops,
            1,
            10
        );
        if (newMaxDrops != dropTable.maxDrops)
        {
            dropTable.maxDrops = newMaxDrops;
            GUI.changed = true;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Drop Entries", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Entry", GUILayout.Height(30)))
        {
            AddDropTableEntry(dropTable);
        }

        EditorGUILayout.Space();

        if (dropTable.dropEntries != null && dropTable.dropEntries.Count > 0)
        {
            dropTableDetailScrollPosition = EditorGUILayout.BeginScrollView(
                dropTableDetailScrollPosition
            );
            for (int i = 0; i < dropTable.dropEntries.Count; i++)
            {
                bool shouldRemove = false;
                DrawDropTableEntry(dropTable, i, out shouldRemove);

                if (shouldRemove)
                {
                    dropTable.dropEntries.RemoveAt(i);
                    i--;
                    GUI.changed = true;
                    SaveDropTableChanges();
                }
            }
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox(
                "No entries in this drop table. Click 'Add Entry' to add items.",
                MessageType.Info
            );
        }

        EditorGUILayout.EndVertical();

        if (GUI.changed)
        {
            SaveDropTableChanges();
        }
    }

    private void AddDropTableEntry(DropTableData dropTable)
    {
        if (dropTable.dropEntries == null)
        {
            dropTable.dropEntries = new List<DropTableEntry>();
        }

        dropTable.dropEntries.Add(
            new DropTableEntry
            {
                itemId = itemDatabase.Values.FirstOrDefault()?.ID ?? Guid.Empty,
                dropRate = 0.1f,
                rarity = ItemRarity.Common,
                minAmount = 1,
                maxAmount = 1,
            }
        );

        SaveDropTableChanges();
        GUI.changed = true;
        Repaint();
    }

    private void SaveDropTableChanges()
    {
        try
        {
            ItemDataEditorUtility.SaveDropTables();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving drop table changes: {e.Message}\n{e.StackTrace}");
        }
    }

    private Dictionary<MonsterType, DropTableData> FilterDropTables()
    {
        return dropTables
            .Where(kvp =>
                string.IsNullOrEmpty(dropTableSearchText)
                || kvp.Key.ToString().ToLower().Contains(dropTableSearchText.ToLower())
            )
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private void DrawEffectRangesTab()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            {
                DrawEffectRangesList();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
            DrawVerticalLine(Color.gray);
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical();
            {
                DrawEffectRangeDetails();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEffectRangesList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Search & Filter", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            effectRangeSearchText = EditorGUILayout.TextField("Search", effectRangeSearchText);
            effectTypeFilter = (EffectType)
                EditorGUILayout.EnumPopup("Effect Type", effectTypeFilter);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Effect Ranges", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            float listHeight = position.height - 300;
            effectRangeListScrollPosition = EditorGUILayout.BeginScrollView(
                effectRangeListScrollPosition,
                GUILayout.Height(listHeight)
            );
            {
                var database = ItemDataEditorUtility.GetEffectRangeDatabase();
                var filteredEffects = FilterEffectRanges(database.effectRanges);

                foreach (var effect in filteredEffects)
                {
                    bool isSelected = effect.effectId == selectedEffectRangeId;
                    GUI.backgroundColor = isSelected ? Color.cyan : Color.white;
                    if (GUILayout.Button(effect.effectName, GUILayout.Height(25)))
                    {
                        selectedEffectRangeId = effect.effectId;
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        if (GUILayout.Button("Create New Effect Range", GUILayout.Height(30)))
        {
            var newRange = new ItemEffectRange
            {
                effectId = Guid.NewGuid(),
                effectName = "New Effect Range",
                description = "",
                subEffectRanges = new List<SubEffectRange>(),
                weight = 1f,
            };
            ItemDataEditorUtility.SaveEffectRange(newRange);
            selectedEffectRangeId = newRange.effectId;
        }
    }

    private void DrawEffectRangeDetails()
    {
        var database = ItemDataEditorUtility.GetEffectRangeDatabase();
        var effectRange = database.effectRanges.FirstOrDefault(e =>
            e.effectId == selectedEffectRangeId
        );

        if (effectRange == null)
        {
            EditorGUILayout.LabelField("Select an effect range to edit", headerStyle);
            return;
        }

        bool changed = false;
        effectRangeDetailScrollPosition = EditorGUILayout.BeginScrollView(
            effectRangeDetailScrollPosition
        );
        {
            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField("ID", effectRange.effectId.ToString());
                    EditorGUI.EndDisabledGroup();

                    string newEffectName = EditorGUILayout.TextField(
                        "Name",
                        effectRange.effectName
                    );
                    if (newEffectName != effectRange.effectName)
                    {
                        effectRange.effectName = newEffectName;
                        changed = true;
                    }

                    string newDescription = EditorGUILayout.TextField(
                        "Description",
                        effectRange.description
                    );
                    if (newDescription != effectRange.description)
                    {
                        effectRange.description = newDescription;
                        changed = true;
                    }

                    float newWeight = EditorGUILayout.Slider("Weight", effectRange.weight, 0f, 1f);
                    if (newWeight != effectRange.weight)
                    {
                        effectRange.weight = newWeight;
                        changed = true;
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("Sub Effects", EditorStyles.boldLabel);
                    EditorGUILayout.Space(5);

                    if (GUILayout.Button("Add Sub Effect"))
                    {
                        effectRange.subEffectRanges.Add(
                            new SubEffectRange
                            {
                                effectType = EffectType.None,
                                minValue = 0f,
                                maxValue = 1f,
                                description = "",
                                isEnabled = true,
                            }
                        );
                        changed = true;
                    }

                    for (int i = 0; i < effectRange.subEffectRanges.Count; i++)
                    {
                        var subEffect = effectRange.subEffectRanges[i];
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                subEffect.isEnabled = EditorGUILayout.Toggle(
                                    subEffect.isEnabled,
                                    GUILayout.Width(20)
                                );
                                EditorGUI.BeginDisabledGroup(!subEffect.isEnabled);

                                EditorGUILayout.BeginVertical();
                                {
                                    EffectType newEffectType = (EffectType)
                                        EditorGUILayout.EnumPopup(
                                            "Effect Type",
                                            subEffect.effectType
                                        );
                                    if (newEffectType != subEffect.effectType)
                                    {
                                        subEffect.effectType = newEffectType;
                                        changed = true;
                                    }

                                    string newSubDescription = EditorGUILayout.TextField(
                                        "Description",
                                        subEffect.description
                                    );
                                    if (newSubDescription != subEffect.description)
                                    {
                                        subEffect.description = newSubDescription;
                                        changed = true;
                                    }

                                    EditorGUILayout.BeginHorizontal();
                                    {
                                        float newMinValue = EditorGUILayout.FloatField(
                                            "Value Range",
                                            subEffect.minValue
                                        );
                                        float newMaxValue = EditorGUILayout.FloatField(
                                            "to",
                                            subEffect.maxValue
                                        );
                                        if (
                                            newMinValue != subEffect.minValue
                                            || newMaxValue != subEffect.maxValue
                                        )
                                        {
                                            subEffect.minValue = newMinValue;
                                            subEffect.maxValue = newMaxValue;
                                            changed = true;
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                EditorGUILayout.EndVertical();

                                EditorGUI.EndDisabledGroup();
                            }
                            EditorGUILayout.EndHorizontal();

                            if (GUILayout.Button("Remove Sub Effect"))
                            {
                                effectRange.subEffectRanges.RemoveAt(i);
                                i--;
                                changed = true;
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10);
                DrawApplicableSkillTypes(effectRange, ref changed);
                DrawApplicableElementTypes(effectRange, ref changed);

                EditorGUILayout.Space(20);
                if (GUILayout.Button("Delete Effect Range", GUILayout.Height(30)))
                {
                    if (
                        EditorUtility.DisplayDialog(
                            "Delete Effect Range",
                            $"Are you sure you want to delete the effect range '{effectRange.effectName}'?",
                            "Yes",
                            "No"
                        )
                    )
                    {
                        ItemDataEditorUtility.DeleteEffectRange(effectRange.effectId);
                        selectedEffectRangeId = Guid.Empty;
                        changed = true;
                        GUIUtility.ExitGUI();
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        if (changed)
        {
            ItemDataEditorUtility.SaveEffectRangeDatabase();
        }
    }

    private List<ItemEffectRange> FilterEffectRanges(List<ItemEffectRange> effects)
    {
        return effects
            .Where(effect =>
                (
                    string.IsNullOrEmpty(effectRangeSearchText)
                    || effect.effectName.ToLower().Contains(effectRangeSearchText.ToLower())
                )
                && (
                    effectTypeFilter == EffectType.None
                    || effect.subEffectRanges.Any(se => se.effectType == effectTypeFilter)
                )
            )
            .ToList();
    }

    private void DrawStatRanges()
    {
        if (CurrentItem == null)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        try
        {
            bool changed = false;
            EditorGUILayout.BeginHorizontal();
            {
                int newMinCount = EditorGUILayout.IntField(
                    "Stat Count",
                    CurrentItem.StatRanges.minStatCount
                );
                int newMaxCount = EditorGUILayout.IntField(
                    "to",
                    CurrentItem.StatRanges.maxStatCount
                );

                if (
                    newMinCount != CurrentItem.StatRanges.minStatCount
                    || newMaxCount != CurrentItem.StatRanges.maxStatCount
                )
                {
                    CurrentItem.StatRanges.minStatCount = newMinCount;
                    CurrentItem.StatRanges.maxStatCount = newMaxCount;
                    changed = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            for (int i = 0; i < CurrentItem.StatRanges.possibleStats.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    var statRange = CurrentItem.StatRanges.possibleStats[i];

                    StatType newStatType = (StatType)
                        EditorGUILayout.EnumPopup("Stat Type", statRange.statType);
                    if (newStatType != statRange.statType)
                    {
                        statRange.statType = newStatType;
                        changed = true;
                    }

                    EditorGUILayout.BeginHorizontal();
                    {
                        float newMinValue = EditorGUILayout.FloatField(
                            "Value Range",
                            statRange.minValue
                        );
                        float newMaxValue = EditorGUILayout.FloatField("to", statRange.maxValue);
                        if (newMinValue != statRange.minValue || newMaxValue != statRange.maxValue)
                        {
                            statRange.minValue = newMinValue;
                            statRange.maxValue = newMaxValue;
                            changed = true;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    float newWeight = EditorGUILayout.Slider("Weight", statRange.weight, 0f, 1f);
                    if (newWeight != statRange.weight)
                    {
                        statRange.weight = newWeight;
                        changed = true;
                    }

                    IncreaseType newIncreaseType = (IncreaseType)
                        EditorGUILayout.EnumPopup("Increase Type", statRange.increaseType);
                    if (newIncreaseType != statRange.increaseType)
                    {
                        statRange.increaseType = newIncreaseType;
                        changed = true;
                    }

                    if (GUILayout.Button("Remove Stat Range"))
                    {
                        ItemDataEditorUtility.RemoveStatRange(CurrentItem, i);
                        i--;
                        changed = true;
                    }
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Stat Range"))
            {
                ItemDataEditorUtility.AddStatRange(CurrentItem);
                changed = true;
            }

            if (changed)
            {
                ItemDataEditorUtility.SaveStatRanges(CurrentItem);
            }
        }
        finally
        {
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawResources()
    {
        if (CurrentItem == null)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            Sprite oldIcon = CurrentItem.Icon;
            Sprite newIcon = (Sprite)
                EditorGUILayout.ObjectField("Icon", oldIcon, typeof(Sprite), false);

            if (newIcon != oldIcon)
            {
                CurrentItem.Icon = newIcon;
                ItemDataEditorUtility.SaveItemData(CurrentItem);
                EditorUtility.SetDirty(this);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private List<ItemData> FilterItems()
    {
        return itemDatabase
            .Values.Where(item =>
                (
                    string.IsNullOrEmpty(searchText)
                    || item.Name.ToLower().Contains(searchText.ToLower())
                )
                && (typeFilter == ItemType.None || item.Type == typeFilter)
                && (item.Rarity >= rarityFilter)
            )
            .ToList();
    }

    private void CreateNewItem()
    {
        var newItem = new ItemData();
        newItem.ID = Guid.NewGuid();
        newItem.Name = "New Item";
        newItem.Description = "New item description";
        newItem.Type = ItemType.None;
        newItem.Rarity = ItemRarity.Common;
        newItem.MaxStack = 1;
        newItem.AccessoryType = AccessoryType.None;

        ItemDataEditorUtility.SaveItemData(newItem);

        RefreshItemDatabase();

        selectedItemId = newItem.ID;
        GUI.changed = true;
    }

    private void DrawDeleteButton()
    {
        EditorGUILayout.Space(20);

        if (GUILayout.Button("Delete Item", GUILayout.Height(30)))
        {
            if (
                EditorUtility.DisplayDialog(
                    "Delete Item",
                    $"Are you sure you want to delete '{CurrentItem.Name}'?",
                    "Delete",
                    "Cancel"
                )
            )
            {
                Guid itemId = CurrentItem.ID;
                ItemDataEditorUtility.DeleteItemData(itemId);
                selectedItemId = Guid.Empty;

                EditorApplication.delayCall += () =>
                {
                    RefreshData();
                    Repaint();
                };

                EditorUtility.SetDirty(this);
            }
        }
    }

    private void DrawVerticalLine(Color color)
    {
        var rect = EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(1));

        EditorGUI.DrawRect(rect, color);
    }

    private void LoadAllData()
    {
        EditorUtility.DisplayProgressBar("Loading Data", "Loading items...", 0.3f);

        try
        {
            Guid previousSelectedId = selectedItemId;

            RefreshData();

            if (previousSelectedId != Guid.Empty && !itemDatabase.ContainsKey(previousSelectedId))
            {
                selectedItemId = Guid.Empty;
            }

            Logger.Log(typeof(ItemDataEditorWindow), "All data loaded successfully!");
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(ItemDataEditorWindow), $"Error loading data: {e.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void SaveAllData()
    {
        EditorUtility.DisplayProgressBar("Saving Data", "Saving items...", 0.3f);

        try
        {
            Dictionary<Guid, Sprite> iconReferences = new Dictionary<Guid, Sprite>();

            foreach (var item in itemDatabase.Values)
            {
                if (item.Icon != null)
                    iconReferences[item.ID] = item.Icon;
            }

            foreach (var item in itemDatabase.Values)
            {
                ItemDataEditorUtility.SaveItemData(item);
            }

            ItemDataEditorUtility.SaveDatabase();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorApplication.delayCall += () =>
            {
                RefreshData();

                foreach (var kvp in iconReferences)
                {
                    if (itemDatabase.TryGetValue(kvp.Key, out var item))
                    {
                        item.Icon = kvp.Value;
                        ItemDataEditorUtility.SaveItemData(item);
                    }
                }

                Repaint();
            };
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(ItemDataEditorWindow),
                $"Error saving data: {e.Message}\n{e.StackTrace}"
            );
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void DrawEffectRangesSection()
    {
        EditorGUILayout.LabelField("Effect Ranges", EditorStyles.boldLabel);

        var database = ItemDataEditorUtility.GetEffectRangeDatabase();

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("Add Effect Range", GUILayout.Width(120)))
            {
                GenericMenu menu = new GenericMenu();

                foreach (var effectRange in database.effectRanges)
                {
                    bool isAlreadyAdded = CurrentItem.EffectRanges.effectIDs.Contains(
                        effectRange.effectId
                    );
                    if (!isAlreadyAdded)
                    {
                        menu.AddItem(
                            new GUIContent(effectRange.effectName),
                            false,
                            () =>
                            {
                                CurrentItem.EffectRanges.effectIDs.Add(effectRange.effectId);
                                ItemDataEditorUtility.SaveItemData(CurrentItem);
                            }
                        );
                    }
                }

                menu.ShowAsContext();
            }
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < CurrentItem.EffectRanges.effectIDs.Count; i++)
        {
            var effectRange = database.GetEffectRange(CurrentItem.EffectRanges.effectIDs[i]);
            if (effectRange == null)
                continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField($"Effect: {effectRange.effectName}");
                EditorGUILayout.LabelField($"Description: {effectRange.description}");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Sub Effects:", EditorStyles.boldLabel);
                foreach (var subEffect in effectRange.subEffectRanges)
                {
                    if (!subEffect.isEnabled)
                        continue;
                    EditorGUILayout.LabelField(
                        $"- {subEffect.effectType}: {subEffect.minValue} to {subEffect.maxValue}"
                    );
                    if (!string.IsNullOrEmpty(subEffect.description))
                    {
                        EditorGUILayout.LabelField($"  {subEffect.description}");
                    }
                }

                if (GUILayout.Button("Remove"))
                {
                    CurrentItem.EffectRanges.effectIDs.RemoveAt(i);
                    ItemDataEditorUtility.SaveItemData(CurrentItem);
                    i--;
                }
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawApplicableSkillTypes(ItemEffectRange effectRange, ref bool changed)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Applicable Skill Types", EditorStyles.boldLabel);

            if (effectRange.applicableSkills == null)
                effectRange.applicableSkills = new SkillType[0];

            var skillTypes = Enum.GetValues(typeof(SkillType));
            foreach (SkillType skillType in skillTypes)
            {
                bool isSelected =
                    System.Array.IndexOf(effectRange.applicableSkills, skillType) != -1;
                bool newValue = EditorGUILayout.Toggle(skillType.ToString(), isSelected);

                if (newValue != isSelected)
                {
                    ItemDataEditorUtility.UpdateSkillTypes(effectRange, skillType, newValue);
                    changed = true;
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawApplicableElementTypes(ItemEffectRange effectRange, ref bool changed)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("Applicable Element Types", EditorStyles.boldLabel);

            if (effectRange.applicableElements == null)
                effectRange.applicableElements = new ElementType[0];

            var elementTypes = Enum.GetValues(typeof(ElementType));
            foreach (ElementType elementType in elementTypes)
            {
                bool isSelected = Array.IndexOf(effectRange.applicableElements, elementType) != -1;
                bool newValue = EditorGUILayout.Toggle(elementType.ToString(), isSelected);

                if (newValue != isSelected)
                {
                    ItemDataEditorUtility.UpdateElementTypes(effectRange, elementType, newValue);
                    changed = true;
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawDropTableEntry(DropTableData dropTable, int index, out bool shouldRemove)
    {
        shouldRemove = false;
        if (
            dropTable == null
            || dropTable.dropEntries == null
            || index < 0
            || index >= dropTable.dropEntries.Count
        )
            return;

        var entry = dropTable.dropEntries[index];
        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Entry {index + 1}", EditorStyles.boldLabel);
        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            shouldRemove = true;
            return;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Item", GUILayout.Width(100));

            string buttonText = "Select Item";
            Color originalColor = GUI.color;

            if (entry.itemId != Guid.Empty && itemDatabase.ContainsKey(entry.itemId))
            {
                var selectedItem = itemDatabase[entry.itemId];
                buttonText = selectedItem.Name;

                switch (selectedItem.Rarity)
                {
                    case ItemRarity.Uncommon:
                        GUI.color = new Color(0.0f, 1.0f, 0.0f);
                        break;
                    case ItemRarity.Rare:
                        GUI.color = new Color(0.0f, 0.5f, 1.0f);
                        break;
                    case ItemRarity.Epic:
                        GUI.color = new Color(0.5f, 0.0f, 1.0f);
                        break;
                    case ItemRarity.Legendary:
                        GUI.color = new Color(1.0f, 0.5f, 0.0f);
                        break;
                }
            }

            if (GUILayout.Button(buttonText, GUILayout.Height(20)))
            {
                ItemSelectorPopup.Show(
                    itemDatabase,
                    (selectedItem) =>
                    {
                        entry.itemId = selectedItem.ID;
                        entry.rarity = selectedItem.Rarity;
                        GUI.changed = true;
                        SaveDropTableChanges();
                    }
                );
            }

            GUI.color = originalColor;
        }
        EditorGUILayout.EndHorizontal();

        float newDropRate = EditorGUILayout.Slider("Drop Rate", entry.dropRate, 0f, 1f);
        if (Math.Abs(newDropRate - entry.dropRate) > float.Epsilon)
        {
            entry.dropRate = newDropRate;
            GUI.changed = true;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Amount Range", GUILayout.Width(100));

        EditorGUILayout.LabelField("Min:", GUILayout.Width(30));
        int newMinQuantity = EditorGUILayout.IntField(entry.minAmount, GUILayout.Width(50));

        EditorGUILayout.LabelField("Max:", GUILayout.Width(30));
        int newMaxQuantity = EditorGUILayout.IntField(entry.maxAmount, GUILayout.Width(50));

        if (newMinQuantity != entry.minAmount || newMaxQuantity != entry.maxAmount)
        {
            entry.minAmount = Mathf.Max(1, newMinQuantity);
            entry.maxAmount = Mathf.Max(entry.minAmount, newMaxQuantity);
            GUI.changed = true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    public class ItemSelectorPopup : EditorWindow
    {
        private string searchText = "";
        private Vector2 scrollPosition;
        private ItemType typeFilter = ItemType.None;
        private ItemRarity rarityFilter = ItemRarity.Common;
        private Dictionary<Guid, ItemData> itemDatabase;
        private Action<ItemData> onItemSelected;

        public static void Show(Dictionary<Guid, ItemData> database, Action<ItemData> callback)
        {
            if (database == null || database.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "알림",
                    "아이템 데이터베이스가 비어있습니다.\n먼저 아이템을 생성해주세요.",
                    "확인"
                );
                return;
            }

            var window = GetWindow<ItemSelectorPopup>("아이템 선택");
            window.itemDatabase = database;
            window.onItemSelected = callback;
            window.minSize = new Vector2(400, 500);
            window.maxSize = new Vector2(600, 800);
            window.ShowAuxWindow();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("검색 & 필터", EditorStyles.boldLabel);
                searchText = EditorGUILayout.TextField("검색", searchText);
                typeFilter = (ItemType)EditorGUILayout.EnumPopup("아이템 타입", typeFilter);
                rarityFilter = (ItemRarity)EditorGUILayout.EnumPopup("희귀도", rarityFilter);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("아이템 목록", EditorStyles.boldLabel);

                var filteredItems = itemDatabase
                    .Values.Where(item =>
                        (
                            string.IsNullOrEmpty(searchText)
                            || item.Name.ToLower().Contains(searchText.ToLower())
                        )
                        && (typeFilter == ItemType.None || item.Type == typeFilter)
                        && (item.Rarity >= rarityFilter)
                    )
                    .OrderBy(item => item.Rarity)
                    .ThenBy(item => item.Name)
                    .ToList();

                if (filteredItems.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "검색 조건에 맞는 아이템이 없습니다.\n필터 조건을 변경해보세요.",
                        MessageType.Info
                    );
                }
                else
                {
                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                    ItemRarity currentRarity = ItemRarity.Common;
                    foreach (var item in filteredItems)
                    {
                        if (item.Rarity != currentRarity)
                        {
                            EditorGUILayout.Space(5);
                            EditorGUILayout.LabelField(
                                $"[ {item.Rarity} ]",
                                EditorStyles.boldLabel
                            );
                            currentRarity = item.Rarity;
                        }

                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        {
                            if (item.Icon != null)
                            {
                                GUILayout.Label(
                                    new GUIContent(item.Icon.texture),
                                    GUILayout.Width(32),
                                    GUILayout.Height(32)
                                );
                            }
                            else
                            {
                                GUILayout.Label(
                                    "No Icon",
                                    GUILayout.Width(32),
                                    GUILayout.Height(32)
                                );
                            }

                            EditorGUILayout.BeginVertical();
                            {
                                EditorGUILayout.LabelField(item.Name, EditorStyles.boldLabel);
                                EditorGUILayout.LabelField(
                                    $"타입: {item.Type}",
                                    EditorStyles.miniLabel
                                );
                                if (!string.IsNullOrEmpty(item.Description))
                                {
                                    EditorGUILayout.LabelField(
                                        item.Description,
                                        EditorStyles.miniLabel
                                    );
                                }
                            }
                            EditorGUILayout.EndVertical();

                            if (GUILayout.Button("선택", GUILayout.Width(60)))
                            {
                                onItemSelected?.Invoke(item);
                                Close();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("취소"))
            {
                Close();
            }
        }
    }
}
