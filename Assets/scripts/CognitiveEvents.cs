// --- CognitiveEvents.cs - Event System Integration ---
using UnityEngine;

/// <summary>
/// 🧠 COGNITIVE EVENTS SYSTEM
/// 
/// Central event system that allows all game systems to communicate with 
/// the enhanced cognitive feedback system. Other scripts can trigger these
/// events to notify the cognitive AI about important game happenings.
/// 
/// Usage Examples:
/// • CognitiveEvents.OnItemHoverStart?.Invoke(itemData, slot);
/// • CognitiveEvents.OnCombatEvent?.Invoke("enemy_defeated", enemyPosition);
/// • CognitiveEvents.OnPlayerStateChanged?.Invoke("low_health");
/// </summary>
public static class CognitiveEvents
{
    // === INVENTORY & ITEM EVENTS ===
    /// <summary>Triggered when player starts hovering over an item</summary>
    public static System.Action<ChestItemData, UnifiedSlot> OnItemHoverStart;
    
    /// <summary>Triggered when player stops hovering over an item</summary>
    public static System.Action<ChestItemData, UnifiedSlot> OnItemHoverEnd;
    
    /// <summary>Triggered when player uses/activates an item</summary>
    public static System.Action<ChestItemData> OnItemUsed;
    
    /// <summary>Triggered when player acquires a new item type for the first time</summary>
    public static System.Action<ChestItemData> OnNewItemTypeDiscovered;
    
    // === HEALTH & SURVIVAL EVENTS ===
    /// <summary>Triggered when player health changes (eventType: "damage_taken", "health_restored", "critical_health")</summary>
    public static System.Action<string, float> OnPlayerHealthChanged;
    
    /// <summary>Triggered when player dies</summary>
    public static System.Action<string> OnPlayerDeath; // Death cause as string
    
    /// <summary>Triggered when player is revived</summary>
    public static System.Action OnPlayerRevived;
    
    // === MOVEMENT & NAVIGATION EVENTS ===
    /// <summary>Triggered when player state changes (walking, flying, sliding, etc.)</summary>
    public static System.Action<string> OnPlayerStateChanged;
    
    /// <summary>Triggered for notable movement achievements</summary>
    public static System.Action<string, Vector3> OnMovementAchievement; // Type and position
    
    /// <summary>Triggered when player discovers a new area</summary>
    public static System.Action<string> OnAreaDiscovered;
    
    // === INVENTORY & UI EVENTS ===
    /// <summary>Triggered when inventory UI is opened</summary>
    public static System.Action OnInventoryOpened;
    
    /// <summary>Triggered when inventory UI is closed</summary>
    public static System.Action OnInventoryClosed;
    
    // === COMBAT EVENTS ===
    /// <summary>Triggered for combat-related events (enemy_spotted, enemy_defeated, combat_start, etc.)</summary>
    public static System.Action<string, Vector3> OnCombatEvent;
    
    /// <summary>Triggered when player takes damage</summary>
    public static System.Action<float, string> OnDamageTaken; // Amount and damage type
    
    /// <summary>Triggered when player defeats an enemy</summary>
    public static System.Action<GameObject> OnEnemyDefeated;
    
    // === PROGRESSION EVENTS ===
    /// <summary>Triggered when player levels up or gains significant XP</summary>
    public static System.Action<int, int> OnProgressionMilestone; // New level, total XP
    
    /// <summary>Triggered when player achieves a gameplay milestone</summary>
    public static System.Action<string> OnAchievementUnlocked;
    
    // === INTERACTION EVENTS ===
    /// <summary>Triggered when player interacts with world objects</summary>
    public static System.Action<string, GameObject> OnWorldInteraction; // Interaction type, object
    
    /// <summary>Triggered when player uses a powerup</summary>
    public static System.Action<string> OnPowerUpActivated; // PowerUp type
    
    /// <summary>Triggered when powerup effect ends</summary>
    public static System.Action<string> OnPowerUpDeactivated; // PowerUp type
    
    // === PERFORMANCE ANALYSIS EVENTS ===
    /// <summary>Triggered when player performs an action that can be analyzed for efficiency</summary>
    public static System.Action<string, float> OnPerformanceEvent; // Action type, efficiency score
    
    /// <summary>Triggered when player makes strategic decisions</summary>
    public static System.Action<string, object[]> OnStrategicDecision; // Decision type, context data
    
    // === ENVIRONMENTAL EVENTS ===
    /// <summary>Triggered when environmental conditions change</summary>
    public static System.Action<string, float> OnEnvironmentalChange; // Change type, intensity
    
    /// <summary>Triggered when player encounters environmental hazards</summary>
    public static System.Action<string, Vector3> OnHazardEncounter; // Hazard type, position
    
    // === SYSTEM EVENTS ===
    /// <summary>Triggered when game systems need cognitive attention</summary>
    public static System.Action<string, string> OnSystemAlert; // System name, alert message
    
    /// <summary>Triggered when debug information should be displayed</summary>
    public static System.Action<string> OnDebugMessage;
    
    // === HELPER METHODS ===
    
    /// <summary>
    /// Trigger a generic cognitive event with custom data
    /// </summary>
    public static void TriggerCustomEvent(string eventType, object data = null)
    {
        OnSystemAlert?.Invoke("CustomEvent", $"{eventType}:{data}");
    }
    
    /// <summary>
    /// Trigger a performance analysis event
    /// </summary>
    public static void TriggerPerformanceAnalysis(string actionType, float efficiency)
    {
        OnPerformanceEvent?.Invoke(actionType, efficiency);
    }
    
    /// <summary>
    /// Trigger a health-related cognitive event
    /// </summary>
    public static void TriggerHealthEvent(string eventType, float healthPercentage)
    {
        OnPlayerHealthChanged?.Invoke(eventType, healthPercentage);
    }
    
    /// <summary>
    /// Trigger a combat analysis event
    /// </summary>
    public static void TriggerCombatAnalysis(string combatType, Vector3 position, object additionalData = null)
    {
        OnCombatEvent?.Invoke(combatType, position);
        if (additionalData != null)
        {
            OnStrategicDecision?.Invoke("combat_strategy", new[] { combatType, additionalData });
        }
    }
}