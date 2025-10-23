using UnityEngine;

/// <summary>
/// VestItem - ScriptableObject for vest items that increase armor plate capacity
/// Similar to BackpackItem but for armor plates instead of inventory slots
/// </summary>
[CreateAssetMenu(fileName = "New Vest", menuName = "Inventory/Vest Item")]
public class VestItem : ChestItemData
{
    [Header("Vest Configuration")]
    [Tooltip("Vest tier (1 = basic, 2 = advanced, 3 = tactical)")]
    public int vestTier = 1;
    
    [Tooltip("Maximum number of armor plates this vest can hold")]
    public int maxPlates = 1;
    
    [Header("Vest Info")]
    [Tooltip("Display name for the vest")]
    public string vestDisplayName = "T1 Vest";
    
    [Tooltip("Description of the vest")]
    [TextArea(3, 5)]
    public string vestDescription = "Basic tactical vest. Holds 1 armor plate.";
    
    void OnValidate()
    {
        // Ensure item type is set correctly
        itemType = "Vest";
        
        // Clamp values
        vestTier = Mathf.Max(1, vestTier);
        maxPlates = Mathf.Max(1, maxPlates);
        
        // Auto-set display name if empty
        if (string.IsNullOrEmpty(vestDisplayName))
        {
            vestDisplayName = $"T{vestTier} Vest";
        }
    }
    
    /// <summary>
    /// Get formatted display name
    /// </summary>
    public string GetDisplayName()
    {
        return vestDisplayName;
    }
    
    /// <summary>
    /// Get formatted description with stats
    /// </summary>
    public string GetFullDescription()
    {
        return $"{vestDescription}\n\nCapacity: {maxPlates} plate{(maxPlates > 1 ? "s" : "")}";
    }
}
