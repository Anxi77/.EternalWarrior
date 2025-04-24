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
        bool overwrite = true
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
            var properties = typeof(T).GetProperties(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
            );

            var headerLine = string.Join(",", properties.Select(p => p.Name.ToLower()));
            csv.AppendLine(headerLine);

            int count = 0;
            foreach (var data in dataList)
            {
                if (data == null)
                {
                    Debug.LogWarning("Skipping null data entry");
                    continue;
                }

                var values = properties.Select(p =>
                {
                    var value = p.GetValue(data);
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
                count++;
            }

            File.WriteAllText(fullPath, csv.ToString());
            Debug.Log($"Successfully saved {count} entries to {fullPath}");
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving bulk CSV data: {e.Message}\n{e.StackTrace}");
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
                    Debug.LogWarning(
                        $"Failed to parse value '{values[i]}' for property '{prop.Name}': {e.Message}"
                    );
                }
            }
        }

        return data;
    }

    public static List<T> LoadBulkData(string path, string fileName)
    {
        Debug.Log($"[CSVIO] 시작: {path}/{fileName}.csv 로드 시도");

        List<T> resultList = new List<T>();
        TextAsset csvFile = Resources.Load<TextAsset>($"{path}/{fileName}");

        if (csvFile == null)
        {
            Debug.LogError($"[CSVIO] 오류: {path}/{fileName}.csv 파일을 찾을 수 없습니다");
            return resultList;
        }

        Debug.Log($"[CSVIO] 성공: CSV 파일 로드됨, 크기: {csvFile.text.Length} 바이트");

        string[] lines = csvFile.text.Split('\n');
        Debug.Log($"[CSVIO] 총 {lines.Length} 행 발견");

        if (lines.Length <= 1)
        {
            Debug.LogError("[CSVIO] 오류: CSV 파일에 데이터가 없습니다 (헤더만 있거나 빈 파일)");
            return resultList;
        }

        // 헤더 분석
        string[] headers = lines[0].Trim().Split(',');
        Debug.Log($"[CSVIO] 헤더: {string.Join(", ", headers)}");

        // 모든 속성 정보 가져오기
        var properties = typeof(T).GetProperties();
        Debug.Log($"[CSVIO] 클래스 {typeof(T).Name}의 속성 수: {properties.Length}");

        // Enum 속성 확인
        foreach (var prop in properties)
        {
            if (prop.PropertyType.IsEnum)
            {
                Debug.Log($"[CSVIO] Enum 속성 발견: {prop.Name}, 타입: {prop.PropertyType.Name}");
                Debug.Log(
                    $"[CSVIO] 가능한 Enum 값: {string.Join(", ", Enum.GetNames(prop.PropertyType))}"
                );
            }
        }

        // 각 행 처리
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            string[] values = line.Split(',');
            Debug.Log($"[CSVIO] 행 {i}: 값 수 = {values.Length}, 첫번째 값 = {values[0]}");

            T item = new T();

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                string header = headers[j];
                string value = values[j];

                // 속성 찾기
                var property = properties.FirstOrDefault(p =>
                    string.Equals(p.Name, header, StringComparison.OrdinalIgnoreCase)
                );

                if (property == null)
                {
                    Debug.LogWarning(
                        $"[CSVIO] 경고: 헤더 '{header}'에 해당하는 속성이 클래스에 없습니다"
                    );
                    continue;
                }

                try
                {
                    // Enum 처리
                    if (property.PropertyType.IsEnum)
                    {
                        Debug.Log(
                            $"[CSVIO] Enum 변환 시도: '{value}' -> {property.PropertyType.Name}"
                        );

                        if (Enum.TryParse(property.PropertyType, value, true, out object enumValue))
                        {
                            Debug.Log($"[CSVIO] Enum 변환 성공: '{value}' -> {enumValue}");
                            property.SetValue(item, enumValue);
                        }
                        else
                        {
                            Debug.LogError(
                                $"[CSVIO] Enum 변환 실패: '{value}'는 {property.PropertyType.Name}의 유효한 값이 아닙니다"
                            );
                            // 기본값 0 설정 (보통 None에 해당)
                            property.SetValue(item, 0);
                        }
                    }
                    // 다른 타입 처리
                    else
                    {
                        // 타입 변환 처리 (기존 코드 사용)
                        // ...

                        // 간단한 디버그 메시지
                        Debug.Log($"[CSVIO] 값 설정: {header} = {value}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"[CSVIO] 오류: {header} 속성에 '{value}' 값 설정 중 예외 발생: {ex.Message}"
                    );
                }
            }

            resultList.Add(item);
        }

        Debug.Log($"[CSVIO] 완료: 총 {resultList.Count}개 항목 로드됨");
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
                Debug.Log($"Created new CSV file: {fullPath}");
#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating default CSV file: {e.Message}\n{e.StackTrace}");
        }
    }
}
