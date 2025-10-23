using UnityEngine;
using System;

/// <summary>
/// Manages player's sprint energy system
/// Energy depletes when sprinting and regenerates when not sprinting
/// </summary>
public class PlayerEnergySystem : MonoBehaviour
{
    [Header("Energy Settings")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float currentEnergy = 100f;
    
    [Header("Energy Depletion")]
    [Tooltip("Energy consumed per second while sprinting")]
    [SerializeField] private float energyDepletionRate = 30f; // Balanced depletion (3.3 seconds of sprint)
    
    [Header("Energy Regeneration")]
    [Tooltip("Energy regenerated per second when not sprinting")]
    [SerializeField] private float energyRegenRate = 35f; // Fast regeneration for responsive gameplay
    [Tooltip("Delay before energy starts regenerating after sprinting stops")]
    [SerializeField] private float regenDelay = 0.8f; // Short delay before regen starts
    [Tooltip("Initial burst speed multiplier (like health regen)")]
    [SerializeField] private float regenBurstMultiplier = 2.5f; // Initial burst regen
    [Tooltip("Duration of initial burst regeneration")]
    [SerializeField] private float regenBurstDuration = 1.5f; // Burst duration
    
    [Header("Minimum Energy for Sprint")]
    [Tooltip("Minimum energy required to start/continue sprinting")]
    [SerializeField] private float minEnergyToSprint = 8f; // Small buffer to prevent sprint stuttering
    
    [Header("Movement Controller References")]
    [Tooltip("Reference to movement controller for grounded checks")]
    [SerializeField] private AAAMovementController movementController;
    [Tooltip("Reference to crouch controller for sliding checks")]
    [SerializeField] private CleanAAACrouch crouchController;
    [Tooltip("Reference to camera controller for FOV changes")]
    [SerializeField] private AAACameraController cameraController;
    
    // State tracking
    private float lastSprintTime = 0f;
    private bool canSprint = true;
    private float regenStartTime = 0f; // Track when regeneration actually started
    private bool isRegenerating = false; // Track if energy is currently regenerating
    private bool wasSprintingLastFrame = false; // 🔥 Track sprint state changes for FOV
    
    // Public properties
    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public bool CanSprint => canSprint && currentEnergy >= minEnergyToSprint;
    public bool IsCurrentlySprinting
    {
        get
        {
            bool sprinting = IsSprinting();
            bool grounded = IsGrounded();
            bool sliding = IsSliding();
            return sprinting && grounded && !sliding;
        }
    }
    // Events for UI updates and sprint state changes
    public static event Action<float, float> OnEnergyChanged; // (current, max)
    public static event Action OnSprintInterrupted; // Fired when sprint is forced to stop due to no energy
    
    void Start()
    {
        currentEnergy = maxEnergy;
        canSprint = true;
        
        // Auto-find controllers if not assigned
        if (movementController == null)
            movementController = GetComponent<AAAMovementController>();
        if (crouchController == null)
            crouchController = GetComponent<CleanAAACrouch>();
        if (cameraController == null)
            cameraController = GetComponentInChildren<AAACameraController>();
        
        // Notify UI of initial energy state
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }
    
    void Update()
    {
        // CRITICAL: Skip all energy logic during bleeding out
        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null && playerHealth.isBleedingOut)
        {
            return; // Energy system disabled during bleeding out
        }
        
        // 🔥 SMART FOV: Detect sprint state changes and update FOV
        bool isSprintingNow = IsCurrentlySprinting;
        
        if (isSprintingNow != wasSprintingLastFrame)
        {
            if (isSprintingNow)
            {
                // Sprint just started
                if (cameraController != null)
                {
                    cameraController.SetSprintFOV();
                }
            }
            else
            {
                // Sprint just stopped
                if (cameraController != null)
                {
                    cameraController.SetNormalFOV();
                }
            }
            
            wasSprintingLastFrame = isSprintingNow;
        }
        
        // Handle energy regeneration
        if (!IsSprinting() && Time.time - lastSprintTime >= regenDelay)
        {
            RegenerateEnergy();
        }
        
        // Update sprint availability
        UpdateSprintAvailability();
    }
    
    /// <summary>
    /// Consume energy while sprinting
    /// </summary>
    public void ConsumeSprint(float deltaTime)
    {
        bool wasAbleToSprint = canSprint;
        
        // Reset regeneration state when sprinting
        if (isRegenerating)
        {
            isRegenerating = false;
        }
        
        if (currentEnergy <= 0)
        {
            currentEnergy = 0;
            canSprint = false;
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            
            // Fire event when sprint is interrupted due to energy depletion
            if (wasAbleToSprint)
            {
                OnSprintInterrupted?.Invoke();
                Debug.Log("[PlayerEnergySystem] Sprint interrupted - out of energy!");
            }
            return;
        }
        
        currentEnergy -= energyDepletionRate * deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        lastSprintTime = Time.time;
        
        // Check if we just ran out of energy
        if (currentEnergy <= 0 && wasAbleToSprint)
        {
            canSprint = false;
            OnSprintInterrupted?.Invoke();
            Debug.Log("[PlayerEnergySystem] Sprint interrupted - energy depleted!");
        }
        
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }
    
    /// <summary>
    /// Regenerate energy when not sprinting - uses burst mechanic like health regen
    /// </summary>
    private void RegenerateEnergy()
    {
        if (currentEnergy < maxEnergy)
        {
            // Track when regeneration starts
            if (!isRegenerating)
            {
                isRegenerating = true;
                regenStartTime = Time.time;
                // Debug.Log($"[PlayerEnergySystem] Energy regeneration started! Current: {currentEnergy:F1}/{maxEnergy}");
            }
            
            // Calculate regeneration amount with fast-then-slow burst curve
            float regenTime = Time.time - regenStartTime;
            float regenMultiplier = 1f;
            
            if (regenTime < regenBurstDuration)
            {
                // Initial burst phase - fast regeneration (motivates stopping sprint)
                float burstProgress = regenTime / regenBurstDuration;
                // Smooth transition from burst to normal speed
                regenMultiplier = Mathf.Lerp(regenBurstMultiplier, 1f, burstProgress);
            }
            else
            {
                // Normal regeneration phase
                regenMultiplier = 1f;
            }
            
            // Apply regeneration with multiplier
            float regenAmount = energyRegenRate * regenMultiplier * Time.deltaTime;
            currentEnergy += regenAmount;
            currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
            
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            
            // Check if fully recharged
            if (currentEnergy >= maxEnergy)
            {
                isRegenerating = false;
                // Debug.Log("[PlayerEnergySystem] Energy fully recharged");
            }
        }
        else
        {
            // Energy is full, reset regeneration state
            if (isRegenerating)
            {
                isRegenerating = false;
            }
        }
    }
    
    /// <summary>
    /// Update whether player can sprint based on energy
    /// </summary>
    private void UpdateSprintAvailability()
    {
        // Can sprint again once we have minimum energy
        if (!canSprint && currentEnergy >= minEnergyToSprint)
        {
            canSprint = true;
        }
        // Can't sprint if energy is depleted
        else if (canSprint && currentEnergy < minEnergyToSprint)
        {
            canSprint = false;
        }
    }
    
    /// <summary>
    /// Check if currently sprinting (based on input)
    /// </summary>
    private bool IsSprinting()
    {
        // CRITICAL FIX: Sprint requires MOVEMENT + Sprint key!
        // Check if sprint key is held AND player is actually moving
        bool sprintKeyHeld = Input.GetKey(Controls.Boost);
        bool hasMovementInput = Input.GetKey(Controls.MoveForward) || 
                                Input.GetKey(Controls.MoveBackward) || 
                                Input.GetKey(Controls.MoveLeft) || 
                                Input.GetKey(Controls.MoveRight);
        
        return sprintKeyHeld && canSprint && hasMovementInput;
    }
    
    /// <summary>
    /// Check if player is grounded via movement controller
    /// </summary>
    private bool IsGrounded()
    {
        // SAFE FALLBACK: Return true if no movement controller
        if (movementController == null)
        {
            Debug.LogWarning("[PlayerEnergySystem] No AAAMovementController found - assuming grounded");
            return true;
        }
        return movementController.IsGrounded;
    }
    
    /// <summary>
    /// Check if player is sliding via crouch controller
    /// </summary>
    private bool IsSliding()
    {
        // SAFE FALLBACK: Return false if no crouch controller
        if (crouchController == null)
        {
            // This is normal - not all setups have crouch controller
            return false;
        }
        return crouchController.IsSliding;
    }
    
    /// <summary>
    /// Force set energy to a specific value (for powerups, etc.)
    /// </summary>
    public void SetEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(amount, 0, maxEnergy);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }
    
    /// <summary>
    /// Add energy (for pickups, etc.)
    /// </summary>
    public void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }
}
