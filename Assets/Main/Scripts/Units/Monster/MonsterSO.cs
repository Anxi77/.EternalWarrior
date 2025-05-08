using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MonsterEntry
{
    public MonsterType type;
    public Monster monster;
}

[CreateAssetMenu(fileName = "MonsterSO", menuName = "ScriptableObjects/MonsterSO", order = 1)]
public class MonsterSO : ScriptableObject
{
    [Header("Spawn Settings")]
    public float spawnInterval;
    public Vector2Int minMaxCount;
    public Vector2 minMaxDist;
    public Vector2 bossSpawnOffset = new Vector2(0, 5f);

    [Header("Monster Data")]
    [SerializeField]
    public List<MonsterEntry> monsters = new List<MonsterEntry>();

    private Dictionary<MonsterType, Monster> _monsterDict;

    public Dictionary<MonsterType, Monster> MonsterData
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
        _monsterDict = new Dictionary<MonsterType, Monster>();
        foreach (var entry in monsters)
        {
            if (!_monsterDict.ContainsKey(entry.type))
            {
                _monsterDict.Add(entry.type, entry.monster);
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
        foreach (var type in Enum.GetValues(typeof(MonsterType)))
        {
            if (!monsters.Exists(m => m.type == (MonsterType)type))
            {
                monsters.Add(new MonsterEntry { type = (MonsterType)type, monster = null });
            }
        }

        HashSet<MonsterType> types = new HashSet<MonsterType>();
        List<MonsterEntry> duplicates = new List<MonsterEntry>();

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

        _isDirty = true;
    }
}
