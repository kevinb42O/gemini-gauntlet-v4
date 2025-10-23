using UnityEngine;

/// <summary>
/// Diagnostic tool to check HandOverheatVisuals configuration
/// Add this to your player and run in Play mode to see debug info
/// </summary>
public class HandOverheatDiagnostic : MonoBehaviour
{
    [Header("Run Diagnostic")]
    [Tooltip("Check this in Inspector to run diagnostic")]
    public bool runDiagnostic = false;
    
    void Update()
    {
        if (runDiagnostic)
        {
            runDiagnostic = false;
            RunDiagnostic();
        }
    }
    
    [ContextMenu("Run Hand Overheat Diagnostic")]
    public void RunDiagnostic()
    {
        Debug.Log("═══════════════════════════════════════════════════");
        Debug.Log("🔍 HAND OVERHEAT DIAGNOSTIC REPORT");
        Debug.Log("═══════════════════════════════════════════════════");
        
        // Find all HandOverheatVisuals in scene
        HandOverheatVisuals[] allHandVisuals = FindObjectsOfType<HandOverheatVisuals>();
        
        Debug.Log($"\n📊 Found {allHandVisuals.Length} HandOverheatVisuals components in scene:");
        
        if (allHandVisuals.Length == 0)
        {
            Debug.LogError("❌ NO HandOverheatVisuals components found! You need to add them to your hand GameObjects!");
        }
        else if (allHandVisuals.Length == 1)
        {
            Debug.LogWarning("⚠️ Only 1 HandOverheatVisuals found! You need 2 (one for each hand)!");
        }
        
        foreach (var visual in allHandVisuals)
        {
            Debug.Log($"\n─────────────────────────────────────────────────");
            Debug.Log($"GameObject: {visual.gameObject.name}");
            Debug.Log($"Full Path: {GetGameObjectPath(visual.gameObject)}");
            Debug.Log($"isPrimary: {visual.isPrimary} → {(visual.isPrimary ? "LEFT HAND (LMB)" : "RIGHT HAND (RMB)")}");
            Debug.Log($"Component Enabled: {visual.enabled} {(visual.enabled ? "✅" : "❌ DISABLED!")}");
            Debug.Log($"GameObject Active: {visual.gameObject.activeInHierarchy} {(visual.gameObject.activeInHierarchy ? "✅" : "❌ INACTIVE!")}");
            
            // Check validation that happens in Awake
            if (!visual.enabled)
            {
                Debug.LogError($"⚠️ COMPONENT IS DISABLED! This means validation failed in Awake(). Check Console for error messages about missing prefabs or path points!");
            }
        }
        
        // Summary
        Debug.Log($"\n─────────────────────────────────────────────────");
        Debug.Log($"📊 SUMMARY:");
        int primaryCount = 0;
        int secondaryCount = 0;
        int enabledCount = 0;
        
        foreach (var visual in allHandVisuals)
        {
            if (visual.isPrimary) primaryCount++;
            else secondaryCount++;
            if (visual.enabled) enabledCount++;
        }
        
        Debug.Log($"Total components: {allHandVisuals.Length}");
        Debug.Log($"Primary (LEFT) hands: {primaryCount} {(primaryCount == 1 ? "✅" : "❌")}");
        Debug.Log($"Secondary (RIGHT) hands: {secondaryCount} {(secondaryCount == 1 ? "✅" : "❌")}");
        Debug.Log($"Enabled components: {enabledCount}/{allHandVisuals.Length} {(enabledCount == allHandVisuals.Length ? "✅" : "❌")}");
        
        // Check PlayerOverheatManager
        Debug.Log($"\n─────────────────────────────────────────────────");
        Debug.Log("🎮 PLAYER OVERHEAT MANAGER STATUS:");
        
        if (PlayerOverheatManager.Instance != null)
        {
            Debug.Log($"✅ PlayerOverheatManager found");
            Debug.Log($"Primary Hand Visuals: {PlayerOverheatManager.Instance.ActivePrimaryHandVisuals?.gameObject.name ?? "NULL"}");
            Debug.Log($"Secondary Hand Visuals: {PlayerOverheatManager.Instance.ActiveSecondaryHandVisuals?.gameObject.name ?? "NULL"}");
            
            Debug.Log($"\n🔥 CURRENT HEAT VALUES:");
            Debug.Log($"Primary (LEFT) Heat: {PlayerOverheatManager.Instance.CurrentHeatPrimary:F1}/{PlayerOverheatManager.Instance.maxHeat:F1}");
            Debug.Log($"Secondary (RIGHT) Heat: {PlayerOverheatManager.Instance.CurrentHeatSecondary:F1}/{PlayerOverheatManager.Instance.maxHeat:F1}");
        }
        else
        {
            Debug.LogError("❌ PlayerOverheatManager NOT FOUND!");
        }
        
        // Expected configuration
        Debug.Log($"\n─────────────────────────────────────────────────");
        Debug.Log("✅ EXPECTED CONFIGURATION:");
        Debug.Log("LEFT hand GameObject → HandOverheatVisuals → isPrimary = TRUE");
        Debug.Log("RIGHT hand GameObject → HandOverheatVisuals → isPrimary = FALSE");
        
        Debug.Log($"\n⚠️ COMMON ISSUES:");
        Debug.Log("1. Both hands have isPrimary = true (or both false)");
        Debug.Log("2. Left hand component is disabled");
        Debug.Log("3. Left hand GameObject is inactive");
        Debug.Log("4. HandOverheatVisuals not on correct hand GameObject");
        
        Debug.Log("═══════════════════════════════════════════════════");
    }
    
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        
        return path;
    }
}
