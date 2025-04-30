using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CSVIO<T>
    where T : class, new()
{
    public static void SaveData(string path, string key, T data)
    {
        string fullPath = Path.Combine(Application.dataPath, "Resources", path, $"{key}.csv");
        string directory = Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var csv = new StringBuilder();
        var properties = typeof(T).GetProperties();

        csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        var values = properties.Select(p => p.GetValue(data)?.ToString() ?? "");
        csv.AppendLine(string.Join(",", values));

        File.WriteAllText(fullPath, csv.ToString());
    }

    public static void SaveBulkData(
        string path,
        string key,
        IEnumerable<T> dataList,
        bool overwrite = true,
        IEnumerable<string> includeFields = null
    )
    {
        try
        {
            if (!dataList.Any())
            {
                return;
            }

            string fullPath = Path.Combine(Application.dataPath, "Resources", path, $"{key}.csv");
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var csv = new StringBuilder();
            var fields = typeof(T).GetFields();

            if (includeFields != null && includeFields.Any())
            {
                fields = fields
                    .Where(f =>
                        includeFields.Any(name =>
                            string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    .ToArray();
            }

            var headers = fields.Select(f => f.Name).Distinct().ToList();
            var headerLine = string.Join(",", headers);
            csv.AppendLine(headerLine);

            foreach (var data in dataList)
            {
                if (data == null)
                {
                    Logger.LogWarning(typeof(CSVIO<T>), "Skipping null data entry");
                    continue;
                }

                var values = fields.Select(f =>
                {
                    var value = f.GetValue(data);
                    if (value == null)
                        return "";

                    if (value is string strValue && strValue.Contains(","))
                        return $"\"{strValue}\"";

                    if (value is bool boolValue)
                        return boolValue ? "1" : "0";

                    return value.ToString();
                });

                var line = string.Join(",", values);
                csv.AppendLine(line);
            }

            File.WriteAllText(fullPath, csv.ToString());
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(CSVIO<T>),
                $"Error saving bulk CSV data: {e.Message}\n{e.StackTrace}"
            );
        }
    }

    public static T LoadData(string path, string key)
    {
        string fullPath = Path.Combine(Application.dataPath, "Resources", path, $"{key}.csv");
        if (!File.Exists(fullPath))
            return null;

        var lines = File.ReadAllLines(fullPath);
        if (lines.Length < 2)
            return null;

        var headers = lines[0].Split(',');
        var values = lines[1].Split(',');

        T data = new T();
        var properties = typeof(T).GetProperties();

        for (int i = 0; i < headers.Length; i++)
        {
            var prop = properties.FirstOrDefault(p => p.Name == headers[i]);
            if (prop != null && i < values.Length)
            {
                try
                {
                    if (prop.PropertyType.IsEnum)
                    {
                        prop.SetValue(data, Enum.Parse(prop.PropertyType, values[i]));
                    }
                    else
                    {
                        prop.SetValue(data, Convert.ChangeType(values[i], prop.PropertyType));
                    }
                }
                catch (Exception e)
                {
                    Logger.LogWarning(
                        typeof(CSVIO<T>),
                        $"Failed to parse value '{values[i]}' for property '{prop.Name}': {e.Message}"
                    );
                }
            }
        }

        return data;
    }

    public static List<T> LoadBulkData(
        string path,
        string fileName,
        IEnumerable<string> includeFields = null
    )
    {
        List<T> resultList = new List<T>();
        TextAsset csvFile = Resources.Load<TextAsset>($"{path}/{fileName}");

        if (csvFile == null)
        {
            return resultList;
        }

        string[] lines = csvFile.text.Split('\n');
        if (lines.Length <= 1)
        {
            return resultList;
        }

        string[] headers = lines[0].Trim().Split(',');
        var fields = typeof(T).GetFields();

        if (includeFields != null && includeFields.Any())
        {
            fields = fields
                .Where(f =>
                    includeFields.Any(name =>
                        string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)
                    )
                )
                .ToArray();
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            string[] values = line.Split(',');
            T item = new T();

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                string header = headers[j];
                string value = values[j].Trim();

                var field = fields.FirstOrDefault(f =>
                    string.Equals(f.Name, header, StringComparison.OrdinalIgnoreCase)
                );

                if (field == null)
                {
                    Logger.LogWarning(
                        typeof(CSVIO<T>),
                        $"Warning: No field found for header '{header}' in class {typeof(T).Name}"
                    );
                    continue;
                }

                try
                {
                    if (string.IsNullOrEmpty(value))
                        continue;

                    if (field.FieldType.IsEnum)
                    {
                        if (Enum.TryParse(field.FieldType, value, true, out object enumValue))
                        {
                            field.SetValue(item, enumValue);
                        }
                        else
                        {
                            field.SetValue(item, 0);
                        }
                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        field.SetValue(item, value == "1");
                    }
                    else if (field.FieldType == typeof(int))
                    {
                        field.SetValue(item, int.Parse(value));
                    }
                    else if (field.FieldType == typeof(float))
                    {
                        field.SetValue(item, float.Parse(value));
                    }
                    else if (field.FieldType == typeof(string))
                    {
                        field.SetValue(item, value.Trim('"'));
                    }
                    else
                    {
                        field.SetValue(item, Convert.ChangeType(value, field.FieldType));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(
                        typeof(CSVIO<T>),
                        $"Error setting value '{value}' for field '{header}': {ex.Message}"
                    );
                }
            }

            resultList.Add(item);
        }

        return resultList;
    }

    public static bool DeleteData(string path, string key)
    {
        string fullPath = Path.Combine(Application.dataPath, "Resources", path, $"{key}.csv");
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return true;
        }
        return false;
    }

    public static void ClearAll(string path)
    {
        string directory = Path.Combine(Application.dataPath, "Resources", path);
        if (Directory.Exists(directory))
        {
            var files = Directory.GetFiles(directory, "*.csv");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
    }

    public static void CreateDefaultFile(string path, string fileName, string headers)
    {
        try
        {
            string fullPath = Path.Combine(
                Application.dataPath,
                "Resources",
                path,
                $"{fileName}.csv"
            );
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, headers + "\n");
#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif
            }
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(CSVIO<T>),
                $"Error creating default CSV file: {e.Message}\n{e.StackTrace}"
            );
        }
    }
}
