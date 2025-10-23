using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Item Set", menuName = "Inventory/Item Set")]
public class ChestItemSet : ScriptableObject
{
    [Tooltip("All items that can appear in this set")]
    public ChestItemData[] items;
    
    // Get a random selection of items from this set
    public List<ChestItemData> GetRandomItems(int count)
    {
        List<ChestItemData> result = new List<ChestItemData>();
        
        if (items == null || items.Length == 0 || count <= 0)
        {
            Debug.LogWarning("ChestItemSet: Cannot get random items - set is empty or count is invalid");
            return result;
        }
        
        // If we want more items than are in the set, clamp the count
        count = Mathf.Min(count, items.Length);
        
        // Create a copy of the items array so we can modify it
        List<ChestItemData> availableItems = new List<ChestItemData>(items);
        
        // Select random items
        for (int i = 0; i < count; i++)
        {
            if (availableItems.Count == 0)
                break;
                
            int randomIndex = Random.Range(0, availableItems.Count);
            result.Add(availableItems[randomIndex]);
            availableItems.RemoveAt(randomIndex); // Remove the item so we don't pick it again
        }
        
        return result;
    }
    
    // Get a weighted random selection favoring higher rarity items
    public List<ChestItemData> GetWeightedRandomItems(int count)
    {
        List<ChestItemData> result = new List<ChestItemData>();
        
        if (items == null || items.Length == 0 || count <= 0)
        {
            Debug.LogWarning("ChestItemSet: Cannot get weighted random items - set is empty or count is invalid");
            return result;
        }
        
        // If we want more items than are in the set, clamp the count
        count = Mathf.Min(count, items.Length);
        
        // Create a copy of the items array
        List<ChestItemData> availableItems = new List<ChestItemData>(items);
        
        // Calculate total weight
        float[] weights = new float[availableItems.Count];
        float totalWeight = 0;
        
        for (int i = 0; i < availableItems.Count; i++)
        {
            weights[i] = availableItems[i].itemRarity; // Higher rarity = higher weight
            totalWeight += weights[i];
        }
        
        // Select weighted random items
        for (int i = 0; i < count; i++)
        {
            if (availableItems.Count == 0)
                break;
                
            // Pick a random value between 0 and the total weight
            float randomValue = Random.Range(0, totalWeight);
            float currentWeight = 0;
            
            for (int j = 0; j < availableItems.Count; j++)
            {
                currentWeight += weights[j];
                if (randomValue <= currentWeight)
                {
                    // We've found our item
                    result.Add(availableItems[j]);
                    
                    // Remove the item's weight from the total
                    totalWeight -= weights[j];
                    
                    // Remove the item from the available items
                    availableItems.RemoveAt(j);
                    
                    // Remove the weight from the weights array
                    System.Collections.Generic.List<float> weightsList = new List<float>(weights);
                    weightsList.RemoveAt(j);
                    weights = weightsList.ToArray();
                    
                    break;
                }
            }
        }
        
        return result;
    }
    
    // Get items of a specific rarity
    public List<ChestItemData> GetItemsByRarity(int rarity)
    {
        List<ChestItemData> result = new List<ChestItemData>();
        
        if (items == null || items.Length == 0)
        {
            Debug.LogWarning("ChestItemSet: Cannot get items by rarity - set is empty");
            return result;
        }
        
        foreach (ChestItemData item in items)
        {
            if (item.itemRarity == rarity)
            {
                result.Add(item);
            }
        }
        
        return result;
    }
}
