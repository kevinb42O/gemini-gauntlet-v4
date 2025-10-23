using UnityEngine;
using UnityEngine.UI;
using GeminiGauntlet.Progression;

namespace GeminiGauntlet.Testing
{
    /// <summary>
    /// Utility script to help set up XP testing by automatically adding XPGranter components
    /// to objects in the scene. This makes it easy to test the XP system.
    /// </summary>
    public class XPTestSetup : MonoBehaviour
    {
        [Header("Auto Setup Options")]
        [SerializeField] private bool setupOnStart = false;
        [SerializeField] private bool findAndSetupExistingObjects = true;
        
        [Header("XP Values for Testing")]
        [SerializeField] private int towerXP = 50;
        [SerializeField] private int skullEnemyXP = 15;
        [SerializeField] private int bossMinionXP = 25;
        [SerializeField] private int gemXP = 5;
        [SerializeField] private int bossXP = 200;
        
        [Header("Manual Setup Buttons (Editor Only)")]
        [SerializeField] private bool setupTowers = false;
        [SerializeField] private bool setupEnemies = false;
        [SerializeField] private bool setupGems = false;
        
        void Start()
        {
            if (setupOnStart)
            {
                SetupAllXPGranters();
            }
        }
        
        void Update()
        {
            // Editor-only manual setup buttons
            #if UNITY_EDITOR
            if (setupTowers)
            {
                setupTowers = false;
                SetupTowersWithXP();
            }
            
            if (setupEnemies)
            {
                setupEnemies = false;
                SetupEnemiesWithXP();
            }
            
            if (setupGems)
            {
                setupGems = false;
                SetupGemsWithXP();
            }
            #endif
        }
        
        [ContextMenu("Setup All XP Granters")]
        public void SetupAllXPGranters()
        {
            SetupTowersWithXP();
            SetupEnemiesWithXP();
            SetupGemsWithXP();
            Debug.Log("[XPTestSetup] All XP granters set up for testing!");
        }
        
        [ContextMenu("Setup Towers with XP")]
        public void SetupTowersWithXP()
        {
            TowerController[] towers = FindObjectsByType<TowerController>(FindObjectsSortMode.None);
            int setupCount = 0;
            
            foreach (var tower in towers)
            {
                if (tower != null)
                {
                    XPGranter granter = tower.GetComponent<XPGranter>();
                    if (granter == null)
                    {
                        granter = tower.gameObject.AddComponent<XPGranter>();
                    }
                    
                    granter.SetupForTower(towerXP);
                    setupCount++;
                }
            }
            
            Debug.Log($"[XPTestSetup] Set up {setupCount} towers with {towerXP} XP each");
        }
        
        [ContextMenu("Setup Enemies with XP")]
        public void SetupEnemiesWithXP()
        {
            // Setup Skull Enemies
            SkullEnemy[] skullEnemies = FindObjectsByType<SkullEnemy>(FindObjectsSortMode.None);
            int skullCount = 0;
            
            foreach (var skull in skullEnemies)
            {
                if (skull != null)
                {
                    XPGranter granter = skull.GetComponent<XPGranter>();
                    if (granter == null)
                    {
                        granter = skull.gameObject.AddComponent<XPGranter>();
                    }
                    
                    // Check if it's a boss minion for different XP values
                    if (skull.isBossMinion)
                    {
                        granter.SetupForEnemy(bossMinionXP);
                        // You can manually set the category to BossMinion if desired
                        // granter.xpCategory = XPGranter.XPCategory.BossMinion;
                    }
                    else
                    {
                        granter.SetupForEnemy(skullEnemyXP);
                    }
                    
                    skullCount++;
                }
            }
            
            // Setup Boss Enemies
            BossEnemy[] bossEnemies = FindObjectsByType<BossEnemy>(FindObjectsSortMode.None);
            int bossCount = 0;
            
            foreach (var boss in bossEnemies)
            {
                if (boss != null)
                {
                    XPGranter granter = boss.GetComponent<XPGranter>();
                    if (granter == null)
                    {
                        granter = boss.gameObject.AddComponent<XPGranter>();
                    }
                    
                    // Setup as boss with high XP value
                    granter.SetupForEnemy(bossXP);
                    // Manually set boss category
                    var categoryField = typeof(XPGranter).GetField("xpCategory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (categoryField != null)
                    {
                        categoryField.SetValue(granter, XPGranter.XPCategory.Boss);
                    }
                    
                    bossCount++;
                }
            }
            
            Debug.Log($"[XPTestSetup] Set up {skullCount} skull enemies and {bossCount} bosses with XP");
        }
        
        [ContextMenu("Setup Gems with XP")]
        public void SetupGemsWithXP()
        {
            Gem[] gems = FindObjectsByType<Gem>(FindObjectsSortMode.None);
            int setupCount = 0;
            
            foreach (var gem in gems)
            {
                if (gem != null)
                {
                    XPGranter granter = gem.GetComponent<XPGranter>();
                    if (granter == null)
                    {
                        granter = gem.gameObject.AddComponent<XPGranter>();
                    }
                    
                    granter.SetupForCollectible(gemXP);
                    setupCount++;
                }
            }
            
            Debug.Log($"[XPTestSetup] Set up {setupCount} gems with {gemXP} XP each");
        }
        
        [ContextMenu("Remove All XP Granters")]
        public void RemoveAllXPGranters()
        {
            XPGranter[] granters = FindObjectsByType<XPGranter>(FindObjectsSortMode.None);
            int removeCount = 0;
            
            foreach (var granter in granters)
            {
                if (granter != null)
                {
                    DestroyImmediate(granter);
                    removeCount++;
                }
            }
            
            Debug.Log($"[XPTestSetup] Removed {removeCount} XP granters from the scene");
        }
        
        [ContextMenu("Create XP Manager if Missing")]
        public void CreateXPManagerIfMissing()
        {
            if (XPManager.Instance == null)
            {
                GameObject xpManagerGO = new GameObject("XPManager");
                xpManagerGO.AddComponent<XPManager>();
                Debug.Log("[XPTestSetup] Created XPManager in the scene");
            }
            else
            {
                Debug.Log("[XPTestSetup] XPManager already exists in the scene");
            }
        }
        
        [ContextMenu("Create XP Summary UI")]
        public void CreateXPSummaryUI()
        {
            // Check if XP Summary UI already exists
            var existingUI = FindObjectOfType<GeminiGauntlet.UI.XPSummaryUI>();
            if (existingUI != null)
            {
                Debug.Log("[XPTestSetup] XP Summary UI already exists in the scene");
                return;
            }
            
            // Create Canvas for XP Summary
            GameObject canvasGO = new GameObject("XP_SummaryCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // High priority to show on top
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Add setup component to auto-create the UI
            var setupComponent = canvasGO.AddComponent<GeminiGauntlet.UI.XPSummaryUIPrefabSetup>();
            
            Debug.Log("[XPTestSetup] Created XP Summary UI Canvas with auto-setup");
        }
        
        [ContextMenu("Test XP Collection")]
        public void TestXPCollection()
        {
            if (XPManager.Instance == null)
            {
                Debug.LogError("[XPTestSetup] XPManager not found! Create one first.");
                return;
            }
            
            // Grant some test XP
            XPManager.Instance.DEBUG_GrantXP(150, "Enemy");
            XPManager.Instance.DEBUG_GrantXP(50, "Collectible");
            XPManager.Instance.DEBUG_GrantXP(200, "Tower");
            
            Debug.Log("[XPTestSetup] Granted test XP - check session totals!");
        }
        
        [ContextMenu("Log XP System Status")]
        public void LogXPSystemStatus()
        {
            Debug.Log("=== XP SYSTEM STATUS ===");
            Debug.Log($"XPManager exists: {XPManager.Instance != null}");
            
            if (XPManager.Instance != null)
            {
                Debug.Log($"Session Total XP: {XPManager.Instance.SessionTotalXP}");
                
                var xpByCategory = XPManager.Instance.GetXPByCategory();
                var countByCategory = XPManager.Instance.GetCountByCategory();
                
                Debug.Log("=== SESSION XP BREAKDOWN ===");
                foreach (var category in xpByCategory.Keys)
                {
                    int count = countByCategory.ContainsKey(category) ? countByCategory[category] : 0;
                    int xp = xpByCategory[category];
                    Debug.Log($"  {category}: {count} items, {xp} XP");
                }
            }
            
            // Check for MenuXPManager (persistent leveling)
            MenuXPManager menuXPManager = FindObjectOfType<MenuXPManager>();
            if (menuXPManager != null)
            {
                Debug.Log("=== PERSISTENT LEVEL DATA ===");
                Debug.Log($"Current Level: {menuXPManager.CurrentLevel}");
                Debug.Log($"Total XP: {menuXPManager.CurrentXP}");
                Debug.Log($"Max Level: {menuXPManager.IsMaxLevel}");
            }
            else
            {
                Debug.Log("MenuXPManager not found in scene (normal for game scenes)");
            }
            
            XPGranter[] granters = FindObjectsByType<XPGranter>(FindObjectsSortMode.None);
            Debug.Log($"XP Granters in scene: {granters.Length}");
            
            foreach (var granter in granters)
            {
                Debug.Log($"  - {granter.gameObject.name}: {granter.XPAmount} XP ({granter.CategoryName})");
            }
        }
    }
}