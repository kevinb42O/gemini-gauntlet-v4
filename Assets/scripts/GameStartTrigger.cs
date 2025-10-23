using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartTrigger : MonoBehaviour
{
    [Header("Game Scene Settings")]
    public string gameSceneName = "GameScene"; // Name of your game scene
    public bool loadSceneAfterSave = true; // Automatically load game scene after saving
    
    [Header("Debug")]
    public bool showDebugLogs = true;

    // Call this method from your "Start Game" button's OnClick event
    public void OnStartGameButtonClick()
    {
        if (showDebugLogs) Debug.Log("🎮 START GAME: Button clicked!");
        
        // Save companion selection data and start cooldowns
        if (PersistentCompanionSelectionManager.Instance != null)
        {
            PersistentCompanionSelectionManager.Instance.OnGameStart();
            if (showDebugLogs) Debug.Log("🎮 START GAME: Cooldowns started, companion data saved, UI cleared");
        }
        else
        {
            Debug.LogWarning("🎮 START GAME: PersistentCompanionSelectionManager not found!");
        }
        
        // Load game scene
        if (loadSceneAfterSave && !string.IsNullOrEmpty(gameSceneName))
        {
            if (showDebugLogs) Debug.Log($"🎮 START GAME: Loading scene '{gameSceneName}'");
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            if (showDebugLogs) Debug.Log("🎮 START GAME: Scene loading disabled or no scene name provided");
        }
    }

    // Alternative method if you want to save without loading scene
    public void SaveCompanionDataOnly()
    {
        if (PersistentCompanionSelectionManager.Instance != null)
        {
            PersistentCompanionSelectionManager.Instance.OnGameStart();
            if (showDebugLogs) Debug.Log("🎮 Companion data saved (no scene load)");
        }
    }

    // Alternative method if you want to load scene without saving (for testing)
    public void LoadGameSceneOnly()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            if (showDebugLogs) Debug.Log($"🎮 Loading scene '{gameSceneName}' (no data save)");
            SceneManager.LoadScene(gameSceneName);
        }
    }
}