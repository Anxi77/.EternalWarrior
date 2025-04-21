using System.Collections.Generic;
using UnityEngine;

public interface IDataManager<TKey, TData>
{
    void LoadRuntimeData();
    TData GetData(TKey key);
    List<TData> GetAllData();
    bool HasData(TKey key);
    Dictionary<TKey, TData> GetDatabase();
    GameObject GetPrefab(TKey key, TData data, params object[] parameters);
    void InitializeDefaultData();
    void ClearAllRuntimeData();
    void SaveRuntimeData();
}
