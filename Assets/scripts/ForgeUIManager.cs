using UnityEngine;

/// <summary>
/// ForgeUIManager - Manages FORGE UI visibility in game context
/// Simple singleton that shows/hides the existing FORGE Canvas
/// </summary>
public class ForgeUIManager : MonoBehaviour
{
    public static ForgeUIManager Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private GameObject forgeUIPanel; // Assign your existing Forge Canvas in Inspector
    
    private bool isForgeUIVisible = false;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ForgeUIManager] Duplicate instance detected - destroying");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Ensure UI starts hidden
        if (forgeUIPanel != null)
        {
            forgeUIPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show the FORGE UI and set game context
    /// </summary>
    public void ShowForgeUI()
    {
        if (forgeUIPanel == null)
        {
            Debug.LogError("[ForgeUIManager] forgeUIPanel not assigned! Please assign the FORGE Canvas in Inspector.");
            return;
        }
        
        Debug.Log("[ForgeUIManager] Showing FORGE UI - setting Game context");
        
        // Enable the existing Forge UI Canvas
        forgeUIPanel.SetActive(true);
        isForgeUIVisible = true;
        
        // Notify ForgeManager it's in game context
        if (ForgeManager.Instance != null)
        {
            ForgeManager.Instance.SetContext(ForgeContext.Game);
        }
        else
        {
            Debug.LogWarning("[ForgeUIManager] ForgeManager.Instance not found!");
        }
    }
    
    /// <summary>
    /// Hide the FORGE UI and reset to menu context
    /// </summary>
    public void HideForgeUI()
    {
        if (forgeUIPanel == null) return;
        
        Debug.Log("[ForgeUIManager] Hiding FORGE UI - resetting to Menu context");
        
        // Disable the Forge UI Canvas
        forgeUIPanel.SetActive(false);
        isForgeUIVisible = false;
        
        // Reset ForgeManager to menu context
        if (ForgeManager.Instance != null)
        {
            ForgeManager.Instance.SetContext(ForgeContext.Menu);
        }
    }
    
    /// <summary>
    /// Check if FORGE UI is currently visible
    /// </summary>
    public bool IsUIVisible()
    {
        return isForgeUIVisible;
    }
    
    void OnDestroy()
    {
        // Cleanup singleton reference
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
