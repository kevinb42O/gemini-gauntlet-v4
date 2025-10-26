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
        Debug.Log($"[WorldItemDropper] 🎯 DropItem() called - Item: {itemData?.itemName}, Count: {count}, Position: {position}");
        
        if (itemData == null)
        {
            Debug.LogError("[WorldItemDropper] ❌ itemData is NULL! Cannot drop item.");
            return;
        }
        
        if (count <= 0)
        {
            Debug.LogError($"[WorldItemDropper] ❌ Invalid count ({count})! Cannot drop item.");
            return;
        }

        // Adjust drop position
        Vector3 dropPosition = position + Vector3.up * dropHeight;
        Debug.Log($"[WorldItemDropper] 📍 Adjusted drop position: {dropPosition} (added {dropHeight} height)");
        
        // Create dropped item
        Debug.Log($"[WorldItemDropper] 🏗️ Creating dropped item...");
        GameObject droppedItem = CreateDroppedItem(itemData, count, dropPosition);
        
        if (droppedItem != null)
        {
            Debug.Log($"[WorldItemDropper] ✅ Dropped item created: {droppedItem.name}");
            
            // Apply physics force
            ApplyDropForce(droppedItem);
            
            // Play drop sound
            PlayDropSound(dropPosition);
            
            Debug.Log($"[WorldItemDropper] ✅✅✅ SUCCESS! Dropped {count}x {itemData.itemName} at {dropPosition}");
        }
        else
        {
            Debug.LogError($"[WorldItemDropper] ❌❌❌ CreateDroppedItem returned NULL!");
        }
    }

    private GameObject CreateDroppedItem(ChestItemData itemData, int count, Vector3 position)
    {
        Debug.Log($"[WorldItemDropper] 🏗️ CreateDroppedItem() - Type: {itemData.GetType().Name}");
        
        GameObject droppedItem;
        
        // Special handling for equippable weapons
        if (itemData is EquippableWeaponItemData weaponData)
        {
            Debug.Log($"[WorldItemDropper] ⚔️ Detected EquippableWeaponItemData: {weaponData.itemName}");
            Debug.Log($"[WorldItemDropper] Weapon Type ID: {weaponData.weaponTypeID}");
            Debug.Log($"[WorldItemDropper] World Item Model: {(weaponData.worldItemModel != null ? weaponData.worldItemModel.name : "NULL")}");
            
            if (weaponData.worldItemModel != null)
            {
                // Use weapon-specific model
                Debug.Log($"[WorldItemDropper] 📦 Instantiating worldItemModel: {weaponData.worldItemModel.name}");
                droppedItem = Instantiate(weaponData.worldItemModel, position, Quaternion.identity);
                Debug.Log($"[WorldItemDropper] ✅ Instantiated: {droppedItem.name}");
                
                // ⚠️ CRITICAL FIX: Ensure spawned item is ACTIVE (visible)
                if (!droppedItem.activeSelf)
                {
                    Debug.Log($"[WorldItemDropper] ⚡ Item was inactive - ACTIVATING NOW!");
                    droppedItem.SetActive(true);
                }
                
                // ⚠️ ADDITIONAL FIX: Ensure all child renderers are enabled
                Renderer[] renderers = droppedItem.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    if (!renderer.enabled)
                    {
                        Debug.Log($"[WorldItemDropper] ⚡ Enabling disabled renderer: {renderer.gameObject.name}");
                        renderer.enabled = true;
                    }
                }
                Debug.Log($"[WorldItemDropper] ✅ Verified {renderers.Length} renderers are enabled");
                
                // Add WorldSwordPickup component if it's a sword
                if (weaponData.weaponTypeID == "sword")
                {
                    Debug.Log($"[WorldItemDropper] ⚔️ Weapon is a sword - adding WorldSwordPickup component");
                    WorldSwordPickup pickup = droppedItem.GetComponent<WorldSwordPickup>();
                    if (pickup == null)
                    {
                        Debug.Log($"[WorldItemDropper] Adding new WorldSwordPickup component...");
                        pickup = droppedItem.AddComponent<WorldSwordPickup>();
                    }
                    else
                    {
                        Debug.Log($"[WorldItemDropper] WorldSwordPickup already exists on prefab");
                    }
                    pickup.swordItemData = weaponData;
                    Debug.Log($"[WorldItemDropper] ✅ WorldSwordPickup configured with swordItemData: {weaponData.itemName}");
                }
                
                Debug.Log($"[WorldItemDropper] ✅ Dropped weapon: {weaponData.itemName} at {position}");
                return droppedItem;
            }
            else
            {
                Debug.LogError($"[WorldItemDropper] ❌ EquippableWeaponItemData has NULL worldItemModel! Cannot spawn weapon.");
            }
        }

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

        // ⚠️ CRITICAL FIX: Ensure spawned item is ACTIVE (visible)
        if (!droppedItem.activeSelf)
        {
            Debug.Log($"[WorldItemDropper] ⚡ Generic item was inactive - ACTIVATING NOW!");
            droppedItem.SetActive(true);
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
