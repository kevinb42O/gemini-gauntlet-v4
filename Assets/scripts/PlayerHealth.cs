// --- PlayerHealth.cs (CORRECTED) ---
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using GeminiGauntlet.Audio;
using GeminiGauntlet.Progression;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public float maxHealth = 5000f;
    private float _currentHealth;
    public float CurrentHealth { get { return _currentHealth; } private set { _currentHealth = value; } }
    public bool isDead { get; private set; } = false;

    [Header("Health Regeneration System")]
    [SerializeField] private float healthRegenRate = 15f; // HP per second (fast-then-slow curve)
    [SerializeField] private float regenDelayAfterDamage = 5f; // Delay before regen starts
    [SerializeField] private float regenBurstMultiplier = 3f; // Initial burst speed multiplier
    [SerializeField] private float regenBurstDuration = 2f; // Duration of initial burst
    
    private float lastDamageTime = -999f; // Initialize to large negative value so regen is available immediately
    private bool isRegenerating = false;
    private Coroutine regenCoroutine;
    
    [Header("Armor Plate System")]
    private ArmorPlateSystem armorPlateSystem;
    
    [Header("Death System")]
    [SerializeField] private float sceneResetDelay = 2f;

    [Header("Damage Feedback")]
    public float hitEffectDuration = 0.15f;
    public CanvasGroup hitEffectCanvasGroup;
    
    [Header("AAA Blood Splat Feedback System")]
    [SerializeField] private float bloodSplatFadeInSpeed = 3f;
    [SerializeField] private float bloodSplatFadeOutSpeed = 1.5f;
    [SerializeField] private float bloodSplatMinAlpha = 0f;
    [SerializeField] private float bloodSplatMaxAlpha = 0.8f;
    [SerializeField] private float bloodSplatLowHealthThreshold = 0.3f; // Show more blood below 30% health
    [SerializeField] private float bloodSplatHitCooldown = 0.3f; // Prevent rapid flickering from stream damage
    
    [Header("Directional Hit Indicator System")]
    [SerializeField] private DirectionalBloodHitIndicator directionalHitIndicator; // Assign in inspector
    
    private float bloodSplatTargetAlpha = 0f;
    private float bloodSplatCurrentAlpha = 0f;
    private float lastBloodSplatHitTime = -999f;
    private Coroutine bloodSplatFadeCoroutine;

    [Header("God Mode PowerUp State (Runtime)")]
    public bool IsGodModeActive { get; private set; } = false;
    
    [Header("Self-Revive Invulnerability Grace Period")]
    public bool IsInvulnerable { get; private set; } = false;
    private Coroutine invulnerabilityCoroutine;
    
    [Header("Simple Cheat Godmode (No Particle Effects)")]
    public bool isInvincible = false; // Set by AAACheatManager for simple godmode
    
    [Header("Instant Cooldown PowerUp State (Runtime)")]
    public bool IsInstantCooldownActive { get; private set; } = false;
    
    [Header("Double Damage PowerUp State (Runtime)")]
    public bool IsDoubleDamageActive { get; private set; } = false;
    
    [Header("Powerup Visual Effects")]
    [SerializeField] private ParticleSystem godModeParticleEffect;
    [SerializeField] private ParticleSystem doubleGemsParticleEffect;
    [SerializeField] private ParticleSystem slowTimeParticleEffect;
    
    // Public property to check if godmode particle effect is assigned
    public bool HasGodModeParticleEffect => godModeParticleEffect != null;
    
    // Sound effects are handled by the GameSoundEvent asset
    private Coroutine _godModeCoroutine;
    private float _godModeEndTime = 0f;
    private GameObject _currentGodModeEffect;
    
    // Powerup effect duration tracking
    private Coroutine _doubleGemsEffectCoroutine;
    private Coroutine _slowTimeEffectCoroutine;

    // --- COMPREHENSIVE MOVEMENT CONTROL REFERENCES ---
    [Header("Movement Controllers (ASSIGN ALL IN INSPECTOR)")]
    [SerializeField] private PlayerMovementManager _playerMovementManager;
    [SerializeField] private CelestialDriftController _celestialDriftController;
    [SerializeField] private AAAMovementController _aaaMovementController;
    [SerializeField] private AAAMovementIntegrator _aaaMovementIntegrator;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private PlayerShooterOrchestrator _playerShooterOrchestrator;
    
    // Movement state tracking
    private enum MovementMode { Flight, Ground }
    private MovementMode _movementModeBeforeDeath = MovementMode.Flight;
    private bool _wasInAAAModeBeforeDeath = false;
    
    private Coroutine _hitEffectCoroutine;
    private float _timeOfDeath = 0f;
    
    [Header("Self-Revive System")]
    [SerializeField] private SelfReviveUIManager selfReviveUIManager;
    [SerializeField] private ReviveSlotController reviveSlotController;
    [SerializeField] private SelfReviveUI selfReviveUI; // Legacy - will be replaced by SelfReviveUIManager
    
    [Header("Bleeding Out System")]
    [SerializeField] private BleedOutUIManager bleedOutUIManager;
    [SerializeField] private DeathCameraController deathCameraController;
    [SerializeField] private BleedOutMovementController bleedOutMovementController; // Crawl movement during bleed out
    private AAACameraController aaaCameraController; // FPS camera controller
    
    [Header("Death Visual Effects")]
    public GameObject bloodOverlayImage; // Assign your blood splat overlay UI element
    private CanvasGroup bloodOverlayCanvasGroup; // Cache the canvas group for alpha control
    private Canvas bloodOverlayCanvas; // Cache the canvas for sorting order management
    
    [Header("Blood Overlay Canvas Settings")]
    [SerializeField] private int bloodOverlayNormalSortOrder = 100; // Normal gameplay sort order
    [SerializeField] private int bloodOverlayPausedSortOrder = -1; // Behind pause menu when paused
    
    // Self-revive state tracking
    private bool isWaitingForRevive = false;
    private bool hasUsedSelfRevive = false;
    private bool isSelfReviving = false;
    private Coroutine bloodBlinkCoroutine;
    
    // Bleeding out state tracking
    public bool isBleedingOut = false; // Public so movement system can check for slow crawl mode
    private Coroutine bloodPulsateCoroutine;

    public static event Action OnPlayerDied;
    public static event Action<float, float> OnHealthChangedForHUD;
    public static event Action<PowerUpType, bool, float> OnPowerUpStatusChangedForHUD;

    void Awake()
    {
        
        _currentHealth = maxHealth;
        isDead = false;
        
        // Initialize all powerup particle effects - start disabled
        if (godModeParticleEffect != null)
        {
            godModeParticleEffect.Stop();
            Debug.Log("[PlayerHealth] GodMode particle effect initialized and stopped", this);
        }
        if (doubleGemsParticleEffect != null)
        {
            doubleGemsParticleEffect.Stop();
            Debug.Log("[PlayerHealth] DoubleGems particle effect initialized and stopped", this);
        }
        if (slowTimeParticleEffect != null)
        {
            slowTimeParticleEffect.Stop();
            Debug.Log("[PlayerHealth] SlowTime particle effect initialized and stopped", this);
        }
        
        // --- COMPREHENSIVE: Get all movement controller references (fallback if not assigned) ---
        if (_characterController == null) _characterController = GetComponent<CharacterController>();
        if (_celestialDriftController == null) _celestialDriftController = GetComponent<CelestialDriftController>();
        if (_playerMovementManager == null) _playerMovementManager = GetComponent<PlayerMovementManager>();
        if (_playerShooterOrchestrator == null) _playerShooterOrchestrator = GetComponent<PlayerShooterOrchestrator>();
        if (_aaaMovementController == null) _aaaMovementController = GetComponent<AAAMovementController>();
        if (_aaaMovementIntegrator == null) _aaaMovementIntegrator = GetComponent<AAAMovementIntegrator>();
        
        
        if (hitEffectCanvasGroup != null) hitEffectCanvasGroup.alpha = 0;
        
        // Cache blood overlay canvas group for alpha control
        if (bloodOverlayImage != null)
        {
            bloodOverlayCanvasGroup = bloodOverlayImage.GetComponent<CanvasGroup>();
            if (bloodOverlayCanvasGroup != null)
            {
                bloodOverlayCanvasGroup.alpha = 0; // Start hidden
                Debug.Log("[PlayerHealth] Found CanvasGroup on blood overlay - using alpha control");
            }
            else
            {
                Debug.Log("[PlayerHealth] No CanvasGroup found on blood overlay - using SetActive control");
            }
            
            // Cache the canvas for sorting order management
            bloodOverlayCanvas = bloodOverlayImage.GetComponentInParent<Canvas>();
            if (bloodOverlayCanvas != null)
            {
                // CRITICAL SAFETY CHECK: Only manage sorting order if canvas has override sorting enabled
                // This prevents breaking shared canvases (like main UI canvas)
                if (bloodOverlayCanvas.overrideSorting)
                {
                    // Set initial sort order for normal gameplay
                    bloodOverlayCanvas.sortingOrder = bloodOverlayNormalSortOrder;
                    Debug.Log($"[PlayerHealth] ✅ Blood overlay canvas found: {bloodOverlayCanvas.name}, managing sort order: {bloodOverlayNormalSortOrder}");
                }
                else
                {
                    Debug.LogWarning($"[PlayerHealth] ⚠️ Blood overlay canvas '{bloodOverlayCanvas.name}' does NOT have overrideSorting enabled!");
                    Debug.LogWarning("[PlayerHealth] ⚠️ Sorting order management DISABLED to prevent breaking shared canvas!");
                    Debug.LogWarning("[PlayerHealth] ⚠️ For proper pause menu layering, put blood overlay on a separate canvas with overrideSorting=true");
                    bloodOverlayCanvas = null; // Disable sorting order management
                }
            }
            else
            {
                Debug.LogWarning("[PlayerHealth] No Canvas found for blood overlay - sorting order management disabled!");
            }
        }
        
        // Initialize armor plate system
        armorPlateSystem = GetComponent<ArmorPlateSystem>();
        if (armorPlateSystem == null)
        {
            Debug.LogWarning("[PlayerHealth] ArmorPlateSystem not found! Plate damage absorption will not work.");
        }
        
        // Initialize self-revive system
        if (reviveSlotController == null)
        {
            reviveSlotController = FindObjectOfType<ReviveSlotController>();
        }
        
        // Initialize bleeding out system
        if (bleedOutUIManager == null)
        {
            bleedOutUIManager = FindObjectOfType<BleedOutUIManager>();
            if (bleedOutUIManager == null)
            {
                // Create BleedOutUIManager at runtime if not found
                GameObject bleedOutGO = new GameObject("BleedOutUIManager");
                bleedOutUIManager = bleedOutGO.AddComponent<BleedOutUIManager>();
                Debug.Log("[PlayerHealth] Created BleedOutUIManager at runtime");
            }
        }
        
        // Find death camera controller
        if (deathCameraController == null)
        {
            deathCameraController = FindObjectOfType<DeathCameraController>();
            if (deathCameraController == null)
            {
                Debug.LogWarning("[PlayerHealth] No DeathCameraController found in scene - creating one");
                GameObject dcObj = new GameObject("DeathCameraController");
                deathCameraController = dcObj.AddComponent<DeathCameraController>();
            }
        }
        
        // Find AAA camera controller
        if (aaaCameraController == null)
        {
            aaaCameraController = FindObjectOfType<AAACameraController>();
            if (aaaCameraController != null)
            {
                Debug.Log("[PlayerHealth] Found AAACameraController - will disable during bleeding out");
            }
        }
        
        // Find BleedOutMovementController
        if (bleedOutMovementController == null)
        {
            bleedOutMovementController = GetComponent<BleedOutMovementController>();
            if (bleedOutMovementController != null)
            {
                Debug.Log("[PlayerHealth] Found BleedOutMovementController - will manage during bleed out");
            }
        }
        
        // Set up bleeding out callbacks
        if (bleedOutUIManager != null)
        {
            bleedOutUIManager.OnBleedOutComplete += OnBleedOutComplete;
            bleedOutUIManager.OnSelfReviveRequested += OnSelfReviveRequested;
            bleedOutUIManager.OnBleedOutProgress += OnBleedOutProgress;
            Debug.Log("[PlayerHealth] BleedOutUIManager callbacks registered");
        }
        
        // Initialize death camera controller
        if (deathCameraController == null)
        {
            deathCameraController = FindObjectOfType<DeathCameraController>();
        }
        
        // DEPRECATED: Legacy self-revive UI systems - kept for backward compatibility
        // The new BleedOutUIManager replaces these systems
        if (selfReviveUIManager != null)
        {
            selfReviveUIManager.enabled = false;
            Debug.Log("[PlayerHealth] Disabled legacy SelfReviveUIManager - using new BleedOutUIManager");
        }
        if (selfReviveUI != null)
        {
            selfReviveUI.enabled = false;
            Debug.Log("[PlayerHealth] Disabled legacy SelfReviveUI - using new BleedOutUIManager");
        }
    }

    void Start()
    {
        OnHealthChangedForHUD?.Invoke(_currentHealth, maxHealth);
        
        // Play player spawn sound
        GameSounds.PlayPlayerSpawn(transform.position);
        
        // Start health regeneration system
        regenCoroutine = StartCoroutine(HealthRegenerationCoroutine());
    }
    
    void FixedUpdate()
    {
        // CRITICAL: Continuously clamp physics during bleeding out to prevent falling
        if (isBleedingOut)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                // Zero out vertical velocity EVERY physics frame
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            }
        }
    }
    
    void OnDestroy()
    {
        Debug.Log("[PlayerHealth] OnDestroy called - Cleaning up powerup states and UI subscriptions", this);
        
        // CRITICAL FIX: Stop ALL coroutines before cleanup
        StopAllCoroutines();
        
        // CRITICAL FIX: Clean up all active powerup states and coroutines
        CleanupAllPowerupStates();
        
        // CRITICAL: Unsubscribe from bleeding out events to prevent memory leaks
        // Use try-catch to handle cases where bleedOutUIManager was destroyed first
        try
        {
            if (bleedOutUIManager != null)
            {
                bleedOutUIManager.OnBleedOutComplete -= OnBleedOutComplete;
                bleedOutUIManager.OnSelfReviveRequested -= OnSelfReviveRequested;
                bleedOutUIManager.OnBleedOutProgress -= OnBleedOutProgress;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerHealth] Exception during event unsubscribe: {e.Message}", this);
        }
    }
    
    /// <summary>
    /// Clean up all active powerup states and coroutines
    /// </summary>
    private void CleanupAllPowerupStates()
    {
        Debug.Log("[PlayerHealth] Cleaning up all powerup states", this);
        
        // Stop all powerup coroutines
        if (_godModeCoroutine != null)
        {
            StopCoroutine(_godModeCoroutine);
            _godModeCoroutine = null;
            Debug.Log("[PlayerHealth] Stopped GodMode coroutine", this);
        }
        
        if (_doubleGemsEffectCoroutine != null)
        {
            StopCoroutine(_doubleGemsEffectCoroutine);
            _doubleGemsEffectCoroutine = null;
            Debug.Log("[PlayerHealth] Stopped DoubleGems effect coroutine", this);
        }
        
        if (_slowTimeEffectCoroutine != null)
        {
            StopCoroutine(_slowTimeEffectCoroutine);
            _slowTimeEffectCoroutine = null;
            Debug.Log("[PlayerHealth] Stopped SlowTime effect coroutine", this);
        }
        
        // Reset powerup states
        IsGodModeActive = false;
        IsInstantCooldownActive = false;
        
        // Stop all particle effects
        if (godModeParticleEffect != null && godModeParticleEffect.isPlaying)
        {
            godModeParticleEffect.Stop();
            Debug.Log("[PlayerHealth] Stopped GodMode particle effect", this);
        }
        
        if (doubleGemsParticleEffect != null && doubleGemsParticleEffect.isPlaying)
        {
            doubleGemsParticleEffect.Stop();
            Debug.Log("[PlayerHealth] Stopped DoubleGems particle effect", this);
        }
        
        if (slowTimeParticleEffect != null && slowTimeParticleEffect.isPlaying)
        {
            slowTimeParticleEffect.Stop();
            Debug.Log("[PlayerHealth] Stopped SlowTime particle effect", this);
        }
    }



    public void TakeDamage(float amount)
    {
        // Check simple cheat godmode (no particle effects)
        if (isInvincible)
        {
            return; // Silent block, no debug spam
        }
        
        // Check invulnerability grace period (self-revive protection)
        if (IsInvulnerable)
        {
            Debug.Log($"[PlayerHealth] TakeDamage blocked by invulnerability grace period! Damage: {amount}", this);
            return;
        }
        
        // Debug logging for godmode functionality
        if (IsGodModeActive)
        {
            Debug.Log($"[PlayerHealth] TakeDamage blocked by godmode! Damage: {amount}, IsGodModeActive: {IsGodModeActive}", this);
            return;
        }
        
        if (isDead || amount <= 0) return;

        // ARMOR PLATE SYSTEM: Route damage through plates first
        float damageToHealth = amount;
        if (armorPlateSystem != null)
        {
            damageToHealth = armorPlateSystem.ProcessDamage(amount);
            Debug.Log($"[PlayerHealth] Damage processed - Original: {amount}, To Health: {damageToHealth}");
        }
        
        // Apply remaining damage to health
        if (damageToHealth > 0)
        {
            _currentHealth -= damageToHealth;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);
            OnHealthChangedForHUD?.Invoke(_currentHealth, maxHealth);
            
            // Add camera trauma based on damage severity
            if (aaaCameraController != null)
            {
                // Scale trauma by damage (0-1 range, capped at max health)
                float traumaAmount = Mathf.Clamp01(damageToHealth / maxHealth) * 0.5f; // Max 0.5 trauma per hit
                aaaCameraController.AddTrauma(traumaAmount);
            }
            
            // Record damage time for regeneration system
            lastDamageTime = Time.time;
            
            // Stop current regeneration if active
            if (regenCoroutine != null)
            {
                StopCoroutine(regenCoroutine);
                isRegenerating = false;
            }
            
            // Start regeneration delay
            regenCoroutine = StartCoroutine(HealthRegenerationCoroutine());
        }

        if (hitEffectCanvasGroup != null && gameObject.activeInHierarchy)
        {
            if (_hitEffectCoroutine != null) StopCoroutine(_hitEffectCoroutine);
            _hitEffectCoroutine = StartCoroutine(HitEffectRoutine());
        }
        
        // AAA Blood Splat Feedback - smooth fade based on health
        TriggerBloodSplatFeedback();

        if (_currentHealth <= 0) Die();
    }
    
    /// <summary>
    /// IDamageable interface implementation - handles directional damage
    /// hitDirection: the direction the ray came from (weapon forward)
    /// </summary>
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        Debug.Log($"[PlayerHealth] IDamageable.TakeDamage called with amount: {amount}, hitPoint: {hitPoint}, hitDirection: {hitDirection}", this);
        
        // Show directional hit indicator if assigned
        if (directionalHitIndicator != null)
        {
            // hitDirection is the direction FROM the attacker TO the player
            directionalHitIndicator.ShowHitFromDirection(hitDirection);
        }
        
        // Route to the main TakeDamage method which handles godmode and damage processing
        TakeDamage(amount);
    }
    
    /// <summary>
    /// Heal the player by the specified amount
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead || amount <= 0) return;
        
        _currentHealth += amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);
        OnHealthChangedForHUD?.Invoke(_currentHealth, maxHealth);
        
        Debug.Log($"[PlayerHealth] Healed {amount} HP. Current health: {_currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// Take damage directly to health, bypassing armor plates
    /// Used for environmental damage like falling
    /// </summary>
    public void TakeDamageBypassArmor(float amount)
    {
        // Check simple cheat godmode (no particle effects)
        if (isInvincible)
        {
            return; // Silent block, no debug spam
        }
        
        // Check invulnerability grace period (self-revive protection)
        if (IsInvulnerable)
        {
            Debug.Log($"[PlayerHealth] TakeDamageBypassArmor blocked by invulnerability grace period! Damage: {amount}", this);
            return;
        }
        
        // Check godmode protection
        if (IsGodModeActive)
        {
            Debug.Log($"[PlayerHealth] TakeDamageBypassArmor blocked by godmode! Damage: {amount}", this);
            return;
        }
        
        if (isDead || amount <= 0) return;
        
        // Apply damage directly to health (bypass armor plates)
        _currentHealth -= amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);
        OnHealthChangedForHUD?.Invoke(_currentHealth, maxHealth);
        
        Debug.Log($"[PlayerHealth] Direct health damage (bypass armor): {amount}, Current health: {_currentHealth}/{maxHealth}");
        
        // Record damage time for regeneration system
        lastDamageTime = Time.time;
        
        // Stop current regeneration if active
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            isRegenerating = false;
        }
        
        // Start regeneration delay
        regenCoroutine = StartCoroutine(HealthRegenerationCoroutine());
        
        // Show hit effect
        if (hitEffectCanvasGroup != null && gameObject.activeInHierarchy)
        {
            if (_hitEffectCoroutine != null) StopCoroutine(_hitEffectCoroutine);
            _hitEffectCoroutine = StartCoroutine(HitEffectRoutine());
        }
        
        // AAA Blood Splat Feedback - smooth fade based on health
        TriggerBloodSplatFeedback();
        
        // Check for death
        if (_currentHealth <= 0) Die();
    }

    private IEnumerator HitEffectRoutine()
    {
        if (hitEffectCanvasGroup == null) yield break;
        hitEffectCanvasGroup.alpha = 1f;
        yield return new WaitForSeconds(hitEffectDuration);
        hitEffectCanvasGroup.alpha = 0f;
        _hitEffectCoroutine = null;
    }

    public void Die()
    {
        // CRITICAL FIX: Check godmode protection FIRST before deactivating anything!
        if (IsGodModeActive)
        {
            Debug.Log("[PlayerHealth] Die() blocked by godmode! Player is invincible.", this);
            return;
        }
        
        // Only deactivate powerups if death is actually proceeding
        // Deactivate instant cooldown if active
        if (IsInstantCooldownActive)
        {
            DeactivateInstantCooldown();
        }
        
        if (isDead || isBleedingOut) return; // Don't die multiple times
        
        // DON'T set isDead yet - only set it when bleed out completes!
        // isDead will be set in OnBleedOutComplete() when timer expires
        _timeOfDeath = Time.time;
        
        Debug.Log("=== PLAYER BLEEDING OUT STARTED ===");
        Debug.Log($"[PlayerHealth] Die() called. bloodOverlayImage={bloodOverlayImage != null}");
        
        // Record stats for this death
        int enemiesKilled = GameStats.CurrentGameTotalEnemiesKilled;
        int gemsCollected = GameStats.CurrentRunGemsCollected;
        int primaryHandLevel = PlayerProgression.Instance != null ? PlayerProgression.Instance.primaryHandLevel : 1;
        int secondaryHandLevel = PlayerProgression.Instance != null ? PlayerProgression.Instance.secondaryHandLevel : 1;
        
        GameStats.RecordPlayerDeathStats(enemiesKilled, gemsCollected, primaryHandLevel, secondaryHandLevel, GetCurrentSurvivalTime());
        
        // Note: Inventory clearing moved to ProceedWithNormalDeath() 
        // If player has self-revive, inventory is preserved until they choose not to use it

        // Check if player has self-revive available
        bool hasSelfRevive = reviveSlotController != null && reviveSlotController.HasRevives() && !hasUsedSelfRevive;
        
        Debug.Log($"[PlayerHealth] Die() - hasSelfRevive={hasSelfRevive}");
        
        // CRITICAL: Save movement mode BEFORE disabling everything
        SaveMovementModeBeforeDeath();
        
        // DON'T play death sound yet - only play when bleeding out finishes!
        // Death sound is for ACTUAL death, not entering bleeding out state
        
        // Start bleeding out sounds (breathing + heartbeat loops)
        GameSounds.StartBleedingOutSounds(transform);
        
        // Show blood overlay (will pulsate during bleed out)
        Debug.Log("[PlayerHealth] Starting bleeding out sequence - Player can crawl around");
        ShowBloodOverlay();
        
        // Start bleeding out sequence (works for both scenarios)
        isBleedingOut = true;
        
        // CRITICAL: Disable shooting IMMEDIATELY
        if (_playerShooterOrchestrator != null)
        {
            _playerShooterOrchestrator.enabled = false;
            Debug.Log("[PlayerHealth] Disabled PlayerShooterOrchestrator - no shooting while bleeding out");
        }
        
        // CRITICAL: CharacterController will be managed by BleedOutMovementController
        // We ensure it exists but DON'T force enable it here (ownership conflict)
        if (_characterController == null)
        {
            Debug.LogError("[PlayerHealth] CharacterController is NULL! Bleeding out movement will fail!");
        }
        
        // CRITICAL: Stop any vertical velocity to prevent falling
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // Zero out vertical velocity
            rb.isKinematic = true; // Make kinematic to prevent physics interference
            Debug.Log("[PlayerHealth] Set Rigidbody to kinematic and zeroed velocity");
        }
        
        if (bleedOutUIManager != null)
        {
            bleedOutUIManager.StartBleedOut(hasSelfRevive);
        }
        else
        {
            Debug.LogError("[PlayerHealth] BleedOutUIManager not found! Cannot show bleeding out UI.");
            // Fallback to immediate death
            ProceedWithNormalDeath();
            return;
        }
        
        // Start pulsating blood overlay
        if (bloodPulsateCoroutine != null)
        {
            StopCoroutine(bloodPulsateCoroutine);
        }
        bloodPulsateCoroutine = StartCoroutine(PulsateBloodOverlay());
        
        // CRITICAL: Disable AAA camera controller to prevent camera fight!
        if (aaaCameraController != null)
        {
            aaaCameraController.enabled = false;
            Debug.Log("[PlayerHealth] Disabled AAACameraController for bleeding out camera");
        }
        
        // Start overhead camera for bleeding out (player can still move)
        if (deathCameraController != null)
        {
            deathCameraController.StartBleedOutCameraMode();
        }
        
        // Show cursor for potential UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (hasSelfRevive)
        {
            CognitiveFeedManager.Instance?.QueueMessage("*CRITICAL DAMAGE DETECTED... Self-revive available...*");
        }
        else
        {
            CognitiveFeedManager.Instance?.QueueMessage("*CRITICAL DAMAGE DETECTED... Bleeding out...*");
        }
    }
    
    private void ProceedWithNormalDeath()
    {
        // Mark as actually dead
        isDead = true;
        isBleedingOut = false;
        
        // Ensure blood overlay is shown for normal death (in case it wasn't shown yet)
        Debug.Log("[PlayerHealth] ProceedWithNormalDeath() - About to call ShowBloodOverlay()");
        ShowBloodOverlay();
        
        // Clear all armor plates on death
        if (armorPlateSystem != null)
        {
            armorPlateSystem.ClearAllPlates();
            Debug.Log("PlayerHealth: Cleared all armor plates on death");
        }
        
        // Clear ALL inventory on normal death - gems, items, self-revive, plates (except stash)
        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            // Clear gems
            inventoryManager.SetGemCount(0);
            
            // Clear all inventory slots (items) - use correct method name
            inventoryManager.ClearInventoryOnDeath();
            
            // Clear self-revive slot - use correct method name
            if (inventoryManager.reviveSlot != null)
            {
                inventoryManager.reviveSlot.SetReviveCount(0);
            }
            
            Debug.Log("PlayerHealth: Cleared all inventory on normal death - gems, items, plates, and self-revive slot");
            
            // CRITICAL: Save cleared inventory state to PersistentItemInventoryManager
            // This ensures respawn doesn't restore old items
            if (PersistentItemInventoryManager.Instance != null)
            {
                PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(inventoryManager);
                PersistentItemInventoryManager.Instance.SaveInventoryData();
                Debug.Log("PlayerHealth: Saved cleared inventory state to PersistentItemInventoryManager");
            }
        }
        
        // Note: Stash remains untouched - only inventory is cleared on normal death
        
        OnPlayerDied?.Invoke();
        
        // Reset hand levels to 1-1 in PlayerProgression when player dies
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.ResetHandLevelsOnDeath(true);
        }
        
        // Play the player death sound (if not already played)
        if (!isWaitingForRevive)
            GameSounds.PlayPlayerDeath(transform.position);

        // Disable all player systems
        if (_playerMovementManager != null) _playerMovementManager.enabled = false;
        if (_celestialDriftController != null) _celestialDriftController.enabled = false;
        if (_playerShooterOrchestrator != null) _playerShooterOrchestrator.enabled = false;
        if (_characterController != null) _characterController.enabled = false;
        
        // Stop physics
        var rb = GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.isKinematic = true;
        }

        // Show cursor for potential menu interactions
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_godModeCoroutine != null)
        {
            StopCoroutine(_godModeCoroutine);
            DeactivateGodModeInternal(false);
        }
        
        // Stop all powerup effect coroutines on death
        if (_doubleGemsEffectCoroutine != null)
        {
            StopCoroutine(_doubleGemsEffectCoroutine);
            _doubleGemsEffectCoroutine = null;
        }
        if (_slowTimeEffectCoroutine != null)
        {
            StopCoroutine(_slowTimeEffectCoroutine);
            _slowTimeEffectCoroutine = null;
        }
        
        // Stop all particle effects on death
        if (doubleGemsParticleEffect != null) doubleGemsParticleEffect.Stop();
        if (slowTimeParticleEffect != null) slowTimeParticleEffect.Stop();

        CognitiveFeedManager.Instance?.QueueMessage("*SYSTEM FAILURE... Restarting...*");
        
        // CRITICAL FIX: Reset the scene after a brief delay
        StartCoroutine(ResetSceneAfterDelay());
    }
    
    private IEnumerator ResetSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(sceneResetDelay);
        
        // CRITICAL: Stop ALL coroutines before scene reload to prevent orphaning
        StopAllCoroutines();
        
        // CRITICAL: Unsubscribe from ALL events before scene reload
        try
        {
            if (bleedOutUIManager != null)
            {
                bleedOutUIManager.OnBleedOutComplete -= OnBleedOutComplete;
                bleedOutUIManager.OnSelfReviveRequested -= OnSelfReviveRequested;
                bleedOutUIManager.OnBleedOutProgress -= OnBleedOutProgress;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerHealth] Exception during pre-reload cleanup: {e.Message}", this);
        }
        
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        try
        {
            SceneManager.LoadScene(currentSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerHealth] Scene reload failed: {e.Message}", this);
            // Fallback: try reloading by index
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public float GetCurrentSurvivalTime()
    {
        return isDead ? _timeOfDeath : Time.timeSinceLevelLoad;
    }

    public void ReviveAndReturnToGame(Transform spawnPoint)
    {
        if (!isDead) return;
        isDead = false;
        _currentHealth = maxHealth;
        OnHealthChangedForHUD?.Invoke(_currentHealth, maxHealth);
        
        // Respawn sound is handled by GameSoundEvent asset
        
        // Teleport player BEFORE re-enabling controllers to avoid physics glitches
        if (_characterController != null) _characterController.enabled = false;
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        // --- FIX: Re-enable the master controller and combat systems ---
        // PlayerMovementManager will handle enabling the correct sub-controller (flight by default).
        if (_playerMovementManager != null) _playerMovementManager.enabled = true;
        if (_playerShooterOrchestrator != null) _playerShooterOrchestrator.enabled = true;

        if (IsGodModeActive) DeactivateGodModeInternal(false);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // --- FIX: Use CognitiveFeedManager for the revival message ---
        CognitiveFeedManager.Instance?.QueueMessage("[System online. Core functions restored.]");
    }

    public void ActivateGodMode(float duration)
    {
        Debug.Log($"[PlayerHealth] ActivateGodMode called with duration: {duration}s. Current IsGodModeActive: {IsGodModeActive}", this);
        
        bool wasAlreadyActive = IsGodModeActive;
        if (IsGodModeActive && _godModeCoroutine != null)
        {
            _godModeEndTime += duration;
            StopCoroutine(_godModeCoroutine);
            Debug.Log($"[PlayerHealth] Extended existing godmode duration. New end time: {_godModeEndTime}", this);
            
            // CRITICAL FIX: Ensure particle effect is still playing when extending duration
            if (godModeParticleEffect != null && !godModeParticleEffect.isPlaying)
            {
                godModeParticleEffect.Play();
                Debug.Log("[PlayerHealth] Restarted GodMode particle effect during duration extension", this);
            }
        }
        else
        {
            IsGodModeActive = true;
            _godModeEndTime = Time.time + duration;
            Debug.Log($"[PlayerHealth] Activated godmode! IsGodModeActive set to: {IsGodModeActive}, End time: {_godModeEndTime}", this);
            
            // CRITICAL: Enable particle effect when GodMode starts
            if (godModeParticleEffect != null)
            {
                // Stop any existing playback first to ensure clean start
                if (godModeParticleEffect.isPlaying)
                {
                    godModeParticleEffect.Stop();
                    Debug.Log("[PlayerHealth] Stopped existing GodMode particle effect before restarting", this);
                }
                
                godModeParticleEffect.Play();
                Debug.Log($"[PlayerHealth] GodMode particle effect started! IsPlaying: {godModeParticleEffect.isPlaying}", this);
                
                // Additional debug info
                Debug.Log($"[PlayerHealth] Particle effect details - Name: {godModeParticleEffect.name}, GameObject: {godModeParticleEffect.gameObject.name}, Active: {godModeParticleEffect.gameObject.activeInHierarchy}", this);
            }
            else
            {
                Debug.LogError("[PlayerHealth] CRITICAL: GodMode particle effect is NOT assigned! Please assign a ParticleSystem to the 'God Mode Particle Effect' field in the PlayerHealth inspector.", this);
            }
        }

        _godModeCoroutine = StartCoroutine(GodModeDurationCoroutine(_godModeEndTime - Time.time));
    }

    /// <summary>
    /// Activate powerup particle effect by type
    /// </summary>
    public void ActivatePowerupEffect(PowerUpType powerupType, float duration)
    {
        switch (powerupType)
        {
            case PowerUpType.GodMode:
                ActivateGodMode(duration);
                break;
                
            case PowerUpType.DoubleGems:
                if (doubleGemsParticleEffect != null)
                {
                    // If effect is already running, just extend the duration
                    if (_doubleGemsEffectCoroutine != null)
                    {
                        StopCoroutine(_doubleGemsEffectCoroutine);
                        Debug.Log("[PlayerHealth] DoubleGems effect extended - restarting duration tracking", this);
                    }
                    else
                    {
                        // Start new effect
                        doubleGemsParticleEffect.Play();
                        Debug.Log("[PlayerHealth] DoubleGems particle effect started", this);
                    }
                    
                    // Always restart duration tracking with new duration
                    _doubleGemsEffectCoroutine = StartCoroutine(PowerupEffectDurationCoroutine(PowerUpType.DoubleGems, duration));
                }
                break;
                
            case PowerUpType.MaxHandUpgrade:
                // MaxHandUpgrade particle effects are handled by HandOverheatVisuals through PlayerOverheatManager
                Debug.Log("[PlayerHealth] MaxHandUpgrade particle effects handled by overheat system", this);
                break;
                
            case PowerUpType.SlowTime:
                if (slowTimeParticleEffect != null)
                {
                    // If effect is already running, just extend the duration
                    if (_slowTimeEffectCoroutine != null)
                    {
                        StopCoroutine(_slowTimeEffectCoroutine);
                        Debug.Log("[PlayerHealth] SlowTime effect extended - restarting duration tracking", this);
                    }
                    else
                    {
                        // Start new effect
                        slowTimeParticleEffect.Play();
                        Debug.Log("[PlayerHealth] SlowTime particle effect started", this);
                    }
                    
                    // Always restart duration tracking with new duration
                    _slowTimeEffectCoroutine = StartCoroutine(PowerupEffectDurationCoroutine(PowerUpType.SlowTime, duration));
                }
                break;
                
            case PowerUpType.InstantCooldown:
                // InstantCooldown particle effects are handled by HandOverheatVisuals through PlayerOverheatManager
                Debug.Log("[PlayerHealth] InstantCooldown particle effects handled by overheat system", this);
                break;
                
            case PowerUpType.DoubleDamage:
                // DoubleDamage particle effects are handled by HandOverheatVisuals through PlayerOverheatManager
                Debug.Log("[PlayerHealth] DoubleDamage particle effects handled by overheat system", this);
                break;
        }
    }

    /// <summary>
    /// Deactivate powerup particle effect by type
    /// </summary>
    public void DeactivatePowerupEffect(PowerUpType powerupType)
    {
        switch (powerupType)
        {
            case PowerUpType.GodMode:
                if (godModeParticleEffect != null)
                {
                    godModeParticleEffect.Stop();
                    Debug.Log("[PlayerHealth] GodMode particle effect stopped", this);
                }
                break;
                
            case PowerUpType.DoubleGems:
                if (doubleGemsParticleEffect != null)
                {
                    doubleGemsParticleEffect.Stop();
                    Debug.Log("[PlayerHealth] DoubleGems particle effect stopped", this);
                }
                // Stop duration tracking coroutine if running
                if (_doubleGemsEffectCoroutine != null)
                {
                    StopCoroutine(_doubleGemsEffectCoroutine);
                    _doubleGemsEffectCoroutine = null;
                }
                break;
                
            case PowerUpType.MaxHandUpgrade:
                // MaxHandUpgrade particle effects are handled by HandOverheatVisuals through PlayerOverheatManager
                Debug.Log("[PlayerHealth] MaxHandUpgrade particle effects handled by overheat system", this);
                break;
                
            case PowerUpType.SlowTime:
                if (slowTimeParticleEffect != null)
                {
                    slowTimeParticleEffect.Stop();
                    Debug.Log("[PlayerHealth] SlowTime particle effect stopped", this);
                }
                // Stop duration tracking coroutine if running
                if (_slowTimeEffectCoroutine != null)
                {
                    StopCoroutine(_slowTimeEffectCoroutine);
                    _slowTimeEffectCoroutine = null;
                }
                break;
                
            case PowerUpType.InstantCooldown:
                // InstantCooldown particle effects are handled by HandOverheatVisuals through PlayerOverheatManager
                Debug.Log("[PlayerHealth] InstantCooldown particle effects handled by overheat system", this);
                break;
                
            case PowerUpType.DoubleDamage:
                // DoubleDamage particle effects are handled by HandOverheatVisuals through PlayerOverheatManager
                Debug.Log("[PlayerHealth] DoubleDamage particle effects handled by overheat system", this);
                break;
        }
    }

    private IEnumerator GodModeDurationCoroutine(float activeDuration)
    {
        Debug.Log($"[PlayerHealth] GodMode duration coroutine started - Duration: {activeDuration}s", this);
        
        float timer = 0f;
        float lastUpdateTime = 0f;
        const float UPDATE_INTERVAL = 0.1f; // Update HUD every 0.1 seconds instead of every frame
        
        while (timer < activeDuration)
        {
            float timeLeft = activeDuration - timer;
            
            // PERFORMANCE FIX: Only update HUD every 0.1 seconds, not every frame
            if (timer - lastUpdateTime >= UPDATE_INTERVAL || timer == 0f)
            {
                OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.GodMode, true, timeLeft);
                lastUpdateTime = timer;
            }

            // You'll need a way to show these messages, for now, we'll use the cognitive feed
            // if (timeLeft <= 5.0f && Mathf.Abs(timeLeft - Mathf.Round(timeLeft)) < 0.05f) 
            // {
            //     CognitiveFeedManager.Instance?.QueueMessage($"[God Mode ending in {Mathf.RoundToInt(timeLeft)}...]");
            // }

            // CRITICAL FIX: Use unscaled time to prevent interference with SlowTime powerup
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        
        Debug.Log($"[PlayerHealth] GodMode duration expired - Deactivating", this);
        DeactivateGodModeInternal(true);
    }

    /// <summary>
    /// Generic coroutine to handle powerup effect duration for non-GodMode powerups
    /// </summary>
    private IEnumerator PowerupEffectDurationCoroutine(PowerUpType powerupType, float duration)
    {
        Debug.Log($"[PlayerHealth] Starting {powerupType} particle effect duration tracking for {duration} seconds", this);
        
        // CRITICAL FIX: Use WaitForSecondsRealtime to prevent interference with SlowTime powerup
        yield return new WaitForSecondsRealtime(duration);
        
        // Automatically stop the particle effect when duration expires
        DeactivatePowerupEffect(powerupType);
        
        Debug.Log($"[PlayerHealth] {powerupType} particle effect duration expired - effect stopped", this);
        
        // Clear the coroutine reference
        switch (powerupType)
        {
            case PowerUpType.DoubleGems:
                _doubleGemsEffectCoroutine = null;
                break;
            case PowerUpType.SlowTime:
                _slowTimeEffectCoroutine = null;
                break;
        }
    }

    private void DeactivateGodModeInternal(bool fireEventAndFeed)
    {
        if (!IsGodModeActive) return;
        IsGodModeActive = false;

        // Disable particle effect when GodMode ends
        if (godModeParticleEffect != null)
        {
            godModeParticleEffect.Stop();
            Debug.Log("[PlayerHealth] GodMode particle effect stopped", this);
        }

        if (fireEventAndFeed)
        {
            OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.GodMode, false, 0);
            CognitiveFeedManager.Instance?.QueueMessage("[Invulnerability has faded.]");
        }
        if (_godModeCoroutine != null) { StopCoroutine(_godModeCoroutine); _godModeCoroutine = null; }
    }
    
    // === INSTANT COOLDOWN POWERUP SYSTEM ===
    
    /// <summary>
    /// Activate instant cooldown powerup - drains heat and prevents heat gain
    /// </summary>
    public void ActivateInstantCooldown(float duration)
    {
        if (IsInstantCooldownActive)
        {
            Debug.Log("[PlayerHealth] InstantCooldown already active - extending duration", this);
            // Could extend duration here if desired
            return;
        }
        
        IsInstantCooldownActive = true;
        Debug.Log($"[PlayerHealth] InstantCooldown activated for {duration} seconds", this);
        
        // Particle effects are activated by PowerupInventoryManager through overheat system
        
        // Start the cooldown coroutine
        StartCoroutine(InstantCooldownCoroutine(duration));
        
        // Notify HUD system
        OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.InstantCooldown, true, duration);
        
        // Provide player feedback
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.QueueMessage("[Instant Cooldown Activated - Heat Immunity!]");
        }
    }
    
    /// <summary>
    /// Coroutine that handles the instant cooldown effect
    /// </summary>
    private IEnumerator InstantCooldownCoroutine(float duration)
    {
        Debug.Log($"[PlayerHealth] Starting InstantCooldown coroutine for {duration} seconds", this);
        
        // Get reference to overheat manager
        PlayerOverheatManager overheatManager = PlayerOverheatManager.Instance;
        if (overheatManager == null)
        {
            Debug.LogError("[PlayerHealth] PlayerOverheatManager not found! InstantCooldown will not work properly.", this);
            IsInstantCooldownActive = false;
            yield break;
        }
        
        // Phase 1: Rapidly drain heat to 0 (smooth draining effect)
        float drainDuration = 1.5f; // Time to drain heat to 0
        float drainTimer = 0f;
        
        float initialPrimaryHeat = overheatManager.CurrentHeatPrimary;
        float initialSecondaryHeat = overheatManager.CurrentHeatSecondary;
        
        Debug.Log($"[PlayerHealth] Starting heat drain - Primary: {initialPrimaryHeat}, Secondary: {initialSecondaryHeat}", this);
        
        while (drainTimer < drainDuration)
        {
            float progress = drainTimer / drainDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            // Calculate current heat levels (smoothly decreasing to 0)
            float currentPrimaryHeat = Mathf.Lerp(initialPrimaryHeat, 0f, smoothProgress);
            float currentSecondaryHeat = Mathf.Lerp(initialSecondaryHeat, 0f, smoothProgress);
            
            // Set heat levels directly (bypassing normal heat system)
            overheatManager.SetHeatDirectly(true, currentPrimaryHeat);
            overheatManager.SetHeatDirectly(false, currentSecondaryHeat);
            
            // CRITICAL FIX: Use unscaled time to prevent interference with SlowTime powerup
            drainTimer += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Ensure heat is exactly 0
        overheatManager.ResetHandHeat(true, true);
        overheatManager.ResetHandHeat(false, true);
        
        Debug.Log("[PlayerHealth] Heat drain complete - both hands at 0 heat", this);
        
        // Phase 2: Maintain heat immunity for remaining duration
        float immunityDuration = duration - drainDuration;
        float immunityTimer = 0f;
        float lastUpdateTime = 0f;
        const float UPDATE_INTERVAL = 1.0f; // Update HUD every 1 second instead of every frame
        
        Debug.Log($"[PlayerHealth] Starting heat immunity phase for {immunityDuration} seconds", this);
        
        while (immunityTimer < immunityDuration && IsInstantCooldownActive)
        {
            // Continuously reset heat to 0 to prevent any heat gain
            if (overheatManager.CurrentHeatPrimary > 0)
            {
                overheatManager.ResetHandHeat(true, true);
            }
            if (overheatManager.CurrentHeatSecondary > 0)
            {
                overheatManager.ResetHandHeat(false, true);
            }
            
            // Update HUD with remaining time
            float timeLeft = immunityDuration - immunityTimer;
            
            // PERFORMANCE FIX: Only update HUD every 1 second, not every frame
            if (immunityTimer - lastUpdateTime >= UPDATE_INTERVAL || immunityTimer == 0f)
            {
                OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.InstantCooldown, true, timeLeft);
                lastUpdateTime = immunityTimer;
            }
            
            // CRITICAL FIX: Use unscaled time to prevent interference with SlowTime powerup
            immunityTimer += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Deactivate instant cooldown
        DeactivateInstantCooldown();
    }
    
    /// <summary>
    /// Deactivate instant cooldown powerup
    /// </summary>
    private void DeactivateInstantCooldown()
    {
        if (!IsInstantCooldownActive) return;
        
        IsInstantCooldownActive = false;
        Debug.Log("[PlayerHealth] InstantCooldown deactivated - heat system restored", this);
        
        // Particle effects are deactivated automatically by duration in overheat system
        
        // Notify HUD system
        OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.InstantCooldown, false, 0);
        
        // Provide player feedback
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.QueueMessage("[Instant Cooldown ended - Heat system restored]");
        }
    }
    
    // === DOUBLE DAMAGE POWERUP SYSTEM ===
    
    /// <summary>
    /// Activate double damage powerup - doubles damage to enemies and gems
    /// </summary>
    public void ActivateDoubleDamage(float duration)
    {
        if (IsDoubleDamageActive)
        {
            Debug.Log("[PlayerHealth] DoubleDamage already active - extending duration", this);
            // Could extend duration here if desired
            return;
        }
        
        IsDoubleDamageActive = true;
        Debug.Log($"[PlayerHealth] DoubleDamage activated for {duration} seconds", this);
        
        // Particle effects are activated by PowerupInventoryManager through overheat system
        
        // Start the double damage coroutine
        StartCoroutine(DoubleDamageCoroutine(duration));
        
        // Notify HUD system
        OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.DoubleDamage, true, duration);
        
        // Provide player feedback
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.QueueMessage("[Double Damage Activated - 2x Damage!]");
        }
    }
    
    /// <summary>
    /// Coroutine that handles the double damage effect
    /// </summary>
    private IEnumerator DoubleDamageCoroutine(float duration)
    {
        Debug.Log($"[PlayerHealth] Starting DoubleDamage coroutine for {duration} seconds", this);
        
        float timer = 0f;
        float lastUpdateTime = 0f;
        const float UPDATE_INTERVAL = 1.0f; // Update HUD every 1 second instead of every frame
        
        while (timer < duration)
        {
            float timeLeft = duration - timer;
            
            // PERFORMANCE FIX: Only update HUD every 1 second, not every frame
            if (timer - lastUpdateTime >= UPDATE_INTERVAL || timer == 0f)
            {
                OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.DoubleDamage, true, timeLeft);
                lastUpdateTime = timer;
            }
            
            yield return null;
            timer += Time.unscaledDeltaTime;
        }
        
        // Deactivate when duration expires
        DeactivateDoubleDamage();
    }
    
    /// <summary>
    /// Deactivate double damage powerup
    /// </summary>
    private void DeactivateDoubleDamage()
    {
        if (!IsDoubleDamageActive) return;
        
        IsDoubleDamageActive = false;
        Debug.Log("[PlayerHealth] DoubleDamage deactivated - damage system restored", this);
        
        // Particle effects are deactivated automatically by duration in overheat system
        
        // Notify HUD system
        OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.DoubleDamage, false, 0);
        
        // Provide player feedback
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.QueueMessage("[Double Damage ended - Normal damage restored]");
        }
    }
    
    // === BLEEDING OUT EVENT HANDLERS ===
    
    /// <summary>
    /// Called when bleed out timer expires (player dies)
    /// </summary>
    private void OnBleedOutComplete()
    {
        Debug.Log("[PlayerHealth] OnBleedOutComplete - Player died from bleeding out");
        
        // Stop pulsating blood overlay
        if (bloodPulsateCoroutine != null)
        {
            StopCoroutine(bloodPulsateCoroutine);
            bloodPulsateCoroutine = null;
        }
        
        isBleedingOut = false;
        
        // Deactivate bleed out movement controller
        if (bleedOutMovementController != null && bleedOutMovementController.IsActive())
        {
            bleedOutMovementController.DeactivateBleedOutMovement();
            Debug.Log("[PlayerHealth] BleedOutMovementController deactivated after bleed out death");
        }
        
        // Stop bleeding out sounds
        GameSounds.StopBleedingOutSounds();
        
        // NOW play the death sound (only when ACTUALLY dying, not when entering bleed out)
        GameSounds.PlayPlayerDeath(transform.position);
        Debug.Log("<color=red>💀 DEATH SOUND PLAYED - Timer expired</color>");
        
        // NOW set isDead flag (only when actually dead)
        isDead = true;
        Debug.Log("=== PLAYER ACTUALLY DEAD ===");
        
        // NOW disable movement (only when actually dead)
        DisableAllMovementForDeath();
        
        // Camera is already in overhead position from bleeding out
        // No need to move it again
        
        // CRITICAL: Mark as waiting for revive to prevent duplicate death sound in ProceedWithNormalDeath
        isWaitingForRevive = true;
        
        // Proceed with normal death after a delay
        StartCoroutine(DelayedNormalDeath(2f));
    }
    
    /// <summary>
    /// Called when player presses E to use self-revive during bleed out
    /// </summary>
    private void OnSelfReviveRequested()
    {
        Debug.Log("[PlayerHealth] OnSelfReviveRequested - Player wants to use self-revive");
        
        // Consume the self-revive item
        if (reviveSlotController != null && reviveSlotController.HasRevives())
        {
            reviveSlotController.RemoveRevives(1);
            Debug.Log("[PlayerHealth] Self-revive consumed from inventory");
        }
        
        // Mark that we've used self-revive this life
        hasUsedSelfRevive = true;
        
        // Stop bleeding out (we're being revived, not dying!)
        isBleedingOut = false;
        
        // Stop bleeding out sounds
        GameSounds.StopBleedingOutSounds();
        
        // Make sure isDead is false (in case it was set)
        isDead = false;
        
        // DON'T RE-ENABLE CAMERA YET - wait for PerformSelfRevive() to avoid state corruption!
        // The camera will be properly re-enabled after physics and movement are restored.
        
        // Stop death camera
        if (deathCameraController != null)
        {
            deathCameraController.StopDeathSequence();
        }
        
        if (bleedOutUIManager != null)
        {
            bleedOutUIManager.StopBleedOut();
        }
        
        // Stop pulsating blood overlay
        if (bloodPulsateCoroutine != null)
        {
            StopCoroutine(bloodPulsateCoroutine);
            bloodPulsateCoroutine = null;
        }
        
        // Start blinking effect during revive process
        isSelfReviving = true;
        if (bloodBlinkCoroutine != null) StopCoroutine(bloodBlinkCoroutine);
        bloodBlinkCoroutine = StartCoroutine(BlinkBloodOverlay());
        
        // Start revive process with delay (2 seconds for smooth animation)
        StartCoroutine(DelayedSelfRevive(2f)); // 2 second revive animation time
        
        // Provide feedback
        CognitiveFeedManager.Instance?.QueueMessage("[Self-Revive activated... Restoring systems...]");
    }
    
    /// <summary>
    /// Called each frame with bleed out progress (0-1, where 1 = full health, 0 = dead)
    /// Used to control blood overlay pulsation speed
    /// </summary>
    private void OnBleedOutProgress(float progress)
    {
        // This is handled by the PulsateBloodOverlay coroutine
        // No need to do anything here, but we keep the event for future use
    }
    
    /// <summary>
    /// Delayed normal death to allow camera sequence to play
    /// </summary>
    private IEnumerator DelayedNormalDeath(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ProceedWithNormalDeath();
    }
    
    /// <summary>
    /// Delayed self-revive to allow blinking animation
    /// </summary>
    private IEnumerator DelayedSelfRevive(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        PerformSelfRevive();
    }
    
    /// <summary>
    /// Perform the actual self-revive process
    /// </summary>
    private void PerformSelfRevive()
    {
        // Ensure player is not marked as dead
        isDead = false;
        isBleedingOut = false;
        
        // Stop blinking and hide blood overlay
        isSelfReviving = false;
        HideBloodOverlay();
        
        // Restore health to full
        _currentHealth = maxHealth;
        isDead = false;
        
        // Update health UI
        OnHealthChangedForHUD?.Invoke(_currentHealth, maxHealth);
        
        // CRITICAL: Deactivate BleedOutMovementController FIRST to release CharacterController ownership
        if (bleedOutMovementController != null && bleedOutMovementController.IsActive())
        {
            bleedOutMovementController.DeactivateBleedOutMovement();
            Debug.Log("[PlayerHealth] ✅ BleedOutMovementController deactivated - CharacterController ownership released");
        }
        
        // CRITICAL: Clear ALL velocity states BEFORE re-enabling components
        ClearAllPhysicsStates();
        
        // COMPREHENSIVE: Restore movement to the state before death
        RestoreMovementAfterRevive();
        
        // Restore cursor lock
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // CRITICAL: Re-enable AAA camera controller AFTER movement is restored
        // This prevents camera state corruption and trick mode activation
        if (aaaCameraController != null)
        {
            // CRITICAL FIX: Force reset trick system state before re-enabling
            aaaCameraController.ForceResetTrickSystemForRevive();
            
            aaaCameraController.enabled = true;
            Debug.Log("[PlayerHealth] ✅ Re-enabled AAACameraController after self-revive (AFTER movement restore)");
        }
        
        // CRITICAL: Start 3-second invulnerability grace period after self-revive
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
        }
        invulnerabilityCoroutine = StartCoroutine(InvulnerabilityGracePeriod(3f));
        Debug.Log("[PlayerHealth] ✅ Self-revive completed - 3-second invulnerability grace period started", this);
        
        // Play revive sound (after sound system reset)
        GameSounds.PlayPowerUpEnd(transform.position);
        
        // Show revival message with invulnerability notification
        CognitiveFeedManager.Instance?.QueueMessage("[SYSTEM RESTORED... Core functions online. Protection active for 3 seconds.]");
    }
    
    /// <summary>
    /// Invulnerability grace period coroutine - prevents damage for specified duration after self-revive
    /// </summary>
    private IEnumerator InvulnerabilityGracePeriod(float duration)
    {
        IsInvulnerable = true;
        Debug.Log($"[PlayerHealth] ✨ Invulnerability grace period started for {duration} seconds", this);
        
        // Optional: Flash godmode particle effect briefly to show invulnerability
        if (godModeParticleEffect != null)
        {
            godModeParticleEffect.Play();
        }
        
        // Wait for the grace period duration (use unscaled time so SlowTime doesn't affect it)
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // End invulnerability
        IsInvulnerable = false;
        
        // Stop particle effect
        if (godModeParticleEffect != null)
        {
            godModeParticleEffect.Stop();
        }
        
        Debug.Log("[PlayerHealth] ✨ Invulnerability grace period ended - player can take damage again", this);
        CognitiveFeedManager.Instance?.QueueMessage("[Protection period ended]");
        
        invulnerabilityCoroutine = null;
    }
    
    /// <summary>
    /// Reset self-revive state when starting a new life/level
    /// </summary>
    public void ResetSelfReviveState()
    {
        hasUsedSelfRevive = false;
        isWaitingForRevive = false;
        isBleedingOut = false;
        isSelfReviving = false;
        
        Debug.Log("[PlayerHealth] ResetSelfReviveState() - clearing bleed out UI and blood overlay");
        
        // Deactivate bleed out movement controller if active
        if (bleedOutMovementController != null && bleedOutMovementController.IsActive())
        {
            bleedOutMovementController.DeactivateBleedOutMovement();
            Debug.Log("[PlayerHealth] BleedOutMovementController deactivated in ResetSelfReviveState");
        }
        
        // Stop bleeding out
        if (bleedOutUIManager != null)
        {
            bleedOutUIManager.StopBleedOut();
        }
        
        // Stop all blood overlay coroutines
        if (bloodPulsateCoroutine != null)
        {
            StopCoroutine(bloodPulsateCoroutine);
            bloodPulsateCoroutine = null;
        }
        if (bloodBlinkCoroutine != null)
        {
            StopCoroutine(bloodBlinkCoroutine);
            bloodBlinkCoroutine = null;
        }
            
        // Hide blood overlay when resetting
        HideBloodOverlay();
    }
    
    // === AAA BLOOD SPLAT FEEDBACK SYSTEM ===
    
    /// <summary>
    /// Trigger blood splat visual and audio feedback when player takes damage
    /// Uses smooth fading and health-based intensity for AAA feel
    /// </summary>
    private void TriggerBloodSplatFeedback()
    {
        // CRITICAL FIX: For fall damage, bypass cooldown to ensure it always shows
        bool isFallDamage = (Time.time - lastDamageTime) < 0.1f; // Fall damage happens immediately after taking damage
        
        // Check cooldown to prevent rapid flickering from stream damage (but not for fall damage)
        if (!isFallDamage && Time.time - lastBloodSplatHitTime < bloodSplatHitCooldown)
        {
            return; // Skip this hit to prevent visual spam
        }
        
        lastBloodSplatHitTime = Time.time;
        
        // Play hit sound with cooldown built into SoundEvent
        GameSounds.PlayPlayerHit(transform.position, 1f);
        
        // Calculate target alpha based on health percentage
        float healthPercent = GetHealthPercentage();
        
        if (healthPercent <= bloodSplatLowHealthThreshold)
        {
            // Low health - show more intense blood splat
            // Lerp from max alpha at 0% health to half alpha at threshold
            float lowHealthProgress = healthPercent / bloodSplatLowHealthThreshold;
            bloodSplatTargetAlpha = Mathf.Lerp(bloodSplatMaxAlpha, bloodSplatMaxAlpha * 0.5f, lowHealthProgress);
        }
        else
        {
            // Normal health - brief flash only
            bloodSplatTargetAlpha = bloodSplatMaxAlpha * 0.3f;
        }
        
        // ENHANCED: Ensure blood overlay is properly initialized before starting fade
        if (bloodOverlayImage != null && bloodOverlayCanvasGroup == null)
        {
            // Try to get CanvasGroup if it wasn't found in Start()
            bloodOverlayCanvasGroup = bloodOverlayImage.GetComponent<CanvasGroup>();
            if (bloodOverlayCanvasGroup != null)
            {
                Debug.Log("[PlayerHealth] ✅ Found CanvasGroup for blood overlay during damage");
            }
        }
        
        // Start or restart the fade coroutine
        if (bloodSplatFadeCoroutine != null)
        {
            StopCoroutine(bloodSplatFadeCoroutine);
        }
        
        if (bloodOverlayCanvasGroup != null || bloodOverlayImage != null)
        {
            bloodSplatFadeCoroutine = StartCoroutine(BloodSplatFadeCoroutine());
            Debug.Log($"[PlayerHealth] 🩸 Blood splat triggered - target alpha: {bloodSplatTargetAlpha:F2}");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] ❌ Cannot show blood splat - bloodOverlayImage or CanvasGroup not assigned!");
        }
    }
    
    /// <summary>
    /// PUBLIC API: Trigger dramatic blood splat with custom intensity (0-1)
    /// Used by FallingDamageSystem for severe fall/collision damage
    /// </summary>
    public void TriggerDramaticBloodSplat(float intensity)
    {
        // Force bypass cooldown for dramatic impacts
        lastBloodSplatHitTime = Time.time;
        
        // Set blood splat intensity directly (0-1 scale)
        bloodSplatTargetAlpha = Mathf.Lerp(bloodSplatMaxAlpha * 0.4f, bloodSplatMaxAlpha, intensity);
        
        // Play hit sound with intensity
        GameSounds.PlayPlayerHit(transform.position, Mathf.Lerp(0.7f, 1.0f, intensity));
        
        // Ensure blood overlay is initialized
        if (bloodOverlayImage != null && bloodOverlayCanvasGroup == null)
        {
            bloodOverlayCanvasGroup = bloodOverlayImage.GetComponent<CanvasGroup>();
        }
        
        // Start or restart the fade coroutine
        if (bloodSplatFadeCoroutine != null)
        {
            StopCoroutine(bloodSplatFadeCoroutine);
        }
        
        if (bloodOverlayCanvasGroup != null || bloodOverlayImage != null)
        {
            bloodSplatFadeCoroutine = StartCoroutine(BloodSplatFadeCoroutine());
            Debug.Log($"<color=red>💉 [DRAMATIC BLOOD SPLAT] Intensity: {intensity:F2}, Target Alpha: {bloodSplatTargetAlpha:F2}</color>");
        }
    }
    
    /// <summary>
    /// Smooth fade coroutine for blood splat overlay - AAA quality
    /// CRITICAL FIX: Uses unscaled time to work during pause (Time.timeScale = 0)
    /// </summary>
    private IEnumerator BloodSplatFadeCoroutine()
    {
        // ENHANCED: Work with both CanvasGroup and direct GameObject activation
        if (bloodOverlayCanvasGroup == null && bloodOverlayImage == null) 
        {
            Debug.LogWarning("[PlayerHealth] No blood overlay components available for fade!");
            yield break;
        }
        
        // CRITICAL FIX: Check if pause menu is active and adjust sorting order
        UpdateBloodOverlaySortingOrder();
        
        // CRITICAL: Ensure blood overlay GameObject is active and independent
        if (bloodOverlayImage != null && !bloodOverlayImage.activeInHierarchy)
        {
            // Try to activate it
            bloodOverlayImage.SetActive(true);
            Debug.Log("[PlayerHealth] ✅ Activated blood overlay GameObject for fade");
        }
        
        // If no CanvasGroup, use simple GameObject activation as fallback
        if (bloodOverlayCanvasGroup == null && bloodOverlayImage != null)
        {
            Debug.Log("[PlayerHealth] 🩸 No CanvasGroup - using simple activation for blood overlay");
            bloodOverlayImage.SetActive(true);
            
            // Hold for a brief moment then deactivate
            yield return new WaitForSecondsRealtime(1.0f);
            bloodOverlayImage.SetActive(false);
            yield break;
        }
        
        // Fade in to target alpha
        while (bloodSplatCurrentAlpha < bloodSplatTargetAlpha)
        {
            // CRITICAL FIX: Use unscaled deltaTime so it works even when paused (timeScale = 0)
            bloodSplatCurrentAlpha += bloodSplatFadeInSpeed * Time.unscaledDeltaTime;
            bloodSplatCurrentAlpha = Mathf.Min(bloodSplatCurrentAlpha, bloodSplatTargetAlpha);
            
            // Safety check - ensure CanvasGroup still exists
            if (bloodOverlayCanvasGroup != null)
            {
                bloodOverlayCanvasGroup.alpha = bloodSplatCurrentAlpha;
            }
            else
            {
                Debug.LogWarning("[PlayerHealth] CanvasGroup became null during fade!");
                yield break;
            }
            
            yield return null;
        }
        
        // Hold at target alpha briefly (use unscaled time)
        yield return new WaitForSecondsRealtime(0.1f);
        
        // Fade out smoothly
        while (bloodSplatCurrentAlpha > bloodSplatMinAlpha)
        {
            // CRITICAL FIX: Use unscaled deltaTime so it works even when paused (timeScale = 0)
            bloodSplatCurrentAlpha -= bloodSplatFadeOutSpeed * Time.unscaledDeltaTime;
            bloodSplatCurrentAlpha = Mathf.Max(bloodSplatCurrentAlpha, bloodSplatMinAlpha);
            
            // Safety check - ensure CanvasGroup still exists
            if (bloodOverlayCanvasGroup != null)
            {
                bloodOverlayCanvasGroup.alpha = bloodSplatCurrentAlpha;
            }
            else
            {
                Debug.LogWarning("[PlayerHealth] CanvasGroup became null during fade!");
                yield break;
            }
            
            yield return null;
        }
        
        // Ensure fully faded out
        if (bloodOverlayCanvasGroup != null)
        {
            bloodOverlayCanvasGroup.alpha = bloodSplatMinAlpha;
        }
        bloodSplatCurrentAlpha = bloodSplatMinAlpha;
        bloodSplatFadeCoroutine = null;
    }
    
    // === BLOOD OVERLAY METHODS (Death System) ===
    
    /// <summary>
    /// Show the blood splat overlay when player dies (instant, full opacity)
    /// CRITICAL FIX: Ensures blood overlay works independently of pause menu
    /// </summary>
    private void ShowBloodOverlay()
    {
        Debug.Log($"[PlayerHealth] ShowBloodOverlay() called");
        Debug.Log($"[PlayerHealth] bloodOverlayImage = {bloodOverlayImage}");
        Debug.Log($"[PlayerHealth] bloodOverlayCanvasGroup = {bloodOverlayCanvasGroup}");
        
        if (bloodOverlayImage != null)
        {
            Debug.Log($"[PlayerHealth] bloodOverlayImage.name = {bloodOverlayImage.name}");
            Debug.Log($"[PlayerHealth] bloodOverlayImage.activeSelf = {bloodOverlayImage.activeSelf}");
            Debug.Log($"[PlayerHealth] bloodOverlayImage.activeInHierarchy = {bloodOverlayImage.activeInHierarchy}");
            
            // CRITICAL FIX: Ensure the GameObject is active FIRST
            if (!bloodOverlayImage.activeSelf)
            {
                bloodOverlayImage.SetActive(true);
                Debug.Log("[PlayerHealth] ✅ Activated blood overlay GameObject");
            }
            
            // Use CanvasGroup alpha if available, otherwise rely on SetActive above
            if (bloodOverlayCanvasGroup != null)
            {
                Debug.Log($"[PlayerHealth] Using CanvasGroup - before: alpha = {bloodOverlayCanvasGroup.alpha}");
                
                // CRITICAL FIX: Ensure CanvasGroup is configured to allow rendering
                bloodOverlayCanvasGroup.alpha = 1f;
                bloodOverlayCanvasGroup.interactable = false; // Don't block UI interactions
                bloodOverlayCanvasGroup.blocksRaycasts = false; // Don't block raycasts
                
                Debug.Log($"[PlayerHealth] Using CanvasGroup - after: alpha = {bloodOverlayCanvasGroup.alpha}");
                Debug.Log($"[PlayerHealth] CanvasGroup interactable = {bloodOverlayCanvasGroup.interactable}");
                Debug.Log($"[PlayerHealth] CanvasGroup blocksRaycasts = {bloodOverlayCanvasGroup.blocksRaycasts}");
            }
            else
            {
                Debug.Log("[PlayerHealth] No CanvasGroup - blood overlay controlled via SetActive only");
            }
            
            // Start blinking if we're self-reviving
            if (isSelfReviving)
            {
                if (bloodBlinkCoroutine != null) StopCoroutine(bloodBlinkCoroutine);
                bloodBlinkCoroutine = StartCoroutine(BlinkBloodOverlay());
            }
        }
        else
        {
            Debug.LogError("[PlayerHealth] bloodOverlayImage is NULL - cannot show blood overlay! Did you assign it in the inspector?");
        }
    }
    
    /// <summary>
    /// Updates blood overlay sorting order based on pause menu state
    /// CRITICAL FIX: Ensures blood overlay doesn't block pause menu
    /// </summary>
    private void UpdateBloodOverlaySortingOrder()
    {
        // CRITICAL SAFETY CHECK: Only update if we have a valid canvas with override sorting
        if (bloodOverlayCanvas == null) return;
        if (!bloodOverlayCanvas.overrideSorting)
        {
            // Canvas doesn't have override sorting - skip to prevent breaking shared canvas
            return;
        }
        
        // Check if pause menu is active
        bool isPauseMenuActive = false;
        if (UIManager.Instance != null)
        {
            // Access pause menu panel through UIManager
            var pauseMenuPanel = UIManager.Instance.pauseMenuPanel;
            if (pauseMenuPanel != null)
            {
                isPauseMenuActive = pauseMenuPanel.activeSelf;
            }
        }
        
        // Adjust sorting order based on pause state
        int targetSortOrder = isPauseMenuActive ? bloodOverlayPausedSortOrder : bloodOverlayNormalSortOrder;
        
        if (bloodOverlayCanvas.sortingOrder != targetSortOrder)
        {
            bloodOverlayCanvas.sortingOrder = targetSortOrder;
            Debug.Log($"[PlayerHealth] 🎨 Blood overlay sort order updated: {targetSortOrder} (Pause menu: {isPauseMenuActive})");
        }
    }
    
    /// <summary>
    /// PUBLIC METHOD - Called by UIManager when pause menu state changes
    /// </summary>
    public void OnPauseMenuStateChanged(bool isPaused)
    {
        Debug.Log($"[PlayerHealth] Pause menu state changed: {isPaused}");
        UpdateBloodOverlaySortingOrder();
    }
    
    /// <summary>
    /// PUBLIC TEST METHOD - Force show blood overlay for testing
    /// </summary>
    public void TEST_ShowBloodOverlay()
    {
        Debug.Log("=== TEST_ShowBloodOverlay() called ===");
        ShowBloodOverlay();
    }
    
    /// <summary>
    /// PUBLIC TEST METHOD - Test godmode particle effect
    /// </summary>
    [ContextMenu("Test GodMode Particle Effect")]
    public void TEST_GodModeParticleEffect()
    {
        Debug.Log("=== TEST_GodModeParticleEffect() called ===");
        
        if (godModeParticleEffect != null)
        {
            Debug.Log($"[TEST] GodMode particle effect found: {godModeParticleEffect.name}");
            Debug.Log($"[TEST] GameObject: {godModeParticleEffect.gameObject.name}");
            Debug.Log($"[TEST] Active in hierarchy: {godModeParticleEffect.gameObject.activeInHierarchy}");
            
            ActivatePowerupEffect(PowerUpType.GodMode, 10f);
        }
        else
        {
            Debug.LogError("[TEST] GodMode particle effect is NULL!");
        }
    }
    
    /// <summary>
    /// PUBLIC TEST METHOD - Test all powerup particle effects
    /// </summary>
    [ContextMenu("Test All Powerup Particle Effects")]
    public void TEST_AllPowerupParticleEffects()
    {
        Debug.Log("=== TEST_AllPowerupParticleEffects() called ===");
        
        // Test each powerup effect for 5 seconds
        if (godModeParticleEffect != null)
        {
            Debug.Log("[TEST] Testing GodMode particle effect");
            ActivatePowerupEffect(PowerUpType.GodMode, 5f);
        }
        else
        {
            Debug.LogError("[TEST] GodMode particle effect is NULL!");
        }
        
        if (doubleGemsParticleEffect != null)
        {
            Debug.Log("[TEST] Testing DoubleGems particle effect");
            ActivatePowerupEffect(PowerUpType.DoubleGems, 5f);
        }
        else
        {
            Debug.LogError("[TEST] DoubleGems particle effect is NULL!");
        }
        
        // MaxHandUpgrade particle effects are now handled by HandOverheatVisuals system
        Debug.Log("[TEST] MaxHandUpgrade particle effects handled by overheat system - testing via ActivatePowerupEffect");
        ActivatePowerupEffect(PowerUpType.MaxHandUpgrade, 5f);
        
        if (slowTimeParticleEffect != null)
        {
            Debug.Log("[TEST] Testing SlowTime particle effect");
            ActivatePowerupEffect(PowerUpType.SlowTime, 5f);
        }
        else
        {
            Debug.LogError("[TEST] SlowTime particle effect is NULL!");
        }
    }
    
    /// <summary>
    /// Hide the blood splat overlay when player revives
    /// </summary>
    private void HideBloodOverlay()
    {
        if (bloodBlinkCoroutine != null)
        {
            StopCoroutine(bloodBlinkCoroutine);
            bloodBlinkCoroutine = null;
        }
        
        if (bloodOverlayImage != null)
        {
            // Use CanvasGroup alpha if available, otherwise use SetActive
            if (bloodOverlayCanvasGroup != null)
            {
                bloodOverlayCanvasGroup.alpha = 0f;
                Debug.Log("[PlayerHealth] Hiding blood overlay via CanvasGroup alpha = 0");
            }
            else
            {
                bloodOverlayImage.SetActive(false);
                Debug.Log("[PlayerHealth] Hiding blood overlay via SetActive(false)");
            }
        }
    }
    
    /// <summary>
    /// Pulsate the blood overlay during bleeding out
    /// Starts slow, gets faster as death approaches
    /// CRITICAL FIX: Uses unscaled time to work during pause
    /// </summary>
    private IEnumerator PulsateBloodOverlay()
    {
        Debug.Log("[PlayerHealth] Starting blood pulsation effect");
        
        // Ensure blood overlay is active and visible
        if (bloodOverlayImage != null)
        {
            bloodOverlayImage.SetActive(true);
        }
        
        float minAlpha = 0.4f; // Minimum alpha during pulse
        float maxAlpha = 0.9f; // Maximum alpha during pulse
        float minPulseSpeed = 1.0f; // Slowest pulse (at full health)
        float maxPulseSpeed = 4.0f; // Fastest pulse (near death)
        
        while (isBleedingOut && bloodOverlayImage != null)
        {
            // Get current bleed out progress from UI manager
            float progress = bleedOutUIManager != null ? bleedOutUIManager.GetBleedOutProgress() : 0.5f;
            
            // Calculate pulse speed based on progress (faster as death approaches)
            // progress: 1 = full health, 0 = dead
            float pulseSpeed = Mathf.Lerp(maxPulseSpeed, minPulseSpeed, progress);
            
            // Pulse in (fade to max alpha)
            float t = 0f;
            float pulseDuration = 0.5f / pulseSpeed;
            
            while (t < pulseDuration && isBleedingOut)
            {
                t += Time.unscaledDeltaTime;
                float normalizedTime = t / pulseDuration;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, normalizedTime);
                
                if (bloodOverlayCanvasGroup != null)
                {
                    bloodOverlayCanvasGroup.alpha = alpha;
                }
                
                yield return null;
            }
            
            // Pulse out (fade to min alpha)
            t = 0f;
            while (t < pulseDuration && isBleedingOut)
            {
                t += Time.unscaledDeltaTime;
                float normalizedTime = t / pulseDuration;
                float alpha = Mathf.Lerp(maxAlpha, minAlpha, normalizedTime);
                
                if (bloodOverlayCanvasGroup != null)
                {
                    bloodOverlayCanvasGroup.alpha = alpha;
                }
                
                yield return null;
            }
        }
        
        Debug.Log("[PlayerHealth] Blood pulsation effect ended");
    }
    
    /// <summary>
    /// Blink the blood overlay during self-revive
    /// CRITICAL FIX: Uses unscaled time to work during pause
    /// </summary>
    private IEnumerator BlinkBloodOverlay()
    {
        while (isSelfReviving && bloodOverlayImage != null)
        {
            // Hide overlay
            if (bloodOverlayCanvasGroup != null)
            {
                bloodOverlayCanvasGroup.alpha = 0f;
            }
            else
            {
                bloodOverlayImage.SetActive(false);
            }
            // CRITICAL FIX: Use WaitForSecondsRealtime so it works when paused (timeScale = 0)
            yield return new WaitForSecondsRealtime(0.3f);
            
            // Show overlay
            if (bloodOverlayCanvasGroup != null)
            {
                bloodOverlayCanvasGroup.alpha = 1f;
            }
            else
            {
                bloodOverlayImage.SetActive(true);
            }
            // CRITICAL FIX: Use WaitForSecondsRealtime so it works when paused (timeScale = 0)
            yield return new WaitForSecondsRealtime(0.3f);
        }
    }
    
    // === NEW MOVEMENT CONTROL METHODS ===
    
    /// <summary>
    /// Save the current movement mode before death
    /// </summary>
    private void SaveMovementModeBeforeDeath()
    {
        // Check if we're in AAA ground mode
        if (_aaaMovementIntegrator != null && _aaaMovementIntegrator.useAAAMovement)
        {
            _wasInAAAModeBeforeDeath = true;
            _movementModeBeforeDeath = MovementMode.Ground;
        }
        else
        {
            _wasInAAAModeBeforeDeath = false;
            _movementModeBeforeDeath = MovementMode.Flight;
        }
    }
    
    /// <summary>
    /// Disable ALL movement components to prevent any movement during death/revive
    /// </summary>
    private void DisableAllMovementForDeath()
    {
        
        // Disable master movement manager
        if (_playerMovementManager != null) _playerMovementManager.enabled = false;
        
        // Disable flight controller
        if (_celestialDriftController != null) _celestialDriftController.enabled = false;
        
        // Disable ground movement
        if (_aaaMovementController != null) _aaaMovementController.enabled = false;
        
        // Disable movement integrator
        if (_aaaMovementIntegrator != null) _aaaMovementIntegrator.enabled = false;
        
        // Disable character controller
        if (_characterController != null) _characterController.enabled = false;
        
        // Disable shooting
        if (_playerShooterOrchestrator != null) _playerShooterOrchestrator.enabled = false;
        
        // Stop all physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
    
    /// <summary>
    /// Clear all physics states to prevent slingshot effects
    /// </summary>
    private void ClearAllPhysicsStates()
    {
        
        // Clear Rigidbody velocities
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // Don't set kinematic yet - wait for movement restore
        }
        
        // Clear AAA Movement Controller internal velocity
        if (_aaaMovementController != null)
        {
            var velocityField = typeof(AAAMovementController).GetField("velocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (velocityField != null)
            {
                velocityField.SetValue(_aaaMovementController, Vector3.zero);
            }
        }
        
        // Clear any platform velocities if stored
        if (_aaaMovementIntegrator != null)
        {
            // Use reflection to clear any cached platform velocities
            var platformVelField = typeof(AAAMovementIntegrator).GetField("lastPlatformVelocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (platformVelField != null)
            {
                platformVelField.SetValue(_aaaMovementIntegrator, Vector3.zero);
            }
        }
    }
    
    /// <summary>
    /// Restore movement to the state it was before death
    /// </summary>
    private void RestoreMovementAfterRevive()
    {
        
        // Re-enable shooting first
        if (_playerShooterOrchestrator != null) _playerShooterOrchestrator.enabled = true;
        
        // Re-enable physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        // Re-enable character controller
        if (_characterController != null) _characterController.enabled = true;
        
        // Restore movement based on saved state
        if (_movementModeBeforeDeath == MovementMode.Ground && _wasInAAAModeBeforeDeath)
        {
            // Restore to ground mode
            
            // Enable AAA components
            if (_aaaMovementIntegrator != null)
            {
                _aaaMovementIntegrator.enabled = true;
                _aaaMovementIntegrator.useAAAMovement = true;
            }
            if (_aaaMovementController != null) _aaaMovementController.enabled = true;
            
            // Disable flight
            if (_celestialDriftController != null) _celestialDriftController.enabled = false;
            
            // Enable movement manager last
            if (_playerMovementManager != null) _playerMovementManager.enabled = true;
        }
        else
        {
            // Restore to flight mode (default)
            
            // Enable flight components
            if (_celestialDriftController != null) 
            {
                _celestialDriftController.enabled = true;
                // CRITICAL: Reset flight state to ensure clean reinitialization after revive
                _celestialDriftController.ResetFlightState(true);
            }
            
            // Disable AAA ground mode
            if (_aaaMovementIntegrator != null)
            {
                _aaaMovementIntegrator.enabled = true;
                _aaaMovementIntegrator.useAAAMovement = false;
            }
            if (_aaaMovementController != null) _aaaMovementController.enabled = false;
            
            // Enable movement manager last
            if (_playerMovementManager != null) _playerMovementManager.enabled = true;
        }
    }
    
    // === HEALTH REGENERATION SYSTEM ===
    
    /// <summary>
    /// Health regeneration coroutine - waits for delay then regenerates health with fast-then-slow curve
    /// </summary>
    private IEnumerator HealthRegenerationCoroutine()
    {
        Debug.Log("[PlayerHealth] Health regeneration coroutine started");
        
        while (!isDead)
        {
            // Wait for delay after taking damage
            float timeSinceLastDamage = Time.time - lastDamageTime;
            if (timeSinceLastDamage < regenDelayAfterDamage)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }
            
            // Check if health is already full
            if (_currentHealth >= maxHealth)
            {
                if (isRegenerating)
                {
                    Debug.Log("[PlayerHealth] Health regeneration stopped - health is full");
                }
                isRegenerating = false;
                yield return new WaitForSeconds(0.1f);
                continue;
            }
            
            // Start regenerating
            if (!isRegenerating)
            {
                isRegenerating = true;
                Debug.Log($"[PlayerHealth] Health regeneration started! Current: {_currentHealth}/{maxHealth}, Time since damage: {timeSinceLastDamage:F1}s");
            }
            
            // Calculate regeneration amount with fast-then-slow curve
            float regenTime = timeSinceLastDamage - regenDelayAfterDamage;
            float regenMultiplier = 1f;
            
            if (regenTime < regenBurstDuration)
            {
                // Initial burst phase - fast regeneration
                float burstProgress = regenTime / regenBurstDuration;
                // Smooth transition from burst to normal speed
                regenMultiplier = Mathf.Lerp(regenBurstMultiplier, 1f, burstProgress);
            }
            else
            {
                // Normal regeneration phase
                regenMultiplier = 1f;
            }
            
            // Apply regeneration
            float regenAmount = healthRegenRate * regenMultiplier * Time.deltaTime;
            _currentHealth += regenAmount;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);
            
            // Notify UI
            OnHealthChangedForHUD?.Invoke(_currentHealth, maxHealth);
            
            // Check if fully healed
            if (_currentHealth >= maxHealth)
            {
                isRegenerating = false;
                Debug.Log("[PlayerHealth] Health fully regenerated");
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Trigger instant health regeneration (called when plates are applied at low health)
    /// </summary>
    public void TriggerInstantHealthRegeneration()
    {
        // Reset damage timer to trigger immediate regeneration
        lastDamageTime = Time.time - regenDelayAfterDamage;
        
        Debug.Log("[PlayerHealth] Instant health regeneration triggered!");
    }
    
    /// <summary>
    /// Get current health percentage (0-1)
    /// </summary>
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? _currentHealth / maxHealth : 0f;
    }
    
    /// <summary>
    /// Check if health is low (below 20%)
    /// </summary>
    public bool IsHealthLow()
    {
        return GetHealthPercentage() < 0.2f;
    }
}