using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// Debug script to test chest panel visibility directly.
/// Attach to any GameObject in the scene for testing.
/// </summary>
public class ChestPanelDebugger : MonoBehaviour
{
    [Header("References")]
    public GameObject chestInventoryPanel;
    public ChestInventoryPanelController panelController;
    
    [Header("Debug Controls")]
    [Tooltip("Key to toggle panel visibility")]
    public KeyCode toggleKey = KeyCode.F1;
    
    [Tooltip("Key to force panel visible")]
    public KeyCode showKey = KeyCode.F2;
    
    [Tooltip("Key to force panel hidden")]
    public KeyCode hideKey = KeyCode.F3;
    
    [Tooltip("Enable debug logs")]
    public bool enableLogs = true;
    
    private void Start()
    {
        // Try to find panel if not assigned
        if (chestInventoryPanel == null)
        {
            chestInventoryPanel = GameObject.Find("ChestInventoryPanel");
            if (chestInventoryPanel != null && enableLogs)
            {
                Debug.Log($"ChestPanelDebugger: Found panel automatically: {chestInventoryPanel.name}");
            }
        }
        
        // Try to get controller if not assigned
        if (panelController == null && chestInventoryPanel != null)
        {
            panelController = chestInventoryPanel.GetComponent<ChestInventoryPanelController>();
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ForceShowPanel();
        }
    }
    
    [ContextMenu("Force Show Panel")]
    void ForceShowPanel()
    {
        if (chestInventoryPanel == null) return;
        
        // Step 1: Activate the GameObject
        chestInventoryPanel.SetActive(true);
        Debug.Log($"Step 1 - Panel active after SetActive(true): {chestInventoryPanel.activeSelf}");
        
        // Step 2: Check and fix CanvasGroup
        CanvasGroup cg = chestInventoryPanel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            Debug.Log($"CanvasGroup found - Alpha before: {cg.alpha}, Interactable: {cg.interactable}");
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
            Debug.Log($"CanvasGroup fixed - Alpha after: {cg.alpha}");
        }
        
        // Step 3: Check parent hierarchy
        Transform parent = chestInventoryPanel.transform.parent;
        while (parent != null)
        {
            Debug.Log($"Parent: {parent.name}, Active: {parent.gameObject.activeSelf}");
            if (!parent.gameObject.activeSelf)
            {
                Debug.LogWarning($"PARENT {parent.name} IS INACTIVE! This might be the problem!");
            }
            parent = parent.parent;
        }
        
        Debug.Log($"Final state - Panel active: {chestInventoryPanel.activeSelf}, activeInHierarchy: {chestInventoryPanel.activeInHierarchy}");
    }
    
    [ContextMenu("Find and Show Panel")]
    void FindAndShowPanel()
    {
        Debug.Log("=== FIND AND SHOW PANEL DEBUG ===");
        
        // Try to find the panel by name
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("chest") && obj.name.ToLower().Contains("inventory"))
            {
                Debug.Log($"Found potential chest panel: {obj.name}, Active: {obj.activeSelf}");
                obj.SetActive(true);
                
                CanvasGroup cg = obj.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
        }
    }
    
    [ContextMenu("Brute Force Show All Panels")]
    void BruteForceShowAllPanels()
    {
        Debug.Log("=== BRUTE FORCE SHOW ALL PANELS ===");
        
        // Find ALL GameObjects with "panel" in the name
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("panel"))
            {
                Debug.Log($"Found panel: {obj.name}, Making it visible...");
                obj.SetActive(true);
                
                CanvasGroup cg = obj.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
        }
    }
}
