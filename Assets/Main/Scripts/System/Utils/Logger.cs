using System;
using System.Text.RegularExpressions;
using UnityEngine;

public static class Logger
{
    public static void Log(Type type, string message)
    {
        string typeName = type.Name;
        typeName = Regex.Replace(typeName, @"`\d+", "");
        Debug.Log($"<color=#90EE90>[{typeName}] {message} </color>");
    }

    public static void LogWarning(Type type, string message)
    {
        string typeName = type.Name;
        typeName = Regex.Replace(typeName, @"`\d+", "");
        Debug.LogWarning($"<color=#DAA520>[{typeName}] {message} </color>");
    }

    public static void LogError(Type type, string message)
    {
        string typeName = type.Name;
        typeName = Regex.Replace(typeName, @"`\d+", "");
        Debug.LogError($"<color=#FF1493>[{typeName}] {message} </color>");
    }
}
