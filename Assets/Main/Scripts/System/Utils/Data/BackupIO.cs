using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class BackupIO
{
    private const string BACKUP_PATH = "Backups";
    private const int MAX_BACKUPS = 5;

    private static readonly string[] BACKUP_EXTENSIONS = new string[] { ".json", ".csv" };

    public static void CreateBackup(string sourcePath)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(Application.dataPath, BACKUP_PATH, timestamp);

            Directory.CreateDirectory(backupPath);

            CopyDirectory(sourcePath, backupPath);

            CleanupOldBackups();

            Logger.Log(typeof(BackupIO), $"Backup created successfully: {backupPath}");
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(BackupIO), $"Backup failed: {e.Message}");
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        foreach (
            string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories)
        )
        {
            Directory.CreateDirectory(dirPath.Replace(source, target));
        }

        foreach (string filePath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
        {
            if (
                BACKUP_EXTENSIONS.Any(ext =>
                    filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                string newPath = filePath.Replace(source, target);
                File.Copy(filePath, newPath, true);

                string metaPath = filePath + ".meta";
                if (File.Exists(metaPath))
                {
                    string newMetaPath = newPath + ".meta";
                    File.Copy(metaPath, newMetaPath, true);
                    UpdateMetaFileGuid(newMetaPath);
                }
            }
        }
    }

    private static void UpdateMetaFileGuid(string metaFilePath)
    {
        try
        {
            string content = File.ReadAllText(metaFilePath);

            string pattern = @"guid: \w+";
            string newGuid = Guid.NewGuid().ToString("N");
            content = Regex.Replace(content, pattern, $"guid: {newGuid}");

            File.WriteAllText(metaFilePath, content);
        }
        catch (Exception e)
        {
            Logger.LogWarning(
                typeof(BackupIO),
                $"Error Occured while updating GUID: {metaFilePath}, Error: {e.Message}"
            );
        }
    }

    private static void CleanupOldBackups()
    {
        string backupRoot = Path.Combine(Application.dataPath, BACKUP_PATH);

        var backups = Directory
            .GetDirectories(backupRoot)
            .OrderByDescending(d => d)
            .Skip(MAX_BACKUPS);

        foreach (var oldBackup in backups)
        {
            try
            {
                Directory.Delete(oldBackup, true);

                string metaFilePath = oldBackup + ".meta";
                if (File.Exists(metaFilePath))
                {
                    File.Delete(metaFilePath);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(
                    typeof(BackupIO),
                    $"Error Occured while deleting backup: {oldBackup}, Error: {e.Message}"
                );
            }
        }
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    public static bool RestoreFromBackup(string backupTimestamp)
    {
        try
        {
            string backupPath = Path.Combine(Application.dataPath, BACKUP_PATH, backupTimestamp);
            string resourcePath = Path.Combine(Application.dataPath, "Resources");

            if (!Directory.Exists(backupPath))
            {
                Logger.LogError(typeof(BackupIO), $"Backup not found: {backupPath}");
                return false;
            }

            CreateBackup(resourcePath);

            CopyDirectory(backupPath, resourcePath);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(BackupIO), $"Restore failed: {e.Message}");
            return false;
        }
    }
}
