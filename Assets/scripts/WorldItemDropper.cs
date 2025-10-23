using UnityEngine;
using GeminiGauntlet.Audio;

public class WorldItemDropper : MonoBehaviour
{
    [Header("Drop Settings")]
    [Tooltip("Prefab for dropped items in the world")]
    public GameObject droppedItemPrefab;
    [Tooltip("Force applied to dropped items")]
    public float dropForce = 5f;
    [Tooltip("Random spread for drop direction")]
    public float dropSpread = 1f;
    [Tooltip("Height offset for drop position")]
    public float dropHeight = 1f;
    
    [Header("Audio")]
    [Tooltip("Sound to play when item is dropped")]
    public string dropSound = "gemDetach";

    public void DropItem(ChestItemData itemData, int count, Vector3 position)
    {
        if (itemData == null || count <= 0) return;

        // Adjust drop position
        Vector3 dropPosition = position + Vector3.up * dropHeight;
        
        // Create dropped item
        GameObject droppedItem = CreateDroppedItem(itemData, count, dropPosition);
        
        if (droppedItem != null)
        {
            // Apply physics force
            ApplyDropForce(droppedItem);
            
            // Play drop sound
            PlayDropSound(dropPosition);
            
            Debug.Log($"Dropped {count}x {itemData.itemName} at {dropPosition}");
        }
    }

    private GameObject CreateDroppedItem(ChestItemData itemData, int count, Vector3 position)
    {
        GameObject droppedItem;

        if (droppedItemPrefab != null)
        {
            // Use custom prefab
            droppedItem = Instantiate(droppedItemPrefab, position, Quaternion.identity);
        }
        else
        {
            // Create basic dropped item
            droppedItem = CreateBasicDroppedItem(position);
        }

        // Configure the dropped item
        ConfigureDroppedItem(droppedItem, itemData, count);

        return droppedItem;
    }

    private GameObject CreateBasicDroppedItem(Vector3 position)
    {
        // Create basic cube as dropped item
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.transform.position = position;
        item.transform.localScale = Vector3.one * 0.5f;
        item.name = "DroppedItem";

        // Add Rigidbody for physics
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = item.AddComponent<Rigidbody>();
        }
        rb.mass = 0.1f;

        // Make it a trigger for collection
        Collider collider = item.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        return item;
    }

    private void ConfigureDroppedItem(GameObject droppedItem, ChestItemData itemData, int count)
    {
        // Add or get WorldItem component
        WorldItem worldItem = droppedItem.GetComponent<WorldItem>();
        if (worldItem == null)
        {
            worldItem = droppedItem.AddComponent<WorldItem>();
        }

        // Configure world item
        worldItem.SetItemData(itemData, count);

        // Update visual appearance
        UpdateDroppedItemVisual(droppedItem, itemData);
    }

    private void UpdateDroppedItemVisual(GameObject droppedItem, ChestItemData itemData)
    {
        // Try to use item icon as texture
        if (itemData.itemIcon != null)
        {
            Renderer renderer = droppedItem.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Create material with item icon
                Material itemMaterial = new Material(Shader.Find("Standard"));
                
                // Convert sprite to texture
                Texture2D texture = SpriteToTexture(itemData.itemIcon);
                if (texture != null)
                {
                    itemMaterial.mainTexture = texture;
                }
                
                // Apply rarity color tint
                itemMaterial.color = itemData.GetRarityColor();
                
                renderer.material = itemMaterial;
            }
        }

        // Scale based on rarity
        float scale = 0.5f + (itemData.itemRarity * 0.1f);
        droppedItem.transform.localScale = Vector3.one * scale;
    }

    private void ApplyDropForce(GameObject droppedItem)
    {
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Random direction with upward bias
        Vector3 randomDirection = new Vector3(
            Random.Range(-dropSpread, dropSpread),
            Random.Range(0.5f, 1.5f), // Upward bias
            Random.Range(-dropSpread, dropSpread)
        ).normalized;

        // Apply force
        rb.AddForce(randomDirection * dropForce, ForceMode.Impulse);

        // Add random torque for spinning
        Vector3 randomTorque = new Vector3(
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f)
        );
        rb.AddTorque(randomTorque, ForceMode.Impulse);
    }

    private void PlayDropSound(Vector3 position)
    {
        // Sound removed per user request
        Debug.Log("Item dropped");
    }

    private Texture2D SpriteToTexture(Sprite sprite)
    {
        if (sprite == null) return null;

        try
        {
            // Get the sprite's texture
            Texture2D originalTexture = sprite.texture;
            
            // Create new texture with sprite's rect
            Rect spriteRect = sprite.rect;
            Texture2D newTexture = new Texture2D((int)spriteRect.width, (int)spriteRect.height);
            
            // Copy pixels from original texture
            Color[] pixels = originalTexture.GetPixels(
                (int)spriteRect.x,
                (int)spriteRect.y,
                (int)spriteRect.width,
                (int)spriteRect.height
            );
            
            newTexture.SetPixels(pixels);
            newTexture.Apply();
            
            return newTexture;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not convert sprite to texture: {e.Message}");
            return null;
        }
    }

    // Utility method to drop item at player position
    public void DropItemAtPlayer(ChestItemData itemData, int count = 1)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 dropPosition = player.transform.position + player.transform.forward * 2f;
            DropItem(itemData, count, dropPosition);
        }
        else
        {
            Debug.LogWarning("Player not found for item drop!");
        }
    }

    // Method to drop multiple items at once
    public void DropItems(System.Collections.Generic.Dictionary<ChestItemData, int> items, Vector3 position)
    {
        foreach (var kvp in items)
        {
            // Spread out multiple drops
            Vector3 spreadPosition = position + new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)
            );
            
            DropItem(kvp.Key, kvp.Value, spreadPosition);
        }
    }
}
