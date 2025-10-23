using UnityEngine;
using GeminiGauntlet.Progression;

/// <summary>
/// Emergency XP System Debugger - tells us exactly what's broken
/// </summary>
public class XPDebugger : MonoBehaviour
{
    [Header("Debug Controls")]
    public bool enableDebugLogs = true;
    public KeyCode testKey = KeyCode.F1;
    public KeyCode forceExitKey = KeyCode.F2;
    
    void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            DebugXPSystem();
        }
        
        if (Input.GetKeyDown(forceExitKey))
        {
            ForceExit();
        }
    }
    
    [ContextMenu("Debug XP System")]
    public void DebugXPSystem()
    {
        Debug.Log("=== XP SYSTEM DEBUG ===");
        
        // Check XPManager
        if (XPManager.Instance == null)
        {
            Debug.LogError("❌ XPManager.Instance is NULL! XP system is completely broken!");
            return;
        }
        else
        {
            Debug.Log("✅ XPManager found");
            Debug.Log($"Session XP: {XPManager.Instance.SessionTotalXP}");
            
            var categories = XPManager.Instance.GetXPByCategory();
            foreach (var kvp in categories)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} XP");
            }
        }
        
        // Check ExitZone
        ExitZone exitZone = FindObjectOfType<ExitZone>();
        if (exitZone == null)
        {
            Debug.LogError("❌ No ExitZone found in scene!");
        }
        else
        {
            Debug.Log("✅ ExitZone found: " + exitZone.name);
        }
        
        // Check XP Summary UI
        var xpSummaryUI = FindObjectOfType<GeminiGauntlet.UI.XPSummaryUI>();
        if (xpSummaryUI == null)
        {
            Debug.LogWarning("⚠️ XPSummaryUI not found");
        }
        else
        {
            Debug.Log("✅ XPSummaryUI found: " + xpSummaryUI.name);
        }
        
        // REMOVED: EmergencyXPFix check - using centralized persistence now
        
        // Check XP Granters
        var granters = FindObjectsByType<XPGranter>(FindObjectsSortMode.None);
        Debug.Log($"XP Granters in scene: {granters.Length}");
    }
    
    [ContextMenu("Force Grant Test XP")]
    public void ForceGrantTestXP()
    {
        if (XPManager.Instance != null)
        {
            XPManager.Instance.DEBUG_GrantXP(100, "Enemy");
            XPManager.Instance.DEBUG_GrantXP(50, "Collectible");
            XPManager.Instance.DEBUG_GrantXP(200, "Tower");
            Debug.Log("✅ Forced test XP granted");
        }
        else
        {
            Debug.LogError("❌ Cannot grant XP - XPManager is null!");
        }
    }
    
    [ContextMenu("Force Exit")]
    public void ForceExit()
    {
        Debug.Log("🚨 FORCING EXIT SEQUENCE");
        
        if (XPManager.Instance == null)
        {
            Debug.LogError("❌ XPManager is null - going directly to menu");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            return;
        }
        
        // Save XP manually
        var summaryData = XPManager.Instance.GetXPSummaryData();
        Debug.Log($"Total session XP: {summaryData.sessionTotalXP}");
        
        if (summaryData.sessionTotalXP > 0)
        {
            // Save to PlayerPrefs
            // FIXED: Use LastSessionXP instead of writing to PersistentXP directly
            PlayerPrefs.SetInt("LastSessionXP", summaryData.sessionTotalXP);
            PlayerPrefs.Save();
            
            Debug.Log($"✅ FORCE SAVED: {summaryData.sessionTotalXP} session XP as LastSessionXP (MenuXPManager will process it)");
            
            // Show summary in console
            Debug.Log("=== MANUAL XP SUMMARY ===");
            foreach (var category in summaryData.categoryBreakdown)
            {
                Debug.Log($"{category.GetDisplayName()}: {category.count} x {category.xpPerItem}xp = {category.totalXP}xp");
            }
            Debug.Log($"TOTAL XP: {summaryData.sessionTotalXP}xp");
        }
        
        // Go to menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("🚨 XPDebugger: Player entered trigger - forcing exit!");
            ForceExit();
        }
    }
}