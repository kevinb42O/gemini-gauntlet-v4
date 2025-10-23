using UnityEngine;

[CreateAssetMenu(fileName = "FlightUnlockItem", menuName = "Inventory/Flight Unlock Item")]
public class FlightUnlockItemData : ChestItemData
{
    [Header("Flight Unlock Item")]
    [Tooltip("Icon to show for the flight unlock ability")]
    public Sprite flightIcon;
    
    void Awake()
    {
        // Set default values for flight unlock item
        if (string.IsNullOrEmpty(itemName))
            itemName = "Flight Unlock";
        
        if (string.IsNullOrEmpty(description))
            description = "Grants the ability to fly using CelestialDrift controls";
            
        if (string.IsNullOrEmpty(itemType))
            itemType = "Ability";
            
        // Use the flight icon if set, otherwise keep existing itemIcon
        if (flightIcon != null)
            itemIcon = flightIcon;
    }
    
    void OnValidate()
    {
        // Auto-assign values in editor
        itemName = "Flight Unlock";
        description = "Grants the ability to fly using CelestialDrift controls";
        itemType = "Ability";
        
        if (flightIcon != null)
            itemIcon = flightIcon;
    }
}
