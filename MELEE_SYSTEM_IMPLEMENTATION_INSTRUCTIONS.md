# PER-HAND MELEE SYSTEM - IMPLEMENTATION INSTRUCTIONS FOR CLAUDE SONNET 4.5

## EXECUTIVE SUMMARY

OBJECTIVE: Implement per-hand combat mode system (shooting/melee) with zero refactoring of existing shooting systems.

CONSTRAINTS:
- 0% code bloat
- 100% backward compatibility with shooting systems
- Shooting systems remain completely untouched except for enable/disable
- Full expandability for future melee weapon inventory system
- Each hand operates independently
- Separate idle animations per mode per hand

SOLUTION ARCHITECTURE: Inventory-driven mode switching (AAA pattern from God of War, Devil May Cry)

---

## AAA MODE SWITCHING SOLUTION

### PROBLEM ANALYSIS
```
USER REQUIREMENTS:
1. Fast mode switching
2. Per-hand independent modes
3. No button spam complexity
4. Future: Equip sword to left hand while right hand shoots
5. Zero damage to existing shooting systems

REJECTED APPROACH: Toggle button (Backspace)
- Requires manual toggling per hand
- Confusing UX (which hand am I toggling?)
- Modal state hell
- Doesn't scale to multiple melee weapons

APPROVED APPROACH: Inventory-driven mode assignment
```

### INVENTORY-DRIVEN MODE SWITCHING (AAA PATTERN)

```
STATE RULES:
- Hand with SHOOTING WEAPON equipped → SHOOTING MODE (default)
- Hand with MELEE WEAPON equipped → MELEE MODE
- Hand with NOTHING equipped → MELEE MODE (bare fists)

CORE MECHANIC: SHOOTING WEAPON AS EQUIPPABLE ITEM
- Shooting weapons (left/right hand guns) are ITEMS in inventory
- Equipping gun to hand → enters shooting mode
- Unequipping gun from hand → enters melee mode (bare fists)
- Guns stored in inventory when not equipped to hand slots and can NEVER BE LOST! 

EXAMPLE FLOW:
1. Both hands start with SHOOTING WEAPONS equipped (default loadout)
2. Player opens inventory, unequips LEFT GUN from left hand slot
3. LEFT hand automatically enters MELEE mode (bare fists)
4. LEFT GUN now sits in inventory as keepable item
5. RIGHT hand remains in SHOOTING mode 
6. Player finds "Sword" item, equips to LEFT hand (replaces bare fists)
7. LEFT hand still in MELEE mode, now with sword damage/animations
8. Player unequips sword, re-equips LEFT GUN from inventory
9. LEFT hand returns to SHOOTING mode

WEAPON SLOT BEHAVIOR:
- Empty hand slot = melee mode (bare fists)
- Gun in slot = shooting mode
- Melee weapon in slot = melee mode (weapon damage/animations)

INVENTORY STORAGE:
- Guns can be stored in inventory when not equipped
- Allows carrying guns as backup weapons
- Re-equip from inventory to return to shooting mode
- Melee weapons also stored in inventory

BENEFITS:
✓ Zero button complexity
✓ Clear visual feedback (hand holds weapon = that mode)
✓ Scales perfectly to multiple melee weapons
✓ Guns become strategic choice (ranged power vs melee freedom)
✓ Familiar pattern (God of War weapon system)
✓ No modal confusion - inventory dictates mode
✓ Realistic: Can't shoot without gun equipped
```

### TECHNICAL IMPLEMENTATION

```
HAND MODE STATE MACHINE (INVENTORY-DRIVEN):
┌─────────────────────────────────────────────────────────────┐
│              PER-HAND MODE MANAGER                          │
│                                                             │
│  LEFT HAND:                    RIGHT HAND:                  │
│  ┌──────────────┐             ┌──────────────┐             │
│  │  SHOOTING    │             │  SHOOTING    │             │
│  │ (normal Lshooting hand equipped│             │ (normal Rshooting hand equipped│             │
│  └──────┬───────┘             └──────┬───────┘             │
│         │                            │                      │
│         ↓                            ↓                      │
│  ┌──────────────┐             ┌──────────────┐             │
│  │    MELEE     │             │    MELEE     │             │
│  │(punch mode/weapon)│             │(punch mode/weapon)│             │
│  └──────────────┘             └──────────────┘             │
│                                                             │
│  Transition triggers:                                       │
│  EquipShootingWeapon(hand) → SHOOTING mode                 │
│  UnequipShootingWeapon(hand) → MELEE mode (bare fists)     │
│  EquipMeleeWeapon(hand, weaponData) → MELEE mode (weapon)  │
│  UnequipMeleeWeapon(hand) → MELEE mode (bare fists)        │
└─────────────────────────────────────────────────────────────┘

INVENTORY ITEM TYPES:
┌──────────────────────────────────────────────┐
│  SHOOTING WEAPONS (stored in inventory)      │
│  - Left Hand Gun (enables shooting mode)     │
│  - Right Hand Gun (enables shooting mode)    │
│                                              │
│  MELEE WEAPONS (stored in inventory)         │
│  - Fists (always available, default melee)   │
│  - Sword (found/purchased, equippable)       │
│  - Axe (future weapon)                       │
│  - Hammer (future weapon)                    │
└──────────────────────────────────────────────┘

HAND WEAPON SLOTS:
┌─────────────────┬─────────────────┐
│  LEFT HAND SLOT │ RIGHT HAND SLOT │
├─────────────────┼─────────────────┤
│ [Left Gun]      │ [Right Gun]     │  ← Default loadout (both shooting)
│                 │                 │
│ [Empty]         │ [Right Gun]     │  ← Left fists, right shoots
│                 │                 │
│ [Sword]         │ [Empty]         │  ← Left sword, right fists
│                 │                 │
│ [Left Gun]      │ [Hammer]        │  ← Left shoots, right hammer
└─────────────────┴─────────────────┘
```

---

## SYSTEM ARCHITECTURE

### COMPONENT HIERARCHY

```
Player GameObject
├── PlayerCombatModeManager (NEW)
│   ├── Tracks left hand mode state
│   ├── Tracks right hand mode state
│   ├── Subscribes to PlayerInputHandler events
│   ├── Routes input to correct executor per hand
│   └── Manages HandFiringMechanics enable/disable
│
├── PlayerMeleeExecutor (NEW)
│   ├── Handles left hand punch execution
│   ├── Handles right hand punch execution
│   ├── Per-hand cooldown tracking
│   ├── Animation requests
│   └── Audio playback
│
├── PlayerMeleeInventory (NEW - Phase 2)
│   ├── Tracks equipped melee weapons per hand
│   ├── Notifies CombatModeManager on equip/unequip
│   └── Provides weapon data to MeleeExecutor
│
├── PlayerShooterOrchestrator (EXISTING - unchanged)
├── PlayerInputHandler (EXISTING - unchanged)
├── HandFiringMechanics (Primary) (EXISTING - enable/disable only)
├── HandFiringMechanics (Secondary) (EXISTING - enable/disable only)
├── PlayerAnimationStateManager (EXISTING - unchanged)
├── LayeredHandAnimationController (EXISTING - unchanged)
└── HandUIManager (EXISTING - add fade method only)
```

### DATA FLOW

```
INPUT ROUTING (Per-Hand Independent):

LMB PRESS:
PlayerInputHandler.OnPrimaryTapAction
    ↓
PlayerCombatModeManager.HandlePrimaryInput()
    ↓
[Check leftHandMode]
    ↓
┌─────────────┴─────────────┐
│                           │
SHOOTING                 MELEE
│                           │
ShooterOrchestrator     MeleeExecutor
.HandlePrimaryTap()     .ExecuteLeftPunch()
│                           │
HandFiringMechanics     Animation + Audio
(left hand enabled)     (left hand punch)


RMB PRESS:
PlayerInputHandler.OnSecondaryTapAction
    ↓
PlayerCombatModeManager.HandleSecondaryInput()
    ↓
[Check rightHandMode]
    ↓
┌─────────────┴─────────────┐
│                           │
SHOOTING                 MELEE
│                           │
ShooterOrchestrator     MeleeExecutor
.HandleSecondaryTap()   .ExecuteRightPunch()
│                           │
HandFiringMechanics     Animation + Audio
(right hand enabled)    (right hand punch)
```

---

## IMPLEMENTATION PHASES

### PHASE 1: ENUM EXPANSION
**FILE:** `HandAnimationData.cs`

**LOCATION:** Line 133 (HandAnimationState enum)

**ACTION:** Add new states to enum:
```csharp
public enum HandAnimationState
{
    Idle = 0,
    Walk = 1,
    Sprint = 2,
    Jump = 3,
    Land = 4,
    Slide = 5,
    Dive = 6,
    Shotgun = 7,
    Beam = 8,
    FlyForward = 9,
    FlyUp = 10,
    FlyDown = 11,
    FlyStrafeLeft = 12,
    FlyStrafeRight = 13,
    FlyBoost = 14,
    TakeOff = 15,
    ArmorPlate = 16,
    Emote = 17,
    MeleeIdleLeft = 18,    // NEW - Left hand melee idle
    MeleeIdleRight = 19,   // NEW - Right hand melee idle
    MeleePunchLeft = 20,   // NEW - Left hand punch attack
    MeleePunchRight = 21   // NEW - Right hand punch attack
}
```

**FILE:** `HandAnimationData.cs`

**LOCATION:** After line 49 (after emote clips section)

**ACTION:** Add melee animation clip fields:
```csharp
[Header("Melee Animations")]
[Tooltip("Left hand melee idle animation (fists ready stance)")]
public AnimationClip meleeIdleLeftClip;
[Tooltip("Right hand melee idle animation (fists ready stance)")]
public AnimationClip meleeIdleRightClip;
[Tooltip("Left hand punch animation (0.4-0.5s duration)")]
public AnimationClip punchLeftClip;
[Tooltip("Right hand punch animation (0.4-0.5s duration)")]
public AnimationClip punchRightClip;
```

**FILE:** `HandAnimationData.cs`

**LOCATION:** Inside GetClipForState() method (after line 107, before default case)

**ACTION:** Add melee cases to switch statement:
```csharp
case HandAnimationState.MeleeIdleLeft: return meleeIdleLeftClip;
case HandAnimationState.MeleeIdleRight: return meleeIdleRightClip;
case HandAnimationState.MeleePunchLeft: return punchLeftClip;
case HandAnimationState.MeleePunchRight: return punchRightClip;
```

---

### PHASE 2: AUDIO SYSTEM INTEGRATION

**FILE:** `Assets/scripts/Audio/FIXSOUNDSCRIPTS/SoundEvents.cs`

**LOCATION:** After line 135 (after "► COMBAT: Special Attacks" section)

**ACTION:** Add melee sound arrays:
```csharp
[Header("► COMBAT: Melee Attacks")]
[Tooltip("Left hand punch sounds - plays on left hand punch")]
public SoundEvent[] punchLeftSounds;
[Tooltip("Right hand punch sounds - plays on right hand punch")]
public SoundEvent[] punchRightSounds;
```

**FILE:** `Assets/scripts/Audio/FIXSOUNDSCRIPTS/GameSoundsHelper.cs`

**LOCATION:** End of GameSounds class (before closing brace)

**ACTION:** Add static melee audio methods:
```csharp
/// <summary>
/// Play left hand punch sound at position
/// </summary>
public static void PlayPunchLeft(Vector3 position, float volume = 1f)
{
    if (SoundEventsManager.Events?.punchLeftSounds != null && SoundEventsManager.Events.punchLeftSounds.Length > 0)
    {
        SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.punchLeftSounds, position, volume);
    }
}

/// <summary>
/// Play right hand punch sound at position
/// </summary>
public static void PlayPunchRight(Vector3 position, float volume = 1f)
{
    if (SoundEventsManager.Events?.punchRightSounds != null && SoundEventsManager.Events.punchRightSounds.Length > 0)
    {
        SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.punchRightSounds, position, volume);
    }
}
```

---

### PHASE 3: UI FADE SYSTEM

**FILE:** `Assets/scripts/HandUIManager.cs`

**LOCATION:** After OnDisable() method (around line 620)

**ACTION:** Add fade coroutine:
```csharp
/// <summary>
/// Fade out the entire hand UI container over specified duration
/// Used when switching to melee mode where overheat UI is not needed
/// </summary>
public IEnumerator FadeOutUI(float duration)
{
    CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
    if (canvasGroup == null)
    {
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
    
    float elapsed = 0f;
    float startAlpha = canvasGroup.alpha;
    
    while (elapsed < duration)
    {
        elapsed += Time.unscaledDeltaTime;
        canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
        yield return null;
    }
    
    canvasGroup.alpha = 0f;
    gameObject.SetActive(false);
}

/// <summary>
/// Fade in the entire hand UI container over specified duration
/// Used when switching back to shooting mode
/// </summary>
public IEnumerator FadeInUI(float duration)
{
    CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
    if (canvasGroup == null)
    {
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
    
    gameObject.SetActive(true);
    
    float elapsed = 0f;
    float startAlpha = canvasGroup.alpha;
    
    while (elapsed < duration)
    {
        elapsed += Time.unscaledDeltaTime;
        canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / duration);
        yield return null;
    }
    
    canvasGroup.alpha = 1f;
}
```

---

### PHASE 4: PLAYER MELEE EXECUTOR (CORE LOGIC)

**FILE:** `Assets/scripts/PlayerMeleeExecutor.cs` (CREATE NEW)

**FULL CONTENTS:**
```csharp
using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// Handles all melee combat execution for player hands.
/// Manages per-hand cooldowns, animation requests, and audio playback.
/// Works in tandem with PlayerCombatModeManager for mode-aware execution.
/// </summary>
public class PlayerMeleeExecutor : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private PlayerAnimationStateManager animationStateManager;
    [SerializeField] private LayeredHandAnimationController handController;
    
    [Header("Melee Settings")]
    [Tooltip("Cooldown between punches for each hand (seconds)")]
    [Range(0.1f, 2f)] public float punchCooldown = 0.5f;
    
    [Tooltip("Audio volume for punch sounds")]
    [Range(0f, 1f)] public float punchAudioVolume = 0.8f;
    
    [Header("Emit Points")]
    [Tooltip("Left hand punch origin point")]
    public Transform leftHandEmitPoint;
    [Tooltip("Right hand punch origin point")]
    public Transform rightHandEmitPoint;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Per-hand cooldown tracking
    private float _nextLeftPunchTime = 0f;
    private float _nextRightPunchTime = 0f;
    
    void Awake()
    {
        // Auto-find references if not assigned
        if (animationStateManager == null)
            animationStateManager = GetComponent<PlayerAnimationStateManager>();
        if (handController == null)
            handController = GetComponent<LayeredHandAnimationController>();
            
        // Validate critical references
        if (animationStateManager == null)
            Debug.LogError("[PlayerMeleeExecutor] PlayerAnimationStateManager not found! Melee animations will not work.", this);
        if (handController == null)
            Debug.LogError("[PlayerMeleeExecutor] LayeredHandAnimationController not found! Melee animations will not work.", this);
    }
    
    /// <summary>
    /// Execute left hand punch attack
    /// Called by PlayerCombatModeManager when LMB pressed and left hand is in melee mode
    /// </summary>
    public void ExecuteLeftPunch()
    {
        // Check cooldown
        if (Time.time < _nextLeftPunchTime)
        {
            if (enableDebugLogs)
                Debug.Log($"[PlayerMeleeExecutor] Left punch on cooldown. Ready in {_nextLeftPunchTime - Time.time:F2}s");
            return;
        }
        
        // Set next allowed punch time (cooldown starts on button press)
        _nextLeftPunchTime = Time.time + punchCooldown;
        
        // Request punch animation
        if (handController != null && handController.leftHandController != null)
        {
            handController.leftHandController.PlayAnimation(HandAnimationState.MeleePunchLeft);
            if (enableDebugLogs)
                Debug.Log("[PlayerMeleeExecutor] Left hand punch animation requested");
        }
        
        // Play punch audio
        Vector3 audioPosition = leftHandEmitPoint != null ? leftHandEmitPoint.position : transform.position;
        GameSounds.PlayPunchLeft(audioPosition, punchAudioVolume);
        
        if (enableDebugLogs)
            Debug.Log($"[PlayerMeleeExecutor] Left punch executed. Next punch at {_nextLeftPunchTime:F2}");
    }
    
    /// <summary>
    /// Execute right hand punch attack
    /// Called by PlayerCombatModeManager when RMB pressed and right hand is in melee mode
    /// </summary>
    public void ExecuteRightPunch()
    {
        // Check cooldown
        if (Time.time < _nextRightPunchTime)
        {
            if (enableDebugLogs)
                Debug.Log($"[PlayerMeleeExecutor] Right punch on cooldown. Ready in {_nextRightPunchTime - Time.time:F2}s");
            return;
        }
        
        // Set next allowed punch time (cooldown starts on button press)
        _nextRightPunchTime = Time.time + punchCooldown;
        
        // Request punch animation
        if (handController != null && handController.rightHandController != null)
        {
            handController.rightHandController.PlayAnimation(HandAnimationState.MeleePunchRight);
            if (enableDebugLogs)
                Debug.Log("[PlayerMeleeExecutor] Right hand punch animation requested");
        }
        
        // Play punch audio
        Vector3 audioPosition = rightHandEmitPoint != null ? rightHandEmitPoint.position : transform.position;
        GameSounds.PlayPunchRight(audioPosition, punchAudioVolume);
        
        if (enableDebugLogs)
            Debug.Log($"[PlayerMeleeExecutor] Right punch executed. Next punch at {_nextRightPunchTime:F2}");
    }
    
    /// <summary>
    /// Transition left hand to melee idle animation
    /// Called when left hand enters melee mode
    /// </summary>
    public void EnterLeftHandMeleeIdle()
    {
        if (handController != null && handController.leftHandController != null)
        {
            handController.leftHandController.PlayAnimation(HandAnimationState.MeleeIdleLeft);
            if (enableDebugLogs)
                Debug.Log("[PlayerMeleeExecutor] Left hand entered melee idle");
        }
    }
    
    /// <summary>
    /// Transition right hand to melee idle animation
    /// Called when right hand enters melee mode
    /// </summary>
    public void EnterRightHandMeleeIdle()
    {
        if (handController != null && handController.rightHandController != null)
        {
            handController.rightHandController.PlayAnimation(HandAnimationState.MeleeIdleRight);
            if (enableDebugLogs)
                Debug.Log("[PlayerMeleeExecutor] Right hand entered melee idle");
        }
    }
    
    /// <summary>
    /// Transition left hand to shooting idle animation
    /// Called when left hand exits melee mode
    /// </summary>
    public void ExitLeftHandMeleeIdle()
    {
        if (handController != null && handController.leftHandController != null)
        {
            handController.leftHandController.PlayAnimation(HandAnimationState.Idle);
            if (enableDebugLogs)
                Debug.Log("[PlayerMeleeExecutor] Left hand exited melee idle (returned to shooting idle)");
        }
    }
    
    /// <summary>
    /// Transition right hand to shooting idle animation
    /// Called when right hand exits melee mode
    /// </summary>
    public void ExitRightHandMeleeIdle()
    {
        if (handController != null && handController.rightHandController != null)
        {
            handController.rightHandController.PlayAnimation(HandAnimationState.Idle);
            if (enableDebugLogs)
                Debug.Log("[PlayerMeleeExecutor] Right hand exited melee idle (returned to shooting idle)");
        }
    }
    
    /// <summary>
    /// Reset left hand punch cooldown (for testing or special cases)
    /// </summary>
    public void ResetLeftPunchCooldown()
    {
        _nextLeftPunchTime = 0f;
    }
    
    /// <summary>
    /// Reset right hand punch cooldown (for testing or special cases)
    /// </summary>
    public void ResetRightPunchCooldown()
    {
        _nextRightPunchTime = 0f;
    }
    
    /// <summary>
    /// Check if left hand can punch (cooldown ready)
    /// </summary>
    public bool CanLeftPunch()
    {
        return Time.time >= _nextLeftPunchTime;
    }
    
    /// <summary>
    /// Check if right hand can punch (cooldown ready)
    /// </summary>
    public bool CanRightPunch()
    {
        return Time.time >= _nextRightPunchTime;
    }
}
```

---

### PHASE 5: PLAYER COMBAT MODE MANAGER (ROUTING ORCHESTRATOR)

**FILE:** `Assets/scripts/PlayerCombatModeManager.cs` (CREATE NEW)

**FULL CONTENTS:**
```csharp
using UnityEngine;
using System.Collections;

/// <summary>
/// Central orchestrator for per-hand combat mode management.
/// Routes input to appropriate executors based on each hand's current mode.
/// Manages HandFiringMechanics enable/disable and UI visibility per hand.
/// Zero modifications to existing shooting systems - only enable/disable control.
/// </summary>
public class PlayerCombatModeManager : MonoBehaviour
{
    public enum CombatMode
    {
        Shooting,  // Hand uses HandFiringMechanics (default)
        Melee      // Hand uses PlayerMeleeExecutor
    }
    
    [Header("Current Hand Modes (Inspector Visibility)")]
    [SerializeField] [Tooltip("Current mode for LEFT hand (LMB controlled)")]
    private CombatMode leftHandMode = CombatMode.Shooting;
    
    [SerializeField] [Tooltip("Current mode for RIGHT hand (RMB controlled)")]
    private CombatMode rightHandMode = CombatMode.Shooting;
    
    [Header("System References")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private PlayerShooterOrchestrator shooterOrchestrator;
    [SerializeField] private PlayerMeleeExecutor meleeExecutor;
    [SerializeField] private HandFiringMechanics primaryHandMechanics;
    [SerializeField] private HandFiringMechanics secondaryHandMechanics;
    [SerializeField] private PlayerOverheatManager overheatManager;
    [SerializeField] private HandUIManager handUIManager;
    
    [Header("UI Fade Settings")]
    [Tooltip("Duration of UI fade transition when changing modes")]
    [Range(0.1f, 1f)] public float uiFadeDuration = 0.2f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Public read-only properties
    public CombatMode LeftHandMode => leftHandMode;
    public CombatMode RightHandMode => rightHandMode;
    
    void Awake()
    {
        // Auto-find references if not assigned
        if (inputHandler == null)
            inputHandler = PlayerInputHandler.Instance;
        if (shooterOrchestrator == null)
            shooterOrchestrator = GetComponent<PlayerShooterOrchestrator>();
        if (meleeExecutor == null)
            meleeExecutor = GetComponent<PlayerMeleeExecutor>();
        if (overheatManager == null)
            overheatManager = GetComponent<PlayerOverheatManager>();
        if (handUIManager == null)
            handUIManager = FindObjectOfType<HandUIManager>();
            
        // Find HandFiringMechanics if not assigned
        if (primaryHandMechanics == null || secondaryHandMechanics == null)
        {
            HandFiringMechanics[] mechanics = GetComponentsInChildren<HandFiringMechanics>();
            if (mechanics.Length >= 2)
            {
                primaryHandMechanics = mechanics[0];
                secondaryHandMechanics = mechanics[1];
            }
        }
        
        // Validate critical references
        if (inputHandler == null)
            Debug.LogError("[PlayerCombatModeManager] PlayerInputHandler not found!", this);
        if (shooterOrchestrator == null)
            Debug.LogError("[PlayerCombatModeManager] PlayerShooterOrchestrator not found!", this);
        if (meleeExecutor == null)
            Debug.LogError("[PlayerCombatModeManager] PlayerMeleeExecutor not found!", this);
    }
    
    void OnEnable()
    {
        SubscribeToInput();
    }
    
    void OnDisable()
    {
        UnsubscribeFromInput();
    }
    
    void SubscribeToInput()
    {
        if (inputHandler == null) return;
        
        // Subscribe to ALL input events
        // Mode manager decides where to route based on current hand modes
        inputHandler.OnPrimaryTapAction += HandlePrimaryTap;
        inputHandler.OnPrimaryHoldStartedAction += HandlePrimaryHoldStarted;
        inputHandler.OnPrimaryHoldEndedAction += HandlePrimaryHoldEnded;
        
        inputHandler.OnSecondaryTapAction += HandleSecondaryTap;
        inputHandler.OnSecondaryHoldStartedAction += HandleSecondaryHoldStarted;
        inputHandler.OnSecondaryHoldEndedAction += HandleSecondaryHoldEnded;
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Subscribed to input events");
    }
    
    void UnsubscribeFromInput()
    {
        if (inputHandler == null) return;
        
        inputHandler.OnPrimaryTapAction -= HandlePrimaryTap;
        inputHandler.OnPrimaryHoldStartedAction -= HandlePrimaryHoldStarted;
        inputHandler.OnPrimaryHoldEndedAction -= HandlePrimaryHoldEnded;
        
        inputHandler.OnSecondaryTapAction -= HandleSecondaryTap;
        inputHandler.OnSecondaryHoldStartedAction -= HandleSecondaryHoldStarted;
        inputHandler.OnSecondaryHoldEndedAction -= HandleSecondaryHoldEnded;
    }
    
    // ============================================================================
    // INPUT ROUTING - Per-Hand Mode-Aware Dispatch
    // ============================================================================
    
    private void HandlePrimaryTap()
    {
        if (leftHandMode == CombatMode.Shooting)
        {
            // Route to shooting system
            if (shooterOrchestrator != null)
                shooterOrchestrator.HandlePrimaryTap();
        }
        else // leftHandMode == CombatMode.Melee
        {
            // Route to melee system
            if (meleeExecutor != null)
                meleeExecutor.ExecuteLeftPunch();
        }
    }
    
    private void HandlePrimaryHoldStarted()
    {
        if (leftHandMode == CombatMode.Shooting)
        {
            if (shooterOrchestrator != null)
                shooterOrchestrator.HandlePrimaryHoldStarted();
        }
        // Melee mode: Hold behavior not yet implemented (future: charge punch)
    }
    
    private void HandlePrimaryHoldEnded()
    {
        if (leftHandMode == CombatMode.Shooting)
        {
            if (shooterOrchestrator != null)
                shooterOrchestrator.HandlePrimaryHoldEnded();
        }
        // Melee mode: Hold behavior not yet implemented
    }
    
    private void HandleSecondaryTap()
    {
        if (rightHandMode == CombatMode.Shooting)
        {
            if (shooterOrchestrator != null)
                shooterOrchestrator.HandleSecondaryTap();
        }
        else // rightHandMode == CombatMode.Melee
        {
            if (meleeExecutor != null)
                meleeExecutor.ExecuteRightPunch();
        }
    }
    
    private void HandleSecondaryHoldStarted()
    {
        if (rightHandMode == CombatMode.Shooting)
        {
            if (shooterOrchestrator != null)
                shooterOrchestrator.HandleSecondaryHoldStarted();
        }
        // Melee mode: Hold behavior not yet implemented
    }
    
    private void HandleSecondaryHoldEnded()
    {
        if (rightHandMode == CombatMode.Shooting)
        {
            if (shooterOrchestrator != null)
                shooterOrchestrator.HandleSecondaryHoldEnded();
        }
        // Melee mode: Hold behavior not yet implemented
    }
    
    // ============================================================================
    // MODE SWITCHING API - Called by Inventory System
    // ============================================================================
    
    /// <summary>
    /// Equip shooting weapon to left hand (enters shooting mode)
    /// Called when player equips left gun from inventory to left hand slot
    /// </summary>
    public void EquipLeftHandShootingWeapon()
    {
        if (leftHandMode == CombatMode.Shooting)
        {
            if (enableDebugLogs)
                Debug.Log("[PlayerCombatModeManager] Left hand already has shooting weapon equipped");
            return;
        }
        
        SetLeftHandShootingMode();
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Left hand gun equipped - entered SHOOTING mode");
    }
    
    /// <summary>
    /// Unequip shooting weapon from left hand (enters melee mode with bare fists)
    /// Called when player removes left gun from left hand slot
    /// Gun is stored back in inventory
    /// </summary>
    public void UnequipLeftHandShootingWeapon()
    {
        if (leftHandMode == CombatMode.Melee)
        {
            if (enableDebugLogs)
                Debug.Log("[PlayerCombatModeManager] Left hand shooting weapon already unequipped");
            return;
        }
        
        SetLeftHandMeleeMode();
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Left hand gun unequipped - entered MELEE mode (bare fists)");
    }
    
    /// <summary>
    /// Equip melee weapon to left hand (stays in melee mode, changes weapon data)
    /// Called when player equips sword/axe/etc to left hand slot
    /// </summary>
    public void EquipLeftHandMeleeWeapon(/* MeleeWeaponData weaponData - Phase 7 */)
    {
        // Ensure we're in melee mode
        if (leftHandMode != CombatMode.Melee)
        {
            SetLeftHandMeleeMode();
        }
        
        // Future: Pass weapon data to MeleeExecutor for damage/animation overrides
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Left hand melee weapon equipped");
    }
    
    /// <summary>
    /// Unequip melee weapon from left hand (stays in melee mode, returns to bare fists)
    /// Called when player removes sword/axe/etc from left hand slot
    /// Melee weapon is stored back in inventory
    /// </summary>
    public void UnequipLeftHandMeleeWeapon()
    {
        // Stay in melee mode, just reset to bare fists
        // Future: Notify MeleeExecutor to use default fist damage/animations
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Left hand melee weapon unequipped - using bare fists");
    }
    
    /// <summary>
    /// Equip shooting weapon to right hand (enters shooting mode)
    /// Called when player equips right gun from inventory to right hand slot
    /// </summary>
    public void EquipRightHandShootingWeapon()
    {
        if (rightHandMode == CombatMode.Shooting)
        {
            if (enableDebugLogs)
                Debug.Log("[PlayerCombatModeManager] Right hand already has shooting weapon equipped");
            return;
        }
        
        SetRightHandShootingMode();
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Right hand gun equipped - entered SHOOTING mode");
    }
    
    /// <summary>
    /// Unequip shooting weapon from right hand (enters melee mode with bare fists)
    /// Called when player removes right gun from right hand slot
    /// Gun is stored back in inventory
    /// </summary>
    public void UnequipRightHandShootingWeapon()
    {
        if (rightHandMode == CombatMode.Melee)
        {
            if (enableDebugLogs)
                Debug.Log("[PlayerCombatModeManager] Right hand shooting weapon already unequipped");
            return;
        }
        
        SetRightHandMeleeMode();
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Right hand gun unequipped - entered MELEE mode (bare fists)");
    }
    
    /// <summary>
    /// Equip melee weapon to right hand (stays in melee mode, changes weapon data)
    /// Called when player equips sword/axe/etc to right hand slot
    /// </summary>
    public void EquipRightHandMeleeWeapon(/* MeleeWeaponData weaponData - Phase 7 */)
    {
        // Ensure we're in melee mode
        if (rightHandMode != CombatMode.Melee)
        {
            SetRightHandMeleeMode();
        }
        
        // Future: Pass weapon data to MeleeExecutor for damage/animation overrides
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Right hand melee weapon equipped");
    }
    
    /// <summary>
    /// Unequip melee weapon from right hand (stays in melee mode, returns to bare fists)
    /// Called when player removes sword/axe/etc from right hand slot
    /// Melee weapon is stored back in inventory
    /// </summary>
    public void UnequipRightHandMeleeWeapon()
    {
        // Stay in melee mode, just reset to bare fists
        // Future: Notify MeleeExecutor to use default fist damage/animations
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Right hand melee weapon unequipped - using bare fists");
    }
    
    // ============================================================================
    // INTERNAL MODE SWITCH HANDLERS
    // ============================================================================
    
    /// <summary>
    /// Internal method to transition left hand to melee mode
    /// </summary>
    private void SetLeftHandMeleeMode()
    {
        if (leftHandMode == CombatMode.Melee) return;
        
        leftHandMode = CombatMode.Melee;
        
        // Disable left hand shooting mechanics
        if (primaryHandMechanics != null)
            primaryHandMechanics.enabled = false;
        
        // Stop any active left hand shooting
        if (shooterOrchestrator != null)
            shooterOrchestrator.StopPrimaryHandShooting();
        
        // Stop left hand heat processing
        if (overheatManager != null)
            overheatManager.SetHandFiringState(true, false);
        
        // Fade out overheat UI if BOTH hands now in melee
        if (handUIManager != null && rightHandMode == CombatMode.Melee)
            StartCoroutine(handUIManager.FadeOutUI(uiFadeDuration));
        
        // Enter melee idle animation
        if (meleeExecutor != null)
            meleeExecutor.EnterLeftHandMeleeIdle();
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Left hand → MELEE mode");
    }
    
    /// <summary>
    /// Internal method to transition left hand to shooting mode
    /// </summary>
    private void SetLeftHandShootingMode()
    {
        if (leftHandMode == CombatMode.Shooting) return;
        
        leftHandMode = CombatMode.Shooting;
        
        // Enable left hand shooting mechanics
        if (primaryHandMechanics != null)
            primaryHandMechanics.enabled = true;
        
        // Fade in overheat UI (at least one hand now shooting)
        if (handUIManager != null)
            StartCoroutine(handUIManager.FadeInUI(uiFadeDuration));
        
        // Exit melee idle animation
        if (meleeExecutor != null)
            meleeExecutor.ExitLeftHandMeleeIdle();
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Left hand → SHOOTING mode");
    }
    
    /// <summary>
    /// Internal method to transition right hand to melee mode
    /// </summary>
    private void SetRightHandMeleeMode()
    {
        if (rightHandMode == CombatMode.Melee) return;
        
        rightHandMode = CombatMode.Melee;
        
        // Disable right hand shooting mechanics
        if (secondaryHandMechanics != null)
            secondaryHandMechanics.enabled = false;
        
        // Stop any active right hand shooting
        if (shooterOrchestrator != null)
            shooterOrchestrator.StopSecondaryHandShooting();
        
        // Stop right hand heat processing
        if (overheatManager != null)
            overheatManager.SetHandFiringState(false, false);
        
        // Fade out overheat UI if BOTH hands now in melee
        if (handUIManager != null && leftHandMode == CombatMode.Melee)
            StartCoroutine(handUIManager.FadeOutUI(uiFadeDuration));
        
        // Enter melee idle animation
        if (meleeExecutor != null)
            meleeExecutor.EnterRightHandMeleeIdle();
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Right hand → MELEE mode");
    }
    
    /// <summary>
    /// Internal method to transition right hand to shooting mode
    /// </summary>
    private void SetRightHandShootingMode()
    {
        if (rightHandMode == CombatMode.Shooting) return;
        
        rightHandMode = CombatMode.Shooting;
        
        // Enable right hand shooting mechanics
        if (secondaryHandMechanics != null)
            secondaryHandMechanics.enabled = true;
        
        // Fade in overheat UI (at least one hand now shooting)
        if (handUIManager != null)
            StartCoroutine(handUIManager.FadeInUI(uiFadeDuration));
        
        // Exit melee idle animation
        if (meleeExecutor != null)
            meleeExecutor.ExitRightHandMeleeIdle();
        
        if (enableDebugLogs)
            Debug.Log("[PlayerCombatModeManager] Right hand → SHOOTING mode");
    }
    
    // ============================================================================
    // TESTING METHODS - For inspector-based mode switching during development
    // ============================================================================
    
    [ContextMenu("Test: Left Hand → Melee Mode")]
    public void TestLeftHandMelee()
    {
        UnequipLeftHandShootingWeapon();
    }
    
    [ContextMenu("Test: Left Hand → Shooting Mode")]
    public void TestLeftHandShooting()
    {
        EquipLeftHandShootingWeapon();
    }
    
    [ContextMenu("Test: Right Hand → Melee Mode")]
    public void TestRightHandMelee()
    {
        UnequipRightHandShootingWeapon();
    }
    
    [ContextMenu("Test: Right Hand → Shooting Mode")]
    public void TestRightHandShooting()
    {
        EquipRightHandShootingWeapon();
    }
    
    [ContextMenu("Test: Both Hands → Melee Mode")]
    public void TestBothHandsMelee()
    {
        UnequipLeftHandShootingWeapon();
        UnequipRightHandShootingWeapon();
    }
    
    [ContextMenu("Test: Both Hands → Shooting Mode")]
    public void TestBothHandsShooting()
    {
        EquipLeftHandShootingWeapon();
        EquipRightHandShootingWeapon();
    }
    
    [ContextMenu("Test: Equip Left Melee Weapon")]
    public void TestEquipLeftMeleeWeapon()
    {
        EquipLeftHandMeleeWeapon();
    }
    
    [ContextMenu("Test: Unequip Left Melee Weapon")]
    public void TestUnequipLeftMeleeWeapon()
    {
        UnequipLeftHandMeleeWeapon();
    }
}
```

---

### PHASE 6: PLAYERSHOOTER ORCHESTRATOR INTEGRATION

**FILE:** `Assets/scripts/PlayerShooterOrchestrator.cs`

**LOCATION:** Make methods public (search for "private void Handle")

**ACTION:** Change ALL Handle methods from private to public:
```csharp
// CHANGE FROM:
private void HandlePrimaryTap()

// CHANGE TO:
public void HandlePrimaryTap()

// REPEAT FOR:
// - HandlePrimaryHoldStarted()
// - HandlePrimaryHoldEnded()
// - HandleSecondaryTap()
// - HandleSecondaryHoldStarted()
// - HandleSecondaryHoldEnded()
```

**REASON:** PlayerCombatModeManager needs to call these methods directly when routing shooting input.

---

## INTEGRATION CHECKLIST

### UNITY INSPECTOR SETUP

**PLAYER GAMEOBJECT:**
1. Add `PlayerMeleeExecutor` component
2. Add `PlayerCombatModeManager` component

**PLAYERCOMBATMODEMANAGER INSPECTOR:**
```
Input Handler: [Auto-found from PlayerInputHandler.Instance]
Shooter Orchestrator: [Drag PlayerShooterOrchestrator]
Melee Executor: [Drag PlayerMeleeExecutor]
Primary Hand Mechanics: [Drag left HandFiringMechanics]
Secondary Hand Mechanics: [Drag right HandFiringMechanics]
Overheat Manager: [Drag PlayerOverheatManager]
Hand UI Manager: [Auto-found from scene]
UI Fade Duration: 0.2
Enable Debug Logs: false (true for testing)
```

**PLAYERMELEEEXECUTOR INSPECTOR:**
```
Animation State Manager: [Auto-found from Player]
Hand Controller: [Auto-found from Player]
Punch Cooldown: 0.5
Punch Audio Volume: 0.8
Left Hand Emit Point: [Drag left hand transform]
Right Hand Emit Point: [Drag right hand transform]
Enable Debug Logs: false (true for testing)
```

### ANIMATION SETUP

**ANIMATOR SETUP:**
User must create 4 animation clips:
1. `L_meleeIdle.anim` - Left hand melee idle (fists ready)
2. `R_meleeIdle.anim` - Right hand melee idle (fists ready)
3. `L_Punch.anim` - Left hand punch (0.4-0.5s duration)
4. `R_Punch.anim` - Right hand punch (0.4-0.5s duration)

**HAND POSITION GUIDANCE:**
- Start position: Visible at side/bottom of screen (idle stance)
- End position: Same as start (return to idle)
- Punch arc: Forward thrust toward screen center
- Duration: 0.4-0.5 seconds for snappy AAA feel

**ASSIGN TO SCRIPTABLE OBJECT:**
- Open HandAnimationData ScriptableObject in inspector
- Assign animation clips to new melee fields
- Save asset

### AUDIO SETUP

**SOUND EVENTS ASSET:**
1. Open SoundEvents ScriptableObject
2. Create new SoundEvent entries for punch sounds
3. Assign audio clips to:
   - `punchLeftSounds` array
   - `punchRightSounds` array
4. Configure SoundEvent settings:
   - Category: Combat
   - Volume: 0.8
   - Pitch: 1.0
   - Pitch Variation: 0.05
   - 3D Settings: minDistance=5, maxDistance=30

---

## TESTING PROTOCOL

### TEST 1: MODE SWITCHING
```
STEPS:
1. Play scene
2. Select Player GameObject in hierarchy
3. Right-click PlayerCombatModeManager component
4. Click "Test: Left Hand → Melee Mode"
5. Observe console logs (if debug enabled)
6. Verify left hand animation changes to melee idle
7. Press LMB - should trigger left punch (NOT shotgun)
8. Press RMB - should trigger right shotgun (shooting still works)
9. Right-click component → "Test: Left Hand → Shooting Mode"
10. Verify left hand returns to shooting idle
11. Press LMB - should trigger shotgun again

EXPECTED RESULTS:
✓ Left hand switches between melee/shooting independently
✓ Right hand continues functioning in original mode
✓ No errors in console
✓ UI fades out when entering melee, fades in when returning to shooting
```

### TEST 2: PER-HAND INDEPENDENCE
```
STEPS:
1. Test: Both Hands → Melee Mode
2. Press LMB and RMB rapidly
3. Verify both hands punch with independent cooldowns
4. Test: Left Hand → Melee, Right Hand → Shooting
5. Press LMB (punch) and RMB (shoot) simultaneously
6. Verify both actions occur independently

EXPECTED RESULTS:
✓ Each hand operates completely independently
✓ Can punch and shoot at the same time with different hands
✓ Cooldowns track separately per hand
```

### TEST 3: AUDIO PLAYBACK
```
STEPS:
1. Ensure audio clips assigned to SoundEvents
2. Enable audio in Unity (unmute)
3. Set melee executor punch volume to 0.8
4. Test punch from each hand
5. Verify distinct sounds play at correct positions

EXPECTED RESULTS:
✓ Left punch plays left punch sound
✓ Right punch plays right punch sound
✓ Sounds play at hand emit point positions (3D audio)
```

### TEST 4: COOLDOWN ENFORCEMENT
```
STEPS:
1. Set punch cooldown to 2.0 seconds (easy to observe)
2. Enter melee mode on left hand
3. Press LMB rapidly (spam clicks)
4. Observe console logs
5. Verify only one punch every 2 seconds

EXPECTED RESULTS:
✓ Only one punch executes per cooldown period
✓ Spam clicks are ignored
✓ Console shows "on cooldown" messages
```

---

## FUTURE EXPANSION ROADMAP

### PHASE 7: INVENTORY SYSTEM (Future Implementation)

**FILE:** `Assets/scripts/PlayerMeleeInventory.cs` (CREATE LATER)

**PURPOSE:** Track equipped melee weapons per hand

**ARCHITECTURE:**
```csharp
public class PlayerMeleeInventory : MonoBehaviour
{
    [System.Serializable]
    public class MeleeWeaponData
    {
        public string weaponName;
        public float damageMultiplier;
        public float cooldownMultiplier;
        public GameObject weaponModel; // Visual mesh
        public AnimationClip[] attackAnimations; // Override punch anims
    }
    
    private MeleeWeaponData leftHandWeapon = null;
    private MeleeWeaponData rightHandWeapon = null;
    
    private PlayerCombatModeManager modeManager;
    
    public void EquipWeaponToLeftHand(MeleeWeaponData weapon)
    {
        leftHandWeapon = weapon;
        modeManager.SetLeftHandMeleeMode();
        // Spawn weapon model on left hand
        // Override punch animation with weapon swing
    }
    
    public void UnequipLeftHandWeapon()
    {
        // Destroy weapon model
        leftHandWeapon = null;
        modeManager.SetLeftHandShootingMode();
    }
    
    // Same for right hand...
}
```

**INTEGRATION:**
- Inventory UI calls EquipWeaponToLeftHand() when user drags sword to left hand slot
- Automatically triggers mode switch via CombatModeManager
- Zero UI complexity - drag-and-drop simplicity

### PHASE 8: MELEE DAMAGE SYSTEM (Future)

**FILE:** `Assets/scripts/MeleeDamageDetector.cs` (CREATE LATER)

**PURPOSE:** Detect punch collisions with enemies

**ARCHITECTURE:**
```csharp
public class MeleeDamageDetector : MonoBehaviour
{
    public float punchDamage = 25f;
    public float punchRange = 2f;
    public LayerMask enemyLayer;
    
    // Called by PlayerMeleeExecutor during punch animation
    public void DetectPunchHit(bool isLeftHand)
    {
        Transform emitPoint = isLeftHand ? leftHandEmitPoint : rightHandEmitPoint;
        Collider[] hits = Physics.OverlapSphere(emitPoint.position, punchRange, enemyLayer);
        
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<SkullEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(punchDamage);
            }
        }
    }
}
```

**INTEGRATION:**
- PlayerMeleeExecutor calls MeleeDamageDetector.DetectPunchHit() during punch animation
- Uses animation events or timed delay (0.2s into punch)

---

## CRITICAL SUCCESS FACTORS

### ZERO BLOAT CHECKLIST
- [ ] No duplicate animation state management
- [ ] No redundant mode tracking variables
- [ ] No unnecessary Update() loops
- [ ] No hard-coded weapon data in executor
- [ ] No direct HandFiringMechanics modifications (only enable/disable)

### BACKWARD COMPATIBILITY CHECKLIST
- [ ] PlayerShooterOrchestrator methods only changed from private to public
- [ ] HandFiringMechanics code 100% untouched
- [ ] PlayerInputHandler code 100% untouched
- [ ] PlayerAnimationStateManager code 100% untouched
- [ ] Overheat system continues functioning when hand returns to shooting

### EXPANDABILITY CHECKLIST
- [ ] MeleeExecutor doesn't hard-code weapon types
- [ ] CombatModeManager uses simple enable/disable pattern
- [ ] Animation system supports multiple melee animation sets
- [ ] Audio system supports weapon-specific sounds (future)
- [ ] Inventory system can trigger mode changes externally

---

## COMMON PITFALLS TO AVOID

### PITFALL 1: Breaking Shooting System
**SYMPTOM:** Shooting stops working after adding melee
**CAUSE:** PlayerCombatModeManager not routing input correctly
**FIX:** Verify HandlePrimaryTap() checks leftHandMode == CombatMode.Shooting before routing to ShooterOrchestrator

### PITFALL 2: Animation State Conflicts
**SYMPTOM:** Hands stuck in weird animation states
**CAUSE:** Multiple systems trying to control same animator
**FIX:** Ensure PlayerCombatModeManager fully disables HandFiringMechanics when in melee mode

### PITFALL 3: Cooldown Not Enforcing
**SYMPTOM:** Can spam punches
**CAUSE:** Cooldown check before setting next punch time
**FIX:** Set _nextLeftPunchTime BEFORE executing punch, not after

### PITFALL 4: Audio Not Playing
**SYMPTOM:** Punch has no sound
**CAUSE:** SoundEvents arrays not assigned in ScriptableObject
**FIX:** Open SoundEvents asset, verify punchLeftSounds and punchRightSounds have clips assigned

### PITFALL 5: UI Not Fading
**SYMPTOM:** Overheat UI stays visible in melee mode
**CAUSE:** CanvasGroup component missing on HandUIManager
**FIX:** FadeOutUI() coroutine auto-adds CanvasGroup if missing

---

## IMPLEMENTATION ORDER

**EXECUTE IN THIS EXACT SEQUENCE:**

1. **PHASE 1:** Enum expansion (HandAnimationData.cs)
2. **PHASE 2:** Audio system (SoundEvents.cs, GameSoundsHelper.cs)
3. **PHASE 3:** UI fade (HandUIManager.cs)
4. **PHASE 4:** Create PlayerMeleeExecutor.cs
5. **PHASE 5:** Create PlayerCombatModeManager.cs
6. **PHASE 6:** Modify PlayerShooterOrchestrator (make methods public)
7. **Unity Inspector Setup**
8. **Animation Creation** (user task)
9. **Audio Assignment** (user task)
10. **Testing Protocol** (all 4 tests)

---

## SUCCESS METRICS

**IMPLEMENTATION COMPLETE WHEN:**
- [ ] Can switch left hand to melee mode via context menu
- [ ] Can switch right hand to melee mode independently
- [ ] LMB triggers punch when left hand in melee, shotgun when in shooting
- [ ] RMB triggers punch when right hand in melee, shotgun when in shooting
- [ ] Punch sounds play correctly from each hand
- [ ] Punch cooldown enforces 0.5s delay per hand
- [ ] Overheat UI fades out in melee, fades in when returning to shooting
- [ ] Zero errors in console during mode switching
- [ ] Shooting system still works 100% when both hands in shooting mode
- [ ] Can punch with both hands simultaneously (respecting per-hand cooldowns)

---

## FINAL VALIDATION COMMAND

**Run these tests in sequence before declaring success:**

```
1. Fresh scene load
2. Test: Both Hands → Shooting Mode (default state)
3. Fire shotgun with both hands - verify works
4. Test: Left Hand → Melee Mode
5. LMB (punch), RMB (shoot) - verify both work
6. Test: Right Hand → Melee Mode
7. LMB (punch), RMB (punch) - verify both punch
8. Test: Both Hands → Shooting Mode
9. Fire shotgun with both hands - verify still works
10. No errors in console throughout entire sequence
```

**IF ALL TESTS PASS:** Implementation successful. Ready for Phase 7 (Inventory System).

**IF ANY TEST FAILS:** Review corresponding troubleshooting section above.

---

## ARCHITECTURE PHILOSOPHY

**SINGLE RESPONSIBILITY:**
- PlayerInputHandler: Detect input only
- PlayerCombatModeManager: Route input based on mode
- PlayerShooterOrchestrator: Execute shooting logic
- PlayerMeleeExecutor: Execute melee logic
- HandFiringMechanics: Shooting mechanics (enabled/disabled externally)

**OPEN/CLOSED PRINCIPLE:**
- System open for extension (new melee weapons via inventory)
- System closed for modification (zero changes to shooting code)

**DEPENDENCY INVERSION:**
- CombatModeManager depends on abstractions (public method calls)
- Not tightly coupled to specific weapon implementations

**KISS (Keep It Simple, Stupid):**
- Mode = enum with 2 values
- Routing = if statement checking mode
- Enable/disable = single boolean property
- No complex state machines, no events, no observers

---

END OF IMPLEMENTATION INSTRUCTIONS.

EXECUTE PHASES 1-6 IN ORDER.
VALIDATE WITH TESTING PROTOCOL.
REPORT SUCCESS OR FAILURE WITH SPECIFIC TEST RESULTS.
