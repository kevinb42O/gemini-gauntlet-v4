using UnityEngine;
using GeminiGauntlet.Progression;
using GeminiGauntlet.UI;

/// <summary>
/// 🎪 AERIAL TRICK XP SYSTEM - EXTRAORDINARY EDITION
/// Rewards sick tricks with escalating XP and mind-blowing visual feedback!
/// The longer you're in the air, the more you spin, the MORE XP YOU GET!
/// </summary>
public class AerialTrickXPSystem : MonoBehaviour
{
    public static AerialTrickXPSystem Instance { get; private set; }
    
    [Header("=== XP CALCULATION ===")]
    [SerializeField] private int baseAirXP = 10; // Base XP for any air trick
    [SerializeField] private float xpPerSecondAirtime = 5f; // XP per second in air
    [SerializeField] private float xpPer360Rotation = 15f; // XP per full rotation (any axis)
    [SerializeField] private float comboMultiplier = 1.5f; // Multiplier for multi-axis tricks
    [SerializeField] private int perfectLandingBonus = 50; // Bonus for clean landing
    
    [Header("=== TRICK THRESHOLDS ===")]
    [SerializeField] private float minAirtimeForXP = 0.5f; // Minimum airtime to get XP
    [SerializeField] private float minRotationForTrick = 180f; // Minimum rotation to count as trick
    [SerializeField] private float cleanLandingThreshold = 25f; // Max deviation for "perfect" landing
    
    [Header("=== VISUAL SETTINGS ===")]
    [SerializeField] private float spawnDistanceInFront = 3000f;
    [SerializeField] private float textSizeMultiplier = 6f; // Tricks are BIGGER than wall jumps!
    [Tooltip("Delay before showing text (lets camera stabilize after landing)")]
    [SerializeField] private float textDisplayDelay = 0.15f; // Small delay for camera stabilization
    [Tooltip("How long the XP text stays visible (seconds)")]
    [SerializeField] private float textDisplayDuration = 3f; // Duration XP text is visible
    
    [Header("=== AAA SCI-FI COLORS ===")]
    [SerializeField] private Color smallTrickColor = new Color(0.5f, 1f, 0.5f); // Light Green
    [SerializeField] private Color mediumTrickColor = new Color(0f, 1f, 1f); // Cyan
    [SerializeField] private Color bigTrickColor = new Color(1f, 0.5f, 1f); // Neon Pink
    [SerializeField] private Color insaneTrickColor = new Color(1f, 0.3f, 0f); // Hot Orange
    [SerializeField] private Color godlikeTrickColor = new Color(1f, 0f, 0.5f); // Hot Pink
    [SerializeField] private Color perfectLandingColor = new Color(1f, 0.8f, 0f); // Gold
    
    [Header("=== AUDIO ===")]
    [SerializeField] private bool enableAudio = true; // Enable trick landing sounds
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Session stats
    private int totalTricksLanded = 0;
    private int totalXPFromTricks = 0;
    private int biggestTrickXP = 0;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (showDebugLogs) Debug.Log("[TrickXP] Instance created");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Call this when player lands from a trick
    /// </summary>
    public void OnTrickLanded(float airtime, Vector3 rotations, Vector3 landingPosition, bool isPerfectLanding)
    {
        // Check minimum airtime
        if (airtime < minAirtimeForXP)
        {
            if (showDebugLogs) Debug.Log($"[TrickXP] Airtime too short: {airtime:F2}s");
            return;
        }
        
        // Calculate total rotation (all axes)
        float totalRotation = Mathf.Abs(rotations.x) + Mathf.Abs(rotations.y) + Mathf.Abs(rotations.z);
        
        // Check minimum rotation
        if (totalRotation < minRotationForTrick)
        {
            if (showDebugLogs) Debug.Log($"[TrickXP] Rotation too small: {totalRotation:F0}°");
            return;
        }
        
        // Calculate XP components
        int airtimeXP = Mathf.RoundToInt(airtime * xpPerSecondAirtime);
        int rotationXP = Mathf.RoundToInt((totalRotation / 360f) * xpPer360Rotation);
        
        // Combo multiplier if rotating on multiple axes
        int axesUsed = 0;
        if (Mathf.Abs(rotations.x) > minRotationForTrick) axesUsed++;
        if (Mathf.Abs(rotations.y) > minRotationForTrick) axesUsed++;
        if (Mathf.Abs(rotations.z) > minRotationForTrick) axesUsed++;
        
        float comboMult = axesUsed > 1 ? Mathf.Pow(comboMultiplier, axesUsed - 1) : 1f;
        
        // Total XP (before combo multiplier!)
        int baseXP = Mathf.RoundToInt((baseAirXP + airtimeXP + rotationXP) * comboMult);
        
        // Perfect landing bonus
        if (isPerfectLanding)
        {
            baseXP += perfectLandingBonus;
        }
        
        // 🔥 COMBO MULTIPLIER SYSTEM!
        float xpComboMultiplier = 1f;
        bool hasCombo = false;
        if (ComboMultiplierSystem.Instance != null)
        {
            // Add this trick to the combo
            float trickAwesomeness = Mathf.Max(1f, totalRotation / 360f); // Bigger tricks = more combo value
            ComboMultiplierSystem.Instance.AddTrick(trickAwesomeness);
            
            // Get the multiplier
            xpComboMultiplier = ComboMultiplierSystem.Instance.GetMultiplier();
            hasCombo = xpComboMultiplier > 1f;
        }
        
        // Apply combo multiplier to XP!
        int totalXP = Mathf.RoundToInt(baseXP * xpComboMultiplier);
        
        // Grant XP
        if (XPManager.Instance != null)
        {
            string trickName = GetTrickName(totalRotation, airtime, axesUsed);
            if (hasCombo)
            {
                trickName += $" [COMBO x{xpComboMultiplier:F1}!]";
            }
            XPManager.Instance.GrantXP(totalXP, "Aerial Tricks", trickName);
        }
        
        // Update stats
        totalTricksLanded++;
        totalXPFromTricks += totalXP;
        if (totalXP > biggestTrickXP) biggestTrickXP = totalXP;
        
        // Show visual feedback with delay (lets camera stabilize after landing)
        StartCoroutine(ShowTrickFeedbackDelayed(landingPosition, totalXP, airtime, rotations, axesUsed, isPerfectLanding, xpComboMultiplier, textDisplayDelay));
        
        // 🎵 PLAY TRICK LANDING SOUNDS!
        if (enableAudio)
        {
            int fullRotations = Mathf.FloorToInt(totalRotation / 360f);
            GeminiGauntlet.Audio.GameSounds.PlayTrickLandingSound(landingPosition, airtime, fullRotations, isPerfectLanding, 1.0f);
            
            // Play combo sound if multiplier is active!
            if (hasCombo && xpComboMultiplier > 1.5f)
            {
                GeminiGauntlet.Audio.GameSounds.PlayComboMultiplierSound(landingPosition, xpComboMultiplier, 1.0f);
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[TrickXP] 🎪 TRICK LANDED! Airtime: {airtime:F1}s, Rotation: {totalRotation:F0}°, Axes: {axesUsed}, Base XP: {baseXP}, Combo: x{xpComboMultiplier:F1}, Total XP: {totalXP}");
        }
    }
    
    /// <summary>
    /// Generate trick name based on performance
    /// </summary>
    private string GetTrickName(float totalRotation, float airtime, int axesUsed)
    {
        // Count full rotations
        int fullRotations = Mathf.FloorToInt(totalRotation / 360f);
        
        // Determine trick type
        string trickType = "";
        if (axesUsed >= 3)
        {
            trickType = "CHAOS SPIN";
        }
        else if (axesUsed == 2)
        {
            trickType = "COMBO FLIP";
        }
        else
        {
            trickType = fullRotations >= 2 ? "MULTI-FLIP" : "FLIP";
        }
        
        // Determine size
        string size = "";
        if (airtime > 3f || fullRotations >= 4)
        {
            size = "GODLIKE ";
        }
        else if (airtime > 2f || fullRotations >= 3)
        {
            size = "INSANE ";
        }
        else if (airtime > 1.5f || fullRotations >= 2)
        {
            size = "BIG ";
        }
        
        return $"{size}{trickType} x{fullRotations}";
    }
    
    /// <summary>
    /// Coroutine to show feedback with delay (camera stabilization)
    /// </summary>
    private System.Collections.IEnumerator ShowTrickFeedbackDelayed(Vector3 position, int xpEarned, float airtime, Vector3 rotations, int axesUsed, bool isPerfect, float comboMult, float delay)
    {
        // Wait for camera to stabilize after landing
        yield return new WaitForSeconds(delay);
        
        // Now show the feedback with stable camera position
        ShowTrickFeedback(position, xpEarned, airtime, rotations, axesUsed, isPerfect, comboMult);
    }
    
    /// <summary>
    /// Show EXTRAORDINARY visual feedback
    /// </summary>
    private void ShowTrickFeedback(Vector3 position, int xpEarned, float airtime, Vector3 rotations, int axesUsed, bool isPerfect, float comboMult = 1f)
    {
        if (FloatingTextManager.Instance == null) return;
        
        // Spawn position (in front of player)
        Vector3 spawnPosition = position;
        if (Camera.main != null)
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            if (cameraForward.sqrMagnitude > 0.01f)
            {
                cameraForward.Normalize();
                spawnPosition += cameraForward * spawnDistanceInFront;
            }
        }
        
        // Build trick description
        float totalRotation = Mathf.Abs(rotations.x) + Mathf.Abs(rotations.y) + Mathf.Abs(rotations.z);
        int fullRotations = Mathf.FloorToInt(totalRotation / 360f);
        
        string trickText = BuildTrickText(airtime, fullRotations, axesUsed, isPerfect, xpEarned, comboMult);
        
        // Get color based on trick size
        Color color = GetTrickColor(airtime, fullRotations, isPerfect);
        
        // Calculate font size (bigger for better tricks!)
        int baseFontSize = 60 + (fullRotations * 15) + Mathf.RoundToInt(airtime * 10);
        
        // 🔥 BOOST SIZE FOR COMBOS! (makes them feel EPIC!)
        if (comboMult > 1f)
        {
            float comboBoost = 1f + (comboMult - 1f) * 0.15f; // +15% per multiplier point
            baseFontSize = Mathf.RoundToInt(baseFontSize * comboBoost);
        }
        
        int fontSize = Mathf.RoundToInt(baseFontSize * textSizeMultiplier);
        fontSize = Mathf.Min(fontSize, 1500); // Higher cap for epic combos!
        
        // Show it with TRICKS style (bold italic, extraordinary!) with custom duration
        FloatingTextManager.Instance.ShowFloatingText(trickText, spawnPosition, color, fontSize, lockRotation: true, style: FloatingTextManager.TextStyle.Tricks, customDuration: textDisplayDuration);
    }
    
    /// <summary>
    /// Build extraordinary trick text with EPIC combo breakdown!
    /// </summary>
    private string BuildTrickText(float airtime, int rotations, int axes, bool isPerfect, int xp, float comboMult = 1f)
    {
        string text = "";
        
        // Title based on performance
        if (airtime > 3f || rotations >= 4)
        {
            text += ">> GODLIKE TRICK! <<\n";
        }
        else if (airtime > 2f || rotations >= 3)
        {
            text += ">> INSANE TRICK! <<\n";
        }
        else if (airtime > 1.5f || rotations >= 2)
        {
            text += ">> BIG TRICK! <<\n";
        }
        else
        {
            text += ">> NICE TRICK! <<\n";
        }
        
        // Stats
        text += $"{airtime:F1}s AIRTIME\n";
        text += $"{rotations}x ROTATIONS\n";
        
        if (axes > 1)
        {
            text += $"{axes}-AXIS COMBO!\n";
        }
        
        if (isPerfect)
        {
            text += "** PERFECT LANDING! **\n";
        }
        
        // 🔥 ULTIMATE COMBO BREAKDOWN!
        if (comboMult > 1f && ComboMultiplierSystem.Instance != null)
        {
            var comboInfo = ComboMultiplierSystem.Instance.GetComboInfo();
            int wallJumps = comboInfo.wallJumps;
            int tricks = comboInfo.tricks;
            float points = comboInfo.points;
            
            // Color based on multiplier intensity!
            string comboColor = GetComboColor(comboMult);
            
            text += $"\n<color={comboColor}>╔═══════════════════╗</color>\n";
            text += $"<color={comboColor}>║  *** COMBO CHAIN ***  ║</color>\n";
            text += $"<color={comboColor}>╠═══════════════════╣</color>\n";
            
            // Show what contributed to the combo
            if (wallJumps > 0)
            {
                text += $"<color={comboColor}>║ >> {wallJumps}× Wall Jump{(wallJumps > 1 ? "s" : "")}  ║</color>\n";
            }
            if (tricks > 1) // More than 1 because current trick is included
            {
                text += $"<color={comboColor}>║ >> {tricks}× Trick{(tricks > 1 ? "s" : "")}      ║</color>\n";
            }
            
            text += $"<color={comboColor}>╠═══════════════════╣</color>\n";
            text += $"<color={comboColor}>║ MULTIPLIER: x{comboMult:F1}  ║</color>\n";
            text += $"<color={comboColor}>╚═══════════════════╝</color>\n";
        }
        
        // XP (bigger if combo!)
        string xpColor = comboMult > 1f ? "#00FF00" : "#FFD700";
        text += $"\n<color={xpColor}>+{xp} XP</color>";
        
        return text;
    }
    
    /// <summary>
    /// Get color for combo display based on multiplier intensity
    /// </summary>
    private string GetComboColor(float multiplier)
    {
        if (multiplier >= 5f) return "#FF00FF"; // Magenta - GODLIKE
        if (multiplier >= 4f) return "#FF0080"; // Hot Pink - INSANE
        if (multiplier >= 3f) return "#FF3300"; // Red-Orange - EPIC
        if (multiplier >= 2f) return "#FF6600"; // Orange - BIG
        return "#FFAA00"; // Yellow-Orange - GOOD
    }
    
    /// <summary>
    /// Get color based on trick awesomeness
    /// </summary>
    private Color GetTrickColor(float airtime, int rotations, bool isPerfect)
    {
        if (isPerfect)
        {
            return perfectLandingColor; // Gold for perfect!
        }
        else if (airtime > 3f || rotations >= 4)
        {
            return godlikeTrickColor; // Hot pink for godlike
        }
        else if (airtime > 2f || rotations >= 3)
        {
            return insaneTrickColor; // Hot orange for insane
        }
        else if (airtime > 1.5f || rotations >= 2)
        {
            return bigTrickColor; // Neon pink for big
        }
        else if (airtime > 1f || rotations >= 1)
        {
            return mediumTrickColor; // Cyan for medium
        }
        else
        {
            return smallTrickColor; // Light green for small
        }
    }
    
    /// <summary>
    /// Get session stats
    /// </summary>
    public (int tricks, int xp, int biggest) GetSessionStats()
    {
        return (totalTricksLanded, totalXPFromTricks, biggestTrickXP);
    }
    
    /// <summary>
    /// Reset session
    /// </summary>
    public void ResetSession()
    {
        totalTricksLanded = 0;
        totalXPFromTricks = 0;
        biggestTrickXP = 0;
        if (showDebugLogs) Debug.Log("[TrickXP] Session reset");
    }
}
