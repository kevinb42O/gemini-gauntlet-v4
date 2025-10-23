using UnityEngine;

public class SkyboxManager : MonoBehaviour
{
    [Header("Skybox Persistence")]
    [Tooltip("The currently selected skybox material")]
    public Material selectedSkybox;
    
    [Tooltip("Default skybox to use if none is selected")]
    public Material defaultSkybox;
    
    [Header("Auto-Apply Settings")]
    [Tooltip("Automatically apply the selected skybox when this scene loads")]
    public bool autoApplyOnStart = true;
    
    [Tooltip("Save skybox selection to PlayerPrefs for persistence across game sessions")]
    public bool saveToPlayerPrefs = true;
    
    [Tooltip("PlayerPrefs key for saving skybox selection")]
    public string playerPrefsKey = "SelectedSkyboxIndex";
    
    // Singleton instance
    private static SkyboxManager _instance;
    public static SkyboxManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SkyboxManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SkyboxManager");
                    _instance = go.AddComponent<SkyboxManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    
    
    void Awake()
    {
        // Implement singleton pattern
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            // Destroy duplicate instances - the singleton already has the skybox data
            Destroy(gameObject);
            return;
        }
        
        // Load saved skybox selection
        if (saveToPlayerPrefs)
        {
            LoadSkyboxSelection();
        }
    }
    
    void Start()
    {
        // Delay initialization to ensure MenuSkyboxChanger has initialized first
        StartCoroutine(DelayedInitialization());
    }
    
    private System.Collections.IEnumerator DelayedInitialization()
    {
        // Wait a frame to ensure other components have initialized
        yield return null;
        
        Debug.Log("=== SkyboxManager DelayedInitialization START ===");
        
        // Load saved selection
        if (saveToPlayerPrefs)
        {
            Debug.Log("Loading saved skybox selection...");
            LoadSkyboxSelection();
        }
        
        Debug.Log($"About to apply skybox - Selected: {selectedSkybox?.name ?? "None"}, AutoApply: {autoApplyOnStart}");
        
        if (autoApplyOnStart)
        {
            ApplySelectedSkybox();
        }
        
        Debug.Log("=== SkyboxManager DelayedInitialization END ===");
    }
    
    
    
    /// <summary>
    /// Test loading the saved skybox selection
    /// </summary>
    [ContextMenu("Test Load Saved Skybox")]
    public void TestLoadSavedSkybox()
    {
        Debug.Log("Testing skybox loading...");
        LoadSkyboxSelection();
        ApplySelectedSkybox();
    }
    
    /// <summary>
    /// Show current skybox selection info
    /// </summary>
    [ContextMenu("Show Current Selection")]
    public void ShowCurrentSelection()
    {
        Debug.Log($"Current Selection - Material: {selectedSkybox?.name ?? "None"}");
        Debug.Log($"Current RenderSettings.skybox: {RenderSettings.skybox?.name ?? "None"}");
        
        // Check PlayerPrefs
        if (PlayerPrefs.HasKey(playerPrefsKey))
        {
            string savedName = PlayerPrefs.GetString(playerPrefsKey, "");
            Debug.Log($"PlayerPrefs saved skybox: {savedName}");
        }
        else
        {
            Debug.Log("No PlayerPrefs data found");
        }
    }
    
    /// <summary>
    /// Set the selected skybox material directly
    /// </summary>
    /// <param name="skyboxMaterial">The skybox material to select</param>
    public void SetSelectedSkybox(Material skyboxMaterial)
    {
        if (skyboxMaterial != null)
        {
            selectedSkybox = skyboxMaterial;
            
            // Apply immediately to current scene
            RenderSettings.skybox = selectedSkybox;
            
            // Save to PlayerPrefs if enabled
            if (saveToPlayerPrefs)
            {
                SaveSkyboxSelection();
            }
            
            Debug.Log($"Selected skybox set to: {skyboxMaterial.name}");
        }
        else
        {
            Debug.LogWarning("Attempted to set null skybox material!");
        }
    }
    
    /// <summary>
    /// Apply the currently selected skybox to the scene
    /// </summary>
    public void ApplySelectedSkybox()
    {
        Material skyboxToApply = selectedSkybox;
        
        // Use default if no skybox is selected
        if (skyboxToApply == null)
        {
            skyboxToApply = defaultSkybox;
        }
        
        if (skyboxToApply != null)
        {
            RenderSettings.skybox = skyboxToApply;
            Debug.Log($"Applied skybox: {skyboxToApply.name}");
        }
        else
        {
            Debug.LogWarning("No skybox available to apply!");
        }
    }
    
    /// <summary>
    /// Get the currently selected skybox material
    /// </summary>
    /// <returns>The selected skybox material, or null if none selected</returns>
    public Material GetSelectedSkybox()
    {
        return selectedSkybox;
    }
    
    /// <summary>
    /// Check if a skybox is currently selected
    /// </summary>
    /// <returns>True if a skybox is selected, false otherwise</returns>
    public bool HasSelectedSkybox()
    {
        return selectedSkybox != null;
    }
    
    /// <summary>
    /// Get the name of the currently selected skybox
    /// </summary>
    /// <returns>Name of the selected skybox</returns>
    public string GetSelectedSkyboxName()
    {
        if (selectedSkybox != null)
        {
            return selectedSkybox.name;
        }
        return "None";
    }
    
    /// <summary>
    /// Reset to default skybox
    /// </summary>
    public void ResetToDefault()
    {
        selectedSkybox = defaultSkybox;
        ApplySelectedSkybox();
        
        if (saveToPlayerPrefs)
        {
            SaveSkyboxSelection();
        }
    }
    
    /// <summary>
    /// Save the current skybox selection to PlayerPrefs
    /// </summary>
    private void SaveSkyboxSelection()
    {
        if (selectedSkybox != null)
        {
            PlayerPrefs.SetString(playerPrefsKey, selectedSkybox.name);
            PlayerPrefs.Save();
            Debug.Log($"Saved skybox selection: {selectedSkybox.name}");
        }
        else
        {
            PlayerPrefs.DeleteKey(playerPrefsKey);
            PlayerPrefs.Save();
            Debug.Log("Cleared skybox selection (no skybox selected)");
        }
    }
    
    /// <summary>
    /// Load skybox selection from PlayerPrefs
    /// </summary>
    private void LoadSkyboxSelection()
    {
        Debug.Log($"LoadSkyboxSelection - PlayerPrefs key exists: {PlayerPrefs.HasKey(playerPrefsKey)}");
        
        if (PlayerPrefs.HasKey(playerPrefsKey))
        {
            string savedSkyboxName = PlayerPrefs.GetString(playerPrefsKey, "");
            
            Debug.Log($"Found saved skybox name: {savedSkyboxName}");
            
            if (!string.IsNullOrEmpty(savedSkyboxName))
            {
                // Try to find a material with this name in Resources
                Material foundMaterial = Resources.Load<Material>(savedSkyboxName);
                
                if (foundMaterial != null)
                {
                    selectedSkybox = foundMaterial;
                    Debug.Log($"✓ Successfully loaded skybox from Resources: {savedSkyboxName}");
                }
                else
                {
                    Debug.LogWarning($"✗ Could not find skybox material '{savedSkyboxName}' in Resources folder");
                    selectedSkybox = null;
                }
            }
        }
        else
        {
            Debug.LogWarning($"No saved skybox selection found in PlayerPrefs (key: {playerPrefsKey})");
            selectedSkybox = null;
        }
    }
    
    /// <summary>
    /// Clear saved skybox selection from PlayerPrefs
    /// </summary>
    public void ClearSavedSelection()
    {
        if (PlayerPrefs.HasKey(playerPrefsKey))
        {
            PlayerPrefs.DeleteKey(playerPrefsKey);
            PlayerPrefs.Save();
            Debug.Log("Cleared saved skybox selection");
        }
    }
    
}
