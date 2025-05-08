using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class MonsterSystem : MonoBehaviour, IInitializable
{
    public bool IsInitialized { get; private set; }
    public float spawnInterval;

    private MonsterSO monsterSO;

    private Coroutine spawnCoroutine;
    private bool isSpawning = false;
    private bool isBossDefeated = false;
    private Vector3 lastBossPosition;

    public bool IsBossDefeated => isBossDefeated;
    public Vector3 LastBossPosition => lastBossPosition;

    public void Initialize()
    {
        if (!PoolManager.Instance.IsInitialized)
        {
            Logger.LogWarning(typeof(MonsterSystem), "Waiting for PoolManager to initialize...");
            return;
        }
        try
        {
            monsterSO = Resources.Load<MonsterSO>("SO/MonsterSO");
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
            yield return new WaitForSeconds(spawnInterval);
            int enemyCount = Random.Range(monsterSO.minMaxCount.x, monsterSO.minMaxCount.y);
            SpawnMonsters(enemyCount);
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
                    monsterSO.MonsterData[MonsterType.Bat].gameObject,
                    spawnPos,
                    Quaternion.identity
                );
                monster.Initialize();
            }
            else
            {
                Monster monster = PoolManager.Instance.Spawn<RangedMonster>(
                    monsterSO.MonsterData[MonsterType.Wasp].gameObject,
                    spawnPos,
                    Quaternion.identity
                );
                monster.Initialize();
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
                (ranPos * (monsterSO.minMaxDist.y - monsterSO.minMaxDist.x))
                + (ranPos.normalized * monsterSO.minMaxDist.x);
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
        float maxSearchRadius = monsterSO.minMaxDist.y;
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
            playerPos + new Vector3(monsterSO.bossSpawnOffset.x, monsterSO.bossSpawnOffset.y, 0);

        BossMonster boss = PoolManager.Instance.Spawn<BossMonster>(
            monsterSO.MonsterData[MonsterType.Boss].gameObject,
            spawnPos,
            Quaternion.identity
        );

        boss.Initialize();

        isBossDefeated = false;
    }

    public void OnBossDefeated(Vector3 position)
    {
        isBossDefeated = true;
        lastBossPosition = position;
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
