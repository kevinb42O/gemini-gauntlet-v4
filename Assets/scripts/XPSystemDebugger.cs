using UnityEngine;
using GeminiGauntlet.Progression;
using GeminiGauntlet.UI;

namespace GeminiGauntlet.Debugging
{
    /// <summary>
    /// Comprehensive XP system debugger to track down issues
    /// </summary>
    public class XPSystemDebugger : MonoBehaviour
    {
        [Header("Debug Controls")]
        [SerializeField] private bool logAllXPGrants = true;
        [SerializeField] private bool showRealTimeStats = true;
        [SerializeField] private float updateInterval = 2f;
        
        private float lastUpdateTime = 0f;
        
        void Start()
        {
            Debug.Log("=== XP SYSTEM DEBUGGER STARTED ===");
            LogSystemStatus();
        }
        
        void Update()
        {
            if (showRealTimeStats && Time.time - lastUpdateTime >= updateInterval)
            {
                LogRealTimeStats();
                lastUpdateTime = Time.time;
            }
        }
        
        [ContextMenu("Log System Status")]
        public void LogSystemStatus()
        {
            Debug.Log("=== XP SYSTEM STATUS ===");
            
            // Check XPManager
            var xpManager = GeminiGauntlet.Progression.XPManager.Instance;
            if (xpManager == null)
            {
                Debug.LogError("❌ XPManager.Instance is NULL!");
                var foundXpManager = FindObjectOfType<GeminiGauntlet.Progression.XPManager>();
                if (foundXpManager != null)
                {
                    Debug.LogWarning($"⚠️ Found XPManager in scene: {foundXpManager.name}, but Instance is null!");
                }
            }
            else
            {
                Debug.Log($"✅ XPManager.Instance found: {xpManager.name}");
            }
            
            // Check MenuXPManager
            var menuXpManager = GeminiGauntlet.Progression.MenuXPManager.Instance;
            if (menuXpManager == null)
            {
                Debug.Log("⚠️ MenuXPManager.Instance is NULL (normal if in game scene)");
            }
            else
            {
                Debug.Log($"✅ MenuXPManager.Instance found: {menuXpManager.name}");
                Debug.Log($"   Current Level: {menuXpManager.CurrentLevel}");
                Debug.Log($"   Current XP: {menuXpManager.CurrentXP}");
            }
            
            // Check PlayerPrefs
            int playerLevel = PlayerPrefs.GetInt("PlayerLevel", -1);
            int persistentXP = PlayerPrefs.GetInt("PersistentXP", -1);
            int lastSessionXP = PlayerPrefs.GetInt("LastSessionXP", -1);
            
            Debug.Log("=== PLAYERPREFS DATA ===");
            Debug.Log($"PlayerLevel: {(playerLevel == -1 ? "NOT SET" : playerLevel.ToString())}");
            Debug.Log($"PersistentXP: {(persistentXP == -1 ? "NOT SET" : persistentXP.ToString())}");
            Debug.Log($"LastSessionXP: {(lastSessionXP == -1 ? "NOT SET" : lastSessionXP.ToString())}");
            
            Debug.Log("========================");
        }
        
        [ContextMenu("Log Real-Time Stats")]
        public void LogRealTimeStats()
        {
            var xpManager = GeminiGauntlet.Progression.XPManager.Instance;
            if (xpManager != null)
            {
                var summaryData = xpManager.GetXPSummaryData();
                Debug.Log($"[XP Stats] Session Total: {summaryData.sessionTotalXP} XP");
                
                foreach (var category in summaryData.categoryBreakdown)
                {
                    Debug.Log($"[XP Stats]   {category.categoryName}: {category.totalXP} XP ({category.count}x)");
                }
            }
        }
        
        [ContextMenu("Test Enemy Kill")]
        public void TestEnemyKill()
        {
            Debug.Log("=== TESTING ENEMY KILL ===");
            GeminiGauntlet.Progression.XPHooks.OnEnemyKilled("skull");
            LogRealTimeStats();
        }
        
        [ContextMenu("Test Gem Collection")]
        public void TestGemCollection()
        {
            Debug.Log("=== TESTING GEM COLLECTION ===");
            GeminiGauntlet.Progression.XPHooks.OnGemCollected();
            LogRealTimeStats();
        }
        
        [ContextMenu("Test Tower Destruction")]
        public void TestTowerDestruction()
        {
            Debug.Log("=== TESTING TOWER DESTRUCTION ===");
            GeminiGauntlet.Progression.XPHooks.OnTowerDestroyed();
            LogRealTimeStats();
        }
        
        [ContextMenu("Test Platform Conquest")]
        public void TestPlatformConquest()
        {
            Debug.Log("=== TESTING PLATFORM CONQUEST ===");
            GeminiGauntlet.Progression.XPHooks.OnPlatformConquered();
            LogRealTimeStats();
        }
        
        [ContextMenu("Force Show XP Summary")]
        public void ForceShowXPSummary()
        {
            Debug.Log("=== FORCE SHOWING XP SUMMARY ===");
            var xpSummaryUI = FindObjectOfType<XPSummaryUI>();
            if (xpSummaryUI != null)
            {
                var summaryData = GeminiGauntlet.Progression.XPManager.Instance?.GetXPSummaryData();
                if (summaryData != null)
                {
                    // Call the summary UI directly
                    Debug.Log($"Summary data: {summaryData.sessionTotalXP} total XP");
                    foreach (var cat in summaryData.categoryBreakdown)
                    {
                        Debug.Log($"  {cat.categoryName}: {cat.totalXP} XP");
                    }
                }
            }
            else
            {
                Debug.LogWarning("XPSummaryUI not found in scene");
            }
        }
    }
}