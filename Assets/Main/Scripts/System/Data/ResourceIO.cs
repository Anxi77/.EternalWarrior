using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 리소스 입출력을 관리하는 제네릭 클래스
/// </summary>
public static class ResourceIO<T>
    where T : Object
{
    private const string RESOURCES_PATH = "Assets/Resources/";
    private const string LOG_PREFIX = "[ResourceIO] ";

    private static readonly Dictionary<string, T> cache = new Dictionary<string, T>();

    /// <summary>
    /// 리소스 데이터를 저장합니다.
    /// </summary>
    /// <param name="path">저장할 경로</param>
    /// <param name="data">저장할 데이터</param>
    /// <returns>저장 성공 여부</returns>
    public static bool SaveData(string path, T data)
    {
        if (data == null || string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"{LOG_PREFIX}Cannot save null data or empty path");
            return false;
        }

        if (typeof(T) == typeof(GameObject))
        {
            SavePrefab(path, data as GameObject);
        }
        else
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

#if UNITY_EDITOR

                if (data is Sprite sprite)
                {
                    SaveSprite(path, sprite);
                }
                else if (data is GameObject prefab)
                {
                    SavePrefab(path, prefab);
                }

                AssetDatabase.Refresh();
#endif
                cache[path] = data;
                Debug.Log($"{LOG_PREFIX}Saved resource to: {path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"{LOG_PREFIX}Error saving resource: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
        return false;
    }

    public static T LoadData(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        try
        {
            if (cache.TryGetValue(key, out T cachedData))
                return cachedData;

#if UNITY_EDITOR
            string assetPath = Path.Combine(RESOURCES_PATH, key);
            if (File.Exists(assetPath))
            {
                T data = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (data != null)
                {
                    cache[key] = data;
                    return data;
                }
            }
#endif

            T resourceData = Resources.Load<T>(key);
            if (resourceData != null)
            {
                cache[key] = resourceData;
                return resourceData;
            }

            Debug.LogWarning($"Failed to load resource: {key}");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading resource: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    public static bool DeleteData(string key)
    {
        try
        {
#if UNITY_EDITOR
            string assetPath = Path.Combine(RESOURCES_PATH, key);
            if (File.Exists(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
                cache.Remove(key);
                AssetDatabase.Refresh();
                return true;
            }
#endif
            cache.Remove(key);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting resource: {e.Message}");
            return false;
        }
    }

    public static void ClearCache()
    {
        cache.Clear();
        Resources.UnloadUnusedAssets();
    }

#if UNITY_EDITOR
    private static void SaveSprite(string path, Sprite sprite)
    {
        try
        {
            string sourcePath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogError("Source sprite path is null or empty");
                return;
            }

            string targetPath = Path.Combine(RESOURCES_PATH, path + '.' + GetExtensionForType());
            string directory = Path.GetDirectoryName(targetPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (sourcePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"Source and target are the same, skipping copy: {targetPath}");
                return;
            }

            if (File.Exists(targetPath))
            {
                AssetDatabase.DeleteAsset(targetPath);
            }

            bool success = AssetDatabase.CopyAsset(sourcePath, targetPath);
            if (success)
            {
                TextureImporter importer = AssetImporter.GetAtPath(targetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
            }
            else
            {
                Debug.LogError($"Failed to copy sprite from {sourcePath} to {targetPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving sprite: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void SavePrefab(string path, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError($"Cannot save null prefab to path: {path}");
            return;
        }

        try
        {
            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
            {
                Debug.LogError(
                    "Cannot save an instance of a prefab. Please use the original prefab from the Project window."
                );
                return;
            }

            string fullPath = Path.Combine(Application.dataPath, "Resources", path + ".prefab");
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, fullPath);
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving prefab: {e.Message}");
        }
    }

    private static string GetExtensionForType()
    {
        if (typeof(T) == typeof(Sprite) || typeof(T) == typeof(Texture2D))
            return "png";
        if (typeof(T) == typeof(GameObject))
            return "prefab";
        return "";
    }
#endif
}
