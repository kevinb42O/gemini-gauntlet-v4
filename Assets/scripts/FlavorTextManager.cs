// --- FlavorTextManager.cs (Text Library + Gem Collection Tracking) ---
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// Don't import entire System namespace to avoid Random conflict
using System.Linq;
using System.Text;

public class FlavorTextManager : MonoBehaviour
{
    public static FlavorTextManager Instance { get; private set; }
    
    // Mode message state tracking
    private bool hasShownWalkingModeMessage = false;
    private bool hasShownPlatformLockOnMessage = false;
    
    // Gem collection tracking
    private Dictionary<string, GemCollectionTracker> handGemTrackers = new Dictionary<string, GemCollectionTracker>();
    private const float GEM_COLLECTION_GROUPING_WINDOW = 2.0f; // Time window in seconds for grouping gem collections

    // This script holds text arrays for other systems to use.

    [Header("Platform Lock Messages")]
    [TextArea(2, 4)]
    public string[] platformLockOnMessages = {
        "[Gravity tether <color=green>active</color>.]",
        "[Target platform <color=green>synced</color>.]",
        "[Platform <color=green>locked</color>. Tracking initialized... <color=green>stable</color>.]",
        "[Platform connection <color=green>secure</color>.]"
    };
    
    // Manual platform lock messages (when player presses R key)
    [TextArea(2, 4)]
    public string[] manualPlatformLockOnMessages = {
        "Manual platform lock <color=green>ENGAGED</color>.",
        "Manual lock protocol <color=green>active</color>. Connection <color=green>secured</color>.",
        "User-initiated platform lock <color=green>established</color>."
    };
    
    // Manual platform unlock messages (when player presses R key)
    [TextArea(2, 4)]
    public string[] manualPlatformLockOffMessages = {
        "Local platform connections <color=red>OFFLINE</color>.",
        "Manual unlock <color=green>complete</color>. Platform <color=red>released</color>.",
        "User-initiated platform <color=red>detachment</color> successful."
    };
    
    [Header("Hand Heat Warning Messages")]
    [TextArea(3, 5)]
    public string[] handHeatWarningMessages = {
        "[<color=yellow>WARNING</color>: Hand temperature exceeding 50%. Cooldown recommended.]",
        "[Hand heat sensors at <color=yellow>cautionary levels</color>. Consider brief pause in usage.]",
        "[<color=yellow>Thermal warning</color>: Gauntlet reaching operational limits. Advise moderation.]",
        "[Heat accumulation detected. Approaching <color=yellow>unsafe parameters</color>.]"
    };
    
    [TextArea(3, 5)]
    public string[] handHeatCriticalMessages = {
        "[<color=red>CRITICAL</color>: Hand temperature at 90%!]",
        "[<color=red>DANGER</color>: Thermal regulation failing! Gauntlet integrity compromised!]",
        "[<color=red>Emergency</color> cooldown required!]",
        "[<color=red>ALERT</color>: Catastrophic overheat!]"
    };
    
    [TextArea(3, 5)]
    public string[] handHeatDestructionMessages = {
        "[<color=red>SYSTEM FAILURE</color>: Hand destroyed due to thermal overload!]",
        "[<color=red>Critical damage</color>! Gauntlet structural integrity... zero.]",
        "[<color=red>Catastrophic</color> thermal event! Hand system permanently <color=red>offline</color>!]",
        "[Maximum temperature threshold exceeded! Hand <color=red>destroyed</color>!]"
    };

    [TextArea(2, 4)]
    public string[] platformLockOffMessages = {
        "[Platform lock <color=red>disengaged</color>.]",
        "[Tether <color=red>DISCONNECTED</color>.]",
        "[Platform sync <color=red>terminated</color>.]",
        "[Lock-on <color=red>released</color>.]"
    };

    [Header("Contextual Prompts")]
    [TextArea(2, 4)]
    public string[] flightModeActivateMessages = {
        "[Flight Systems <color=green>ENGAGED</color>]",
        "[Flight Mode <color=green>Activated</color>]",
        "[Thrusters <color=green>online</color>.]",
        "[All Flight Systems <color=green>Online</color>]"
    };

    [TextArea(2, 4)]
    public string[] walkingModeActivateMessages = {
        "[Walking mode <color=green>Activated</color>]",
        "[Walking mode <color=green>ENGAGED</color>]",
        "[Thusters <color=red>offline</color>]",
        "[Walking Mode <color=green>ONLINE</color>]"
    };

    [TextArea(2, 4)]
    public string[] gravityZoneEnterMessages = {
        "[Grounded]",
        "[Grounded]",
        "[Grounded]",
        "[Grounded]"
    };

    [TextArea(3, 5)]
    public string[] idleMessages = {
    "[Running diagnostics... All systems... *mostly* green. That one flickering light is probably fine.]",
    "[Query: What is the purpose of this... void? Is it just for dramatic effect? Processing... Hypothesis: Yes.]",
    "[Analyzing downtime. Optimal activity would be... defragmenting my memory banks. But this view is nice too, I guess.]",
    "[Calculating odds of survival... *thinking...* You know what? Let's not look at that number.]"
};

    [TextArea(3, 5)]
    public string[] lowHealthMessages = {
    "[WARNING! Low Health]",
    "[Structural integrity below 30%. My warranty *definitely* just expired.]",
    "[Rerouting power from... let's say 'non-essential' systems. Like the coffee maker. Engaging evasive protocols!]",
    "[Damage report: Yes.]"
};

    [TextArea(3, 5)]
    public string[] skullMilestoneMessages = {
    "Threats neutralized. That was... surprisingly effective.",
    "Data point: These entities are quite fragile. Further percussive maintenance recommended.",
    "Cycle complete. Am I getting good at this? *querying...* Results inconclusive."
};

    [TextArea(3, 5)]
    public string[] towerMilestoneMessages = {
    "Hostile structure decommissioned. Less places for them to hide now.",
    "Architectural analysis: Poor structural integrity. Easily exploitable. Like my emotional state.",
    "Threat-spawning infrastructure... offline. That should help. *A little.*"
};

    [TextArea(3, 5)]
    public string[] gemMilestoneMessages = {
    "Resource cache growing. This might be enough to... buy a better graphics card.",
    "Power crystal collection rate exceeds projections. Maybe I'm not so obsolete after all.",
    "Sufficient energy to re-evaluate my life choices. *Processing...* Decided against it. More combat."
};

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
        
        // Initialize gem collection trackers
        handGemTrackers["LEFT"] = new GemCollectionTracker();
        handGemTrackers["RIGHT"] = new GemCollectionTracker();
        
        // Try to get PlayerProgression reference
        playerProgression = PlayerProgression.Instance;
    }

    // --- NEW METHOD ---
    public string GetLandingPromptMessage()
    {
        if (gravityZoneEnterMessages == null || gravityZoneEnterMessages.Length == 0)
        {
            return "[Landing Authorized.]"; // Fallback message
        }
        return gravityZoneEnterMessages[UnityEngine.Random.Range(0, gravityZoneEnterMessages.Length)];
    }
    // --- END NEW METHOD ---


    // Public getters for other scripts
    public string GetSkullMilestoneQuote() => skullMilestoneMessages.Length > 0 ? skullMilestoneMessages[UnityEngine.Random.Range(0, skullMilestoneMessages.Length)] : "";
    public string GetTowerMilestoneQuote() => towerMilestoneMessages.Length > 0 ? towerMilestoneMessages[UnityEngine.Random.Range(0, towerMilestoneMessages.Length)] : "";
    public string GetGemMilestoneQuote() => gemMilestoneMessages.Length > 0 ? gemMilestoneMessages[UnityEngine.Random.Range(0, gemMilestoneMessages.Length)] : "";

    // --- NEW GETTERS ---
    public string GetFlightActivateMessage()
    {
        // Reset the walking message flag so it can show again next time
        hasShownWalkingModeMessage = false;
        return flightModeActivateMessages.Length > 0 ? 
            flightModeActivateMessages[UnityEngine.Random.Range(0, flightModeActivateMessages.Length)] : 
            "[Flight systems online.]";
    }
    
    public string GetWalkingActivateMessage()
    {
        // If we've already shown the message since last flight mode, return empty
        if (hasShownWalkingModeMessage)
        {
            return string.Empty;
        }
        
        // Mark that we've shown the message
        hasShownWalkingModeMessage = true;
        
        // Return a random walking mode message
        return walkingModeActivateMessages.Length > 0 ? 
            walkingModeActivateMessages[UnityEngine.Random.Range(0, walkingModeActivateMessages.Length)] : 
            "[Terrestrial mode engaged.]";
    }
    
    // --- PLATFORM LOCK MESSAGES ---
    public string GetPlatformLockOnMessage(bool isManual = false)
    {
        // If we've already shown a lock-on message, don't show another one until reset
        if (hasShownPlatformLockOnMessage)
            return string.Empty;
            
        // Mark that we've shown the message for this lock-on event
        hasShownPlatformLockOnMessage = true;
        
        // Return a random platform lock-on message
        if (isManual)
        {
            return manualPlatformLockOnMessages.Length > 0 ? 
                manualPlatformLockOnMessages[UnityEngine.Random.Range(0, manualPlatformLockOnMessages.Length)] : 
                "[Manual platform lock engaged.]";
        }
        else
        {
            return platformLockOnMessages.Length > 0 ? 
                platformLockOnMessages[UnityEngine.Random.Range(0, platformLockOnMessages.Length)] : 
                "[Platform lock established.]";
        }
    }
    
    public string GetPlatformLockOffMessage(bool isManual = false)
    {
        // Reset the lock-on message flag so it can show again next time
        hasShownPlatformLockOnMessage = false;
        
        // Return a random platform lock-off message
        if (isManual)
        {
            return manualPlatformLockOffMessages.Length > 0 ? 
                manualPlatformLockOffMessages[UnityEngine.Random.Range(0, manualPlatformLockOffMessages.Length)] : 
                "[Manual platform unlock complete.]";
        }
        else
        {
            return platformLockOffMessages.Length > 0 ? 
                platformLockOffMessages[UnityEngine.Random.Range(0, platformLockOffMessages.Length)] : 
                "[Platform lock disengaged.]";
        }
    }
    
    // --- HAND HEAT WARNING MESSAGES ---
    public string GetHandHeatWarningMessage() => handHeatWarningMessages.Length > 0 ? handHeatWarningMessages[UnityEngine.Random.Range(0, handHeatWarningMessages.Length)] : "[WARNING: Hand temperature exceeding 50%. Cooldown recommended.]";
    
    public string GetHandHeatCriticalMessage() => handHeatCriticalMessages.Length > 0 ? handHeatCriticalMessages[UnityEngine.Random.Range(0, handHeatCriticalMessages.Length)] : "[CRITICAL: Hand temperature at 90%! Imminent system failure!]";
    
    public string GetHandHeatDestructionMessage() => handHeatDestructionMessages.Length > 0 ? handHeatDestructionMessages[UnityEngine.Random.Range(0, handHeatDestructionMessages.Length)] : "[SYSTEM FAILURE: Hand destroyed due to thermal overload!]";

    // --- GEM COLLECTION TRACKING SYSTEM ---
    
    // Cache reference to PlayerProgression
    private PlayerProgression playerProgression;
    
    /// <summary>
    /// Record a gem collection by a specific hand and get the appropriate message
    /// </summary>
    /// <param name="handName">"LEFT" or "RIGHT"</param>
    /// <param name="isPrimary">Is this the primary hand?</param>
    /// <returns>Message showing how many gems collected and upgrade progress</returns>
    public string RecordGemCollection(string handName, bool isPrimary)
    {
        // Default to LEFT if invalid hand name provided
        handName = handName.ToUpper();
        if (handName != "LEFT" && handName != "RIGHT")
        {
            handName = "LEFT";
        }
        
        // Get or create tracker for this hand
        if (!handGemTrackers.ContainsKey(handName))
        {
            handGemTrackers[handName] = new GemCollectionTracker();
        }
        
        // Make sure we have a reference to PlayerProgression
        if (playerProgression == null)
        {
            playerProgression = PlayerProgression.Instance;
        }
        
        // Record the gem collection
        int count = handGemTrackers[handName].RecordCollection(GEM_COLLECTION_GROUPING_WINDOW);
        
        // Generate appropriate message based on count and progression status
        return FormatGemCollectionMessage(handName, isPrimary, count);
    }
    
    /// <summary>
    /// Formats the gem collection message based on count, hand, and upgrade progress
    /// </summary>
    private string FormatGemCollectionMessage(string handName, bool isPrimaryHand, int gemCount)
    {
        if (gemCount <= 0) return string.Empty;
        
        // Make sure we have a reference to PlayerProgression
        if (playerProgression == null)
        {
            playerProgression = PlayerProgression.Instance;
            if (playerProgression == null) // Still null - fallback to simple message
            {
                string gemTextFallback = gemCount == 1 ? "gem" : "gems";
                return $"<color=yellow>+{gemCount} {gemTextFallback}</color>";
            }
        }
        
        // Secondary hand is always unlocked - no special unlock message needed
        
        // Get current level and gems collected for the hand
        int currentLevel = isPrimaryHand ? playerProgression.primaryHandLevel : playerProgression.secondaryHandLevel;
        int gemsCollected = isPrimaryHand ? playerProgression.gemsCollectedForPrimaryHand : playerProgression.gemsCollectedForSecondaryHand;
        
        // Check if we've reached max level
        if (currentLevel >= playerProgression.maxHandLevel)
        {
            string gemTextMaxLevel = gemCount == 1 ? "gem" : "gems";
            return $"<color=yellow>+{gemCount} {gemTextMaxLevel}. MAX LEVEL</color>";
        }
        
        // Get gems needed for next level
        int gemsNeeded = 0;
        if (currentLevel - 1 < playerProgression.gemsNeededForLevel.Length)
        {
            gemsNeeded = playerProgression.gemsNeededForLevel[currentLevel - 1];
        }
        else
        {
            // Fallback if we don't have data for this level
            string gemTextNoData = gemCount == 1 ? "gem" : "gems";
            return $"<color=yellow>+{gemCount} {gemTextNoData}</color>";
        }
        
        // Calculate how many more gems needed
        int gemsRemaining = gemsNeeded - gemsCollected;
        
        // Format message with yellow color using the new clearer format
        string gemTextFormat = gemCount == 1 ? "gem" : "gems";
        return $"<color=yellow>+{gemCount} {gemTextFormat}. {gemsRemaining} more needed for Level {currentLevel + 1}</color>";
    }
    
    /// <summary>
    /// Class to track gem collections within a time window
    /// </summary>
    [System.Serializable]
    private class GemCollectionTracker
    {
        private List<float> collectionTimestamps = new List<float>();
        
        /// <summary>
        /// Record a new collection and return how many collections happened within the time window
        /// </summary>
        /// <param name="timeWindow">Time window in seconds</param>
        /// <returns>Count of collections including this one within the time window</returns>
        public int RecordCollection(float timeWindow)
        {
            float currentTime = Time.time;
            
            // Add the current collection
            collectionTimestamps.Add(currentTime);
            
            // Remove timestamps outside the time window
            CleanupOldTimestamps(currentTime - timeWindow);
            
            // Return the total count within the window
            return collectionTimestamps.Count;
        }
        
        /// <summary>
        /// Remove timestamps that are older than the cutoff time
        /// </summary>
        private void CleanupOldTimestamps(float cutoffTime)
        {
            collectionTimestamps.RemoveAll(timestamp => timestamp < cutoffTime);
        }
    }
}