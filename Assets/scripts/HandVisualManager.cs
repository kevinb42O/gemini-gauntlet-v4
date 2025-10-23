// --- HandVisualManager.cs (NEW SCRIPT) ---
using UnityEngine;

public class HandVisualManager : MonoBehaviour
{
    [Tooltip("Assign all visual GameObjects for this hand, ordered by level (Element 0 for Level 1, etc.). These should typically be children of this GameObject.")]
    public GameObject[] visualsByLevel;

    private GameObject _currentActiveVisual = null;
    private int _currentLevel = 0; // 0 means uninitialized or no visual active

    /// <summary>
    /// Sets the active visual based on the provided level.
    /// Levels are 1-indexed.
    /// </summary>
    /// <param name="level">The desired hand level (1-indexed).</param>
    /// <returns>The activated GameObject visual, or null if not found or level is invalid.</returns>
    public GameObject SetActiveLevelVisual(int level)
    {
        if (visualsByLevel == null || visualsByLevel.Length == 0)
        {
            if (_currentActiveVisual != null)
            {
                _currentActiveVisual.SetActive(false); // Deactivate any old one
                _currentActiveVisual = null;
            }
            _currentLevel = 0;
            Debug.LogWarning($"HandVisualManager on {gameObject.name}: 'Visuals By Level' array is not assigned or empty. No visuals can be shown.", this);
            return null;
        }

        _currentLevel = level;
        int levelIndex = level - 1; // Convert 1-indexed level to 0-indexed array access

        GameObject newlyActivatedVisual = null;

        for (int i = 0; i < visualsByLevel.Length; i++)
        {
            if (visualsByLevel[i] != null)
            {
                bool shouldBeActive = (i == levelIndex);
                if (visualsByLevel[i].activeSelf != shouldBeActive)
                {
                    visualsByLevel[i].SetActive(shouldBeActive);
                }

                if (shouldBeActive)
                {
                    newlyActivatedVisual = visualsByLevel[i];
                }
            }
        }

        if (newlyActivatedVisual == null && level > 0 && level <= visualsByLevel.Length)
        {
            // This might happen if the GameObject at visualsByLevel[levelIndex] is null
            if (visualsByLevel.Length > levelIndex && levelIndex >= 0 && visualsByLevel[levelIndex] == null)
            {
                Debug.LogWarning($"HandVisualManager on {gameObject.name}: Visual for level {level} (index {levelIndex}) is NULL in the 'Visuals By Level' array. Hand will be invisible.", this);
            }
        }
        else if (level > visualsByLevel.Length && visualsByLevel.Length > 0)
        {
            Debug.LogWarning($"HandVisualManager on {gameObject.name}: Requested level {level} is out of bounds. Max defined is {visualsByLevel.Length}. Activating highest available if possible.", this);
            // Fallback: activate the highest available level if requested level is too high
            if (visualsByLevel.Length > 0)
            {
                int highestIndex = visualsByLevel.Length - 1;
                if (visualsByLevel[highestIndex] != null)
                {
                    if (!visualsByLevel[highestIndex].activeSelf) visualsByLevel[highestIndex].SetActive(true);
                    newlyActivatedVisual = visualsByLevel[highestIndex];
                    _currentLevel = highestIndex + 1; // Update current level to what's actually shown
                    Debug.LogWarning($"HandVisualManager on {gameObject.name}: Activated level {GetCurrentLevel()} as fallback.", this);
                }
            }
        }
        else if (level <= 0) // Handle level 0 or less (e.g. "locked" state)
        {
            // All visuals were already deactivated by the loop. newlyActivatedVisual will be null.
            // Debug.Log($"HandVisualManager on {gameObject.name}: Requested level {level}. All visuals deactivated.", this);
        }


        _currentActiveVisual = newlyActivatedVisual;
        return _currentActiveVisual;
    }

    /// <summary>
    /// Gets the currently active hand visual GameObject.
    /// </summary>
    public GameObject GetCurrentActiveVisual()
    {
        if (_currentActiveVisual != null && !_currentActiveVisual.activeSelf)
        {
            if (_currentLevel > 0)
            {
                // Re-validate if the current visual became inactive externally
                return SetActiveLevelVisual(_currentLevel);
            }
            else
            {
                _currentActiveVisual = null;
            }
        }
        return _currentActiveVisual;
    }

    /// <summary>
    /// Gets the current level this visual manager is set to (1-indexed).
    /// Returns 0 if uninitialized or no visual is active for level 0.
    /// </summary>
    public int GetCurrentLevel()
    {
        return _currentLevel;
    }

    void Start()
    {
        if (visualsByLevel != null && visualsByLevel.Length > 0)
        {
            bool oneWasActiveInEditor = false;
            int activeIndexFromEditor = -1;

            for (int i = 0; i < visualsByLevel.Length; i++)
            {
                if (visualsByLevel[i] != null && visualsByLevel[i].activeSelf)
                {
                    if (!oneWasActiveInEditor)
                    {
                        oneWasActiveInEditor = true;
                        activeIndexFromEditor = i;
                    }
                    else
                    {
                        visualsByLevel[i].SetActive(false); // Ensure only one is active if multiple were set in editor
                    }
                }
            }

            if (_currentLevel == 0 && _currentActiveVisual == null) // If not yet initialized by Orchestrator
            {
                if (oneWasActiveInEditor)
                {
                    // If a visual was active in editor, assume it's the intended starting visual (likely level 1)
                    // SetActiveLevelVisual will ensure others are off and correctly set _currentLevel and _currentActiveVisual
                    SetActiveLevelVisual(activeIndexFromEditor + 1);
                }
                else
                {
                    // No visual was active in editor, and not yet initialized.
                    // Set to level 0 (or 1 if you always start at level 1 visually).
                    // Setting to 0 ensures all are off if level 0 means "no visual".
                    SetActiveLevelVisual(0);
                }
            }
        }
        else if (_currentLevel == 0 && _currentActiveVisual == null) // No visuals defined at all
        {
            SetActiveLevelVisual(0); // Will log warning and do nothing else
        }
    }
}