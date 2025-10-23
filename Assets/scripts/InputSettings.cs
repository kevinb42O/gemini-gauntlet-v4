using UnityEngine;

/// <summary>
/// ScriptableObject for configuring all game input keys in the Inspector
/// This allows complete customization of controls without touching code
/// </summary>
[CreateAssetMenu(fileName = "InputSettings", menuName = "Game/Input Settings")]
public class InputSettings : ScriptableObject
{
    [Header("=== MOVEMENT CONTROLS ===")]
    [Tooltip("Move forward key")]
    public KeyCode moveForward = KeyCode.W;
    [Tooltip("Move backward key")]
    public KeyCode moveBackward = KeyCode.S;
    [Tooltip("Move left key")]
    public KeyCode moveLeft = KeyCode.A;
    [Tooltip("Move right key")]
    public KeyCode moveRight = KeyCode.D;
    
    [Header("=== CORE ACTIONS ===")]
    [Tooltip("Jump key")]
    public KeyCode jump = KeyCode.Space;
    [Tooltip("Sprint/Boost key")]
    public KeyCode sprint = KeyCode.LeftShift;
    [Tooltip("Crouch/Slide key")]
    public KeyCode crouch = KeyCode.LeftControl;
    [Tooltip("Flight toggle key")]
    public KeyCode flightToggle = KeyCode.F;
    
    [Header("=== FLIGHT CONTROLS ===")]
    [Tooltip("Roll left in flight")]
    public KeyCode rollLeft = KeyCode.Q;
    [Tooltip("Roll right in flight")]
    public KeyCode rollRight = KeyCode.E;
    [Tooltip("Down thrust in flight")]
    public KeyCode downThrust = KeyCode.LeftControl;
    
    [Header("=== COMBAT & INTERACTION ===")]
    [Tooltip("Interact with objects (chests, doors, etc.)")]
    public KeyCode interact = KeyCode.E;
    [Tooltip("Apply armor plates")]
    public KeyCode armorPlate = KeyCode.R;
    [Tooltip("Tactical dive")]
    public KeyCode dive = KeyCode.X;
    [Tooltip("Reload weapon")]
    public KeyCode reload = KeyCode.R;
    [Tooltip("Melee attack")]
    public KeyCode melee = KeyCode.V;
    [Tooltip("Toggle sword mode (right hand only)")]
    public KeyCode swordModeToggle = KeyCode.Backspace;
    
    [Header("=== EMOTE KEYS (RIGHT HAND ONLY) ===")]
    [Tooltip("Emote 1 - Up Arrow")]
    public KeyCode emote1 = KeyCode.UpArrow;
    [Tooltip("Emote 2 - Down Arrow")]
    public KeyCode emote2 = KeyCode.DownArrow;
    [Tooltip("Emote 3 - Left Arrow")]
    public KeyCode emote3 = KeyCode.LeftArrow;
    [Tooltip("Emote 4 - Right Arrow")]
    public KeyCode emote4 = KeyCode.RightArrow;
    
    [Header("=== UTILITY KEYS ===")]
    [Tooltip("Pause menu")]
    public KeyCode pause = KeyCode.Escape;
    [Tooltip("Inventory")]
    public KeyCode inventory = KeyCode.Tab;
    [Tooltip("Walk modifier")]
    public KeyCode walk = KeyCode.LeftAlt;
    [Tooltip("Lock-on target")]
    public KeyCode lockOn = KeyCode.R;
    
    [Header("=== DEBUG KEYS ===")]
    [Tooltip("Debug key for testing")]
    public KeyCode debug = KeyCode.F5;
    
    /// <summary>
    /// Called when the ScriptableObject is loaded or values change in Inspector
    /// This updates the static Controls class with new values
    /// </summary>
    void OnValidate()
    {
        // Update Controls class when values change in Inspector
        if (Application.isPlaying)
        {
            Controls.UpdateFromSettings(this);
        }
    }
    
    /// <summary>
    /// Initialize Controls class with these settings
    /// </summary>
    public void InitializeControls()
    {
        Controls.UpdateFromSettings(this);
    }
}
