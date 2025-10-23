using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Diagnostic script to help identify UIManager pause menu issues.
/// Attach this to any GameObject in your scene and run the diagnostics.
/// </summary>
public class UIManagerDiagnostic : MonoBehaviour
{
    [Header("Diagnostic Controls")]
    [SerializeField] private bool runDiagnosticsOnStart = true;
    [SerializeField] private bool enableDebugKeys = true;
    
    private UIManager uiManager;

    void Start()
    {
        if (runDiagnosticsOnStart)
        {
            RunFullDiagnostic();
        }
    }

    void Update()
    {
        if (enableDebugKeys)
        {
            // F1 - Run full diagnostic
            if (Input.GetKeyDown(KeyCode.F1))
            {
                RunFullDiagnostic();
            }
            
            // F2 - Test pause menu toggle
            if (Input.GetKeyDown(KeyCode.F2))
            {
                TestPauseMenuToggle();
            }
            
            // F3 - Force show pause menu
            if (Input.GetKeyDown(KeyCode.F3))
            {
                ForceShowPauseMenu();
            }
            
            // F4 - Check pause menu hierarchy
            if (Input.GetKeyDown(KeyCode.F4))
            {
                CheckPauseMenuHierarchy();
            }
        }
    }

    [ContextMenu("Run Full Diagnostic")]
    public void RunFullDiagnostic()
    {
        Debug.Log("=== UIManager Pause Menu Diagnostic ===");
        
        // Find UIManager
        uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("[Diagnostic] UIManager not found in scene!");
            return;
        }
        
        Debug.Log($"[Diagnostic] UIManager found: {uiManager.name}");
        
        // Check pause menu panel reference
        CheckPauseMenuReference();
        
        // Check Canvas and Canvas Group settings
        CheckCanvasSettings();
        
        // Check for conflicting UI elements
        CheckForConflicts();
        
        // Check Input System
        CheckInputSystem();
        
        // Check Time Scale
        CheckTimeScale();
        
        Debug.Log("=== Diagnostic Complete ===");
    }

    private void CheckPauseMenuReference()
    {
        Debug.Log("--- Checking Pause Menu Reference ---");
        
        // Use reflection to access private pauseMenuPanel field
        var pauseMenuField = typeof(UIManager).GetField("pauseMenuPanel", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (pauseMenuField != null)
        {
            GameObject pauseMenuPanel = (GameObject)pauseMenuField.GetValue(uiManager);
            
            if (pauseMenuPanel == null)
            {
                Debug.LogError("[Diagnostic] pauseMenuPanel is NULL! Assign it in the UIManager inspector.");
            }
            else
            {
                Debug.Log($"[Diagnostic] pauseMenuPanel assigned: {pauseMenuPanel.name}");
                Debug.Log($"[Diagnostic] pauseMenuPanel active in hierarchy: {pauseMenuPanel.activeInHierarchy}");
                Debug.Log($"[Diagnostic] pauseMenuPanel active self: {pauseMenuPanel.activeSelf}");
                
                // Check if it has a Canvas Group
                CanvasGroup canvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    Debug.Log($"[Diagnostic] CanvasGroup found - Alpha: {canvasGroup.alpha}, Interactable: {canvasGroup.interactable}, BlocksRaycasts: {canvasGroup.blocksRaycasts}");
                }
                else
                {
                    Debug.Log("[Diagnostic] No CanvasGroup found on pause menu panel");
                }
            }
        }
        else
        {
            Debug.LogError("[Diagnostic] Could not access pauseMenuPanel field via reflection");
        }
    }

    private void CheckCanvasSettings()
    {
        Debug.Log("--- Checking Canvas Settings ---");
        
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"[Diagnostic] Found {canvases.Length} Canvas objects in scene");
        
        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"[Diagnostic] Canvas: {canvas.name} - Render Mode: {canvas.renderMode}, Sort Order: {canvas.sortingOrder}, Enabled: {canvas.enabled}");
            
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Debug.Log($"[Diagnostic] Canvas {canvas.name} is Screen Space Overlay (good for UI)");
            }
        }
    }

    private void CheckForConflicts()
    {
        Debug.Log("--- Checking for Conflicts ---");
        
        // Check for multiple UIManagers
        UIManager[] uiManagers = FindObjectsOfType<UIManager>();
        if (uiManagers.Length > 1)
        {
            Debug.LogWarning($"[Diagnostic] Found {uiManagers.Length} UIManager instances! This could cause conflicts.");
            foreach (var manager in uiManagers)
            {
                Debug.LogWarning($"[Diagnostic] UIManager: {manager.name} on GameObject: {manager.gameObject.name}");
            }
        }
        
        // Check for EventSystem
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("[Diagnostic] No EventSystem found! UI interactions won't work. Add one to your scene.");
        }
        else
        {
            Debug.Log($"[Diagnostic] EventSystem found: {eventSystem.name}");
        }
    }

    private void CheckInputSystem()
    {
        Debug.Log("--- Checking Input System ---");
        
        Debug.Log($"[Diagnostic] Pause key (Escape) currently pressed: {Input.GetKey(Controls.Pause)}");
        Debug.Log($"[Diagnostic] Current Time.timeScale: {Time.timeScale}");
        Debug.Log($"[Diagnostic] Cursor lock state: {Cursor.lockState}");
        Debug.Log($"[Diagnostic] Cursor visible: {Cursor.visible}");
    }

    private void CheckTimeScale()
    {
        Debug.Log("--- Checking Time Scale ---");
        
        if (Time.timeScale == 0f)
        {
            Debug.LogWarning("[Diagnostic] Time.timeScale is 0! Game is paused. This might be expected if pause menu should be showing.");
        }
        else
        {
            Debug.Log($"[Diagnostic] Time.timeScale is normal: {Time.timeScale}");
        }
    }

    [ContextMenu("Test Pause Menu Toggle")]
    public void TestPauseMenuToggle()
    {
        Debug.Log("--- Testing Pause Menu Toggle ---");
        
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }
        
        if (uiManager != null)
        {
            // Try to call TogglePauseMenu via reflection
            var toggleMethod = typeof(UIManager).GetMethod("TogglePauseMenu", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (toggleMethod != null)
            {
                Debug.Log("[Diagnostic] Calling TogglePauseMenu()...");
                toggleMethod.Invoke(uiManager, null);
            }
            else
            {
                Debug.LogError("[Diagnostic] TogglePauseMenu method not found!");
            }
        }
        else
        {
            Debug.LogError("[Diagnostic] UIManager not found!");
        }
    }

    [ContextMenu("Force Show Pause Menu")]
    public void ForceShowPauseMenu()
    {
        Debug.Log("--- Force Showing Pause Menu ---");
        
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }
        
        if (uiManager != null)
        {
            // Try to call ForceShowPauseMenu via reflection
            var forceMethod = typeof(UIManager).GetMethod("ForceShowPauseMenu", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (forceMethod != null)
            {
                Debug.Log("[Diagnostic] Calling ForceShowPauseMenu()...");
                forceMethod.Invoke(uiManager, null);
            }
            else
            {
                Debug.LogError("[Diagnostic] ForceShowPauseMenu method not found!");
            }
        }
        else
        {
            Debug.LogError("[Diagnostic] UIManager not found!");
        }
    }

    [ContextMenu("Check Pause Menu Hierarchy")]
    public void CheckPauseMenuHierarchy()
    {
        Debug.Log("--- Checking Pause Menu Hierarchy ---");
        
        // Find all GameObjects with "pause" in the name
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("pause"))
            {
                Debug.Log($"[Diagnostic] Found pause-related object: {obj.name} - Active: {obj.activeSelf} - Path: {GetGameObjectPath(obj)}");
                
                // Check for UI components
                Canvas canvas = obj.GetComponent<Canvas>();
                CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
                Button[] buttons = obj.GetComponentsInChildren<Button>();
                
                if (canvas != null)
                    Debug.Log($"[Diagnostic]   Has Canvas component");
                if (canvasGroup != null)
                    Debug.Log($"[Diagnostic]   Has CanvasGroup - Alpha: {canvasGroup.alpha}");
                if (buttons.Length > 0)
                    Debug.Log($"[Diagnostic]   Has {buttons.Length} buttons");
            }
        }
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }

    void OnGUI()
    {
        if (enableDebugKeys)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("UIManager Diagnostic Controls:");
            GUILayout.Label("F1 - Run Full Diagnostic");
            GUILayout.Label("F2 - Test Pause Toggle");
            GUILayout.Label("F3 - Force Show Pause Menu");
            GUILayout.Label("F4 - Check Pause Menu Hierarchy");
            GUILayout.EndArea();
        }
    }
}