using UnityEngine;
using UnityEditor;

namespace GeminiGauntlet.Debugging
{
    /// <summary>
    /// Utility to restore player level in case of PlayerPrefs corruption
    /// </summary>
    public class RestorePlayerLevel : MonoBehaviour
    {
        [Header("Level Recovery")]
        [SerializeField] private int targetLevel = 7;
        [SerializeField] private bool calculateXPFromLevel = true;
        
        [Header("Manual XP Override")]
        [SerializeField] private int manualXP = 0;
        
        [Space]
        [SerializeField] private bool showCurrentData = true;
        
        void Start()
        {
            if (showCurrentData)
            {
                ShowCurrentPlayerData();
            }
        }
        
        [ContextMenu("Show Current Player Data")]
        public void ShowCurrentPlayerData()
        {
            int currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
            int currentXP = PlayerPrefs.GetInt("PersistentXP", 0);
            int lastSessionXP = PlayerPrefs.GetInt("LastSessionXP", 0);
            
            Debug.Log($"=== CURRENT PLAYER DATA ===");
            Debug.Log($"Level: {currentLevel}");
            Debug.Log($"Persistent XP: {currentXP}");
            Debug.Log($"Last Session XP: {lastSessionXP}");
            Debug.Log($"==========================");
        }
        
        [ContextMenu("Restore Level 7")]
        public void RestoreLevel7()
        {
            RestoreToLevel(7);
        }
        
        [ContextMenu("Restore to Target Level")]
        public void RestoreToTargetLevel()
        {
            RestoreToLevel(targetLevel);
        }
        
        public void RestoreToLevel(int level)
        {
            Debug.Log($"🔧 RESTORING PLAYER TO LEVEL {level}...");
            
            // Calculate XP needed for this level
            int xpForLevel = calculateXPFromLevel ? CalculateXPForLevel(level) : manualXP;
            
            // Save the data
            PlayerPrefs.SetInt("PlayerLevel", level);
            PlayerPrefs.SetInt("PersistentXP", xpForLevel);
            PlayerPrefs.Save();
            
            Debug.Log($"✅ RESTORED: Level {level} with {xpForLevel} XP");
            Debug.Log($"🎯 Restart your game to see the restored level!");
            
            // Show confirmation
            ShowCurrentPlayerData();
        }
        
        private int CalculateXPForLevel(int targetLevel)
        {
            // Match MenuXPManager's XP calculation
            int baseXP = 100;
            float growthMultiplier = 1.5f;
            int totalXP = 0;
            
            for (int level = 1; level < targetLevel; level++)
            {
                int xpForThisLevel = Mathf.RoundToInt(baseXP * Mathf.Pow(growthMultiplier, level - 1));
                totalXP += xpForThisLevel;
            }
            
            return totalXP;
        }
        
        [ContextMenu("Clear All Player Data (DANGER!)")]
        public void ClearAllPlayerData()
        {
            Debug.LogWarning("⚠️ CLEARING ALL PLAYER DATA!");
            PlayerPrefs.DeleteKey("PlayerLevel");
            PlayerPrefs.DeleteKey("PersistentXP");
            PlayerPrefs.DeleteKey("LastSessionXP");
            PlayerPrefs.Save();
            Debug.Log("🗑️ All player data cleared. Level will reset to 1.");
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(RestorePlayerLevel))]
    public class RestorePlayerLevelEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            RestorePlayerLevel script = (RestorePlayerLevel)target;
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("📊 Show Current Data", GUILayout.Height(30)))
            {
                script.ShowCurrentPlayerData();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("🎯 Restore to Level 7", GUILayout.Height(30)))
            {
                script.RestoreLevel7();
            }
            
            if (GUILayout.Button("🔧 Restore to Target Level", GUILayout.Height(30)))
            {
                script.RestoreToTargetLevel();
            }
            
            GUILayout.Space(10);
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("🗑️ CLEAR ALL DATA (DANGER!)", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Confirm Data Clear", 
                    "This will delete ALL player progress! Are you sure?", 
                    "Yes, Delete Everything", "Cancel"))
                {
                    script.ClearAllPlayerData();
                }
            }
            GUI.backgroundColor = Color.white;
        }
    }
#endif
}