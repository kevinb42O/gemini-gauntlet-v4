using UnityEngine;

/// <summary>
/// 🎯 UNIFIED IMPACT SYSTEM - EVENT BROADCASTER
/// 
/// Centralized event system for broadcasting impact events.
/// Uses C# events for lightweight, zero-allocation communication.
/// 
/// Architecture:
/// - FallingDamageSystem (Authority) → Calculates ImpactData → Broadcasts event
/// - AAACameraController (Listener) → Subscribes → Handles trauma/spring/superhero landing
/// - Audio Systems (Listener) → Subscribes → Plays impact sounds
/// - Visual Effects (Listener) → Subscribes → Triggers particle effects
/// 
/// Benefits:
/// - Single source of truth (FallingDamageSystem)
/// - Decoupled systems (listeners don't need references to each other)
/// - Extensible (add new listeners without modifying core)
/// - Performant (C# events, no allocations)
/// - Debuggable (single event to monitor)
/// 
/// Author: Senior Coding Expert
/// Date: 2025-10-16
/// </summary>
public static class ImpactEventBroadcaster
{
    // ========== EVENT DEFINITION ==========
    
    /// <summary>
    /// Fired when player lands and creates an impact.
    /// Subscribe to this event to receive impact notifications.
    /// 
    /// Usage:
    /// void Awake() { ImpactEventBroadcaster.OnImpact += HandleImpact; }
    /// void OnDestroy() { ImpactEventBroadcaster.OnImpact -= HandleImpact; }
    /// void HandleImpact(ImpactData impact) { /* process impact */ }
    /// </summary>
    public static event System.Action<ImpactData> OnImpact;
    
    // ========== DEBUG SETTINGS ==========
    
    /// <summary>
    /// Enable detailed debug logging for impact events
    /// Useful for debugging impact detection and event propagation
    /// </summary>
    public static bool EnableDebugLogging = false;
    
    // ========== BROADCASTING ==========
    
    /// <summary>
    /// Broadcast an impact event to all subscribers.
    /// Called by FallingDamageSystem when a landing is detected.
    /// 
    /// Thread Safety: Must be called from main Unity thread.
    /// Performance: O(n) where n = number of subscribers (typically 3-5)
    /// </summary>
    /// <param name="impact">Impact data containing all metrics and flags</param>
    public static void BroadcastImpact(ImpactData impact)
    {
        // Check if anyone is listening
        if (OnImpact == null)
        {
            if (EnableDebugLogging)
            {
                Debug.LogWarning("[IMPACT SYSTEM] No listeners subscribed to impact events!");
            }
            return;
        }
        
        // Broadcast to all listeners
        OnImpact.Invoke(impact);
        
        // Debug logging
        if (EnableDebugLogging)
        {
            Debug.Log($"[IMPACT SYSTEM] 📢 BROADCAST: {GetImpactDebugString(impact)}");
        }
    }
    
    // ========== UTILITY METHODS ==========
    
    /// <summary>
    /// Get the number of active listeners (for debugging)
    /// </summary>
    public static int GetListenerCount()
    {
        return OnImpact != null ? OnImpact.GetInvocationList().Length : 0;
    }
    
    /// <summary>
    /// Get formatted debug string for impact data
    /// </summary>
    private static string GetImpactDebugString(ImpactData impact)
    {
        string severityIcon = impact.severity switch
        {
            ImpactSeverity.None => "⚪",
            ImpactSeverity.Light => "🟢",
            ImpactSeverity.Moderate => "🟡",
            ImpactSeverity.Severe => "🟠",
            ImpactSeverity.Lethal => "🔴",
            _ => "❓"
        };
        
        return $"{severityIcon} {impact.severity} Impact | " +
               $"Fall: {impact.fallDistance:F0}u | " +
               $"Air: {impact.airTime:F2}s | " +
               $"Speed: {impact.impactSpeed:F0}u/s | " +
               $"Damage: {impact.damageAmount:F0} | " +
               $"Trauma: {impact.traumaIntensity:F2} | " +
               $"Superhero: {(impact.shouldTriggerSuperheroLanding ? "YES 🦸" : "NO")}";
    }
    
    /// <summary>
    /// Clear all event subscribers (useful for cleanup/testing)
    /// WARNING: Use with caution - this will break all active listeners!
    /// </summary>
    public static void ClearAllListeners()
    {
        if (OnImpact != null)
        {
            foreach (System.Delegate d in OnImpact.GetInvocationList())
            {
                OnImpact -= (System.Action<ImpactData>)d;
            }
        }
        
        if (EnableDebugLogging)
        {
            Debug.Log("[IMPACT SYSTEM] All listeners cleared");
        }
    }
    
    /// <summary>
    /// Get list of all listener names (for debugging)
    /// </summary>
    public static string GetListenerNames()
    {
        if (OnImpact == null) return "No listeners";
        
        var listeners = OnImpact.GetInvocationList();
        var names = new System.Collections.Generic.List<string>();
        
        foreach (var listener in listeners)
        {
            string targetName = listener.Target != null ? listener.Target.GetType().Name : "Static";
            string methodName = listener.Method.Name;
            names.Add($"{targetName}.{methodName}");
        }
        
        return string.Join(", ", names);
    }
}
