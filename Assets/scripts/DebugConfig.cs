using UnityEngine;

/// <summary>
/// Global debug configuration - disable all debug logging for production
/// Add DISABLE_DEBUG_LOGS to your Scripting Define Symbols in Player Settings
/// Or use this class to control debug logging at runtime
/// </summary>
public static class DebugConfig
{
    // Set this to false to disable ALL debug logging in your game
    public const bool ENABLE_DEBUG_LOGS = false;
    
    /// <summary>
    /// Conditional debug log - only logs if ENABLE_DEBUG_LOGS is true
    /// </summary>
    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOGS")]
    public static void Log(object message)
    {
        Debug.Log(message);
    }
    
    /// <summary>
    /// Conditional debug log with context - only logs if ENABLE_DEBUG_LOGS is true
    /// </summary>
    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOGS")]
    public static void Log(object message, Object context)
    {
        Debug.Log(message, context);
    }
    
    /// <summary>
    /// Conditional debug warning - only logs if ENABLE_DEBUG_LOGS is true
    /// </summary>
    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOGS")]
    public static void LogWarning(object message)
    {
        Debug.LogWarning(message);
    }
    
    /// <summary>
    /// Conditional debug warning with context - only logs if ENABLE_DEBUG_LOGS is true
    /// </summary>
    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOGS")]
    public static void LogWarning(object message, Object context)
    {
        Debug.LogWarning(message, context);
    }
    
    // Always keep LogError enabled - errors should always be visible
    public static void LogError(object message)
    {
        Debug.LogError(message);
    }
    
    public static void LogError(object message, Object context)
    {
        Debug.LogError(message, context);
    }
}
