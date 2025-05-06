using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ItemEditorWindow : EditorWindow
{
    private ItemData CurrentItem
    {
        get
        {
            if (selectedItemId == Guid.Empty)
                return null;
            return itemDatabase.TryGetValue(selectedItemId, out var item) ? item : null;
        }
    }
    private Dictionary<Guid, ItemData> itemDatabase = new();
    private string searchText = "";
    private ItemType typeFilter = ItemType.None;
    private ItemRarity rarityFilter = ItemRarity.Common;
    private Guid selectedItemId;
    private GUIStyle headerStyle;
    private Vector2 itemListScrollPosition;
    private Vector2 itemDetailScrollPosition;

    private bool showStatRanges = true;
    private bool showEffects = true;
    private bool showResources = true;

    [MenuItem("Anxi/RPG/Item/Item Editor")]
    public static void ShowWindow()
    {
        GetWindow<ItemEditorWindow>("Item Editor");
    }

    private void OnEnable()
    {
        LoadAllData();
    }

    private void OnGUI()
    {
        if (headerStyle == null)
        {
            InitializeStyles();
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical();
        {
            float footerHeight = 25f;
            float contentHeight = position.height - footerHeight - 35f;
            EditorGUILayout.BeginVertical(GUILayout.Height(contentHeight));
            {
                DrawItemsTab();
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

    private void DrawVerticalLine(Color color)
    {
        var rect = EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(1));

        EditorGUI.DrawRect(rect, color);
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
                    if (isSelected)
                    {
                        GUI.backgroundColor = Color.white;
                    }
                    else
                    {
                        switch (item.Rarity)
                        {
                            case ItemRarity.Common:
                                GUI.backgroundColor = RarityColor.Common;
                                break;
                            case ItemRarity.Uncommon:
                                GUI.backgroundColor = RarityColor.Uncommon;
                                break;
                            case ItemRarity.Rare:
                                GUI.backgroundColor = RarityColor.Rare;
                                break;
                            case ItemRarity.Epic:
                                GUI.backgroundColor = RarityColor.Epic;
                                break;
                            case ItemRarity.Legendary:
                                GUI.backgroundColor = RarityColor.Legendary;
                                break;
                            default:
                                GUI.backgroundColor = Color.white;
                                break;
                        }
                    }
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
                        RefreshItemDatabase();
                        Repaint();
                    };

                    EditorUtility.SetDirty(this);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
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

    private void RefreshItemDatabase()
    {
        itemDatabase = ItemDataEditorUtility.GetItemDatabase();
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
                    RefreshItemDatabase();
                    Repaint();
                };

                EditorUtility.SetDirty(this);
            }
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
                RefreshItemDatabase();

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
                typeof(ItemEditorWindow),
                $"Error saving data: {e.Message}\n{e.StackTrace}"
            );
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void LoadAllData()
    {
        EditorUtility.DisplayProgressBar("Loading Data", "Loading items...", 0.3f);

        try
        {
            Guid previousSelectedId = selectedItemId;

            RefreshItemDatabase();

            if (previousSelectedId != Guid.Empty && !itemDatabase.ContainsKey(previousSelectedId))
            {
                selectedItemId = Guid.Empty;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(ItemEditorWindow), $"Error loading data: {e.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
