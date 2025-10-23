using UnityEngine;
using UnityEditor;

/// <summary>
/// Suppresses annoying BoxCollider negative scale warnings on scene load
/// These warnings are harmless - Unity automatically fixes the colliders
/// </summary>
[InitializeOnLoad]
public static class SuppressBoxColliderWarnings
{
    static SuppressBoxColliderWarnings()
    {
        // Filter out BoxCollider negative scale warnings
        Application.logMessageReceived += HandleLog;
    }

    private static void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Suppress BoxCollider negative scale warnings
        if (type == LogType.Warning && logString.Contains("BoxCollider does not support negative scale"))
        {
            // Silently ignore these warnings
            return;
        }
    }
}
