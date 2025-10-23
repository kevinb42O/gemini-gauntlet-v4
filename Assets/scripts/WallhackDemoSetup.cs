// ============================================================================
// WALLHACK DEMO SCENE SETUP
// Automatically creates a test scene with enemies to demo the wallhack
// Attach to any GameObject and press SPACE in Play mode to spawn test enemies
// ============================================================================

using UnityEngine;
using System.Collections.Generic;

public class WallhackDemoSetup : MonoBehaviour
{
    [Header("=== DEMO CONTROLS ===")]
    [Tooltip("Press this key to spawn demo enemies")]
    public KeyCode spawnEnemiesKey = KeyCode.Space;
    
    [Tooltip("Press this key to clear demo enemies")]
    public KeyCode clearEnemiesKey = KeyCode.Backspace;
    
    [Header("=== DEMO SETTINGS ===")]
    public int numberOfEnemies = 10;
    public float spawnRadius = 50f;
    public GameObject enemyPrefab;
    public bool createWallsBetween = true;
    
    [Header("=== AUTO-SETUP ===")]
    public bool autoSpawnOnStart = false;
    
    private List<GameObject> spawnedObjects = new List<GameObject>();
    
    void Start()
    {
        if (autoSpawnOnStart)
        {
            SpawnDemoEnemies();
        }
        
        Debug.Log("[WallhackDemo] Press SPACE to spawn test enemies!");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(spawnEnemiesKey))
        {
            SpawnDemoEnemies();
        }
        
        if (Input.GetKeyDown(clearEnemiesKey))
        {
            ClearDemoEnemies();
        }
    }
    
    void SpawnDemoEnemies()
    {
        ClearDemoEnemies(); // Clear old ones first
        
        Vector3 playerPos = transform.position;
        
        for (int i = 0; i < numberOfEnemies; i++)
        {
            // Random position around player
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = playerPos + new Vector3(randomCircle.x, Random.Range(-5f, 5f), randomCircle.y);
            
            // Create enemy (or cube if no prefab)
            GameObject enemy;
            if (enemyPrefab != null)
            {
                enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                enemy = CreateDummyEnemy(spawnPos);
            }
            
            // Tag it
            enemy.tag = "Enemy";
            spawnedObjects.Add(enemy);
            
            // Create wall between some enemies
            if (createWallsBetween && i % 3 == 0)
            {
                GameObject wall = CreateWall(playerPos, spawnPos);
                spawnedObjects.Add(wall);
            }
        }
        
        // Force wallhack rescan
        if (AAAWallhackSystem.Instance != null)
        {
            AAAWallhackSystem.Instance.ForceRescan();
        }
        
        Debug.Log($"[WallhackDemo] Spawned {numberOfEnemies} test enemies!");
    }
    
    void ClearDemoEnemies()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        
        Debug.Log("[WallhackDemo] Cleared demo enemies!");
    }
    
    GameObject CreateDummyEnemy(Vector3 position)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = "DemoEnemy";
        enemy.transform.position = position;
        enemy.transform.localScale = Vector3.one * 2f;
        
        // Random color
        Renderer renderer = enemy.GetComponent<Renderer>();
        renderer.material.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
        
        // Add a simple health component
        DemoHealth health = enemy.AddComponent<DemoHealth>();
        health.maxHealth = 100f;
        health.currentHealth = Random.Range(20f, 100f);
        
        return enemy;
    }
    
    GameObject CreateWall(Vector3 from, Vector3 to)
    {
        Vector3 midPoint = (from + to) / 2f;
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "DemoWall";
        wall.transform.position = midPoint;
        wall.transform.localScale = new Vector3(10f, 20f, 1f);
        wall.transform.LookAt(to);
        
        // Make it look like a wall
        Renderer renderer = wall.GetComponent<Renderer>();
        renderer.material.color = new Color(0.3f, 0.3f, 0.3f);
        
        return wall;
    }
    
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.cyan;
        style.fontStyle = FontStyle.Bold;
        
        int y = Screen.height - 100;
        
        GUI.Label(new Rect(10, y, 400, 25), "🎮 WALLHACK DEMO", style);
        y += 25;
        
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(10, y, 400, 20), $"[{spawnEnemiesKey}] Spawn {numberOfEnemies} Test Enemies", style);
        y += 20;
        GUI.Label(new Rect(10, y, 400, 20), $"[{clearEnemiesKey}] Clear Test Enemies", style);
        y += 20;
        GUI.Label(new Rect(10, y, 400, 20), "[F10] Toggle Wallhack", style);
    }
}

// ============================================================================
// DEMO HEALTH COMPONENT
// Simple health component for testing health-based coloring
// ============================================================================
public class DemoHealth : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        AAACheatSystemIntegration.NotifyEnemyKilled(gameObject);
        Destroy(gameObject);
    }
}
