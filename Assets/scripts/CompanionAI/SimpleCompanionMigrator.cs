using UnityEngine;

/// <summary>
/// Simple migration helper that definitely works
/// </summary>
public class SimpleCompanionMigrator : MonoBehaviour
{
    [Header("SIMPLE MIGRATION TOOL")]
    public CompanionAILegacy oldCompanionScript;
    public bool migrateSettings = true;
    public bool removeOldScriptAfter = false;
    
    [ContextMenu("🚀 MIGRATE TO MODULAR SYSTEM")]
    public void MigrateToModularSystem()
    {
        if (oldCompanionScript == null)
        {
            Debug.LogError("❌ Please assign your old CompanionAILegacy script first!");
            return;
        }
        
        Debug.Log("🔄 Starting migration to modular system...");
        
        // Step 1: Add the new CompanionCore
        var core = gameObject.GetComponent<CompanionAI.CompanionCore>();
        if (core == null)
        {
            core = gameObject.AddComponent<CompanionAI.CompanionCore>();
            Debug.Log("✅ Added CompanionCore");
        }
        
        // Step 2: Copy the player reference
        if (migrateSettings && core != null)
        {
            core.playerTransform = oldCompanionScript.playerTransform;
            Debug.Log("✅ Migrated player reference");
        }
        
        // Step 3: The CompanionCore will automatically add other systems
        Debug.Log("✅ CompanionCore will automatically add other systems (Movement, Combat, etc.)");
        
        // Step 4: Manual settings transfer message
        Debug.Log("📋 NEXT STEPS:");
        Debug.Log("1. Play your scene to test the new system");
        Debug.Log("2. If it works, you can remove the old CompanionAILegacy script");
        Debug.Log("3. Use the ManualMigrationGuide for detailed settings transfer if needed");
        
        // Step 5: Optionally remove old script
        if (removeOldScriptAfter)
        {
            DestroyImmediate(oldCompanionScript);
            Debug.Log("✅ Removed old script");
        }
        
        Debug.Log("🎉 MIGRATION COMPLETE! Test your companion now!");
    }
    
    [ContextMenu("🔙 REVERT MIGRATION")]
    public void RevertMigration()
    {
        Debug.Log("🔄 Reverting migration...");
        
        // Remove new components
        var core = GetComponent<CompanionAI.CompanionCore>();
        if (core != null) 
        {
            DestroyImmediate(core);
            Debug.Log("✅ Removed CompanionCore");
        }
        
        // Note: CompanionCore cleanup will handle removing other systems
        Debug.Log("✅ Migration reverted!");
    }
    
    void Start()
    {
        // Auto-find old script if not assigned
        if (oldCompanionScript == null)
        {
            oldCompanionScript = GetComponent<CompanionAILegacy>();
            if (oldCompanionScript != null)
            {
                Debug.Log("✅ Auto-found CompanionAILegacy script");
            }
        }
    }
}