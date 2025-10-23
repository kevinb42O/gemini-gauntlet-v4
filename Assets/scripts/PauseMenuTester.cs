using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple test script to verify pause menu functionality.
/// Attach this to any GameObject in your scene for quick testing.
/// </summary>
public class PauseMenuTester : MonoBehaviour
{
    [Header("Test Controls")]
    [SerializeField] private bool enableTestKeys = true;
    [SerializeField] private KeyCode testToggleKey = KeyCode.F5;
    [SerializeField] private KeyCode forceShowKey = KeyCode.F6;
    [SerializeField] private KeyCode inspectKey = KeyCode.F7;

    private UIManager uiManager;

    void Start()
    {
        uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }

        if (uiManager == null)
        {
            Debug.LogError("[PauseMenuTester] No UIManager found in scene!");
        }
        else
        {
            Debug.Log("[PauseMenuTester] UIManager found - ready for testing");
        }
    }

    void Update()
    {
        if (!enableTestKeys) return;

        // F5 - Test normal pause toggle
        if (Input.GetKeyDown(testToggleKey))
        {
            TestPauseToggle();
        }

        // F6 - Force show pause menu
        if (Input.GetKeyDown(forceShowKey))
        {
            ForceShowPauseMenu();
        }

        // F7 - Inspect pause menu state
        if (Input.GetKeyDown(inspectKey))
        {
            InspectPauseMenuState();
        }
    }

    [ContextMenu("Test Pause Toggle")]
    public void TestPauseToggle()
    {
        Debug.Log("=== Testing Pause Toggle ===");
        
        if (uiManager == null)
        {
            Debug.LogError("[PauseMenuTester] UIManager is null!");
            return;
        }

        // Call the toggle method
        uiManager.TogglePauseMenu();
        
        Debug.Log("[PauseMenuTester] TogglePauseMenu() called");
    }

    [ContextMenu("Force Show Pause Menu")]
    public void ForceShowPauseMenu()
    {
        Debug.Log("=== Force Showing Pause Menu ===");
        
        if (uiManager == null)
        {
            Debug.LogError("[PauseMenuTester] UIManager is null!");
            return;
        }

        // Use reflection to call the force show method
        var forceMethod = typeof(UIManager).GetMethod("ForceShowPauseMenu");
        if (forceMethod != null)
        {
            forceMethod.Invoke(uiManager, null);
            Debug.Log("[PauseMenuTester] ForceShowPauseMenu() called via reflection");
        }
        else
        {
            Debug.LogError("[PauseMenuTester] ForceShowPauseMenu method not found!");
        }
    }

    [ContextMenu("Inspect Pause Menu State")]
    public void InspectPauseMenuState()
    {
        Debug.Log("=== Inspecting Pause Menu State ===");
        
        if (uiManager == null)
        {
            Debug.LogError("[PauseMenuTester] UIManager is null!");
            return;
        }

        // Use reflection to access the pause menu panel
        var pauseMenuField = typeof(UIManager).GetField("pauseMenuPanel", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (pauseMenuField != null)
        {
            GameObject pauseMenuPanel = (GameObject)pauseMenuField.GetValue(uiManager);
            
            if (pauseMenuPanel == null)
            {
                Debug.LogError("[PauseMenuTester] pauseMenuPanel is NULL! This is the problem!");
                Debug.LogError("[PauseMenuTester] Go to UIManager in inspector and assign the pause menu panel!");
            }
            else
            {
                Debug.Log($"[PauseMenuTester] pauseMenuPanel found: {pauseMenuPanel.name}");
                Debug.Log($"[PauseMenuTester] Panel active in hierarchy: {pauseMenuPanel.activeInHierarchy}");
                Debug.Log($"[PauseMenuTester] Panel active self: {pauseMenuPanel.activeSelf}");
                
                // Check Canvas Group
                CanvasGroup canvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    Debug.Log($"[PauseMenuTester] CanvasGroup - Alpha: {canvasGroup.alpha}, Interactable: {canvasGroup.interactable}, BlocksRaycasts: {canvasGroup.blocksRaycasts}");
                }
                else
                {
                    Debug.Log("[PauseMenuTester] No CanvasGroup found on pause menu panel");
                }

                // Check for buttons
                Button[] buttons = pauseMenuPanel.GetComponentsInChildren<Button>();
                Debug.Log($"[PauseMenuTester] Found {buttons.Length} buttons in pause menu");

                // Check Canvas
                Canvas canvas = pauseMenuPanel.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Debug.Log($"[PauseMenuTester] Canvas - Render Mode: {canvas.renderMode}, Sort Order: {canvas.sortingOrder}, Enabled: {canvas.enabled}");
                }
            }
        }
        else
        {
            Debug.LogError("[PauseMenuTester] Could not access pauseMenuPanel field via reflection");
        }

        // Check game state
        Debug.Log($"[PauseMenuTester] Time.timeScale: {Time.timeScale}");
        Debug.Log($"[PauseMenuTester] Cursor.lockState: {Cursor.lockState}");
        Debug.Log($"[PauseMenuTester] Cursor.visible: {Cursor.visible}");
    }

    void OnGUI()
    {
        if (enableTestKeys)
        {
            GUILayout.BeginArea(new Rect(10, 250, 300, 150));
            GUILayout.Label("Pause Menu Tester:");
            GUILayout.Label($"{testToggleKey} - Test Pause Toggle");
            GUILayout.Label($"{forceShowKey} - Force Show Pause Menu");
            GUILayout.Label($"{inspectKey} - Inspect Pause Menu State");
            
            if (GUILayout.Button("Test Pause Toggle"))
            {
                TestPauseToggle();
            }
            
            if (GUILayout.Button("Force Show Pause Menu"))
            {
                ForceShowPauseMenu();
            }
            
            if (GUILayout.Button("Inspect State"))
            {
                InspectPauseMenuState();
            }
            
            GUILayout.EndArea();
        }
    }
}