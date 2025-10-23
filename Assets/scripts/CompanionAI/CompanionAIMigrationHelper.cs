using UnityEngine;
using CompanionAI;

/// <summary>
/// Helper script to migrate from old CompanionAI to new modular system
/// Attach this to your companion GameObject and click "Migrate to Modular System"
/// </summary>
public class CompanionAIMigrationHelper : MonoBehaviour
{
        [Header("Migration Settings")]
        [Tooltip("The old CompanionAI script to migrate from")]
        public CompanionAILegacy oldCompanionAI;
        
        [Header("Migration Actions")]
        [Space(10)]
        public bool migrateSettings = true;
        public bool removeOldScript = false;
        public bool testNewSystem = true;
        
        [Header("Migration Status")]
        [SerializeField] private bool _migrationComplete = false;
        
        void Start()
        {
            // Auto-find old script if not assigned
            if (oldCompanionAI == null)
            {
                oldCompanionAI = GetComponent<CompanionAILegacy>();
            }
        }
        
        [ContextMenu("Migrate to Modular System")]
        public void MigrateToModularSystem()
        {
            if (_migrationComplete)
            {
                Debug.LogWarning("[Migration] Migration already completed!");
                return;
            }
            
            if (oldCompanionAI == null)
            {
                Debug.LogError("[Migration] No old CompanionAILegacy script found!");
                return;
            }
            
            Debug.Log("[Migration] Starting migration to modular system...");
            
            // Step 1: Add new modular components
            AddModularComponents();
            
            // Step 2: Migrate settings
            if (migrateSettings)
            {
                MigrateSettings();
            }
            
            // Step 3: Test new system
            if (testNewSystem)
            {
                TestNewSystem();
            }
            
            // Step 4: Optionally remove old script
            if (removeOldScript)
            {
                RemoveOldScript();
            }
            
            _migrationComplete = true;
            Debug.Log("[Migration] ✅ Migration completed successfully!");
        }
        
        private void AddModularComponents()
        {
            Debug.Log("[Migration] Adding modular components...");
            
            // Add core component
            CompanionCore core = GetComponent<CompanionCore>();
            if (core == null)
            {
                core = gameObject.AddComponent<CompanionCore>();
            }
            
            // Add system components
            if (GetComponent<CompanionMovement>() == null)
                gameObject.AddComponent<CompanionMovement>();
                
            if (GetComponent<CompanionTargeting>() == null)
                gameObject.AddComponent<CompanionTargeting>();
                
            if (GetComponent<CompanionCombat>() == null)
                gameObject.AddComponent<CompanionCombat>();
                
            if (GetComponent<CompanionAudio>() == null)
                gameObject.AddComponent<CompanionAudio>();
                
            if (GetComponent<CompanionVisualEffects>() == null)
                gameObject.AddComponent<CompanionVisualEffects>();
        }
        
        private void MigrateSettings()
        {
            Debug.Log("[Migration] Migrating settings from old script...");
            
            // Migrate to Core
            CompanionCore core = GetComponent<CompanionCore>();
            if (core != null)
            {
                core.playerTransform = oldCompanionAI.playerTransform;
            }
            
            // Migrate to Movement
            CompanionMovement movement = GetComponent<CompanionMovement>();
            if (movement != null)
            {
                movement.followingDistance = oldCompanionAI.followingDistance;
                movement.minFollowDistance = oldCompanionAI.minFollowDistance;
                movement.maxFollowDistance = oldCompanionAI.maxFollowDistance;
                movement.walkingSpeed = oldCompanionAI.walkingSpeed;
                movement.runningSpeed = oldCompanionAI.runningSpeed;
                movement.moveWhileShooting = oldCompanionAI.moveWhileShooting;
                movement.combatSpeedMultiplier = oldCompanionAI.combatSpeedMultiplier;
                movement.repositionInterval = oldCompanionAI.repositionInterval;
                movement.jumpChance = oldCompanionAI.jumpChance;
                movement.jumpForce = oldCompanionAI.jumpForce;
                movement.groundLayers = oldCompanionAI.groundLayers;
            }
            
            // Migrate to Targeting
            CompanionTargeting targeting = GetComponent<CompanionTargeting>();
            if (targeting != null)
            {
                targeting.detectionRadius = oldCompanionAI.detectionRadius;
                targeting.attackRange = oldCompanionAI.attackRange;
                targeting.emergencyThreatDistance = oldCompanionAI.emergencyThreatDistance;
                targeting.skullEnemyPrefab1 = oldCompanionAI.skullEnemyPrefab1;
                targeting.skullEnemyPrefab2 = oldCompanionAI.skullEnemyPrefab2;
                targeting.skullEnemyPrefab3 = oldCompanionAI.skullEnemyPrefab3;
                targeting.targetTags = oldCompanionAI.targetTags;
                targeting.priorityTagOrder = oldCompanionAI.priorityTagOrder;
                targeting.useIntelligentThreatSystem = oldCompanionAI.useIntelligentThreatSystem;
                targeting.threatScanInterval = oldCompanionAI.threatScanInterval;
                // REMOVED: Swarm logic has been eliminated for better performance
                // targeting.useSwarmSprayTactics = oldCompanionAI.useSwarmSprayTactics;
                // targeting.swarmThreshold = oldCompanionAI.swarmThreshold;
                // targeting.sprayTransitionTime = oldCompanionAI.sprayTransitionTime;
                targeting.enemyScanInterval = oldCompanionAI.enemyScanInterval;
            }
            
            // Migrate to Combat
            CompanionCombat combat = GetComponent<CompanionCombat>();
            if (combat != null)
            {
                combat.leftHandEmitPoint = oldCompanionAI.leftHandEmitPoint;
                combat.rightHandEmitPoint = oldCompanionAI.rightHandEmitPoint;
                combat.shotgunParticlePrefab = oldCompanionAI.shotgunParticlePrefab;
                combat.streamParticlePrefab = oldCompanionAI.streamParticlePrefab;
                combat.streamThreshold = oldCompanionAI.streamThreshold;
                combat.shotgunCooldown = oldCompanionAI.shotgunCooldown;
                combat.streamDamage = oldCompanionAI.streamDamage;
                combat.shotgunDamage = oldCompanionAI.shotgunDamage;
                combat.beamCooldown = oldCompanionAI.beamCooldown;
                combat.beamDuration = oldCompanionAI.beamDuration;
                combat.damageRadius = oldCompanionAI.damageRadius;
                combat.useAreaDamage = oldCompanionAI.useAreaDamage;
                combat.targetTrackingSpeedMultiplier = oldCompanionAI.skullTrackingSpeedMultiplier;
            }
            
            // Migrate to Audio
            CompanionAudio audio = GetComponent<CompanionAudio>();
            if (audio != null)
            {
                audio.shotgunSFX = oldCompanionAI.shotgunSFX;
                audio.streamLoopSFX = oldCompanionAI.streamLoopSFX;
                audio.shotgunVolume = oldCompanionAI.shotgunVolume;
                audio.streamVolume = oldCompanionAI.streamVolume;
            }
            
            // Migrate to Visual Effects
            CompanionVisualEffects effects = GetComponent<CompanionVisualEffects>();
            if (effects != null)
            {
                effects.followingGlowEffectPrefab = oldCompanionAI.followingGlowEffectPrefab;
                effects.combatGlowEffectPrefab = oldCompanionAI.combatGlowEffectPrefab;
            }
        }
        
        private void TestNewSystem()
        {
            Debug.Log("[Migration] Testing new modular system...");
            
            // Verify all components are present
            CompanionCore core = GetComponent<CompanionCore>();
            CompanionMovement movement = GetComponent<CompanionMovement>();
            CompanionTargeting targeting = GetComponent<CompanionTargeting>();
            CompanionCombat combat = GetComponent<CompanionCombat>();
            CompanionAudio audio = GetComponent<CompanionAudio>();
            CompanionVisualEffects effects = GetComponent<CompanionVisualEffects>();
            
            bool allComponentsPresent = core != null && movement != null && targeting != null && 
                                      combat != null && audio != null && effects != null;
            
            if (allComponentsPresent)
            {
                Debug.Log("[Migration] ✅ All modular components present and configured!");
            }
            else
            {
                Debug.LogError("[Migration] ❌ Some modular components are missing!");
            }
        }
        
        private void RemoveOldScript()
        {
            Debug.Log("[Migration] Removing old CompanionAI script...");
            
            if (oldCompanionAI != null)
            {
                DestroyImmediate(oldCompanionAI);
                Debug.Log("[Migration] ✅ Old script removed!");
            }
        }
        
        [ContextMenu("Revert Migration")]
        public void RevertMigration()
        {
            Debug.Log("[Migration] Reverting to old system...");
            
            // Remove new components
            CompanionCore core = GetComponent<CompanionCore>();
            if (core != null) DestroyImmediate(core);
            
            CompanionMovement movement = GetComponent<CompanionMovement>();
            if (movement != null) DestroyImmediate(movement);
            
            CompanionTargeting targeting = GetComponent<CompanionTargeting>();
            if (targeting != null) DestroyImmediate(targeting);
            
            CompanionCombat combat = GetComponent<CompanionCombat>();
            if (combat != null) DestroyImmediate(combat);
            
            CompanionAudio audio = GetComponent<CompanionAudio>();
            if (audio != null) DestroyImmediate(audio);
            
            CompanionVisualEffects effects = GetComponent<CompanionVisualEffects>();
            if (effects != null) DestroyImmediate(effects);
            
            _migrationComplete = false;
            Debug.Log("[Migration] ✅ Migration reverted!");
        }
    }