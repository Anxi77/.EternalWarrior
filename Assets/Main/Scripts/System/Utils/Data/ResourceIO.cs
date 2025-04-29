using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
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
    private static readonly Dictionary<string, T> cache = new Dictionary<string, T>();

    /// <summary>
    /// 리소스 데이터를 저장합니다.
    /// </summary>
    /// <param name="path">저장할 경로</param>
    /// <param name="data">저장할 데이터</param>
    /// <returns>저장 성공 여부</returns>
#if UNITY_EDITOR
    public static bool SaveData(string path, T data)
    {
        if (data == null || string.IsNullOrEmpty(path))
        {
            return false;
        }

        if (data is Sprite sprite)
        {
            SaveSprite(path, sprite);
        }
        else if (data is GameObject prefab)
        {
            SavePrefab(path, prefab);
        }

        AssetDatabase.Refresh();
        cache[path] = data;
        Logger.Log(typeof(ResourceIO<T>), $"Saved resource to: {path}");
        return true;
    }
#endif

    public static T LoadData(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (cache.TryGetValue(key, out T cachedData))
            return cachedData;

        T resourceData = Resources.Load<T>(key);
        if (resourceData != null)
        {
            cache[key] = resourceData;
            return resourceData;
        }

        return null;
    }

#if UNITY_EDITOR
    public static bool DeleteData(string key)
    {
        Logger.Log(typeof(ResourceIO<T>), $"Deleting resource: {key}");
        string assetPath = Path.Combine(RESOURCES_PATH, key);
        if (File.Exists(assetPath))
        {
            AssetDatabase.DeleteAsset(assetPath);
            cache.Remove(key);
            AssetDatabase.Refresh();
            return true;
        }
        cache.Remove(key);
        return true;
    }
#endif

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
                Logger.LogError(typeof(ResourceIO<T>), "Source sprite path is null or empty");
                return;
            }

            string targetPath = Path.Combine(RESOURCES_PATH, path + ".png");
            string directory = Path.GetDirectoryName(targetPath);
            directory = directory.Replace("\\", "/");

            Logger.Log(typeof(ResourceIO<T>), $"Directory: {directory}");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (sourcePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log(
                    typeof(ResourceIO<T>),
                    $"Source and target are the same, skipping copy: {targetPath}"
                );
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
                Logger.LogError(
                    typeof(ResourceIO<T>),
                    $"Failed to copy sprite from {sourcePath} to {targetPath}"
                );
            }
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(ResourceIO<T>),
                $"Error saving sprite: {e.Message}\n{e.StackTrace}"
            );
        }
    }

    private static void SavePrefab(string path, GameObject prefab)
    {
        if (prefab == null)
        {
            Logger.LogError(typeof(ResourceIO<T>), $"Cannot save null prefab to path: {path}");
            return;
        }

        try
        {
            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
            {
                Logger.LogError(
                    typeof(ResourceIO<T>),
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
            Logger.LogError(typeof(ResourceIO<T>), $"Error saving prefab: {e.Message}");
        }
    }

#endif
}
