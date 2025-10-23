using UnityEngine;

/// <summary>
/// Central Tower - The invulnerable capture point that players must defend
/// This is purely aesthetic with a capture radius - cannot be destroyed
/// </summary>
public class CentralTower : MonoBehaviour
{
    [Header("Capture Radius")]
    [Tooltip("Radius around this tower where player must stay to capture the platform")]
    public float captureRadius = 1500f;
    
    [Header("Gem Spawning on Capture")]
    [Tooltip("Gem prefab to spawn when platform is captured")]
    public GameObject gemPrefab;
    
    [Tooltip("Number of gems to spawn in 360° around tower on capture")]
    public int gemsToSpawn = 12;
    
    [Tooltip("Radius at which gems spawn around the tower")]
    public float gemSpawnRadius = 500f;
    
    [Tooltip("Height above tower base to spawn gems")]
    public float gemSpawnHeight = 300f;
    
    [Header("Visual Feedback")]
    [Tooltip("Optional material to apply when capture is in progress")]
    public Material capturingMaterial;
    
    [Tooltip("Optional material to apply when platform is captured")]
    public Material capturedMaterial;
    
    private Material originalMaterial;
    private Renderer towerRenderer;
    
    void Awake()
    {
        // Make this tower invulnerable by removing/disabling any health components
        var health = GetComponent<TowerController>();
        if (health != null)
        {
            Destroy(health);
        }
        
        // Cache renderer for visual feedback
        towerRenderer = GetComponent<Renderer>();
        if (towerRenderer != null && towerRenderer.material != null)
        {
            originalMaterial = towerRenderer.material;
        }
    }
    
    /// <summary>
    /// Check if a position is within the capture radius
    /// </summary>
    public bool IsPositionInCaptureRadius(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        return distance <= captureRadius;
    }
    
    /// <summary>
    /// Visual feedback when capture starts
    /// </summary>
    public void OnCaptureStarted()
    {
        if (towerRenderer != null && capturingMaterial != null)
        {
            towerRenderer.material = capturingMaterial;
        }
    }
    
    /// <summary>
    /// Visual feedback when capture is cancelled
    /// </summary>
    public void OnCaptureCancelled()
    {
        if (towerRenderer != null && originalMaterial != null)
        {
            towerRenderer.material = originalMaterial;
        }
    }
    
    /// <summary>
    /// Spawn gems in 360° around the tower when platform is captured
    /// </summary>
    public void SpawnCaptureRewardGems()
    {
        if (gemPrefab == null)
        {
            Debug.LogWarning("[CentralTower] No gem prefab assigned! Cannot spawn reward gems.");
            return;
        }
        
        Debug.Log($"[CentralTower] Spawning {gemsToSpawn} reward gems in 360° radius!");
        
        float angleStep = 360f / gemsToSpawn;
        
        for (int i = 0; i < gemsToSpawn; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            
            // Calculate spawn position in circle around tower
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * gemSpawnRadius,
                gemSpawnHeight,
                Mathf.Sin(angle) * gemSpawnRadius
            );
            
            Vector3 spawnPosition = transform.position + offset;
            
            // Spawn gem
            GameObject gemObj = Instantiate(gemPrefab, spawnPosition, Quaternion.identity);
            
            // Make sure gem is collectible (not attached to tower)
            Gem gemScript = gemObj.GetComponent<Gem>();
            if (gemScript != null)
            {
                // Gems spawned this way should be immediately collectible
                gemScript.gameObject.SetActive(true);
            }
            
            // Add slight upward velocity for visual effect
            Rigidbody rb = gemObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 outwardDirection = (spawnPosition - transform.position).normalized;
                rb.linearVelocity = outwardDirection * 200f + Vector3.up * 300f;
            }
        }
    }
    
    /// <summary>
    /// Visual feedback when platform is captured
    /// </summary>
    public void OnPlatformCaptured()
    {
        if (towerRenderer != null && capturedMaterial != null)
        {
            towerRenderer.material = capturedMaterial;
        }
        
        // Spawn reward gems
        SpawnCaptureRewardGems();
    }
    
    // Draw capture radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, captureRadius);
        
        // Draw gem spawn radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, gemSpawnRadius);
    }
}
