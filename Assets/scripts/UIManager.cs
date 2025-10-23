// --- UIManager.cs (FINAL, Corrected for Paused State) ---
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Scene Configuration")]
    public string mainMenuSceneName = "MainMenu";
    public string restartSceneName = "GameScene"; // Add a field for the restart scene

    // ... All other variables are unchanged ...
    [Header("Player HUD References")]
    public GameObject playerMainHudContainer;
    public Image playerHealthBarImage;
    public Gradient playerHealthGradient;
    [Header("Combined Heat HUD")]
    public GameObject combinedHeatHudContainer;
    public Image combinedHeatBarImage;
    public Gradient combinedHeatGradient;
    public float heatWarningThreshold = 0.75f;
    public float heatPulsateMinScale = 0.95f;
    public float heatPulsateMaxScale = 1.05f;
    public float heatPulsateSpeed = 2.5f;
    [Header("AOE UI")]
    public GameObject aoeHudContainer;
    public Image aoeBarImage;
    public TextMeshProUGUI aoeStatusText;
    public Gradient aoeBarGradient;
    public float aoeBarPopScale = 1.1f;
    public float aoeBarPopDuration = 0.1f;
    private Coroutine _aoeBarAnimationCoroutine;
    private Vector3 _originalAoeBarScale = Vector3.one;
    public GameObject aoePowerUpChargesDisplay;
    public TextMeshProUGUI aoePowerUpChargesText;
    [Header("Homing Dagger PowerUp UI")]
    public GameObject homingDaggerPowerUpActiveIcon;
    public TextMeshProUGUI homingDaggerPowerUpTimerText;
    [Header("Game State UI Panels")]
    public GameObject gameOverPanel;
    public GameObject pauseMenuPanel;
    [Header("Pause Menu Panel Stats Texts (Optional)")]
    public TextMeshProUGUI pauseMenuCurrentKillsTMP;
    public TextMeshProUGUI pauseMenuTotalGemsTMP;
    public TextMeshProUGUI pauseMenuPrimaryHandLevelTMP;
    public TextMeshProUGUI pauseMenuSecondaryHandLevelTMP;
    public TextMeshProUGUI pauseMenuCurrentSurvivalTimeTMP;
    [Header("Scene Fade Elements")]
    public CanvasGroup fadePanelCanvasGroup;
    public float fadeInDuration = 1.0f;
    public float fadeOutDuration = 1.0f;

    [Header("Missions In-Game UI")]
    [Tooltip("Prefab for the Equipped Missions panel (instantiated in-game)")]
    public GameObject equippedMissionsPanelPrefab;
    [Tooltip("Optional parent for the Equipped Missions panel. If null, will use playerMainHudContainer")] 
    public Transform equippedMissionsParent;
    [Tooltip("Show the Equipped Missions panel during gameplay")] 
    public bool enableEquippedMissionsInGame = true;

    private PlayerHealth _playerHealthScript;
    private Coroutine _heatBarPulsateCoroutine;
    private Vector3 _originalHeatBarScale = Vector3.one;
    private bool _isHeatPulsating = false;
    private bool _isGameActuallyPaused = false;
    private bool _overheatEventSubscribed = false;


    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }
        // ... rest of Awake is unchanged ...
    }

    // --- YES, you MUST have these public methods here for the buttons to call ---
    public void ResumeGame()
    {
        if (_isGameActuallyPaused)
        {
            TogglePauseMenu();
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(FadeOutScene(restartSceneName));
    }

    // Method to update pause menu stats
    private void UpdatePauseMenuStats()
    {
        try
        {
            // Update kills if available (GameStats is static)
            if (pauseMenuCurrentKillsTMP != null)
            {
                pauseMenuCurrentKillsTMP.text = GameStats.CurrentGameTotalEnemiesKilled.ToString();
            }

            // Update gems if available
            if (pauseMenuTotalGemsTMP != null)
            {
                var inventoryManager = InventoryManager.Instance;
                if (inventoryManager != null)
                {
                    pauseMenuTotalGemsTMP.text = inventoryManager.GetGemCount().ToString();
                }
            }

            // Update hand levels if available
            if (pauseMenuPrimaryHandLevelTMP != null || pauseMenuSecondaryHandLevelTMP != null)
            {
                var handLevelManager = HandLevelPersistenceManager.Instance;
                if (handLevelManager != null)
                {
                    if (pauseMenuPrimaryHandLevelTMP != null)
                        pauseMenuPrimaryHandLevelTMP.text = handLevelManager.CurrentPrimaryHandLevel.ToString();
                    if (pauseMenuSecondaryHandLevelTMP != null)
                        pauseMenuSecondaryHandLevelTMP.text = handLevelManager.CurrentSecondaryHandLevel.ToString();
                }
            }

            // Update survival time if available (GameStats is static)
            if (pauseMenuCurrentSurvivalTimeTMP != null)
            {
                pauseMenuCurrentSurvivalTimeTMP.text = FormatTime(GameStats.CurrentRunSurvivalTimeSeconds);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] Error updating pause menu stats: {e.Message}");
        }
    }

    // Debug method to force show pause menu
    [System.Obsolete("Debug method only - remove in production")]
    public void ForceShowPauseMenu()
    {
        Debug.Log("[UIManager] ForceShowPauseMenu called for debugging");
        
        if (pauseMenuPanel == null)
        {
            Debug.LogError("[UIManager] pauseMenuPanel is null!");
            return;
        }

        _isGameActuallyPaused = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        UpdatePauseMenuStats();
        
        Debug.Log($"[UIManager] Pause menu forced to show. Panel active: {pauseMenuPanel.activeSelf}");
    }

   

   
    // --- MODIFIED Coroutine ---
    public IEnumerator FadeOutScene(string sceneToLoad = "")
    {
        if (fadePanelCanvasGroup == null)
        {
            if (!string.IsNullOrEmpty(sceneToLoad)) SceneManager.LoadScene(sceneToLoad);
            yield break;
        }

        fadePanelCanvasGroup.alpha = 0;
        fadePanelCanvasGroup.gameObject.SetActive(true);

        float timer = 0;
        if (fadeOutDuration <= 0) fadeOutDuration = 0.01f;

        while (timer < fadeOutDuration)
        {
            // USE UNSCALED DELTA TIME
            timer += Time.unscaledDeltaTime;
            fadePanelCanvasGroup.alpha = Mathf.Clamp01(timer / fadeOutDuration);
            yield return new WaitForEndOfFrame(); // yield return null is fine here as the timer uses unscaled time
        }

        fadePanelCanvasGroup.alpha = 1f;

        // Reset cursor state before leaving the scene
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }


    void OnEnable()
    {
        PlayerHealth.OnHealthChangedForHUD += UpdatePlayerHealthBar;
        PlayerHealth.OnPlayerDied += HandlePlayerDeathUI;
        AttemptOverheatSubscription();
    }

    void OnDisable()
    {
        PlayerHealth.OnHealthChangedForHUD -= UpdatePlayerHealthBar;
        PlayerHealth.OnPlayerDied -= HandlePlayerDeathUI;
        AttemptOverheatUnsubscription();

        if (Time.timeScale == 0f && _isGameActuallyPaused) Time.timeScale = 1f;
    }

    void Start()
    {
        // --- MODIFICATION HERE ---
        // These two lines will lock the cursor to the center of the screen
        // and make it invisible.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // -------------------------

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) _playerHealthScript = playerObject.GetComponent<PlayerHealth>();
        else Debug.LogError("UIManager: Player GameObject not found in Start! Some UI updates might fail.", this);

        if (!_overheatEventSubscribed) AttemptOverheatSubscription();

        if (_playerHealthScript != null) UpdatePlayerHealthBar(_playerHealthScript.CurrentHealth, _playerHealthScript.maxHealth);
        else UpdatePlayerHealthBar(100, 100);

        if (PlayerOverheatManager.Instance != null)
            HandleIndividualHeatChangeForCombinedBar(true, PlayerOverheatManager.Instance.CurrentHeatPrimary, PlayerOverheatManager.Instance.maxHeat);
        else UpdateCombinedHeatBar(0, 100f);

        // PlayerAOEAbility will update UI through its own internal calls
        if (PlayerAOEAbility.Instance == null) DisplayAOEUnavailable();

        if (fadePanelCanvasGroup != null)
        {
            if (fadePanelCanvasGroup.alpha >= 0.99f && fadePanelCanvasGroup.gameObject.activeSelf) StartCoroutine(FadeInScene());
            else { fadePanelCanvasGroup.alpha = 0f; fadePanelCanvasGroup.gameObject.SetActive(false); }
        }
        else Debug.LogWarning("UIManager: fadePanelCanvasGroup NOT ASSIGNED. Scene fades will not work.", this);

        // Instantiate Equipped Missions panel for in-game HUD
        if (enableEquippedMissionsInGame)
        {
            if (equippedMissionsPanelPrefab == null)
            {
                Debug.LogWarning("UIManager: equippedMissionsPanelPrefab not assigned. Equipped missions UI will not be shown in-game.", this);
            }
            else
            {
                Transform parent = equippedMissionsParent != null ? equippedMissionsParent
                    : (playerMainHudContainer != null ? playerMainHudContainer.transform : transform);
                var instance = Instantiate(equippedMissionsPanelPrefab, parent);
                instance.SetActive(true);

                var equippedUI = instance.GetComponentInChildren<GeminiGauntlet.Missions.UI.EquippedMissionsUI>();
                if (equippedUI != null)
                {
                    equippedUI.RefreshEquippedMissions();
                }
            }
        }
    }

    bool IsRectTransformValidForScale(RectTransform rt)
    {
        return rt != null && rt.gameObject.activeInHierarchy;
    }

    private void AttemptOverheatSubscription()
    {
        if (PlayerOverheatManager.Instance != null && !_overheatEventSubscribed)
        {
            PlayerOverheatManager.Instance.OnHeatChangedForHUD += HandleIndividualHeatChangeForCombinedBar;
            _overheatEventSubscribed = true;
        }
    }

    private void AttemptOverheatUnsubscription()
    {
        if (PlayerOverheatManager.Instance != null && _overheatEventSubscribed)
        {
            PlayerOverheatManager.Instance.OnHeatChangedForHUD -= HandleIndividualHeatChangeForCombinedBar;
            _overheatEventSubscribed = false;
        }
    }

    private void HandleIndividualHeatChangeForCombinedBar(bool isPrimary, float currentHeat, float maxHeat)
    {
        if (PlayerOverheatManager.Instance == null) return;
        float heatToShow = Mathf.Max(PlayerOverheatManager.Instance.CurrentHeatPrimary, PlayerOverheatManager.Instance.CurrentHeatSecondary);
        UpdateCombinedHeatBar(heatToShow, maxHeat);
    }

    public void UpdateCombinedHeatBar(float currentCombinedHeat, float maxHeat)
    {
        if (combinedHeatHudContainer == null || combinedHeatBarImage == null) return;
        combinedHeatHudContainer.SetActive(true);

        float fillAmount = (maxHeat > 0) ? Mathf.Clamp01(currentCombinedHeat / maxHeat) : 0f;
        combinedHeatBarImage.fillAmount = fillAmount;
        if (combinedHeatGradient != null) combinedHeatBarImage.color = combinedHeatGradient.Evaluate(fillAmount);

        bool shouldPulsate = fillAmount >= heatWarningThreshold && fillAmount < 1.0f;

        if (shouldPulsate)
        {
            if (!_isHeatPulsating)
            {
                _isHeatPulsating = true;
                if (_heatBarPulsateCoroutine != null) StopCoroutine(_heatBarPulsateCoroutine);
                if (combinedHeatBarImage.rectTransform != null && _originalHeatBarScale == Vector3.zero) _originalHeatBarScale = combinedHeatBarImage.rectTransform.localScale;
                if (_originalHeatBarScale == Vector3.zero) _originalHeatBarScale = Vector3.one;

                _heatBarPulsateCoroutine = StartCoroutine(PulsateBarCoroutine(combinedHeatBarImage, _originalHeatBarScale, heatPulsateMinScale, heatPulsateMaxScale, heatPulsateSpeed));
            }
        }
        else
        {
            if (_isHeatPulsating)
            {
                _isHeatPulsating = false;
            }
            else if (combinedHeatBarImage.rectTransform != null && combinedHeatBarImage.rectTransform.localScale != _originalHeatBarScale && _originalHeatBarScale != Vector3.zero)
            {
                combinedHeatBarImage.rectTransform.localScale = _originalHeatBarScale;
            }
        }
    }

    private IEnumerator PulsateBarCoroutine(Image barImage, Vector3 originalScaleToUse, float minScaleFactor, float maxScaleFactor, float speed)
    {
        if (barImage == null || !IsRectTransformValidForScale(barImage.rectTransform)) { _isHeatPulsating = false; yield break; }
        RectTransform barRectTransform = barImage.rectTransform;
        Vector3 baseScale = (originalScaleToUse == Vector3.zero) ? barRectTransform.localScale : originalScaleToUse;
        if (baseScale == Vector3.zero) baseScale = Vector3.one;

        float timer = 0; float cycleDuration = (speed > 0.01f) ? 1f / speed : 1f;
        while (_isHeatPulsating)
        {
            timer += Time.unscaledDeltaTime;
            float phase = (timer % cycleDuration) / cycleDuration;
            float sinVal = (Mathf.Sin(phase * Mathf.PI * 2f) + 1f) / 2f;
            float currentScaleFactor = Mathf.Lerp(minScaleFactor, maxScaleFactor, sinVal);
            if (barRectTransform != null) barRectTransform.localScale = Vector3.Scale(baseScale, new Vector3(currentScaleFactor, currentScaleFactor, currentScaleFactor));
            yield return new WaitForEndOfFrame();
        }

        if (barRectTransform != null) barRectTransform.localScale = baseScale;
        _heatBarPulsateCoroutine = null;
    }

    void Update()
    {
        // Check for pause input
        if (Input.GetKeyDown(Controls.Pause))
        {
            Debug.Log("[UIManager] Pause key pressed (Escape)");
            
            bool canPause = true;
            
            // FIXED: More robust game over panel check - also verify it's actually a game over situation
            if (gameOverPanel != null && gameOverPanel.activeSelf) 
            {
                // CRITICAL FIX: Only block pause if player is actually dead
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                PlayerHealth playerHealth = player?.GetComponent<PlayerHealth>();
                
                if (playerHealth != null && playerHealth.isDead)
                {
                    canPause = false;
                    Debug.Log("[UIManager] Cannot pause - player is dead and game over panel is active");
                }
                else
                {
                    // Player is NOT dead but game over panel is active - this is a bug, fix it
                    Debug.LogWarning("[UIManager] FIXING BUG: Game over panel active but player not dead - deactivating panel");
                    gameOverPanel.SetActive(false);
                    canPause = true;
                }
            }

            if (canPause)
            {
                if (pauseMenuPanel == null) 
                { 
                    Debug.LogError("[UIManager] Cannot toggle pause, pauseMenuPanel not assigned!", this); 
                    return; 
                }
                
                TogglePauseMenu();
            }
        }

        // Debug key to force show pause menu (remove in production)
        if (Input.GetKeyDown(KeyCode.P) && Input.GetKey(KeyCode.LeftShift))
        {
            Debug.Log("[UIManager] Debug key pressed - forcing pause menu");
            ForceShowPauseMenu();
        }
    }

    void UpdatePlayerHealthBar(float currentHealth, float maxHealth)
    {
        if (playerHealthBarImage != null)
        {
            float fillAmount = (maxHealth > 0) ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
            playerHealthBarImage.fillAmount = fillAmount;
            if (playerHealthGradient != null) playerHealthBarImage.color = playerHealthGradient.Evaluate(fillAmount);
        }
    }

    public void UpdateHomingDaggerPowerUpStatus(bool isActive, float durationOrValue)
    {
        if (homingDaggerPowerUpActiveIcon != null)
        {
            homingDaggerPowerUpActiveIcon.SetActive(isActive);
        }
        if (homingDaggerPowerUpTimerText != null)
        {
            if (isActive)
            {
                homingDaggerPowerUpTimerText.text = durationOrValue.ToString("F1") + "s";
            }
            else
            {
                homingDaggerPowerUpTimerText.text = "";
            }
        }
    }


    public void TogglePauseMenu()
    {
        Debug.Log($"[UIManager] TogglePauseMenu called. Current paused state: {_isGameActuallyPaused}");
        
        // Null check for pause menu panel
        if (pauseMenuPanel == null)
        {
            Debug.LogError("[UIManager] pauseMenuPanel is null! Cannot toggle pause menu.", this);
            return;
        }

        _isGameActuallyPaused = !_isGameActuallyPaused;
        
        Debug.Log($"[UIManager] Setting pauseMenuPanel active: {_isGameActuallyPaused}");
        pauseMenuPanel.SetActive(_isGameActuallyPaused);

        if (_isGameActuallyPaused)
        {
            Time.timeScale = 0f;
            
            // ⭐ FIX: Disable player movement to prevent floating
            DisablePlayerMovement();
            
            // Free the cursor so you can click buttons
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Update pause menu stats if available
            UpdatePauseMenuStats();
            
            // 🩸 CRITICAL FIX: Notify PlayerHealth about pause state change
            NotifyPlayerHealthPauseState(true);
            
            Debug.Log("[UIManager] Game paused - cursor unlocked, movement disabled");
        }
        else
        {
            Time.timeScale = 1f;
            
            // ⭐ FIX: Re-enable player movement
            EnablePlayerMovement();
            
            // Re-lock the cursor for FPS gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // 🩸 CRITICAL FIX: Notify PlayerHealth about pause state change
            NotifyPlayerHealthPauseState(false);
            
            Debug.Log("[UIManager] Game resumed - cursor locked, movement enabled");
        }
    }

    // 🩸 NEW METHOD: Notify PlayerHealth about pause menu state changes
    private void NotifyPlayerHealthPauseState(bool isPaused)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnPauseMenuStateChanged(isPaused);
                Debug.Log($"[UIManager] ✅ Notified PlayerHealth of pause state: {isPaused}");
            }
        }
    }
    
    // ⭐ NEW METHOD: Disable player movement when paused
    private void DisablePlayerMovement()
    {
        Debug.Log("[UIManager] DisablePlayerMovement called!");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log($"[UIManager] Player found: {player.name}");
            
            // STEP 1: Disable scripts that DEPEND on CharacterController/Rigidbody FIRST
            // (Must disable these before disabling CharacterController/Rigidbody!)
            
            var aaaMovement = player.GetComponent<AAAMovementController>();
            if (aaaMovement != null) 
            {
                aaaMovement.enabled = false;
                Debug.Log("[UIManager] ✅ AAAMovementController disabled");
            }
            
            // CleanAAAMovementController removed - using AAAMovementController only
            
            var aaaCrouch = player.GetComponent<CleanAAACrouch>();
            if (aaaCrouch != null)
            {
                aaaCrouch.enabled = false;
                Debug.Log("[UIManager] ✅ CleanAAACrouch disabled");
            }
            
            var fallingDamage = player.GetComponent<FallingDamageSystem>();
            if (fallingDamage != null)
            {
                fallingDamage.enabled = false;
                Debug.Log("[UIManager] ✅ FallingDamageSystem disabled");
            }
            
            var celestialDrift = player.GetComponent<CelestialDriftController>();
            if (celestialDrift != null)
            {
                celestialDrift.enabled = false;
                Debug.Log("[UIManager] ✅ CelestialDriftController disabled");
            }
            
            // STEP 2: Now disable CharacterController and Rigidbody
            
            var characterController = player.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
                Debug.Log("[UIManager] ✅ CharacterController disabled");
            }
            
            var rigidbody = player.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = true;
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                Debug.Log("[UIManager] ✅ Rigidbody frozen - no more floating!");
            }
        }
        else
        {
            Debug.LogError("[UIManager] ❌ PLAYER NOT FOUND! Make sure your player GameObject has the 'Player' tag!");
        }
    }

    // ⭐ NEW METHOD: Re-enable player movement when unpaused
    private void EnablePlayerMovement()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // STEP 1: Re-enable CharacterController and Rigidbody FIRST
            
            var characterController = player.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = true;
                Debug.Log("[UIManager] ✅ CharacterController enabled");
            }
            
            var rigidbody = player.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = false;
                Debug.Log("[UIManager] ✅ Rigidbody unfrozen");
            }
            
            // STEP 2: Then re-enable scripts that depend on them
            
            var aaaMovement = player.GetComponent<AAAMovementController>();
            if (aaaMovement != null) 
            {
                aaaMovement.enabled = true;
                Debug.Log("[UIManager] ✅ AAAMovementController enabled");
            }
            
            // CleanAAAMovementController removed - using AAAMovementController only
            
            var aaaCrouch = player.GetComponent<CleanAAACrouch>();
            if (aaaCrouch != null)
            {
                aaaCrouch.enabled = true;
                Debug.Log("[UIManager] ✅ CleanAAACrouch enabled");
            }
            
            var fallingDamage = player.GetComponent<FallingDamageSystem>();
            if (fallingDamage != null)
            {
                fallingDamage.enabled = true;
                Debug.Log("[UIManager] ✅ FallingDamageSystem enabled");
            }
            
            var celestialDrift = player.GetComponent<CelestialDriftController>();
            if (celestialDrift != null)
            {
                celestialDrift.enabled = true;
                Debug.Log("[UIManager] ✅ CelestialDriftController enabled");
            }
            
            Debug.Log("[UIManager] ✅ All movement restored!");
        }
    }
    private void HandlePlayerDeathUI()
    {
        if (playerMainHudContainer != null) playerMainHudContainer.SetActive(false);
    }

    private string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds < 0) timeInSeconds = 0;
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        float seconds = timeInSeconds % 60;
        return string.Format("{0:00}m {1:00.0}s", minutes, seconds);
    }

    public void ReturnToMainMenu() // Modified to have no parameters
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // MODIFIED: Uses the public variable instead of a parameter
        StartCoroutine(FadeOutScene(mainMenuSceneName));
    }
    public IEnumerator FadeInScene()
    {
        if (fadePanelCanvasGroup == null) yield break;
        fadePanelCanvasGroup.alpha = 1f; fadePanelCanvasGroup.gameObject.SetActive(true);
        float t = 0; if (fadeInDuration <= 0) fadeInDuration = 0.01f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            fadePanelCanvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeInDuration);
            yield return new WaitForEndOfFrame();
        }
        fadePanelCanvasGroup.alpha = 0; fadePanelCanvasGroup.gameObject.SetActive(false);
    }


    public void DisplayAOEUnavailable()
    {
        if (aoeHudContainer == null) return;
        aoeHudContainer.SetActive(true);

        if (aoeBarImage != null) aoeBarImage.gameObject.SetActive(false);
        if (aoeStatusText != null)
        {
            aoeStatusText.gameObject.SetActive(true);
            aoeStatusText.text = "AOE N/A";
        }
        if (aoePowerUpChargesDisplay != null) aoePowerUpChargesDisplay.SetActive(false);
    }

    public void DisplayAOEReady(int charges = 1)
    {
        if (aoeHudContainer == null || aoeStatusText == null) return;
        aoeHudContainer.SetActive(true);
        if (aoeBarImage != null) aoeBarImage.gameObject.SetActive(true);
        if (aoeStatusText != null) aoeStatusText.gameObject.SetActive(true);

        if (_aoeBarAnimationCoroutine != null) { StopCoroutine(_aoeBarAnimationCoroutine); }
        if (aoeBarImage != null)
        {
            if (aoeBarImage.rectTransform != null && _originalAoeBarScale == Vector3.zero) _originalAoeBarScale = aoeBarImage.rectTransform.localScale;
            if (_originalAoeBarScale == Vector3.zero) _originalAoeBarScale = Vector3.one;

            _aoeBarAnimationCoroutine = StartCoroutine(AnimateBarCoroutine(aoeBarImage, _originalAoeBarScale, aoeBarGradient, 1f, true, aoeBarPopScale, aoeBarPopDuration, true));
        }
        aoeStatusText.text = "AOE RDY";

        if (aoePowerUpChargesDisplay != null)
        {
            bool showCharges = charges > 1;
            aoePowerUpChargesDisplay.SetActive(showCharges);
            if (showCharges && aoePowerUpChargesText != null) aoePowerUpChargesText.text = $"x{charges}";
        }
    }

    public void SetAOEToActiveStateDisplay()
    {
        if (aoeHudContainer == null) return;
        aoeHudContainer.SetActive(true);

        if (aoeBarImage != null) aoeBarImage.gameObject.SetActive(false);
        if (aoeStatusText != null)
        {
            aoeStatusText.gameObject.SetActive(true);
            aoeStatusText.text = "ACTIVE";
        }
        if (aoePowerUpChargesDisplay != null) aoePowerUpChargesDisplay.SetActive(false);
    }

    public void DisplayAOETimeCooldown(float remainingTime, float totalDuration)
    {
        if (aoeHudContainer == null || aoeBarImage == null || aoeStatusText == null) return;
        aoeHudContainer.SetActive(true);
        if (aoeBarImage != null) aoeBarImage.gameObject.SetActive(true);
        if (aoeStatusText != null) aoeStatusText.gameObject.SetActive(true);

        UpdateAOETimeCooldownText(remainingTime);
        float fill = (totalDuration > 0) ? Mathf.Clamp01((totalDuration - remainingTime) / totalDuration) : 1f;
        if (aoeBarImage != null)
        {
            aoeBarImage.fillAmount = fill;
            if (aoeBarGradient != null) aoeBarImage.color = aoeBarGradient.Evaluate(fill);
        }
        if (aoePowerUpChargesDisplay != null) aoePowerUpChargesDisplay.SetActive(false);
    }

    public void UpdateAOETimeCooldownText(float remainingTime)
    {
        if (aoeStatusText != null)
            aoeStatusText.text = remainingTime > 0.1f ? remainingTime.ToString("F1") + "S" : "AOE N/A";
    }

    public void StartAOETimeCooldownVisuals(float totalCooldownDuration)
    {
        if (aoeHudContainer == null || aoeBarImage == null || aoeStatusText == null) return;
        aoeHudContainer.SetActive(true);
        if (aoeBarImage != null) aoeBarImage.gameObject.SetActive(true);
        if (aoeStatusText != null) aoeStatusText.gameObject.SetActive(true);

        if (_aoeBarAnimationCoroutine != null) StopCoroutine(_aoeBarAnimationCoroutine);
        if (aoeBarImage != null)
        {
            aoeBarImage.fillAmount = 0f;
            if (aoeBarGradient != null) aoeBarImage.color = aoeBarGradient.Evaluate(0f);
            _aoeBarAnimationCoroutine = StartCoroutine(AnimateBarFillOverDurationCoroutine(aoeBarImage, aoeBarGradient, 1f, totalCooldownDuration, true));
        }
        UpdateAOETimeCooldownText(totalCooldownDuration);
        if (aoePowerUpChargesDisplay != null) aoePowerUpChargesDisplay.SetActive(false);
    }

    private IEnumerator AnimateBarCoroutine(Image barImage, Vector3 originalScaleToUse, Gradient barGradient, float targetFill, bool pop, float popScaleAmount, float popAnimDuration, bool isAOEBar)
    {
        if (barImage == null) { if (isAOEBar) _aoeBarAnimationCoroutine = null; yield break; }
        RectTransform barRectTransform = barImage.rectTransform;
        Vector3 baseScale = (originalScaleToUse == Vector3.zero && IsRectTransformValidForScale(barRectTransform)) ? barRectTransform.localScale : originalScaleToUse;
        if (baseScale == Vector3.zero) baseScale = Vector3.one;

        if (pop && popAnimDuration > 0 && IsRectTransformValidForScale(barRectTransform))
        {
            float popTimer = 0f; Vector3 popTargetScale = Vector3.Scale(baseScale, new Vector3(popScaleAmount, popScaleAmount, popScaleAmount));
            while (popTimer < popAnimDuration / 2f) { popTimer += Time.unscaledDeltaTime; float p = Mathf.Clamp01(popTimer / (popAnimDuration / 2f)); if (barRectTransform != null) barRectTransform.localScale = Vector3.Lerp(baseScale, popTargetScale, p); yield return null; }
            popTimer = 0f;
            while (popTimer < popAnimDuration / 2f) { popTimer += Time.unscaledDeltaTime; float p = Mathf.Clamp01(popTimer / (popAnimDuration / 2f)); if (barRectTransform != null) barRectTransform.localScale = Vector3.Lerp(popTargetScale, baseScale, p); yield return null; }
            if (barRectTransform != null) barRectTransform.localScale = baseScale;
        }
        barImage.fillAmount = targetFill; if (barGradient != null) barImage.color = barGradient.Evaluate(targetFill);
        if (isAOEBar) _aoeBarAnimationCoroutine = null;
    }

    private IEnumerator AnimateBarFillOverDurationCoroutine(Image barImage, Gradient barGradient, float targetFill, float animationDuration, bool isAOEBar)
    {
        if (barImage == null) { if (isAOEBar) _aoeBarAnimationCoroutine = null; yield break; }
        if (animationDuration <= 0) { barImage.fillAmount = targetFill; if (barGradient != null) barImage.color = barGradient.Evaluate(targetFill); if (isAOEBar) _aoeBarAnimationCoroutine = null; yield break; }
        float initialFill = barImage.fillAmount; float fillAnimTimer = 0f;
        while (fillAnimTimer < animationDuration)
        {
            fillAnimTimer += Time.unscaledDeltaTime; float progress = Mathf.Clamp01(fillAnimTimer / animationDuration);
            float newFillAmount = Mathf.Lerp(initialFill, targetFill, progress);
            barImage.fillAmount = newFillAmount; if (barGradient != null) barImage.color = barGradient.Evaluate(newFillAmount);
            UpdateAOETimeCooldownText(animationDuration - fillAnimTimer);
            yield return new WaitForEndOfFrame();
        }
        barImage.fillAmount = targetFill; if (barGradient != null) barImage.color = barGradient.Evaluate(targetFill);
        UpdateAOETimeCooldownText(0);
        if (isAOEBar) _aoeBarAnimationCoroutine = null;
    }
}