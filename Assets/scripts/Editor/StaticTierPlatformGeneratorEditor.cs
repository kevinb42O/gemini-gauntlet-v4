using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for StaticTierPlatformGenerator.
/// Adds "Generate Platforms" and "Clear Platforms" buttons to the Inspector.
/// </summary>
[CustomEditor(typeof(StaticTierPlatformGenerator))]
public class StaticTierPlatformGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        
        StaticTierPlatformGenerator generator = (StaticTierPlatformGenerator)target;
        
        // Add some space
        EditorGUILayout.Space(10);
        
        // === GENERATION SECTION ===
        EditorGUILayout.LabelField("🎮 PLATFORM GENERATION", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Click 'Generate Platforms' to create static platforms in the editor. " +
            "Platforms will spawn at configured heights. After generation, manually place elevators between platforms.",
            MessageType.Info
        );
        
        EditorGUILayout.Space(5);
        
        // Generate button (BIG and GREEN)
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🚀 GENERATE PLATFORMS", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog(
                "Generate Platforms",
                "This will generate static platforms based on your tier configuration. Continue?",
                "Yes, Generate!",
                "Cancel"))
            {
                Undo.RecordObject(generator, "Generate Platforms");
                generator.GeneratePlatforms();
                EditorUtility.SetDirty(generator);
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(5);
        
        // Clear button (RED and smaller)
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("🗑️ Clear Generated Platforms", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Clear Platforms",
                "This will delete ALL generated platforms (children of this generator). Are you sure?",
                "Yes, Clear All",
                "Cancel"))
            {
                Undo.RecordObject(generator, "Clear Platforms");
                generator.ClearGeneratedPlatforms();
                EditorUtility.SetDirty(generator);
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(10);
        
        // === QUICK SETUP SECTION ===
        EditorGUILayout.LabelField("⚡ QUICK SETUP GUIDE", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Create 3 platform prefabs (Normal, Ice, Fire)\n" +
            "2. Configure 3 tiers in the list above\n" +
            "3. Set tierHeightOffset (e.g., 300 units between levels)\n" +
            "4. Click 'Generate Platforms'\n" +
            "5. Manually drag ElevatorController prefabs onto platforms\n" +
            "6. Connect elevators (set topFloor and bottomFloor)\n" +
            "7. Play your game!",
            MessageType.None
        );
        
        EditorGUILayout.Space(10);
        
        // === STATS SECTION ===
        if (generator.tiers != null && generator.tiers.Count > 0)
        {
            EditorGUILayout.LabelField("📊 CONFIGURATION STATS", EditorStyles.boldLabel);
            
            int totalPlatforms = 0;
            for (int i = 0; i < generator.tiers.Count; i++)
            {
                totalPlatforms += generator.tiers[i].platformCount;
            }
            
            EditorGUILayout.LabelField($"Total Tiers: {generator.tiers.Count}");
            EditorGUILayout.LabelField($"Total Platforms: {totalPlatforms}");
            EditorGUILayout.LabelField($"Height Range: {generator.startingHeight} to {generator.startingHeight + (generator.tiers.Count - 1) * generator.tierHeightOffset} units");
        }
        
        EditorGUILayout.Space(5);
        
        // === WARNINGS SECTION ===
        bool hasWarnings = false;
        
        if (generator.tiers == null || generator.tiers.Count == 0)
        {
            EditorGUILayout.HelpBox("⚠️ No tiers configured! Add at least one tier to the list.", MessageType.Warning);
            hasWarnings = true;
        }
        else
        {
            // Check each tier
            for (int i = 0; i < generator.tiers.Count; i++)
            {
                if (generator.tiers[i].platformPrefab == null)
                {
                    EditorGUILayout.HelpBox($"⚠️ Tier {i + 1} ('{generator.tiers[i].tierName}') has no platform prefab assigned!", MessageType.Warning);
                    hasWarnings = true;
                }
            }
        }
        
        if (!hasWarnings)
        {
            EditorGUILayout.HelpBox("✅ Configuration looks good! Ready to generate.", MessageType.Info);
        }
    }
}
