using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Emergency pause menu fixer - identifies and fixes common pause menu assignment issues
/// </summary>
public class PauseMenuFixer : MonoBehaviour
{
    [Header("Fix Controls")]
    [SerializeField] private bool autoFixOnStart = true;
    
    private UIManager uiManager;

    void Start()
    {
        if (autoFixOnStart)
        {
            FixPauseMenuIssues();
        }
    }

    void Update()
    {
        // F8 - Emergency fix
        if (Input.GetKeyDown(KeyCode.F8))
        {
            FixPauseMenuIssues();
        }
        
        // F9 - List all UI panels
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ListAllUIPanels();
        }
    }

    [ContextMenu("Fix Pause Menu Issues")]
    public void FixPauseMenuIssues()
    {
        Debug.Log("=== EMERGENCY PAUSE MENU FIX ===");
        
        uiManager = UIManager.Instance ?? FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("[PauseMenuFixer] No UIManager found!");
            return;
        }

        // Step 1: Check what's currently assigned
        CheckCurrentAssignment();
        
        // Step 2: Find the correct pause menu panel
        GameObject correctPauseMenu = FindCorrectPauseMenuPanel();
        
        // Step 3: Fix the assignment
        if (correctPauseMenu != null)
        {
            FixPauseMenuAssignment(correctPauseMenu);
        }
        
        // Step 4: Fix game over panel issue
        FixGameOverPanelIssue();
        
        Debug.Log("=== FIX COMPLETE ===");
    }

    private void CheckCurrentAssignment()
    {
        Debug.Log("--- Checking Current Assignment ---");
        
        var pauseMenuField = typeof(UIManager).GetField("pauseMenuPanel", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (pauseMenuField != null)
        {
            GameObject currentAssignment = (GameObject)pauseMenuField.GetValue(uiManager);
            
            if (currentAssignment == null)
            {
                Debug.LogError("[PauseMenuFixer] pauseMenuPanel is NULL!");
            }
            else
            {
                Debug.Log($"[PauseMenuFixer] Currently assigned: {currentAssignment.name}");
                Debug.Log($"[PauseMenuFixer] Path: {GetGameObjectPath(currentAssignment)}");
                
                // Check if it's actually a chest panel
                if (currentAssignment.name.ToLower().Contains("chest") || 
                    currentAssignment.name.ToLower().Contains("inventory"))
                {
                    Debug.LogError("[PauseMenuFixer] ❌ WRONG PANEL! This is a chest/inventory panel, not a pause menu!");
                }
            }
        }
    }

    private GameObject FindCorrectPauseMenuPanel()
    {
        Debug.Log("--- Finding Correct Pause Menu Panel ---");
        
        // Search for GameObjects with pause-related names
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        GameObject bestMatch = null;
        
        foreach (GameObject obj in allObjects)
        {
            string name = obj.name.ToLower();
            
            // Look for pause menu indicators
            if (name.Contains("pause") && name.Contains("menu"))
            {
                Debug.Log($"[PauseMenuFixer] Found pause menu candidate: {obj.name}");
                
                // Check if it has UI components
                if (obj.GetComponent<Canvas>() != null || obj.GetComponentInParent<Canvas>() != null)
                {
                    // Check if it has buttons (good sign for a menu)
                    Button[] buttons = obj.GetComponentsInChildren<Button>();
                    if (buttons.Length > 0)
                    {
                        Debug.Log($"[PauseMenuFixer] ✅ BEST MATCH: {obj.name} (has {buttons.Length} buttons)");
                        bestMatch = obj;
                        break;
                    }
                }
            }
        }
        
        if (bestMatch == null)
        {
            Debug.LogWarning("[PauseMenuFixer] No suitable pause menu found. Creating emergency pause menu...");
            bestMatch = CreateEmergencyPauseMenu();
        }
        
        return bestMatch;
    }

    private GameObject CreateEmergencyPauseMenu()
    {
        Debug.Log("--- Creating Emergency Pause Menu ---");
        
        // Create a new Canvas for the pause menu
        GameObject canvasObj = new GameObject("EmergencyPauseMenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // High priority
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create the pause menu panel
        GameObject pausePanel = new GameObject("EmergencyPauseMenuPanel");
        pausePanel.transform.SetParent(canvasObj.transform, false);
        
        Image panelImage = pausePanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black
        
        RectTransform panelRect = pausePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Add title text
        GameObject titleObj = new GameObject("PauseTitle");
        titleObj.transform.SetParent(pausePanel.transform, false);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "GAME PAUSED";
        titleText.fontSize = 48;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 0.9f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        // Add Resume button
        CreateButton(pausePanel, "ResumeButton", "RESUME", new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.5f), () => {
            if (UIManager.Instance != null) UIManager.Instance.ResumeGame();
        });
        
        // Add Main Menu button
        CreateButton(pausePanel, "MainMenuButton", "MAIN MENU", new Vector2(0.3f, 0.25f), new Vector2(0.7f, 0.35f), () => {
            if (UIManager.Instance != null) UIManager.Instance.ReturnToMainMenu();
        });
        
        // Start with panel inactive
        pausePanel.SetActive(false);
        
        Debug.Log($"[PauseMenuFixer] ✅ Created emergency pause menu: {pausePanel.name}");
        return pausePanel;
    }

    private void CreateButton(GameObject parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform, false);
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => onClick?.Invoke());
        
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        
        // Add button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 24;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void FixPauseMenuAssignment(GameObject correctPauseMenu)
    {
        Debug.Log("--- Fixing Pause Menu Assignment ---");
        
        var pauseMenuField = typeof(UIManager).GetField("pauseMenuPanel", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (pauseMenuField != null)
        {
            pauseMenuField.SetValue(uiManager, correctPauseMenu);
            Debug.Log($"[PauseMenuFixer] ✅ Fixed assignment: {correctPauseMenu.name}");
        }
        else
        {
            Debug.LogError("[PauseMenuFixer] Could not access pauseMenuPanel field!");
        }
    }

    private void FixGameOverPanelIssue()
    {
        Debug.Log("--- Fixing Game Over Panel Issue ---");
        
        var gameOverField = typeof(UIManager).GetField("gameOverPanel", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (gameOverField != null)
        {
            GameObject gameOverPanel = (GameObject)gameOverField.GetValue(uiManager);
            
            if (gameOverPanel != null && gameOverPanel.activeSelf)
            {
                Debug.Log($"[PauseMenuFixer] Game over panel is active: {gameOverPanel.name}");
                Debug.Log("[PauseMenuFixer] Deactivating game over panel to allow pause menu...");
                gameOverPanel.SetActive(false);
            }
            else if (gameOverPanel == null)
            {
                Debug.Log("[PauseMenuFixer] Game over panel is null - this is fine");
            }
            else
            {
                Debug.Log("[PauseMenuFixer] Game over panel exists but is inactive - this is fine");
            }
        }
    }

    [ContextMenu("List All UI Panels")]
    public void ListAllUIPanels()
    {
        Debug.Log("=== ALL UI PANELS IN SCENE ===");
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // Look for objects that might be UI panels
            if (obj.GetComponent<Canvas>() != null || 
                obj.GetComponent<Image>() != null ||
                obj.name.ToLower().Contains("panel") ||
                obj.name.ToLower().Contains("menu") ||
                obj.name.ToLower().Contains("ui"))
            {
                Debug.Log($"[PauseMenuFixer] UI Object: {obj.name} - Active: {obj.activeSelf} - Path: {GetGameObjectPath(obj)}");
                
                // Check for buttons
                Button[] buttons = obj.GetComponentsInChildren<Button>();
                if (buttons.Length > 0)
                {
                    Debug.Log($"[PauseMenuFixer]   └─ Has {buttons.Length} buttons");
                }
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
        GUILayout.BeginArea(new Rect(10, 400, 300, 100));
        GUILayout.Label("Pause Menu Fixer:");
        GUILayout.Label("F8 - Emergency Fix");
        GUILayout.Label("F9 - List All UI Panels");
        
        if (GUILayout.Button("EMERGENCY FIX"))
        {
            FixPauseMenuIssues();
        }
        
        GUILayout.EndArea();
    }
}