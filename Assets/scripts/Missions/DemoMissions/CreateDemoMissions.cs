using UnityEngine;

namespace GeminiGauntlet.Missions.Demo
{
    /// <summary>
    /// Utility script to create demo mission assets
    /// Run this once to generate the 3 starter missions
    /// </summary>
    public class CreateDemoMissions : MonoBehaviour
    {
        [Header("Demo Mission Creation")]
        [Tooltip("Click to create the 3 starter missions as ScriptableObject assets")]
        public bool createDemoMissions = false;
        
        [Header("Output Directory")]
        [Tooltip("Directory where mission assets will be created")]
        public string outputDirectory = "Assets/scripts/Missions/DemoMissions/";
        
        void OnValidate()
        {
            if (createDemoMissions)
            {
                createDemoMissions = false;
                CreateStarterMissions();
            }
        }
        
        /// <summary>
        /// Create the 3 starter missions as ScriptableObject assets
        /// </summary>
        [ContextMenu("Create Starter Missions")]
        public void CreateStarterMissions()
        {
            #if UNITY_EDITOR
            // Ensure output directory exists
            if (!System.IO.Directory.Exists(outputDirectory))
            {
                System.IO.Directory.CreateDirectory(outputDirectory);
            }
            
            // Mission 1: Kill 5 skulls
            CreateMission(
                "mission_kill_skulls", 
                "Skull Hunter", 
                "Eliminate 5 skull enemies to prove your combat prowess.",
                MissionType.Kill, 
                MissionTier.Tier1, 
                5, 
                "skull", 
                true, 
                50, 
                10
            );
            
            // Mission 2: Conquer 2 platforms
            CreateMission(
                "mission_conquer_platforms", 
                "Territory Control", 
                "Conquer 2 platforms to establish your dominance.",
                MissionType.Conquer, 
                MissionTier.Tier1, 
                2, 
                "", 
                true, 
                75, 
                15
            );
            
            // Mission 3: Loot a chest
            CreateMission(
                "mission_loot_chest", 
                "Treasure Hunter", 
                "Find and loot 1 chest to claim its treasures.",
                MissionType.Loot, 
                MissionTier.Tier1, 
                1, 
                "", 
                true, 
                25, 
                5
            );
            
            Debug.Log("Demo missions created successfully!");
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
        
        #if UNITY_EDITOR
        void CreateMission(
            string id, 
            string name, 
            string description, 
            MissionType type, 
            MissionTier tier, 
            int targetCount, 
            string targetSpecifier, 
            bool persistProgress, 
            int xpReward, 
            int gemReward)
        {
            // Create mission asset
            Mission mission = ScriptableObject.CreateInstance<Mission>();
            
            // Set mission properties
            mission.missionID = id;
            mission.missionName = name;
            mission.missionDescription = description;
            mission.missionType = type;
            mission.tier = tier;
            mission.targetCount = targetCount;
            mission.targetSpecifier = targetSpecifier;
            mission.persistProgressOnDeath = persistProgress;
            mission.xpReward = xpReward;
            mission.gemReward = gemReward;
            mission.itemRewards = new ChestItemData[0]; // No item rewards for starter missions
            
            // Set progress template based on mission type
            switch (type)
            {
                case MissionType.Kill:
                    mission.progressTemplate = "Killed: {current}/{target} " + targetSpecifier;
                    break;
                case MissionType.Conquer:
                    mission.progressTemplate = "Conquered: {current}/{target} platforms";
                    break;
                case MissionType.Loot:
                    mission.progressTemplate = "Looted: {current}/{target} chests";
                    break;
                default:
                    mission.progressTemplate = "Progress: {current}/{target}";
                    break;
            }
            
            mission.completionText = "Mission Complete! Return to menu to claim rewards.";
            
            // Save asset
            string assetPath = outputDirectory + id + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(mission, assetPath);
            
            Debug.Log($"Created mission: {name} at {assetPath}");
        }
        #endif
    }
}