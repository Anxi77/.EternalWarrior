using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ItemEffectEditorWindow : EditorWindow
{
    private Guid selectedEffectRangeId = Guid.Empty;
    private string effectRangeSearchText = "";
    private EffectType effectTypeFilter = EffectType.None;
    private Vector2 effectRangeListScrollPosition;
    private Vector2 effectRangeDetailScrollPosition;

    private GUIStyle headerStyle;

    [MenuItem("Anxi/Item Effect Editor")]
    public static void ShowWindow()
    {
        GetWindow<ItemEffectEditorWindow>("Item Effect Editor");
    }

    private void OnEnable()
    {
        LoadAllData();
    }

    private void LoadAllData()
    {
        EditorUtility.DisplayProgressBar("Loading Data", "Loading effects...", 0.3f);

        try
        {
            var database = ItemDataEditorUtility.GetEffectRangeDatabase();
            if (database.effectRanges.Any())
            {
                selectedEffectRangeId = database.effectRanges.First().effectId;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(ItemEffectEditorWindow), $"Error loading data: {e.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void SaveAllData()
    {
        ItemDataEditorUtility.SaveEffectRangeDatabase();
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
                DrawEffectRangesTab();
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
                    selectedEffectRangeId = Guid.Empty;

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
            EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
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

        if (GUILayout.Button("Create New Effect", GUILayout.Height(30)))
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
            EditorGUILayout.LabelField("Select an Effect to edit", headerStyle);
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
                if (GUILayout.Button("Delete Effect", GUILayout.Height(30)))
                {
                    if (
                        EditorUtility.DisplayDialog(
                            "Delete Effect",
                            $"Are you sure you want to delete the effect'{effectRange.effectName}'?",
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

    private void DrawVerticalLine(Color color)
    {
        var rect = EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(1));

        EditorGUI.DrawRect(rect, color);
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
}
