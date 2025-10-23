using System.Collections.Generic;
using UnityEngine;
using CompanionAI;

public class GameCompanionSpawner : MonoBehaviour
{
    [Header("Auto-spawn on Start")]
    public bool spawnOnStart = true;

    [Header("Player Reference (Optional)")]
    public Transform playerTransform;

    [Header("Companion Prefabs (Same order as menu)")]
    public GameObject[] companionPrefabs = new GameObject[4];

    [Header("Spawn Settings")]
    public float spawnRadius = 3f;
    public LayerMask groundLayerMask = 1;

    private void Start()
    {
        Debug.Log(" GameCompanionSpawner: Start() called");

        if (spawnOnStart)
        {
            Debug.Log(" GameCompanionSpawner: Auto-spawn enabled, scheduling spawn in 0.5 seconds");
            Invoke(nameof(SpawnSelectedCompanions), 0.5f);
        }
        else
        {
            Debug.Log("GameCompanionSpawner: Auto-spawn disabled");
        }
    }

    public void SpawnSelectedCompanions()
    {
        Debug.Log(" GAME SPAWN: Reading selection from PlayerPrefs");

        int selectedCount = PlayerPrefs.GetInt("SelectedCompanionCount", 0);
        Debug.Log($" GAME SPAWN: Found {selectedCount} selected companions");

        if (selectedCount == 0)
        {
            Debug.Log(" GAME SPAWN: No companions selected");
            return;
        }

        CompanionCore.ResetSessionCompanions();

        List<GameObject> spawnedCompanions = new List<GameObject>();
        Transform[] spawnPoints = FindSpawnPointsByName();

        for (int i = 0; i < selectedCount; i++)
        {
            int companionIndex = PlayerPrefs.GetInt($"SelectedCompanionIndex_{i}", -1);

            if (companionIndex >= 0 && companionIndex < companionPrefabs.Length && companionPrefabs[companionIndex] != null)
            {
                Vector3 spawnPosition = GetSpawnPosition(spawnPoints, i);
                GameObject spawned = Instantiate(companionPrefabs[companionIndex], spawnPosition, Quaternion.identity);
                spawnedCompanions.Add(spawned);

                Debug.Log($" GAME SPAWN: Spawned {spawned.name} at {spawnPosition} (cooldown already started in menu)");
            }
            else
            {
                Debug.LogError($" GAME SPAWN: Invalid companion index {companionIndex} or missing prefab");
            }
        }

        Debug.Log($" GAME SPAWN: Successfully spawned {spawnedCompanions.Count} companions!");
    }

    private Vector3 GetSpawnPosition(Transform[] spawnPoints, int index)
    {
        if (index < spawnPoints.Length && spawnPoints[index] != null)
        {
            return spawnPoints[index].position;
        }

        Vector3 playerPos = playerTransform != null ? playerTransform.position : FindPlayerPosition();
        float angle = (index * 90f) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle) * spawnRadius, 0f, Mathf.Sin(angle) * spawnRadius);
        Vector3 spawnPosition = playerPos + offset;

        if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayerMask))
        {
            spawnPosition = hit.point;
        }

        return spawnPosition;
    }

    private Vector3 FindPlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform.position : Vector3.zero;
    }

    private Transform[] FindSpawnPointsByName()
    {
        Transform[] spawnPoints = new Transform[4];
        string[] names = { "CompanionSpawnPoint1", "CompanionSpawnPoint2", "CompanionSpawnPoint3", "CompanionSpawnPoint4" };

        for (int i = 0; i < names.Length; i++)
        {
            GameObject obj = GameObject.Find(names[i]);
            if (obj != null)
            {
                spawnPoints[i] = obj.transform;
                Debug.Log($" GAME SPAWN: Found spawn point: {names[i]}");
            }
        }

        return spawnPoints;
    }

    [ContextMenu("Debug PlayerPrefs")]
    public void DebugPlayerPrefs()
    {
        Debug.Log(" GAME DEBUG: Checking PlayerPrefs data");

        int count = PlayerPrefs.GetInt("SelectedCompanionCount", -1);
        Debug.Log($" GAME DEBUG: Found {count} selected companions");

        if (count == -1)
        {
            Debug.LogWarning(" GAME DEBUG: No selection data found in PlayerPrefs! Make sure OnGameStart() was called in menu scene.");
        }
    }
}