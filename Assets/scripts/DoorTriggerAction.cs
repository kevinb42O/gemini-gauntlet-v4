// --- DoorTriggerAction.cs (Ensure GameStats calls are correct) ---
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class DoorTriggerAction : MonoBehaviour
{
    public enum DoorAction { GoToMainMenu, RestartLevel }
    public DoorAction actionToPerform;

    [Tooltip("Only needed if RestartLevel. Name of the main game scene to reload. If empty, uses current scene name.")]
    public string gameSceneToRestart = "GameScene"; // Default, can be overridden
    [Tooltip("Name of the main menu scene.")]
    public string mainMenuSceneName = "MainMenu";

    private bool _triggered = false; // Renamed for convention
    private Collider _doorCollider;
    private AudioSource _localAudioSource;
    private AudioSource _globallyActiveActivationSoundSource;

    void Awake()
    {
        _doorCollider = GetComponent<Collider>();
        if (_doorCollider == null)
        {
            Debug.LogError($"DoorTriggerAction ({name}): Collider component missing! Trigger will not work.", this);
            enabled = false;
            return;
        }
        if (!_doorCollider.isTrigger)
        {
            Debug.LogWarning($"DoorTriggerAction ({name}): Collider on '{gameObject.name}' is not set to 'Is Trigger'. Automatically setting it now.", this);
            _doorCollider.isTrigger = true;
        }
        _localAudioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return; // Prevent multiple triggers

        if (other.CompareTag("Player"))
        {
            _triggered = true;
            Debug.Log($"Door '{gameObject.name}' triggered by player. Action: {actionToPerform}");

            // Reset time scale and cursor state before loading new scene
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (actionToPerform == DoorAction.GoToMainMenu)
            {
                if (string.IsNullOrEmpty(mainMenuSceneName))
                {
                    Debug.LogError($"DoorTriggerAction ({name}): MainMenuSceneName not set! Cannot go to main menu.", this);
                    _triggered = false; // Allow re-trigger if error
                    return;
                }
                // Optional: Add fade out here if UIManager is accessible
                // if (UIManager.Instance != null) StartCoroutine(UIManager.Instance.FadeOutScene(mainMenuSceneName));
                // else SceneManager.LoadScene(mainMenuSceneName);
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else if (actionToPerform == DoorAction.RestartLevel)
            {
                string sceneNameToLoad = string.IsNullOrEmpty(gameSceneToRestart) ? SceneManager.GetActiveScene().name : gameSceneToRestart;

                if (string.IsNullOrEmpty(sceneNameToLoad))
                {
                    Debug.LogError($"DoorTriggerAction ({name}): GameSceneToRestart is not set and could not get current scene name! Cannot restart level.", this);
                    _triggered = false; // Allow re-trigger
                    return;
                }

                // CRITICAL: Reset stats for a new run when restarting
                GameStats.ResetStatsForNewRun();

                // PlayerProgression and PlayerAOEAbility are usually singletons that persist or are re-initialized on scene load.
                // If they have specific "reset for new game run" methods, call them here.
                // However, their Start/Awake methods should handle initialization correctly if they are reloaded with the scene.
                // The calls you had are fine if those singletons persist across scene loads and need explicit reset.
                // If they are part of the scene and get reloaded, their own Start/Awake will handle it.

                if (PlayerProgression.Instance != null)
                {
                    // PlayerProgression.InitializeHandState() is typically called in its own Start.
                    // If this door is used mid-game to "restart" without a full scene reload (unlikely), then explicit call is needed.
                    // For a full SceneManager.LoadScene, it's often not.
                    // However, calling it won't hurt if it's idempotent.
                    PlayerProgression.Instance.InitializeOrResetForNewRun();
                }
                else Debug.LogWarning($"DoorTriggerAction ({name}): PlayerProgression.Instance not found.Progression might not reset as expected on restart.", this);


                if (PlayerAOEAbility.Instance != null)
                {
                    PlayerAOEAbility.Instance.ResetForNewGame();
                }
                else Debug.LogWarning($"DoorTriggerAction ({name}): PlayerAOEAbility.Instance not found. AOE state might not reset as expected on restart.", this);


                // Optional: Add fade out here
                // if (UIManager.Instance != null) StartCoroutine(UIManager.Instance.FadeOutScene(sceneNameToLoad));
                // else SceneManager.LoadScene(sceneNameToLoad);
                SceneManager.LoadScene(sceneNameToLoad);
            }
        }
    }

    void OnDisable()
    {
        if (_localAudioSource != null && _globallyActiveActivationSoundSource == _localAudioSource)
        {
            if (_localAudioSource.isPlaying)
            {
                _localAudioSource.Stop();
            }
            _globallyActiveActivationSoundSource = null;

        }
    }
}