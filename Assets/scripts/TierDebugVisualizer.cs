using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class TierDebugVisualizer : MonoBehaviour
{
    public bool showTierVisualization = true;
    public bool showTowerStatus = true;
    public float innerTierColor = 0.2f; // Hue value (0-1) for inner tier
    public float middleTierColor = 0.5f; // Hue value (0-1) for middle tier
    public float outerTierColor = 0.8f; // Hue value (0-1) for outer tier
    
    private PlatformEncounterManager _platformManager;
    private Dictionary<Transform, Color> _platformColors = new Dictionary<Transform, Color>();
    
    void OnEnable()
    {
        _platformManager = FindAnyObjectByType<PlatformEncounterManager>();
        if (_platformManager == null)
        {
            Debug.LogWarning("TierDebugVisualizer: No PlatformEncounterManager found in scene.");
            return;
        }
    }
    
    void Update()
    {
        if (!showTierVisualization && !showTowerStatus) return;
        
        // Refresh platform colors if needed
        if (_platformManager != null && _platformColors.Count == 0)
        {
            RefreshPlatformColors();
        }
    }
    
    void RefreshPlatformColors()
    {
        _platformColors.Clear();
        
        if (_platformManager.InnerTierPlatforms != null)
        {
            foreach (var platform in _platformManager.InnerTierPlatforms)
            {
                if (platform != null)
                {
                    _platformColors[platform] = Color.HSVToRGB(innerTierColor, 0.7f, 0.8f);
                }
            }
        }
        
        if (_platformManager.MiddleTierPlatforms != null)
        {
            foreach (var platform in _platformManager.MiddleTierPlatforms)
            {
                if (platform != null)
                {
                    _platformColors[platform] = Color.HSVToRGB(middleTierColor, 0.7f, 0.8f);
                }
            }
        }
        
        if (_platformManager.OuterTierPlatforms != null)
        {
            foreach (var platform in _platformManager.OuterTierPlatforms)
            {
                if (platform != null)
                {
                }
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showTierVisualization && !showTowerStatus) return;
        if (_platformManager == null) return;
        
        // Draw platform tier visualization
        if (showTierVisualization)
        {
            foreach (var kvp in _platformColors)
            {
                Transform platform = kvp.Key;
                Color color = kvp.Value;
                
                if (platform != null)
                {
                    Gizmos.color = color;
                    Gizmos.DrawWireSphere(platform.position, 1.5f);
                    
                    // Draw small line upward to indicate platform
                    Gizmos.DrawLine(platform.position, platform.position + Vector3.up * 2f);
                }
            }
        }
        
        // Draw tower status visualization
        if (showTowerStatus)
        {
            foreach (var tower in TowerController.ActiveTowers)
            {
                if (tower != null && tower._associatedPlatformTransform != null)
                {
                    // Draw line from tower to platform
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(tower.transform.position, tower._associatedPlatformTransform.position);
                    
                    // Show skull attack status
                    if (tower.SkullsAttackEnabled)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(tower.transform.position, 1.0f);
                    }
                    else
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(tower.transform.position, 0.8f);
                    }
                }
            }
        }
    }
}
