using UnityEngine;
using GeminiGauntlet.Progression;
using GeminiGauntlet.UI;

/// <summary>
/// Simple tester to force XP Summary to show (bypass exit zone)
/// </summary>
public class XPSummaryTester : MonoBehaviour
{
    [Header("Test Controls")]
    public KeyCode testXPSummaryKey = KeyCode.T;
    public KeyCode forceGrantXPKey = KeyCode.G;
    
    void Update()
    {
        if (Input.GetKeyDown(testXPSummaryKey))
        {
            TestXPSummary();
        }
        
        if (Input.GetKeyDown(forceGrantXPKey))
        {
            ForceGrantXP();
        }
    }
    
    [ContextMenu("Test XP Summary")]
    public void TestXPSummary()
    {
        Debug.Log("🧪 TESTING XP SUMMARY DIRECTLY");
        
        // Check XP Manager
        if (XPManager.Instance == null)
        {
            Debug.LogError("❌ XPManager.Instance is null!");
            return;
        }
        
        var summaryData = XPManager.Instance.GetXPSummaryData();
        Debug.Log($"🔍 Session XP: {summaryData.sessionTotalXP}");
        
        if (summaryData.sessionTotalXP <= 0)
        {
            Debug.LogWarning("⚠️ No XP to show! Grant some XP first (press G)");
            return;
        }
        
        // Find XP Summary UI (include inactive objects)
        var xpSummaryUI = FindObjectOfType<XPSummaryUI>(true);
        if (xpSummaryUI == null)
        {
            Debug.LogError("❌ XPSummaryUI not found in scene!");
            ListAllXPComponents();
            return;
        }
        
        Debug.Log($"✅ Found XPSummaryUI: {xpSummaryUI.name}");
        
        // Check if the summary panel exists
        var summaryPanelField = typeof(XPSummaryUI).GetField("summaryPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var summaryPanel = summaryPanelField?.GetValue(xpSummaryUI) as GameObject;
        
        if (summaryPanel == null)
        {
            Debug.LogError("❌ Summary panel is null! UI not set up properly!");
            return;
        }
        
        Debug.Log($"✅ Summary panel found: {summaryPanel.name}");
        Debug.Log($"🔍 Panel active BEFORE: {summaryPanel.activeSelf}");
        
        // Check Canvas setup
        Canvas canvas = summaryPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ No Canvas found in parent hierarchy!");
        }
        else
        {
            Debug.Log($"✅ Canvas found: {canvas.name}");
            Debug.Log($"🔍 Canvas enabled: {canvas.enabled}");
            Debug.Log($"🔍 Canvas gameObject active: {canvas.gameObject.activeSelf}");
            Debug.Log($"🔍 Canvas render mode: {canvas.renderMode}");
            Debug.Log($"🔍 Canvas sort order: {canvas.sortingOrder}");
        }
        
        // Check RectTransform
        RectTransform rectTransform = summaryPanel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Debug.Log($"🔍 Panel size: {rectTransform.sizeDelta}");
            Debug.Log($"🔍 Panel position: {rectTransform.anchoredPosition}");
            Debug.Log($"🔍 Panel anchors: min={rectTransform.anchorMin}, max={rectTransform.anchorMax}");
        }
        
        // Force show the summary
        Debug.Log("🚀 FORCING XP SUMMARY TO SHOW");
        xpSummaryUI.ShowXPSummary();
        
        // Check panel state AFTER
        Debug.Log($"🔍 Panel active AFTER: {summaryPanel.activeSelf}");
    }
    
    [ContextMenu("Force Grant XP")]
    public void ForceGrantXP()
    {
        if (XPManager.Instance == null)
        {
            Debug.LogError("❌ XPManager.Instance is null!");
            return;
        }
        
        Debug.Log("🎁 Granting test XP...");
        XPManager.Instance.DEBUG_GrantXP(100, "Enemy");
        XPManager.Instance.DEBUG_GrantXP(50, "Collectible");  
        XPManager.Instance.DEBUG_GrantXP(200, "Tower");
        
        Debug.Log("✅ Test XP granted! Press T to test summary");
    }
    
    [ContextMenu("Force Show Panel")]
    public void ForceShowPanel()
    {
        Debug.Log("🔨 FORCE SHOWING ANY XP PANEL");
        
        // Find any GameObject with "XP" and "Panel" in name
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("XP") && obj.name.Contains("Panel"))
            {
                Debug.Log($"🔍 Found XP panel: {obj.name}, active: {obj.activeSelf}");
                obj.SetActive(true);
                Debug.Log($"✅ Forced {obj.name} to be active");
            }
        }
        
        // Also check for Summary panels
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Summary") && obj.name.Contains("Panel"))
            {
                Debug.Log($"🔍 Found Summary panel: {obj.name}, active: {obj.activeSelf}");
                obj.SetActive(true);
                Debug.Log($"✅ Forced {obj.name} to be active");
            }
        }
    }
    
    private void ListAllXPComponents()
    {
        Debug.Log("🔍 SEARCHING FOR XP COMPONENTS:");
        
        var xpManagers = FindObjectsByType<XPManager>(FindObjectsSortMode.None);
        Debug.Log($"XPManager count: {xpManagers.Length}");
        
        var xpSummaryUIs = FindObjectsByType<XPSummaryUI>(FindObjectsSortMode.None);
        Debug.Log($"XPSummaryUI count: {xpSummaryUIs.Length}");
        
        var xpSummarySetups = FindObjectsByType<XPSummaryUIPrefabSetup>(FindObjectsSortMode.None);
        Debug.Log($"XPSummaryUIPrefabSetup count: {xpSummarySetups.Length}");
        
        // Look for Canvas with XP in name
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas.name.Contains("XP") || canvas.name.Contains("Summary"))
            {
                Debug.Log($"Found XP-related Canvas: {canvas.name}");
            }
        }
    }
}