using UnityEngine;
using GeminiGauntlet.Progression;
using GeminiGauntlet.Missions.Integration;

namespace GeminiGauntlet.Missions
{
    /// <summary>
    /// Setup guide and validation for the mission system
    /// This component helps ensure the mission system is properly configured
    /// </summary>
    public class MissionSetupGuide : MonoBehaviour
    {
        [Header("Mission System Validation")]
        [Tooltip("Run validation check to ensure all components are properly configured")]
        public bool runValidationCheck = false;
        
        void OnValidate()
        {
            if (runValidationCheck)
            {
                runValidationCheck = false;
                ValidateMissionSystemSetup();
            }
        }
        
        /// <summary>
        /// Validate that the mission system is properly set up
        /// </summary>
        [ContextMenu("Validate Mission System Setup")]
        public void ValidateMissionSystemSetup()
        {
            Debug.Log("🎯 === MISSION SYSTEM VALIDATION ===");
            
            // Check MissionManager
            bool missionManagerFound = ValidateMissionManager();
            
            // Check XP Integration
            bool xpIntegrationValid = ValidateXPIntegration();
            
            // Check StashManager Integration
            bool stashIntegrationValid = ValidateStashIntegration();
            
            // Check FORGE Integration
            bool forgeIntegrationValid = ValidateForgeIntegration();
            
            // Summary
            Debug.Log("🎯 === VALIDATION SUMMARY ===");
            Debug.Log($"✅ MissionManager: {(missionManagerFound ? "OK" : "❌ MISSING")}");
            Debug.Log($"✅ XP Integration: {(xpIntegrationValid ? "OK" : "❌ ISSUE")}");
            Debug.Log($"✅ Stash Integration: {(stashIntegrationValid ? "OK" : "❌ ISSUE")}");
            Debug.Log($"✅ FORGE Integration: {(forgeIntegrationValid ? "OK" : "⚠️ MANUAL SETUP NEEDED")}");
            
            if (missionManagerFound && xpIntegrationValid && stashIntegrationValid)
            {
                Debug.Log("🎉 Mission system is ready to use!");
            }
            else
            {
                Debug.Log("⚠️ Please address the issues above before using the mission system.");
            }
        }
        
        bool ValidateMissionManager()
        {
            if (MissionManager.Instance == null)
            {
                Debug.LogError("❌ MissionManager not found! Please add MissionManager component to a GameObject in your scene.");
                return false;
            }
            
            var missionManager = MissionManager.Instance;
            
            // Check mission assets
            if (missionManager.allMissions == null || missionManager.allMissions.Length == 0)
            {
                Debug.LogWarning("⚠️ No missions assigned to MissionManager. Use CreateDemoMissions to generate starter missions.");
            }
            else
            {
                Debug.Log($"✅ Found {missionManager.allMissions.Length} missions assigned to MissionManager");
            }
            
            return true;
        }
        
        bool ValidateXPIntegration()
        {
            // Check if XPManager exists
            if (XPManager.Instance == null)
            {
                Debug.LogError("❌ XPManager.Instance not found! Mission XP integration will not work.");
                return false;
            }
            
            Debug.Log("✅ XPManager found - mission XP will be integrated into XP summary");
            return true;
        }
        
        bool ValidateStashIntegration()
        {
            var stashManager = FindObjectOfType<StashManager>();
            if (stashManager == null)
            {
                Debug.LogWarning("⚠️ StashManager not found in scene. Gem rewards will not work in this scene.");
                return false;
            }
            
            // Check gem data
            var gemData = Resources.Load<GemItemData>("Items/GemItemData");
            if (gemData == null)
            {
                Debug.LogError("❌ GemItemData not found at Resources/Items/GemItemData. Gem rewards will not work.");
                return false;
            }
            
            Debug.Log("✅ StashManager and GemItemData found - gem rewards will work");
            return true;
        }
        
        bool ValidateForgeIntegration()
        {
            if (ForgeManager.Instance == null)
            {
                Debug.LogWarning("⚠️ ForgeManager not found. Craft missions will not track automatically.");
                return false;
            }
            
            var forgeIntegration = FindObjectOfType<MissionForgeIntegration>();
            if (forgeIntegration == null)
            {
                Debug.LogWarning("⚠️ MissionForgeIntegration component not found. Add it to track craft missions automatically.");
                Debug.Log("💡 TIP: Add MissionForgeIntegration component to an object in your scene and assign the FORGE output slot.");
                return false;
            }
            
            Debug.Log("✅ FORGE integration components found");
            return true;
        }
        
        /// <summary>
        /// Create a basic mission setup in the current scene
        /// </summary>
        [ContextMenu("Create Basic Mission Setup")]
        public void CreateBasicMissionSetup()
        {
            Debug.Log("🎯 Creating basic mission setup...");
            
            // Create MissionManager if it doesn't exist
            if (MissionManager.Instance == null)
            {
                GameObject missionManagerGO = new GameObject("MissionManager");
                missionManagerGO.AddComponent<MissionManager>();
                Debug.Log("✅ Created MissionManager GameObject");
            }
            
            // Create mission integration components
            GameObject integrationGO = new GameObject("Mission Integration");
            
            // Add FORGE integration if FORGE exists
            if (ForgeManager.Instance != null)
            {
                var forgeIntegration = integrationGO.AddComponent<MissionForgeIntegration>();
                forgeIntegration.forgeOutputSlot = ForgeManager.Instance.outputSlot;
                Debug.Log("✅ Added MissionForgeIntegration component");
            }
            
            Debug.Log("🎉 Basic mission setup complete!");
            Debug.Log("💡 Next steps:");
            Debug.Log("   1. Create mission assets using CreateDemoMissions or manually");
            Debug.Log("   2. Assign missions to MissionManager.allMissions array");
            Debug.Log("   3. Add EquippedMissionsUI to your main menu");
            Debug.Log("   4. Add MissionSelectionUI to your mission selection canvas");
            Debug.Log("   5. Add mission tracking components to your game objects");
        }
    }
}