using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 🧠 SMART DEBUG PROFILE - AI OPTIMIZATION CONFIGURATION
/// Defines which scripts to track and what data to capture for AI analysis
/// Add script TYPE NAMES to track them (e.g., "LayeredHandAnimationController")
/// </summary>
[CreateAssetMenu(fileName = "SmartDebugProfile", menuName = "Cascade Debug/Smart Debug Profile", order = 1)]
public class SmartDebugProfile : ScriptableObject
{
    [Header("🎯 TARGET SCRIPTS")]
    [Tooltip("Add script type names to track (e.g., 'LayeredHandAnimationController')")]
    public List<string> targetScriptNames = new List<string>();
    
    [Header("📊 TRACKING CONFIGURATION")]
    [Tooltip("Track all public fields")]
    public bool trackPublicFields = true;
    
    [Tooltip("Track serialized private fields")]
    public bool trackSerializedFields = true;
    
    [Tooltip("Track public properties")]
    public bool trackProperties = true;
    
    [Tooltip("Track method calls (expensive)")]
    public bool trackMethodCalls = false;
    
    [Header("⏱️ SAMPLING CONFIGURATION")]
    [Tooltip("How often to sample values (seconds)")]
    public float samplingInterval = 0.5f;
    
    [Tooltip("Only log when values change")]
    public bool logOnlyOnChange = true;
    
    [Tooltip("Track min/max/average values over time")]
    public bool trackStatistics = true;
    
    [Header("🎨 OUTPUT CONFIGURATION")]
    [Tooltip("Create separate file for each script")]
    public bool separateFilePerScript = true;
    
    [Tooltip("Include timestamp in filenames")]
    public bool includeTimestamp = true;
    
    [Tooltip("Export to JSON format (easier for AI parsing)")]
    public bool exportAsJSON = false;
    
    [Header("🔍 ADVANCED TRACKING")]
    [Tooltip("Track component references and their states")]
    public bool trackComponentReferences = true;
    
    [Tooltip("Track GameObject hierarchy context")]
    public bool trackHierarchyContext = true;
    
    [Tooltip("Track performance metrics (execution time, memory)")]
    public bool trackPerformanceMetrics = true;
    
    [Header("🎯 SMART FILTERING")]
    [Tooltip("Specific field names to track (empty = all)")]
    public List<string> specificFieldNames = new List<string>();
    
    [Tooltip("Field names to ignore")]
    public List<string> ignoreFieldNames = new List<string>();
    
    [Tooltip("Track fields with these attributes")]
    public List<string> trackAttributeTypes = new List<string>() 
    { 
        "SerializeField", 
        "Range", 
        "Header",
        "Tooltip"
    };

    /// <summary>
    /// Get all script type names being tracked
    /// </summary>
    public List<string> GetTrackedScriptNames()
    {
        return new List<string>(targetScriptNames);
    }

    /// <summary>
    /// Add a script type to track
    /// </summary>
    public void AddScriptType(string typeName)
    {
        if (!targetScriptNames.Contains(typeName))
        {
            targetScriptNames.Add(typeName);
        }
    }

    /// <summary>
    /// Check if a field should be tracked based on filters
    /// </summary>
    public bool ShouldTrackField(string fieldName)
    {
        // If specific fields defined, only track those
        if (specificFieldNames.Count > 0)
        {
            return specificFieldNames.Contains(fieldName);
        }
        
        // Check ignore list
        if (ignoreFieldNames.Contains(fieldName))
        {
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// Validate profile configuration
    /// </summary>
    public bool IsValid()
    {
        if (targetScriptNames == null || targetScriptNames.Count == 0)
        {
            Debug.LogWarning("[SmartDebugProfile] No target scripts assigned!");
            return false;
        }
        
        return true;
    }
}
