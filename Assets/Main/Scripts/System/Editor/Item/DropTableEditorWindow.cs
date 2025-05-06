using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DropTableEditorWindow : EditorWindow
{
    private Dictionary<Guid, ItemData> itemDatabase = new();
    private Dictionary<MonsterType, DropTableData> dropTables = new();
    private string dropTableSearchText = "";
    private MonsterType selectedMonsterType = MonsterType.None;
    private GUIStyle headerStyle;
    private GUIStyle tabStyle;
    private Vector2 dropTableListScrollPosition;
    private Vector2 dropTableDetailScrollPosition;

    [MenuItem("Anxi/RPG/Item/Drop Table Editor")]
    public static void ShowWindow()
    {
        GetWindow<DropTableEditorWindow>("Drop Table Editor");
    }

    private void OnEnable()
    {
        LoadAllData();
    }

    private void OnGUI()
    {
        if (headerStyle == null || tabStyle == null)
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
                DrawDropTablesTab();
            }
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            DrawFooter();
        }
        EditorGUILayout.EndVertical();
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
                    ItemDataEditorUtility.InitializeDefaultDropTables();
                    selectedMonsterType = MonsterType.None;

                    EditorApplication.delayCall += () =>
                    {
                        LoadAllData();
                        Repaint();
                    };

                    EditorUtility.SetDirty(this);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void SaveAllData()
    {
        ItemDataEditorUtility.SaveDropTables();
    }

    private void LoadAllData()
    {
        itemDatabase = ItemDataEditorUtility.GetItemDatabase();
        dropTables = ItemDataEditorUtility.GetDropTables();
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
                itemId = Guid.Empty,
                dropRate = 0.1f,
                rarity = ItemRarity.None,
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

    private void DrawVerticalLine(Color color)
    {
        var rect = EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(1));

        EditorGUI.DrawRect(rect, color);
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
        bool removePressed = GUILayout.Button("Remove", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        if (!removePressed)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Item", GUILayout.Width(100));

                if (entry.itemId != Guid.Empty && itemDatabase.ContainsKey(entry.itemId))
                {
                    var selectedItem = itemDatabase[entry.itemId];
                    Color originalColor = GUI.backgroundColor;

                    switch (selectedItem.Rarity)
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
                    }

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    {
                        if (selectedItem.Icon != null)
                        {
                            GUILayout.Label(
                                new GUIContent(selectedItem.Icon.texture),
                                GUILayout.Width(32),
                                GUILayout.Height(32)
                            );
                        }
                        EditorGUILayout.BeginVertical();
                        {
                            EditorGUILayout.LabelField(selectedItem.Name, EditorStyles.boldLabel);
                            EditorGUILayout.LabelField(
                                $"[{selectedItem.Rarity}] {selectedItem.Type}",
                                EditorStyles.miniLabel
                            );
                        }
                        EditorGUILayout.EndVertical();

                        if (GUILayout.Button("Change", GUILayout.Width(60), GUILayout.Height(32)))
                        {
                            ItemSelectorPopup.Show(
                                itemDatabase,
                                (newSelectedItem) =>
                                {
                                    entry.itemId = newSelectedItem.ID;
                                    entry.rarity = newSelectedItem.Rarity;
                                    GUI.changed = true;
                                    SaveDropTableChanges();
                                }
                            );
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    GUI.backgroundColor = originalColor;
                }
                else
                {
                    if (GUILayout.Button("Select Item", GUILayout.Height(32)))
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
                }
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
        }

        EditorGUILayout.EndVertical();

        if (removePressed)
        {
            shouldRemove = true;
        }
    }
}

public class ItemSelectorPopup : EditorWindow
{
    private string searchText = "";
    private Vector2 scrollPosition;
    private ItemType typeFilter = ItemType.None;
    private ItemRarity rarityFilter = ItemRarity.Common;
    private Dictionary<Guid, ItemData> itemDatabase;
    private Action<ItemData> onItemSelected;
    private GUIStyle darkBackgroundStyle;

    public static void Show(Dictionary<Guid, ItemData> database, Action<ItemData> callback)
    {
        if (database == null || database.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Alert",
                "Item database is empty.\nPlease create items first.",
                "OK"
            );
            return;
        }

        var window = GetWindow<ItemSelectorPopup>("Item Selector");
        window.itemDatabase = database;
        window.onItemSelected = callback;
        window.minSize = new Vector2(400, 500);
        window.maxSize = new Vector2(600, 800);
        window.ShowAuxWindow();
    }

    private void OnEnable()
    {
        darkBackgroundStyle = new GUIStyle();
        darkBackgroundStyle.normal.background = EditorGUIUtility.whiteTexture;
    }

    private void OnGUI()
    {
        var darkRect = new Rect(0, 0, position.width, position.height);
        EditorGUI.DrawRect(darkRect, new Color(0.1f, 0.1f, 0.1f, 1));

        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            EditorGUILayout.LabelField("Search & Filter", headerStyle);

            var searchStyle = new GUIStyle(EditorStyles.textField);
            searchStyle.normal.textColor = Color.white;
            searchText = EditorGUILayout.TextField("Search", searchText, searchStyle);

            typeFilter = (ItemType)EditorGUILayout.EnumPopup("Item Type", typeFilter);
            rarityFilter = (ItemRarity)EditorGUILayout.EnumPopup("Rarity", rarityFilter);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            var listHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            listHeaderStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            EditorGUILayout.LabelField("Item List", listHeaderStyle);

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
                var helpBoxStyle = new GUIStyle(EditorStyles.helpBox);
                helpBoxStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
                EditorGUILayout.LabelField(
                    "No items found matching the search criteria.\nPlease adjust the filter settings.",
                    helpBoxStyle
                );
            }
            else
            {
                EditorGUILayout.Space(5);

                scrollPosition = EditorGUILayout.BeginScrollView(
                    scrollPosition,
                    false,
                    true,
                    GUILayout.ExpandHeight(true)
                );
                foreach (var item in filteredItems)
                {
                    Color originalBgColor = GUI.backgroundColor;

                    switch (item.Rarity)
                    {
                        case ItemRarity.Common:
                            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
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
                            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                            break;
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

                        EditorGUILayout.BeginVertical();
                        {
                            var nameStyle = new GUIStyle(EditorStyles.boldLabel);
                            nameStyle.normal.textColor = Color.white;
                            EditorGUILayout.LabelField(
                                item.Name,
                                nameStyle,
                                GUILayout.ExpandWidth(true)
                            );

                            var infoStyle = new GUIStyle(EditorStyles.miniLabel);
                            infoStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                            EditorGUILayout.LabelField($"Type: {item.Type}", infoStyle);
                            EditorGUILayout.LabelField($"Rarity: {item.Rarity}", infoStyle);
                            if (!string.IsNullOrEmpty(item.Description))
                            {
                                EditorGUILayout.LabelField(item.Description, infoStyle);
                            }
                        }
                        EditorGUILayout.EndVertical();

                        var selectButtonStyle = new GUIStyle(GUI.skin.button);
                        selectButtonStyle.normal.textColor = Color.white;
                        if (GUILayout.Button("Select", selectButtonStyle, GUILayout.Width(60)))
                        {
                            onItemSelected?.Invoke(item);
                            Close();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    GUI.backgroundColor = originalBgColor;
                }

                EditorGUILayout.EndScrollView();
            }
        }
        EditorGUILayout.EndVertical();

        GUI.backgroundColor = originalColor;

        EditorGUILayout.Space(10);

        var closeButtonStyle = new GUIStyle(GUI.skin.button);
        closeButtonStyle.normal.textColor = Color.white;
        if (GUILayout.Button("Close", closeButtonStyle))
        {
            Close();
        }
    }
}
