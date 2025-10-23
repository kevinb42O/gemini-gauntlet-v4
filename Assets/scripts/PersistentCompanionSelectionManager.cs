using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;

public class PersistentCompanionSelectionManager : MonoBehaviour
{
    public static PersistentCompanionSelectionManager Instance { get; private set; }

    [Header("Selection Panel - Always Visible")]
    public Image[] companionSlotBackgrounds; // 4 background images for slots
    public Image[] companionSlotImages; // 4 images for actual companion sprites (clickable)
    public TextMeshProUGUI[] companionLevelTexts; // 4 text fields for companion levels

    [Header("Companion Prefabs for Game")]
    public GameObject[] companionPrefabs = new GameObject[4]; // Direct mapping: [0]=first companion, [1]=second, etc.

    [Header("Auto Spawn Settings")]
    public float spawnRadius = 3f; // Fallback: How far from player to spawn companions
    public LayerMask groundLayerMask = 1; // What counts as ground for spawning

    private List<CompanionData> selectedCompanions = new List<CompanionData>();
    private List<int> selectedCompanionIndices = new List<int>(); // Store original indices
    private const int MAX_COMPANIONS = 4;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DON'T use DontDestroyOnLoad - let it stay in menu scene
            Debug.Log("🏠 PersistentCompanionSelectionManager: Initialized in menu scene");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadCooldownData();
        InitializeSlots();
        StartCoroutine(UpdateCooldowns());
        
        // Refresh UI when returning to menu
        RefreshUIOnSceneLoad();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveCooldownData();
        }
        else
        {
            LoadCooldownData();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveCooldownData();
        }
        else
        {
            LoadCooldownData();
        }
    }

    void OnDestroy()
    {
        SaveCooldownData();
    }

    private void InitializeSlots()
    {
        // Set up click listeners on companion images for unequipping
        for (int i = 0; i < companionSlotImages.Length; i++)
        {
            int slotIndex = i; // Capture for closure
            
            // Add Button component to the image if it doesn't exist
            Button imageButton = companionSlotImages[i].GetComponent<Button>();
            if (imageButton == null)
            {
                imageButton = companionSlotImages[i].gameObject.AddComponent<Button>();
            }
            
            // Set up click listener
            imageButton.onClick.RemoveAllListeners();
            imageButton.onClick.AddListener(() => UnequipCompanion(slotIndex));
        }

        UpdateSlotVisuals();
    }

    public bool EquipCompanion(CompanionData companion)
    {
        Debug.Log($"🎯 EQUIP DEBUG: Attempting to equip companion: {companion?.companionName ?? "NULL"}");
        
        if (companion == null) 
        {
            Debug.LogError("🎯 EQUIP DEBUG: Companion is NULL!");
            return false;
        }
        
        // Check if companion is on cooldown
        if (companion.isOnCooldown)
        {
            Debug.Log($"🎯 EQUIP DEBUG: Companion {companion.companionName} is on cooldown and cannot be equipped!");
            return false;
        }
        
        // Check if already equipped
        if (selectedCompanions.Contains(companion))
        {
            Debug.Log($"🎯 EQUIP DEBUG: Companion {companion.companionName} is already equipped!");
            return false;
        }

        // Check if we have space
        if (selectedCompanions.Count >= MAX_COMPANIONS)
        {
            Debug.Log($"🎯 EQUIP DEBUG: Maximum companions already equipped! Current count: {selectedCompanions.Count}");
            return false;
        }

        // Find the companion's original index in CompanionSelectionManager
        CompanionSelectionManager csm = FindObjectOfType<CompanionSelectionManager>();
        int originalIndex = -1;
        if (csm != null)
        {
            for (int i = 0; i < csm.companions.Length; i++)
            {
                if (csm.companions[i] == companion)
                {
                    originalIndex = i;
                    break;
                }
            }
        }

        selectedCompanions.Add(companion);
        selectedCompanionIndices.Add(originalIndex); // Store the original index
        Debug.Log($"🎯 EQUIP DEBUG: Successfully added {companion.companionName} (index {originalIndex}) to selectedCompanions. New count: {selectedCompanions.Count}");
        
        UpdateSlotVisuals();
        Debug.Log($"✅ EQUIP SUCCESS: Equipped companion: {companion.companionName} at index {originalIndex}");
        return true;
    }

    public void UnequipCompanion(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= selectedCompanions.Count) return;
        CompanionData companion = selectedCompanions[slotIndex];
        selectedCompanions.RemoveAt(slotIndex);
        selectedCompanionIndices.RemoveAt(slotIndex); // Also remove the stored index
        UpdateSlotVisuals();
        Debug.Log($"Unequipped companion: {companion.companionName}");
    }

    private void UpdateSlotVisuals()
    {
        for (int i = 0; i < MAX_COMPANIONS; i++)
        {
            if (i < selectedCompanions.Count && selectedCompanions[i] != null)
            {
                // Show companion
                if (companionSlotImages[i] != null)
                {
                    companionSlotImages[i].sprite = selectedCompanions[i].companionImage;
                    companionSlotImages[i].color = Color.white; // Full opacity
                    
                    // Enable button interaction only if not on cooldown
                    Button imageButton = companionSlotImages[i].GetComponent<Button>();
                    if (imageButton != null)
                    {
                        imageButton.interactable = !selectedCompanions[i].isOnCooldown;
                    }
                }

                // Show companion level
                if (companionLevelTexts[i] != null)
                {
                    selectedCompanions[i].EnsureProgressionInitialized();
                    companionLevelTexts[i].text = $"Lvl {selectedCompanions[i].companionLevel}";
                    companionLevelTexts[i].color = selectedCompanions[i].isOnCooldown ? Color.red : Color.white;
                    Debug.Log($"[PersistentCompanionSelectionManager] Slot {i} - {selectedCompanions[i].companionName} Level {selectedCompanions[i].companionLevel}");
                }
            }
            else
            {
                // Show empty slot
                if (companionSlotImages[i] != null)
                {
                    companionSlotImages[i].sprite = null;
                    companionSlotImages[i].color = Color.clear; // Transparent
                    
                    // Disable button interaction for empty slots
                    Button imageButton = companionSlotImages[i].GetComponent<Button>();
                    if (imageButton != null)
                    {
                        imageButton.interactable = false;
                    }
                }

                // Hide level text
                if (companionLevelTexts[i] != null)
                {
                    companionLevelTexts[i].text = "";
                }
            }
        }
    }

    public List<CompanionData> GetSelectedCompanions()
    {
        return new List<CompanionData>(selectedCompanions);
    }

    public bool IsCompanionEquipped(CompanionData companion)
    {
        return selectedCompanions.Contains(companion);
    }

    public int GetEquippedCount()
    {
        return selectedCompanions.Count;
    }

    // Called when player starts a game - save selection, start cooldowns, and clear UI
    public void OnGameStart()
    {
        Debug.Log($"🎮 OnGameStart called with {selectedCompanions.Count} selected companions");
        
        // Start cooldowns for all selected companions IMMEDIATELY
        StartCooldownsForSelectedCompanions();
        
        // Save selection data for game scene
        SaveSelectionForGame();
        
        // Clear UI (companions are now on cooldown)
        ClearSelectionAfterGame();
        
        Debug.Log("🎮 Game started - cooldowns started, selection saved, and UI cleared");
    }

    // Start cooldowns for all currently selected companions
    private void StartCooldownsForSelectedCompanions()
    {
        Debug.Log($"⏰ COOLDOWN START: Starting cooldowns for {selectedCompanions.Count} selected companions");
        
        for (int i = 0; i < selectedCompanions.Count; i++)
        {
            if (selectedCompanions[i] != null)
            {
                StartCooldownForCompanion(selectedCompanions[i]);
                Debug.Log($"⏰ Started cooldown for {selectedCompanions[i].companionName}");
            }
        }
        
        // Save cooldown data immediately
        SaveCooldownData();
        Debug.Log("⏰ All cooldowns started and saved!");
    }

    // Find spawn points by their GameObject names
    private Transform[] FindSpawnPointsByName()
    {
        Transform[] spawnPoints = new Transform[4];
        
        // Look for GameObjects with specific names
        string[] spawnPointNames = { "CompanionSpawnPoint1", "CompanionSpawnPoint2", "CompanionSpawnPoint3", "CompanionSpawnPoint4" };
        
        Debug.Log("🎯 SPAWN POINTS DEBUG: Searching for spawn points...");
        
        for (int i = 0; i < spawnPointNames.Length; i++)
        {
            GameObject spawnPointObj = GameObject.Find(spawnPointNames[i]);
            if (spawnPointObj != null)
            {
                spawnPoints[i] = spawnPointObj.transform;
                Debug.Log($"✅ Found spawn point: {spawnPointNames[i]} at position {spawnPointObj.transform.position}");
            }
            else
            {
                Debug.LogWarning($"❌ Spawn point not found: {spawnPointNames[i]}");
            }
        }
        
        return spawnPoints;
    }

    // Fallback: Get spawn position around player in a circle formation
    private Vector3 GetFallbackSpawnPosition(Transform playerTransform, int companionIndex)
    {
        Vector3 playerPosition = Vector3.zero;
        
        Debug.Log($"🎯 FALLBACK DEBUG: Getting fallback position for companion {companionIndex}");
        
        // Find player if not provided
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerPosition = player.transform.position;
                Debug.Log($"🎯 FALLBACK DEBUG: Found player by tag at {playerPosition}");
            }
            else
            {
                Debug.LogWarning("🎯 FALLBACK DEBUG: No player found! Using world origin.");
                playerPosition = Vector3.zero;
            }
        }
        else
        {
            playerPosition = playerTransform.position;
            Debug.Log($"🎯 FALLBACK DEBUG: Using provided player transform at {playerPosition}");
        }
        
        // Calculate angle for this companion (spread them around the player)
        float angle = (companionIndex * 90f) * Mathf.Deg2Rad; // 90 degrees apart (0°, 90°, 180°, 270°)
        
        // Calculate position around player
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * spawnRadius,
            0f,
            Mathf.Sin(angle) * spawnRadius
        );
        
        Vector3 spawnPosition = playerPosition + offset;
        Debug.Log($"🎯 FALLBACK DEBUG: Calculated spawn position: {spawnPosition} (player: {playerPosition} + offset: {offset})");
        
        // Try to place on ground using raycast
        RaycastHit hit;
        if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayerMask))
        {
            spawnPosition = hit.point;
            Debug.Log($"🎯 FALLBACK DEBUG: Raycast hit ground at {spawnPosition}");
        }
        else
        {
            Debug.Log($"🎯 FALLBACK DEBUG: No ground found, using calculated position {spawnPosition}");
        }
        
        return spawnPosition;
    }

    // Start cooldown for a specific companion when they enter the game
    private void StartCooldownForCompanion(CompanionData companion)
    {
        // Set cooldown based on companion type
        float cooldownMinutes = GetCooldownMinutesForCompanion(companion);
        companion.currentCooldownTime = cooldownMinutes * 60f; // Convert to seconds
        companion.isOnCooldown = true;
        
        Debug.Log($"Started {cooldownMinutes} minute cooldown for {companion.companionName}");
    }

    // Get cooldown duration based on companion type
    private float GetCooldownMinutesForCompanion(CompanionData companion)
    {
        switch (companion.companionName.ToLower())
        {
            case "medic":
            case "medic companion":
                return 30f; // 30 minutes
            case "loyal":
            case "loyal companion":
                return 60f; // 1 hour
            case "tank":
            case "tank companion":
                return 120f; // 2 hours
            case "aggressive":
            case "aggressive companion":
                return 180f; // 3 hours
            default:
                Debug.LogWarning($"Unknown companion type: {companion.companionName}, using default 60 minutes");
                return 60f; // Default 1 hour
        }
    }

    // Cooldown update coroutine
    private System.Collections.IEnumerator UpdateCooldowns()
    {
        while (true)
        {
            bool visualsNeedUpdate = false;

            foreach (CompanionData companion in selectedCompanions)
            {
                if (companion != null && companion.isOnCooldown)
                {
                    companion.currentCooldownTime -= Time.deltaTime;

                    if (companion.currentCooldownTime <= 0f)
                    {
                        companion.currentCooldownTime = 0f;
                        companion.isOnCooldown = false;
                        visualsNeedUpdate = true;
                        Debug.Log($"Cooldown finished for {companion.companionName}!");
                    }
                }
            }

            if (visualsNeedUpdate)
            {
                UpdateSlotVisuals();
            }

            // Save cooldown data periodically (every 10 seconds)
            if (Time.time % 10f < Time.deltaTime)
            {
                SaveCooldownData();
            }

            yield return null; // Wait for next frame
        }
    }

    // Save cooldown data to PlayerPrefs with real-world timestamps
    private void SaveCooldownData()
    {
        string currentTimeString = DateTime.Now.ToBinary().ToString();
        PlayerPrefs.SetString("CooldownSaveTime", currentTimeString);

        // Save all companions' cooldown data (from CompanionSelectionManager)
        if (FindObjectOfType<CompanionSelectionManager>() != null)
        {
            CompanionSelectionManager csm = FindObjectOfType<CompanionSelectionManager>();
            for (int i = 0; i < csm.companions.Length; i++)
            {
                if (csm.companions[i] != null)
                {
                    string companionKey = $"Companion_{csm.companions[i].companionName}";
                    PlayerPrefs.SetFloat($"{companionKey}_CooldownTime", csm.companions[i].currentCooldownTime);
                    PlayerPrefs.SetInt($"{companionKey}_IsOnCooldown", csm.companions[i].isOnCooldown ? 1 : 0);
                }
            }
        }

        PlayerPrefs.Save();
        Debug.Log("💾 Cooldown data saved to PlayerPrefs!");
    }

    // Load cooldown data and calculate elapsed real-world time
    private void LoadCooldownData()
    {
        if (!PlayerPrefs.HasKey("CooldownSaveTime"))
        {
            Debug.Log("No cooldown save data found - first time running or data cleared");
            return;
        }

        // Calculate elapsed real-world time since last save
        string lastSaveTimeString = PlayerPrefs.GetString("CooldownSaveTime");
        DateTime lastSaveTime = DateTime.FromBinary(Convert.ToInt64(lastSaveTimeString));
        DateTime currentTime = DateTime.Now;
        float elapsedRealTimeSeconds = (float)(currentTime - lastSaveTime).TotalSeconds;

        Debug.Log($"Game was closed for {elapsedRealTimeSeconds / 60f:F1} minutes");

        // Load and update all companions' cooldowns (from CompanionSelectionManager)
        if (FindObjectOfType<CompanionSelectionManager>() != null)
        {
            CompanionSelectionManager csm = FindObjectOfType<CompanionSelectionManager>();
            for (int i = 0; i < csm.companions.Length; i++)
            {
                if (csm.companions[i] != null)
                {
                    string companionKey = $"Companion_{csm.companions[i].companionName}";
                    
                    if (PlayerPrefs.HasKey($"{companionKey}_CooldownTime"))
                    {
                        float savedCooldownTime = PlayerPrefs.GetFloat($"{companionKey}_CooldownTime");
                        bool wasOnCooldown = PlayerPrefs.GetInt($"{companionKey}_IsOnCooldown") == 1;

                        if (wasOnCooldown)
                        {
                            // Subtract elapsed real-world time from cooldown
                            float newCooldownTime = savedCooldownTime - elapsedRealTimeSeconds;
                            
                            if (newCooldownTime <= 0f)
                            {
                                // Cooldown finished while game was closed
                                csm.companions[i].currentCooldownTime = 0f;
                                csm.companions[i].isOnCooldown = false;
                                Debug.Log($"{csm.companions[i].companionName} cooldown finished while game was closed!");
                            }
                            else
                            {
                                // Cooldown still active, update remaining time
                                csm.companions[i].currentCooldownTime = newCooldownTime;
                                csm.companions[i].isOnCooldown = true;
                                Debug.Log($"{csm.companions[i].companionName} has {newCooldownTime / 60f:F1} minutes remaining");
                            }
                        }
                    }
                }
            }
        }

        UpdateSlotVisuals();
    }

    // Save selection data when going to game scene
    public void SaveSelectionForGame()
    {
        Debug.Log($"💾 SAVE DEBUG: Saving {selectedCompanions.Count} selected companions for game");
        
        // Save count
        PlayerPrefs.SetInt("SelectedCompanionCount", selectedCompanions.Count);
        
        // Save each selected companion's index
        for (int i = 0; i < selectedCompanions.Count; i++)
        {
            if (i < selectedCompanionIndices.Count)
            {
                PlayerPrefs.SetInt($"SelectedCompanionIndex_{i}", selectedCompanionIndices[i]);
                Debug.Log($"💾 SAVE DEBUG: Saved companion {i} with index {selectedCompanionIndices[i]}");
            }
        }
        
        PlayerPrefs.Save();
        Debug.Log("💾 SAVE DEBUG: Selection data saved to PlayerPrefs");
    }

    // Clear selected companions (called when returning from game)
    public void ClearSelectionAfterGame()
    {
        Debug.Log($"🔄 CLEAR DEBUG: Clearing {selectedCompanions.Count} selected companions after game");
        selectedCompanions.Clear();
        selectedCompanionIndices.Clear();
        UpdateSlotVisuals();
        Debug.Log("🔄 CLEAR DEBUG: Selected companions cleared - they are now on cooldown");
    }

    // Refresh UI when returning to menu scene
    private void RefreshUIOnSceneLoad()
    {
        // Small delay to ensure UI elements are initialized
        Invoke("UpdateSlotVisuals", 0.1f);
        
        // Also refresh the CompanionSelectionManager if it exists
        CompanionSelectionManager csm = FindObjectOfType<CompanionSelectionManager>();
        if (csm != null)
        {
            // Force refresh of the equip button states
            Invoke("RefreshCompanionSelectionManager", 0.2f);
        }
    }

    private void RefreshCompanionSelectionManager()
    {
        CompanionSelectionManager csm = FindObjectOfType<CompanionSelectionManager>();
        if (csm != null)
        {
            // If there's a currently selected companion, refresh its button state
            CompanionData currentCompanion = csm.GetSelectedCompanion();
            if (currentCompanion != null)
            {
                // Trigger a refresh by calling the display method
                csm.SelectCompanion(System.Array.IndexOf(csm.companions, currentCompanion));
            }
        }
    }

    // Clear all cooldown save data (useful for testing or reset)
    [ContextMenu("Clear Cooldown Save Data")]
    public void ClearCooldownSaveData()
    {
        PlayerPrefs.DeleteKey("CooldownSaveTime");
        
        PlayerPrefs.Save();
        Debug.Log("All cooldown save data cleared!");
    }

    [ContextMenu("Test Save Selection")]
    public void TestSaveSelection()
    {
        Debug.Log("🧪 TEST: Manual save selection test");
        OnGameStart();
    }

    [ContextMenu("Check Selection Data")]
    public void CheckSelectionData()
    {
        Debug.Log("🔍 SELECTION DEBUG:");
        Debug.Log($"   Selected companions count: {selectedCompanions.Count}");
        Debug.Log($"   Selected indices count: {selectedCompanionIndices.Count}");
        
        for (int i = 0; i < selectedCompanions.Count; i++)
        {
            if (selectedCompanions[i] != null)
            {
                int index = i < selectedCompanionIndices.Count ? selectedCompanionIndices[i] : -1;
                Debug.Log($"   Slot {i}: {selectedCompanions[i].companionName} (index: {index})");
            }
        }
        
        // Check PlayerPrefs
        int savedCount = PlayerPrefs.GetInt("SelectedCompanionCount", -1);
        Debug.Log($"   PlayerPrefs saved count: {savedCount}");
        
        for (int i = 0; i < 4; i++)
        {
            int savedIndex = PlayerPrefs.GetInt($"SelectedCompanionIndex_{i}", -999);
            if (savedIndex != -999)
            {
                Debug.Log($"   PlayerPrefs slot {i}: index {savedIndex}");
            }
        }
    }

    private GameObject GetPrefabForCompanion(CompanionData companion, int companionIndex)
    {
        if (companion == null)
        {
            Debug.LogError("🚀 PREFAB DEBUG: Companion is NULL!");
            return null;
        }

        Debug.Log($"🚀 PREFAB DEBUG: Getting prefab for companion index {companionIndex}: '{companion.companionName}'");

        // Use direct index mapping - much simpler and more reliable
        if (companionIndex >= 0 && companionIndex < companionPrefabs.Length && companionPrefabs[companionIndex] != null)
        {
            Debug.Log($"🚀 PREFAB DEBUG: Found prefab at index {companionIndex}: {companionPrefabs[companionIndex].name}");
            return companionPrefabs[companionIndex];
        }

        Debug.LogError($"❌ PREFAB DEBUG: No prefab assigned at index {companionIndex} for companion '{companion.companionName}'");
        return null;
    }
}