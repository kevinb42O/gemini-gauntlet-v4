using UnityEngine;

/// <summary>
/// ElevatorKeycardData.cs - Special keycard for opening elevator doors
/// This is a ONE-TIME USE item that is NOT persistent across scenes
/// Very rare item that only spawns in chests
/// </summary>
[CreateAssetMenu(fileName = "ElevatorKeycard", menuName = "Inventory/Elevator Keycard")]
public class ElevatorKeycardData : ChestItemData
{
    [Header("Elevator Keycard Properties")]
    [Tooltip("This keycard is consumed when used (one-time use)")]
    public bool isOneTimeUse = true;
    
    [Tooltip("Visual indicator color for elevator keycards")]
    public Color elevatorKeycardColor = new Color(1f, 0.84f, 0f); // Gold color
    
    private void OnEnable()
    {
        // Set default properties for elevator keycards
        itemType = "ElevatorKeycard";
        itemRarity = 5; // Legendary rarity (very rare)
        
        // Set gold color for visual distinction
        rarityColor = elevatorKeycardColor;
        
        // Generate ID if not set
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = "Elevator Keycard";
        }
        
        GenerateID();
    }
    
    /// <summary>
    /// Elevator keycards should NOT stack (each is unique)
    /// </summary>
    public override bool IsSameItem(ChestItemData other)
    {
        // Elevator keycards are unique and don't stack
        if (other is ElevatorKeycardData)
        {
            return itemID == other.itemID;
        }
        return false;
    }
}
