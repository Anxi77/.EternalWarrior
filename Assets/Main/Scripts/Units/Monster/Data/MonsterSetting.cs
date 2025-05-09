using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableStatValue
{
    public StatType statType;
    public float value;
}

[Serializable]
public class MonsterData
{
    public MonsterType type;
    public Monster monsterPrefab;
    public StatData statData;
    public ElementType elementType = ElementType.None;

    [SerializeField]
    public List<SerializableStatValue> Stats = new List<SerializableStatValue>();

    public void InitializeStatsFromStatData()
    {
        if (statData == null || statData.baseStats == null)
        {
            return;
        }

        if (Stats.Count > 0)
        {
            return;
        }

        foreach (var pair in statData.baseStats)
        {
            Stats.Add(new SerializableStatValue { statType = pair.Key, value = pair.Value });
        }
    }

    public void UpdateStatData()
    {
        if (statData == null)
        {
            statData = new StatData();
        }

        foreach (var stat in Stats)
        {
            statData.baseStats[stat.statType] = stat.value;
        }
    }
}

[CreateAssetMenu(
    fileName = "MonsterSetting",
    menuName = "ScriptableObjects/MonsterSetting",
    order = 1
)]
public class MonsterSetting : ScriptableObject
{
    [Header("Spawn Settings")]
    public float spawnInterval;
    public Vector2Int minMaxCount;
    public Vector2 spawnOffset;
    public Vector2 bossSpawnOffset = new Vector2(0, 5f);
    public Vector2 dropRadiusRange;
    public Vector2Int expParticleRange;
    public ExpParticle expParticlePrefab;

    [Header("Monster Data")]
    [SerializeField]
    public List<MonsterData> monsters = new List<MonsterData>();

    private Dictionary<MonsterType, MonsterData> _monsterDict;

    public Dictionary<MonsterType, MonsterData> MonsterData
    {
        get
        {
            if (_monsterDict == null || _isDirty)
            {
                RebuildDictionary();
            }
            return _monsterDict;
        }
    }

    private bool _isDirty = true;

    private void RebuildDictionary()
    {
        _monsterDict = new Dictionary<MonsterType, MonsterData>();
        foreach (var entry in monsters)
        {
            if (_isDirty)
            {
                entry.UpdateStatData();
            }

            if (!_monsterDict.ContainsKey(entry.type))
            {
                _monsterDict.Add(entry.type, entry);
            }
        }
        _isDirty = false;
    }

    private void OnEnable()
    {
        _isDirty = true;
    }

    private void OnValidate()
    {
        CheckData();

        foreach (var monster in monsters)
        {
            monster.UpdateStatData();
        }

        _isDirty = true;
    }

    private void CheckData()
    {
        monsters.RemoveAll(m => m.type == MonsterType.None);

        foreach (var type in Enum.GetValues(typeof(MonsterType)))
        {
            if ((MonsterType)type == MonsterType.None)
            {
                continue;
            }

            if (!monsters.Exists(m => m.type == (MonsterType)type))
            {
                var newMonster = new MonsterData
                {
                    type = (MonsterType)type,
                    monsterPrefab = null,
                    statData = new StatData(),
                };
                newMonster.statData.CreateMonsterDefaultStat();

                newMonster.InitializeStatsFromStatData();

                monsters.Add(newMonster);
            }
        }

        HashSet<MonsterType> types = new HashSet<MonsterType>();
        List<MonsterData> duplicates = new List<MonsterData>();

        foreach (var entry in monsters)
        {
            if (types.Contains(entry.type))
            {
                duplicates.Add(entry);
            }
            else
            {
                types.Add(entry.type);
            }
        }

        foreach (var duplicate in duplicates)
        {
            monsters.Remove(duplicate);
        }
    }
}
