using UnityEngine;

/// <summary>
/// Auto-fixes HandOverheatVisuals isPrimary field to match refactored hand system
/// NEW MAPPING: Primary (isPrimary=true) = LEFT hand (LMB)
///              Secondary (isPrimary=false) = RIGHT hand (RMB)
/// Attach to player and run once
/// </summary>
public class HandOverheatVisualsAutoFix : MonoBehaviour
{
    [ContextMenu("Fix Hand Overheat Mapping")]
    void Start()
    {
        FixHandOverheatMapping();
    }
    
    private void FixHandOverheatMapping()
    {
        // Find all HandOverheatVisuals in scene
        HandOverheatVisuals[] allOverheatVisuals = FindObjectsOfType<HandOverheatVisuals>();
        
        Debug.Log($"[HandOverheatVisualsAutoFix] Found {allOverheatVisuals.Length} HandOverheatVisuals components");
        
        foreach (var visual in allOverheatVisuals)
        {
            if (visual == null) continue;
            
            // Check if this is a player hand (not companion/enemy)
            if (!IsPlayerHand(visual.transform))
                continue;
            
            // Determine if this is left or right hand by checking parent hierarchy
            bool isLeftHand = IsLeftHand(visual.transform);
            
            // NEW MAPPING: Primary = LEFT hand, Secondary = RIGHT hand
            bool correctIsPrimaryValue = isLeftHand; // LEFT hand should be isPrimary=true
            
            if (visual.isPrimary != correctIsPrimaryValue)
            {
                Debug.LogWarning($"[HandOverheatVisualsAutoFix] FIXING {visual.gameObject.name}: " +
                    $"Was isPrimary={visual.isPrimary}, should be isPrimary={correctIsPrimaryValue} " +
                    $"({(isLeftHand ? "LEFT" : "RIGHT")} hand)", visual);
                
                visual.isPrimary = correctIsPrimaryValue;
            }
            else
            {
                Debug.Log($"[HandOverheatVisualsAutoFix] OK: {visual.gameObject.name} correctly set to isPrimary={visual.isPrimary} " +
                    $"({(isLeftHand ? "LEFT" : "RIGHT")} hand)", visual);
            }
        }
    }
    
    private bool IsPlayerHand(Transform transform)
    {
        // Check if it's under a Player GameObject
        Transform current = transform;
        while (current != null)
        {
            if (current.name.ToLower().Contains("player") && !current.name.ToLower().Contains("enemy"))
                return true;
            current = current.parent;
        }
        return false;
    }
    
    private bool IsLeftHand(Transform transform)
    {
        // Check transform hierarchy for "left" keyword
        Transform current = transform;
        while (current != null)
        {
            string name = current.name.ToLower();
            
            // Check for left indicators
            if (name.Contains("left") || name.Contains("_l_") || name.Contains("_l1") || 
                name.Contains("lhand") || name.Contains("lefthand"))
            {
                return true;
            }
            
            // Check for right indicators (to return false)
            if (name.Contains("right") || name.Contains("_r_") || name.Contains("_r1") || 
                name.Contains("rhand") || name.Contains("righthand"))
            {
                return false;
            }
            
            current = current.parent;
        }
        
        // Fallback: check position relative to player center
        // This is a last resort if naming doesn't help
        Transform playerRoot = FindPlayerRoot(transform);
        if (playerRoot != null)
        {
            // In local space, left hand should have negative X
            Vector3 localPos = playerRoot.InverseTransformPoint(transform.position);
            return localPos.x < 0;
        }
        
        Debug.LogWarning($"[HandOverheatVisualsAutoFix] Could not determine if {transform.name} is left or right hand!", transform);
        return false; // Default to right if can't determine
    }
    
    private Transform FindPlayerRoot(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name.ToLower().Contains("player") && current.parent == null)
                return current;
            current = current.parent;
        }
        return null;
    }
}
