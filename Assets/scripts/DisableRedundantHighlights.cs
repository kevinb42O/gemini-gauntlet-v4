using UnityEngine;

/// <summary>
/// Utility script to disable redundant ItemHoverHighlight components.
/// Attach this to your ChestSystem game object and run once to clean up competing highlight systems.
/// </summary>
public class DisableRedundantHighlights : MonoBehaviour
{
    public bool removeRedundantComponents = false;

    [ContextMenu("Clean Up Redundant Highlight Components")]
    public void CleanupRedundantHighlightComponents()
    {
        // Find all slots with ChestInventorySlot components
        ChestInventorySlot[] allSlots = FindObjectsOfType<ChestInventorySlot>();
        
        int found = 0;
        int removed = 0;
        
        foreach (ChestInventorySlot slot in allSlots)
        {
            // Check if it also has the redundant ItemHoverHighlight component
            ItemHoverHighlight redundantComponent = slot.GetComponent<ItemHoverHighlight>();
            
            if (redundantComponent != null)
            {
                found++;
                Debug.Log($"Found redundant ItemHoverHighlight on slot {slot.gameObject.name}");
                
                if (removeRedundantComponents)
                {
                    // Disable the component (it will be removed when you save the scene)
                    redundantComponent.enabled = false;
                    removed++;
                    Debug.Log($"Disabled redundant highlight component on {slot.gameObject.name}");
                }
            }
        }
        
        Debug.Log($"Found {found} redundant hover components. {(removeRedundantComponents ? removed + " were disabled." : "Set removeRedundantComponents to true to disable them.")}");
    }
    
    // This will run automatically in Play mode to help with testing
    void Start()
    {
        // Auto-run the cleanup when entering play mode
        CleanupRedundantHighlightComponents();
        
        // Self-destruct after running
        Destroy(this);
    }
}
