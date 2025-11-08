using UnityEngine;

/// <summary>
/// EMERGENCY FIX: Disables ALL debug visualization systems that are cluttering the scene view
/// Attach to any GameObject and it will find and disable debug rays from:
/// - CleanAAACrouch (wall-slide debug rays)
/// - EmitPointScreenCenter (aim debug rays)
/// - Any other systems with debug visualization
/// 
/// USE: Add to Player GameObject, run game, debug rays gone!
/// </summary>
public class DisableAllDebugVisualization : MonoBehaviour
{
    [Header("🚫 DISABLE DEBUG RAYS")]
    [Tooltip("Run on Awake (immediate) or Start (after initialization)?")]
    public bool runOnAwake = true;
    
    [Tooltip("Show what we disabled in console")]
    public bool showDebugLog = true;
    
    void Awake()
    {
        if (runOnAwake)
        {
            DisableAllDebugSystems();
        }
    }
    
    void Start()
    {
        if (!runOnAwake)
        {
            DisableAllDebugSystems();
        }
    }
    
    void DisableAllDebugSystems()
    {
        int disabledCount = 0;
        
        // 1. Disable CleanAAACrouch wall-slide debug
        var crouchController = FindObjectOfType<CleanAAACrouch>();
        if (crouchController != null)
        {
            // Access private field via reflection
            var wallSlideDebugField = typeof(CleanAAACrouch).GetField("showWallSlideDebug", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var debugVisualizationField = typeof(CleanAAACrouch).GetField("showDebugVisualization", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (wallSlideDebugField != null)
            {
                wallSlideDebugField.SetValue(crouchController, false);
                disabledCount++;
                if (showDebugLog) Debug.Log("✅ Disabled CleanAAACrouch.showWallSlideDebug");
            }
            
            if (debugVisualizationField != null)
            {
                debugVisualizationField.SetValue(crouchController, false);
                disabledCount++;
                if (showDebugLog) Debug.Log("✅ Disabled CleanAAACrouch.showDebugVisualization");
            }
        }
        
        // 2. Disable EmitPointScreenCenter debug rays (both hands)
        var emitPoints = FindObjectsOfType<EmitPointScreenCenter>();
        foreach (var emitPoint in emitPoints)
        {
            var showDebugRayField = typeof(EmitPointScreenCenter).GetField("showDebugRay", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (showDebugRayField != null)
            {
                showDebugRayField.SetValue(emitPoint, false);
                disabledCount++;
                if (showDebugLog) Debug.Log($"✅ Disabled EmitPointScreenCenter.showDebugRay on {emitPoint.gameObject.name}");
            }
        }
        
        // 3. Disable HandFiringMechanics debug logging
        var handMechanics = FindObjectsOfType<HandFiringMechanics>();
        foreach (var hand in handMechanics)
        {
            var enableDebugLoggingField = typeof(HandFiringMechanics).GetField("enableDebugLogging", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (enableDebugLoggingField != null)
            {
                enableDebugLoggingField.SetValue(hand, false);
                disabledCount++;
                if (showDebugLog) Debug.Log($"✅ Disabled HandFiringMechanics.enableDebugLogging on {hand.gameObject.name}");
            }
        }
        
        // 4. Disable CompanionAI debug rays
        var companionBehaviors = FindObjectsOfType<CompanionAI.EnemyCompanionBehavior>();
        foreach (var companion in companionBehaviors)
        {
            var showDebugInfoField = typeof(CompanionAI.EnemyCompanionBehavior).GetField("showDebugInfo", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (showDebugInfoField != null)
            {
                showDebugInfoField.SetValue(companion, false);
                disabledCount++;
                if (showDebugLog) Debug.Log($"✅ Disabled EnemyCompanionBehavior.showDebugInfo on {companion.gameObject.name}");
            }
        }
        
        if (showDebugLog)
        {
            Debug.Log($"<color=green>🎯 DEBUG VISUALIZATION PURGE COMPLETE: {disabledCount} systems disabled</color>");
        }
    }
    
    /// <summary>
    /// Public method to re-run the disable operation (useful for runtime calls)
    /// </summary>
    [ContextMenu("Force Disable All Debug Visualization")]
    public void ForceDisableDebugVisualization()
    {
        DisableAllDebugSystems();
    }
}
