using UnityEngine;

/// <summary>
/// Central place to define every gameplay key. Update a value here once and it
/// automatically applies across all scripts that use Controls.*
/// Enhanced for AAA FPS Movement System - Now configurable via InputSettings ScriptableObject!
/// </summary>
public static class Controls
{
    // === MOVEMENT CONTROLS ===
    public static KeyCode MoveForward { get; private set; } = KeyCode.W;
    public static KeyCode MoveBackward { get; private set; } = KeyCode.S;
    public static KeyCode MoveLeft { get; private set; } = KeyCode.A;
    public static KeyCode MoveRight { get; private set; } = KeyCode.D;

    // === CORE ACTIONS ===
    public static KeyCode Jump { get; private set; } = KeyCode.Space;
    public static KeyCode Sprint { get; private set; } = KeyCode.LeftShift;
    public static KeyCode Boost { get; private set; } = KeyCode.LeftShift; // Same as sprint
    public static KeyCode Crouch { get; private set; } = KeyCode.LeftControl;
    public static KeyCode Slide { get; private set; } = KeyCode.LeftControl; // Same as crouch
    public static KeyCode FlightToggle { get; private set; } = KeyCode.F;

    // === FLIGHT CONTROLS ===
    public static KeyCode RollLeft { get; private set; } = KeyCode.Q;
    public static KeyCode RollRight { get; private set; } = KeyCode.E;
    public static KeyCode DownThrust { get; private set; } = KeyCode.LeftControl;
    public static KeyCode UpThrustJump { get; private set; } = KeyCode.Space; // Same as jump

    // === COMBAT & INTERACTION ===
    public static KeyCode Interact { get; private set; } = KeyCode.E;
    public static KeyCode ArmorPlate { get; private set; } = KeyCode.R;
    public static KeyCode Dive { get; private set; } = KeyCode.X;
    public static KeyCode Reload { get; private set; } = KeyCode.R;
    public static KeyCode Melee { get; private set; } = KeyCode.V;
    public static KeyCode LockOn { get; private set; } = KeyCode.R;
    public static KeyCode SwordModeToggle { get; private set; } = KeyCode.Backspace;
    
    // === MOUSE BUTTONS ===
    public static int RopeSwingButton { get; private set; } = 4; // Mouse5 (Side Button 2) - Click to shoot, Hold to retract
    public static int SwordEquipButton { get; private set; } = 3; // Mouse4 (Side Button 1) - Toggle sword mode

    // === EMOTE KEYS (ARROW KEYS - RIGHT HAND ONLY) ===
    public static KeyCode Emote1 { get; private set; } = KeyCode.UpArrow;    // Up Arrow
    public static KeyCode Emote2 { get; private set; } = KeyCode.DownArrow;  // Down Arrow
    public static KeyCode Emote3 { get; private set; } = KeyCode.LeftArrow;  // Left Arrow
    public static KeyCode Emote4 { get; private set; } = KeyCode.RightArrow; // Right Arrow

    // === UTILITY KEYS ===
    public static KeyCode Pause { get; private set; } = KeyCode.Escape;
    public static KeyCode Inventory { get; private set; } = KeyCode.Tab;
    public static KeyCode Walk { get; private set; } = KeyCode.LeftAlt;
    public static KeyCode Debug { get; private set; } = KeyCode.F5;

    // === LEGACY COMPATIBILITY ===
    public static KeyCode ToggleFlight => FlightToggle; // Backward compatibility
    public static KeyCode Lean { get; private set; } = KeyCode.Mouse2; // Middle mouse for leaning

    /// <summary>
    /// Update all key bindings from InputSettings ScriptableObject
    /// Called by InputSettings when values change or at game start
    /// </summary>
    public static void UpdateFromSettings(InputSettings settings)
    {
        if (settings == null) return;

        // Movement controls
        MoveForward = settings.moveForward;
        MoveBackward = settings.moveBackward;
        MoveLeft = settings.moveLeft;
        MoveRight = settings.moveRight;

        // Core actions
        Jump = settings.jump;
        Sprint = settings.sprint;
        Boost = settings.sprint; // Same as sprint
        Crouch = settings.crouch;
        Slide = settings.crouch; // Same as crouch
        FlightToggle = settings.flightToggle;

        // Flight controls
        RollLeft = settings.rollLeft;
        RollRight = settings.rollRight;
        DownThrust = settings.downThrust;
        UpThrustJump = settings.jump; // Same as jump

        // Combat & interaction
        Interact = settings.interact;
        ArmorPlate = settings.armorPlate;
        Dive = settings.dive;
        Reload = settings.reload;
        Melee = settings.melee;
        LockOn = settings.lockOn;
        SwordModeToggle = settings.swordModeToggle;

        // Emote keys
        Emote1 = settings.emote1;
        Emote2 = settings.emote2;
        Emote3 = settings.emote3;
        Emote4 = settings.emote4;

        // Utility keys
        Pause = settings.pause;
        Inventory = settings.inventory;
        Walk = settings.walk;
        Debug = settings.debug;

        UnityEngine.Debug.Log("[CONTROLS] Key bindings updated from InputSettings");
    }

    // Axis helpers for scripts that previously relied on "Horizontal" / "Vertical" Input axes.
    // These return -1, 0, or +1 just like Input.GetAxisRaw but use the layout-aware key constants.
    public static float HorizontalRaw()
    {
        float v = 0f;
        if (Input.GetKey(MoveLeft))  v -= 1f;
        if (Input.GetKey(MoveRight)) v += 1f;
        return v;
    }

    public static float VerticalRaw()
    {
        float v = 0f;
        if (Input.GetKey(MoveBackward)) v -= 1f;
        if (Input.GetKey(MoveForward))  v += 1f;
        return v;
    }
}
