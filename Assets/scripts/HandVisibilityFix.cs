using UnityEngine;

/// <summary>
/// Simple fix to ensure both hands are always visible
/// Attach to player - runs once on start
/// </summary>
public class HandVisibilityFix : MonoBehaviour
{
    void Start()
    {
        EnsureHandsAreVisible();
    }
    
    private void EnsureHandsAreVisible()
    {
        // Find all hand controllers
        IndividualLayeredHandController[] allHands = FindObjectsOfType<IndividualLayeredHandController>();
        
        Debug.Log($"[HandVisibilityFix] Found {allHands.Length} hand controllers");
        
        foreach (var hand in allHands)
        {
            if (hand == null) continue;
            
            // Check if this is a player hand (not companion/enemy)
            if (IsPlayerHand(hand))
            {
                string handType = hand.IsLeftHand ? "LEFT" : "RIGHT";
                bool wasActive = hand.gameObject.activeInHierarchy;
                
                // Ensure hand GameObject is active
                hand.gameObject.SetActive(true);
                
                // Ensure all parent objects are active too
                Transform current = hand.transform;
                while (current != null)
                {
                    if (!current.gameObject.activeSelf)
                    {
                        Debug.Log($"[HandVisibilityFix] Activating parent: {current.name}");
                        current.gameObject.SetActive(true);
                    }
                    current = current.parent;
                }
                
                Debug.Log($"[HandVisibilityFix] {handType} hand ({hand.name}) - Was active: {wasActive}, Is active now: {hand.gameObject.activeInHierarchy}");
            }
        }
    }
    
    private bool IsPlayerHand(IndividualLayeredHandController controller)
    {
        if (controller == null) return false;
        
        // Check if it's under a Player GameObject
        Transform current = controller.transform;
        while (current != null)
        {
            if (current.name.ToLower().Contains("player"))
                return true;
            current = current.parent;
        }
        
        return false;
    }
}
