using UnityEngine;
using GeminiGauntlet.Audio;

public class WorldItem : MonoBehaviour
{
    [Header("Item Data")]
    public ChestItemData itemData;
    public int itemCount = 1;
    
    [Header("Collection Settings")]
    [Tooltip("Distance at which player can collect this item")]
    public float collectionDistance = 2f;
    [Tooltip("Should this item be collected automatically when player gets close?")]
    public bool autoCollect = true;
    [Tooltip("Time before item can be collected (prevents immediate pickup after drop)")]
    public float collectionCooldown = 0.5f;
    
    [Header("Visual Effects")]
    [Tooltip("Should the item bob up and down?")]
    public bool enableBobbing = true;
    [Tooltip("Bobbing speed")]
    public float bobbingSpeed = 2f;
    [Tooltip("Bobbing height")]
    public float bobbingHeight = 0.2f;
    [Tooltip("Should the item rotate?")]
    public bool enableRotation = true;
    [Tooltip("Rotation speed")]
    public float rotationSpeed = 45f;
    
    [Header("Audio")]
    [Tooltip("Sound to play when collected")]
    public string collectionSound = "gemCollection";

    // State
    private float spawnTime;
    private Vector3 originalPosition;
    private bool canBeCollected = false;
    private GameObject player;
    private InventoryManager inventoryManager;

    private void Start()
    {
        spawnTime = Time.time;
        originalPosition = transform.position;
        
        // Find player and inventory manager
        player = GameObject.FindGameObjectWithTag("Player");
        inventoryManager = InventoryManager.Instance;
        
        // Start collection cooldown
        Invoke(nameof(EnableCollection), collectionCooldown);
        
        // Ensure we have a collider for collection
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = collectionDistance;
        }
    }

    private void Update()
    {
        // Visual effects
        if (enableBobbing)
        {
            ApplyBobbing();
        }
        
        if (enableRotation)
        {
            ApplyRotation();
        }
        
        // Auto collection check
        if (canBeCollected && autoCollect && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= collectionDistance)
            {
                TryCollectItem();
            }
        }
    }

    public void SetItemData(ChestItemData data, int count = 1)
    {
        itemData = data;
        itemCount = count;
        
        // Update name for easier identification
        if (itemData != null)
        {
            gameObject.name = $"WorldItem_{itemData.itemName}_{count}";
        }
    }

    private void ApplyBobbing()
    {
        float bobOffset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
        transform.position = originalPosition + Vector3.up * bobOffset;
    }

    private void ApplyRotation()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private void EnableCollection()
    {
        canBeCollected = true;
    }

    public bool TryCollectItem()
    {
        if (!canBeCollected || itemData == null || inventoryManager == null)
        {
            return false;
        }

        // Try to add to inventory
        if (inventoryManager.TryAddItem(itemData, itemCount))
        {
            // Play collection sound
            PlayCollectionSound();
            
            // Destroy this world item
            Destroy(gameObject);
            
            Debug.Log($"Collected {itemCount}x {itemData.itemName}");
            return true;
        }
        else
        {
            Debug.Log($"Cannot collect {itemData.itemName} - inventory full!");
            return false;
        }
    }

    private void PlayCollectionSound()
    {
        // Sound removed per user request
        Debug.Log("Item collected");
    }

    // Trigger-based collection
    private void OnTriggerEnter(Collider other)
    {
        if (!canBeCollected) return;
        
        if (other.CompareTag("Player"))
        {
            TryCollectItem();
        }
    }

    // Manual collection (for E key interaction)
    public void OnInteract()
    {
        TryCollectItem();
    }

    // Get display name for UI
    public string GetDisplayName()
    {
        if (itemData == null) return "Unknown Item";
        
        if (itemCount > 1)
        {
            return $"{itemData.itemName} x{itemCount}";
        }
        else
        {
            return itemData.itemName;
        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collectionDistance);
    }
}
