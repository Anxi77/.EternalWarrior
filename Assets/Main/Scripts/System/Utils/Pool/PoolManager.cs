using System;
using System.Collections;
using UnityEngine;

public class PoolManager : Singleton<PoolManager>
{
    public bool IsInitialized { get; private set; }

    private ObjectPool objectPool;

    public IEnumerator Initialize()
    {
        yield return null;
        yield return new WaitForSeconds(0.5f);

        LoadingManager.Instance.SetLoadingText("Loading Pool");

        if (objectPool == null)
        {
            ObjectPool poolObj = new GameObject("ObjectPool").AddComponent<ObjectPool>();
            poolObj.transform.SetParent(transform);
            objectPool = poolObj;
        }

        objectPool.Initialize();

        yield return new WaitForSeconds(0.5f);

        IsInitialized = true;
    }

    public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation)
        where T : Component
    {
        string originalName = prefab.name;

        T spawnedObj = objectPool.Spawn<T>(prefab, position, rotation);
        if (spawnedObj != null)
        {
            spawnedObj.gameObject.name = originalName;
        }

        return spawnedObj;
    }

    public void Despawn<T>(T obj)
        where T : Component
    {
        objectPool.Despawn(obj);
    }

    public void Despawn<T>(T obj, float delay)
        where T : Component
    {
        StartCoroutine(DespawnCoroutine(obj, delay));
    }

    private IEnumerator DespawnCoroutine<T>(T obj, float delay)
        where T : Component
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            objectPool.Despawn(obj);
        }
    }

    public void ClearAllPools()
    {
        if (objectPool != null)
        {
            objectPool.ClearAllPools();
        }
    }
}
