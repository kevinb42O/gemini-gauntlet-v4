using UnityEngine;

/// <summary>
/// Manages the centralized input system initialization
/// Place this on a GameObject in your scene to initialize Controls with InputSettings
/// </summary>
[DefaultExecutionOrder(-100)] // Execute early to ensure Controls are set before other scripts use them
public class InputManager : MonoBehaviour
{
    [Header("Input Configuration")]
    [Tooltip("InputSettings asset containing all key bindings")]
    [SerializeField] private InputSettings inputSettings;
    
    [Header("Auto-Load Settings")]
    [Tooltip("Automatically try to load InputSettings from Resources folder if not assigned")]
    [SerializeField] private bool autoLoadFromResources = true;
    
    void Awake()
    {
        // Try to load InputSettings if not assigned
        if (inputSettings == null && autoLoadFromResources)
        {
            inputSettings = Resources.Load<InputSettings>("InputSettings");
            if (inputSettings != null)
            {
                Debug.Log("[INPUT MANAGER] Auto-loaded InputSettings from Resources folder");
            }
        }
        
        // Initialize Controls with settings
        if (inputSettings != null)
        {
            inputSettings.InitializeControls();
            Debug.Log("[INPUT MANAGER] Controls initialized with InputSettings");
        }
        else
        {
            Debug.LogWarning("[INPUT MANAGER] No InputSettings assigned! Using default key bindings. " +
                           "Create an InputSettings asset and assign it, or place it in Resources folder.");
        }
    }
    
    void OnValidate()
    {
        // Update controls when settings change in Inspector during play mode
        if (Application.isPlaying && inputSettings != null)
        {
            inputSettings.InitializeControls();
        }
    }
}
