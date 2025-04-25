using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class JSONIO<T>
    where T : class
{
    private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        Converters = new JsonConverter[]
        {
            new StringEnumConverter(), // Enum을 문자열로 변환
        },
    };

    public static void SaveData(string path, string key, T data)
    {
        try
        {
            if (data == null)
            {
                Debug.LogError($"Cannot save null data for key: {key}");
                return;
            }

            string fullPath = Path.Combine(Application.dataPath, "Resources", path, $"{key}.json");
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string jsonData = JsonConvert.SerializeObject(data, jsonSettings);
            File.WriteAllText(fullPath, jsonData);

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            Debug.Log($"[{typeof(T)}] JSON saved successfully: {fullPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving JSON data: {e.Message}\n{e.StackTrace}");
        }
    }

    public static T LoadData(string path, string key)
    {
        try
        {
            string resourcePath = Path.Combine(path, key);
            TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

            if (jsonAsset != null)
            {
                T data = JsonConvert.DeserializeObject<T>(jsonAsset.text, jsonSettings);
                return data;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading JSON data: {e.Message}");
        }

        return null;
    }

    public static bool DeleteData(string path, string key)
    {
        try
        {
            string fullPath = Path.Combine(Application.dataPath, "Resources", path, $"{key}.json");
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting JSON data: {e.Message}");
        }
        return false;
    }

    public static void ClearAll(string path)
    {
        try
        {
            string directory = Path.Combine(Application.dataPath, "Resources", path);
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, "*.json");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Error clearing JSON data: {e.Message}");
        }
    }
}
