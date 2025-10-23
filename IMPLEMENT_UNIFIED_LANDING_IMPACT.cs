// ════════════════════════════════════════════════════════════════════════════
// 🎯 UNIFIED LANDING IMPACT SYSTEM - IMPLEMENTATION CODE
// ════════════════════════════════════════════════════════════════════════════
// 
// This file contains the EXACT code changes needed to unify your landing system.
// Copy the relevant sections into AAACameraController.cs
//
// Author: Claude Sonnet 4.5
// Date: 2025-10-23
// Complexity: Low (just event subscription)
// Time: 5 minutes
// ════════════════════════════════════════════════════════════════════════════

// ════════════════════════════════════════════════════════════════════════════
// CHANGE 1: Add Event Subscription in Start()
// ════════════════════════════════════════════════════════════════════════════
// Location: AAACameraController.cs, Start() method (around line 441)
// Action: ADD this code at the END of Start(), after existing initialization

void Start()
{
    // ... existing Start() code ...
    
    // Lock cursor
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
    
    // 🎯 UNIFIED IMPACT SYSTEM: Subscribe to impact events
    // This makes the camera listen to FallingDamageSystem instead of tracking falls independently
    ImpactEventBroadcaster.OnImpact += OnImpactReceived;
    Debug.Log("[AAACameraController] ✅ Subscribed to unified impact events");
}

// ════════════════════════════════════════════════════════════════════════════
// CHANGE 2: Add Cleanup in OnDestroy()
// ════════════════════════════════════════════════════════════════════════════
// Location: AAACameraController.cs, after OnDisable() method (around line 527)
// Action: ADD this new method (or update existing OnDestroy if present)

void OnDestroy()
{
    // 🎯 UNIFIED IMPACT SYSTEM: Unsubscribe from impact events
    // CRITICAL: Prevents memory leaks and null reference errors
    ImpactEventBroadcaster.OnImpact -= OnImpactReceived;
    
    // Also unsubscribe from sprint interruption event (if not already done)
    PlayerEnergySystem.OnSprintInterrupted -= OnSprintInterrupted;
    
    Debug.Log("[AAACameraController] Unsubscribed from all events");
}

// ════════════════════════════════════════════════════════════════════════════
// CHANGE 3: Add Impact Handler Method
// ════════════════════════════════════════════════════════════════════════════
// Location: AAACameraController.cs, after UpdateLandingImpact() method (around line 803)
// Action: ADD this new method

/// <summary>
/// 🎯 UNIFIED IMPACT SYSTEM - Handle impact events from FallingDamageSystem
/// 
/// This method is called automatically when the player lands.
/// It receives comprehensive impact data from the unified system and applies
/// camera compression based on the calculated severity.
/// 
/// Benefits:
/// - No duplicate fall tracking (FallingDamageSystem handles it)
/// - Consistent thresholds across all systems
/// - Works for ALL jump types (tiny, light, moderate, severe, lethal)
/// - Integrated with aerial tricks, sprint, and other context
/// 
/// Called by: ImpactEventBroadcaster when FallingDamageSystem detects landing
/// </summary>
/// <param name="impact">Comprehensive impact data from unified system</param>
private void OnImpactReceived(ImpactData impact)
{
    // Early exit if landing impact is disabled
    if (!enableLandingImpact) return;
    
    // Skip if no compression needed (impact too small)
    // This happens for falls < 50 units (like stepping off a curb)
    if (impact.compressionAmount <= 0f) return;
    
    // Use compression amount from unified system
    // This value is already scaled by severity tier (Light/Moderate/Severe/Lethal)
    // and calculated using the same thresholds as damage system
    float compressionAmount = -impact.compressionAmount;
    
    // Apply instant compression (knee bend) - this is the "impact" moment
    // The spring physics in UpdateLandingImpact() will smoothly recover from this
    landingCompressionOffset = compressionAmount;
    
    // Set initial downward velocity for spring
    // This makes the impact feel more "punchy" - camera compresses fast, recovers slow
    // Multiplier of 2.0 gives good impact feel without being too jarring
    landingCompressionVelocity = compressionAmount * 2f;
    
    // Apply forward tilt for extra realism (optional)
    // This simulates the player's body leaning forward on impact
    // Scaled by severity: tiny jumps = subtle tilt, big falls = dramatic tilt
    if (enableLandingTilt)
    {
        landingTiltOffset = maxLandingTiltAngle * impact.severityNormalized;
        landingTiltVelocity = landingTiltOffset * 1.5f;
    }
    
    // Reset dynamic wall tilt on landing
    // Prevents weird tilt carryover from wall jumps/tricks
    dynamicWallTiltTarget = 0f;
    dynamicWallTilt = 0f;
    
    // Debug log for tuning (disable in production)
    // Shows: severity tier, fall distance, compression amount, trauma intensity
    Debug.Log($"[CAMERA IMPACT] 🎯 {impact.severity} | " +
              $"Fall: {impact.fallDistance:F0}u | " +
              $"Compression: {compressionAmount:F1} | " +
              $"Trauma: {impact.traumaIntensity:F2} | " +
              $"Trick: {(impact.wasInTrick ? "YES" : "NO")}");
}

// ════════════════════════════════════════════════════════════════════════════
// CHANGE 4 (OPTIONAL): Simplify UpdateLandingImpact()
// ════════════════════════════════════════════════════════════════════════════
// Location: AAACameraController.cs, UpdateLandingImpact() method (around line 699)
// Action: REPLACE the entire method with this simplified version
// 
// NOTE: Only do this AFTER verifying the new system works!
// This removes the old manual fall tracking since we now use events.

private void UpdateLandingImpact()
{
    // Early exit if landing impact is disabled
    if (!enableLandingImpact || movementController == null)
    {
        // Reset all spring values to zero
        landingCompressionOffset = 0f;
        landingCompressionVelocity = 0f;
        landingTiltOffset = 0f;
        landingTiltVelocity = 0f;
        return;
    }
    
    // ════════════════════════════════════════════════════════════════════
    // REMOVED: Manual fall tracking (now handled by unified system)
    // ════════════════════════════════════════════════════════════════════
    // The old code tracked falls independently, which caused:
    // - Duplicate CPU usage
    // - Inconsistent thresholds
    // - No feedback for small jumps
    // 
    // Now: OnImpactReceived() sets landingCompressionOffset directly
    //      This method only handles the spring physics for smooth recovery
    // ════════════════════════════════════════════════════════════════════
    
    // ════════════════════════════════════════════════════════════════════
    // SPRING PHYSICS - Smooth recovery from compression
    // ════════════════════════════════════════════════════════════════════
    // This creates the smooth "knee bend and recover" feel
    // Physics: F = -k*x - c*v (Hooke's law with damping)
    //   k = stiffness (how fast it recovers)
    //   x = displacement (current compression)
    //   c = damping (prevents bouncing)
    //   v = velocity (current spring velocity)
    
    // Calculate spring force for compression
    float springForce = -landingSpringStiffness * landingCompressionOffset;
    float dampingForce = -landingSpringDamping * landingCompressionVelocity;
    float totalForce = springForce + dampingForce;
    
    // Update velocity and position using spring physics
    landingCompressionVelocity += totalForce * Time.deltaTime;
    landingCompressionOffset += landingCompressionVelocity * Time.deltaTime;
    
    // Stop spring when very close to rest (prevents infinite micro-bouncing)
    // Thresholds tuned for 320-unit player scale
    // Human knees don't bounce infinitely - they compress once and return smoothly
    if (Mathf.Abs(landingCompressionOffset) < 0.01f && Mathf.Abs(landingCompressionVelocity) < 0.1f)
    {
        landingCompressionOffset = 0f;
        landingCompressionVelocity = 0f;
    }
    
    // Same spring physics for forward tilt (optional)
    if (enableLandingTilt)
    {
        float tiltSpringForce = -landingSpringStiffness * landingTiltOffset;
        float tiltDampingForce = -landingSpringDamping * landingTiltVelocity;
        float tiltTotalForce = tiltSpringForce + tiltDampingForce;
        
        landingTiltVelocity += tiltTotalForce * Time.deltaTime;
        landingTiltOffset += landingTiltVelocity * Time.deltaTime;
        
        // Stop tilt spring when at rest
        if (Mathf.Abs(landingTiltOffset) < 0.05f && Mathf.Abs(landingTiltVelocity) < 0.5f)
        {
            landingTiltOffset = 0f;
            landingTiltVelocity = 0f;
        }
    }
}

// ════════════════════════════════════════════════════════════════════════════
// CHANGE 5 (OPTIONAL): Remove Obsolete Variables
// ════════════════════════════════════════════════════════════════════════════
// Location: AAACameraController.cs, class fields (around line 391-399)
// Action: DELETE these variables (they're no longer needed)
// 
// NOTE: Only do this AFTER implementing Change 4!

// DELETE THESE LINES:
// private bool wasGrounded = true;
// private float fallStartHeight = 0f;
// private bool isTrackingFall = false;

// ════════════════════════════════════════════════════════════════════════════
// OPTIONAL ENHANCEMENT: Add Small Jump Feedback
// ════════════════════════════════════════════════════════════════════════════
// Location: FallingDamageSystem.cs, CalculateImpactData() method (around line 463)
// Action: REPLACE the final else block with this enhanced version
// 
// This adds subtle feedback for normal jumps (50-320 units)
// Currently, jumps below 320u have NO feedback at all

// FIND THIS CODE:
/*
else
{
    // NO IMPACT (too small)
    impact.severity = ImpactSeverity.None;
    impact.severityNormalized = 0f;
    impact.damageAmount = 0f;
    impact.traumaIntensity = 0f;
    impact.compressionAmount = 0f;
}
*/

// REPLACE WITH THIS:
else if (fallDistance >= 50f) // NEW: Tiny impact threshold (normal jumps)
{
    // TINY IMPACT (normal jumps, no damage, just subtle feedback)
    // This makes even small jumps feel responsive
    impact.severity = ImpactSeverity.Light; // Reuse Light tier (or add Tiny tier)
    float t = Mathf.InverseLerp(50f, minDamageFallHeight, fallDistance);
    impact.severityNormalized = Mathf.Lerp(0.05f, 0.1f, t); // 5-10% severity
    impact.damageAmount = 0f; // No damage for normal jumps
    impact.traumaIntensity = 0f; // No trauma/screen shake
    impact.compressionAmount = landingCompressionAmount * Mathf.Lerp(0.2f, 0.4f, t); // 20-40% compression
    
    Debug.Log($"[TINY IMPACT] Fall: {fallDistance:F0}u, Compression: {impact.compressionAmount:F1}");
}
else
{
    // NO IMPACT (too small - like stepping off a curb)
    impact.severity = ImpactSeverity.None;
    impact.severityNormalized = 0f;
    impact.damageAmount = 0f;
    impact.traumaIntensity = 0f;
    impact.compressionAmount = 0f;
}

// ════════════════════════════════════════════════════════════════════════════
// TESTING CHECKLIST
// ════════════════════════════════════════════════════════════════════════════
// 
// After implementing Changes 1-3:
// [ ] Compile and run game (check for errors)
// [ ] Jump normally (50-100u) - should see debug log
// [ ] Fall from medium height (500u) - should see compression
// [ ] Fall from big height (1000u+) - should see heavy compression
// [ ] Check console for "[CAMERA IMPACT]" logs
// [ ] Verify compression values match fall distance
// 
// After implementing Change 4 (optional cleanup):
// [ ] Verify landing still works (no regression)
// [ ] Check that old fall tracking is gone (no duplicate logs)
// [ ] Test edge cases: elevator, platform, slopes
// 
// After implementing optional enhancement:
// [ ] Jump normally - should see subtle compression now!
// [ ] Verify tiny jumps don't cause damage
// [ ] Check that compression scales smoothly
// 
// ════════════════════════════════════════════════════════════════════════════
// TUNING PARAMETERS
// ════════════════════════════════════════════════════════════════════════════
// 
// If you want to adjust the feel, change these values:
// 
// In FallingDamageSystem.cs (around line 74):
//   landingCompressionAmount = 80f;  // Base compression (increase for more impact)
// 
// In AAACameraController.cs (around line 228-230):
//   landingSpringStiffness = 100f;   // Recovery speed (increase for faster)
//   landingSpringDamping = 1.5f;     // Bounce control (decrease for bouncier)
// 
// Recommended ranges:
//   compressionAmount: 60-120 (80 is good default)
//   springStiffness: 80-200 (100 is good default)
//   springDamping: 1.0-2.0 (1.5 is good default - no bounce)
// 
// ════════════════════════════════════════════════════════════════════════════
// TROUBLESHOOTING
// ════════════════════════════════════════════════════════════════════════════
// 
// Problem: "No impact events received"
// Solution: 
//   1. Check FallingDamageSystem is attached to player
//   2. Check player is actually landing (not on elevator)
//   3. Add debug log in Start() to verify subscription
//   4. Check console for FallingDamageSystem logs
// 
// Problem: "Still no feedback on small jumps"
// Solution:
//   1. Implement optional enhancement (adds tiny impact tier)
//   2. Check minAirTimeForFallDetection (should be 0.3-1.0s)
//   3. Verify fall distance is > 50 units
//   4. Add debug log in OnImpactReceived() to see actual values
// 
// Problem: "Compression feels too weak/strong"
// Solution:
//   1. Adjust landingCompressionAmount in FallingDamageSystem
//   2. Increase for more impact, decrease for less
//   3. Test with different fall heights
// 
// Problem: "Recovery feels too slow/fast"
// Solution:
//   1. Adjust landingSpringStiffness in AAACameraController
//   2. Increase for faster recovery, decrease for slower
//   3. Keep damping at 1.5 to prevent bouncing
// 
// Problem: "Camera bounces on landing"
// Solution:
//   1. Increase landingSpringDamping (1.5 → 2.0)
//   2. This adds more resistance to spring motion
//   3. Values > 1.0 prevent bouncing (critically damped)
// 
// ════════════════════════════════════════════════════════════════════════════
// END OF IMPLEMENTATION CODE
// ════════════════════════════════════════════════════════════════════════════
