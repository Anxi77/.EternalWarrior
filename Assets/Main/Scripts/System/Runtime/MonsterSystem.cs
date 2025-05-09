using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class MonsterSystem : MonoBehaviour, IInitializable
{
    public bool IsInitialized { get; private set; }
    private MonsterSetting monsterSetting;
    private Coroutine spawnCoroutine;
    private bool isSpawning = false;

    public void Initialize()
    {
        if (!PoolManager.Instance.IsInitialized)
        {
            Logger.LogWarning(typeof(MonsterSystem), "Waiting for PoolManager to initialize...");
            return;
        }
        try
        {
            monsterSetting = Resources.Load<MonsterSetting>("SO/MonsterSetting");
            if (monsterSetting == null)
            {
                Logger.LogError(typeof(MonsterSystem), "MonsterSetting not found");
                IsInitialized = false;
                return;
            }
            IsInitialized = true;
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(MonsterSystem),
                $"Error initializing MonsterManager: {e.Message}"
            );
            IsInitialized = false;
        }
    }

    #region Spawn Management
    public void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            spawnCoroutine = StartCoroutine(SpawnCoroutine());
        }
    }

    public void StopSpawning()
    {
        if (isSpawning && spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
            isSpawning = false;
        }

        ClearCurrentEnemies();
    }

    private IEnumerator SpawnCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        while (true)
        {
            int enemyCount = Random.Range(
                monsterSetting.minMaxCount.x,
                monsterSetting.minMaxCount.y
            );
            SpawnMonsters(enemyCount);
            yield return new WaitForSeconds(monsterSetting.spawnInterval);
        }
    }

    private void SpawnMonsters(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 playerPos = GameManager.Instance.PlayerSystem.Player.transform.position;
            Vector2 spawnPos = GetValidSpawnPosition(playerPos);

            if (Random.value < 0.5f)
            {
                Monster monster = PoolManager.Instance.Spawn<MeleeMonster>(
                    monsterSetting.MonsterData[MonsterType.Bat].monsterPrefab.gameObject,
                    spawnPos,
                    Quaternion.identity
                );
                monster.Initialize(monsterSetting.MonsterData[MonsterType.Bat], monsterSetting);
            }
            else
            {
                Monster monster = PoolManager.Instance.Spawn<RangedMonster>(
                    monsterSetting.MonsterData[MonsterType.Wasp].monsterPrefab.gameObject,
                    spawnPos,
                    Quaternion.identity
                );
                monster.Initialize(monsterSetting.MonsterData[MonsterType.Wasp], monsterSetting);
            }
        }
    }

    private Vector2 GetValidSpawnPosition(Vector2 playerPos)
    {
        int maxAttempts = 10;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 ranPos = Random.insideUnitCircle;
            Vector2 spawnPos =
                (ranPos * (monsterSetting.spawnOffset.y - monsterSetting.spawnOffset.x))
                + (ranPos.normalized * monsterSetting.spawnOffset.x);
            Vector2 finalPos = playerPos + spawnPos;

            Node node = GameManager.Instance.PathFindingSystem.GetNodeFromWorldPosition(finalPos);
            if (node != null && node.walkable)
            {
                return finalPos;
            }
        }

        return FindNearestWalkablePosition(playerPos);
    }

    private Vector2 FindNearestWalkablePosition(Vector2 centerPos)
    {
        float searchRadius = 1f;
        float maxSearchRadius = monsterSetting.spawnOffset.y;
        float radiusIncrement = 1f;

        while (searchRadius <= maxSearchRadius)
        {
            for (float angle = 0; angle < 360; angle += 45)
            {
                float radian = angle * Mathf.Deg2Rad;
                Vector2 checkPos =
                    centerPos
                    + new Vector2(
                        Mathf.Cos(radian) * searchRadius,
                        Mathf.Sin(radian) * searchRadius
                    );

                Node node = GameManager.Instance.PathFindingSystem.GetNodeFromWorldPosition(
                    checkPos
                );
                if (node != null && node.walkable)
                {
                    return checkPos;
                }
            }
            searchRadius += radiusIncrement;
        }

        return centerPos;
    }
    #endregion

    #region Boss Management
    public void SpawnStageBoss()
    {
        StopSpawning();
        ClearCurrentEnemies();

        Vector3 playerPos = GameManager.Instance.PlayerSystem.Player.transform.position;
        Vector3 spawnPos =
            playerPos
            + new Vector3(monsterSetting.bossSpawnOffset.x, monsterSetting.bossSpawnOffset.y, 0);

        BossMonster boss = PoolManager.Instance.Spawn<BossMonster>(
            monsterSetting.MonsterData[MonsterType.Ogre].monsterPrefab.gameObject,
            spawnPos,
            Quaternion.identity
        );

        boss.Initialize(monsterSetting.MonsterData[MonsterType.Ogre], monsterSetting);
    }

    public void OnBossDefeated(Vector3 position)
    {
        GameManager.Instance.GetCurrentHandler<StageStateHandler>()?.OnBossDefeated(position);
    }
    #endregion

    private void ClearCurrentEnemies()
    {
        var enemies = FindObjectsOfType<Monster>().Where(e => !(e is BossMonster));
        foreach (var enemy in enemies)
        {
            PoolManager.Instance.Despawn(enemy);
        }
    }
}
