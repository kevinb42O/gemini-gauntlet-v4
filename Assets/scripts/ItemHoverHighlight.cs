using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Simple hover highlight that shows/hides a highlight image overlay
/// Just drag your highlight prefab and it will show/hide on hover
/// </summary>
public class ItemHoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Simple Hover Highlight")]
    [Tooltip("The highlight image/GameObject to show on hover")]
    public GameObject highlightOverlay;
    
    private void Start()
    {
        // Make sure highlight is hidden at start
        if (highlightOverlay != null)
        {
            highlightOverlay.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"ItemHoverHighlight on {gameObject.name}: No highlight overlay assigned!");
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show the highlight overlay
        if (highlightOverlay != null)
        {
            highlightOverlay.SetActive(true);
        }
        
        // Detailed logging about what we're hovering
        LogHoverInfo("HOVERING OVER");
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // Hide the highlight overlay
        if (highlightOverlay != null)
        {
            highlightOverlay.SetActive(false);
        }
        
        // Log when we stop hovering
        LogHoverInfo("STOPPED HOVERING");
    }
    
    /// <summary>
    /// Detailed logging about what object is being hovered
    /// </summary>
    private void LogHoverInfo(string action)
    {
        string logMessage = $"\n=== {action} ITEM ===";
        logMessage += $"\nGameObject: {gameObject.name}";
        logMessage += $"\nFull Path: {GetGameObjectPath(gameObject)}";
        
        // Check for ChestInventorySlot component
        var chestSlot = GetComponent<ChestInventorySlot>();
        if (chestSlot != null)
        {
            logMessage += $"\nType: CHEST SLOT";
            if (chestSlot.currentItem != null)
            {
                logMessage += $"\nItem: {chestSlot.currentItem.itemName}";
                logMessage += $"\nCount: {chestSlot.itemCount}";
                logMessage += $"\nRarity: {chestSlot.currentItem.itemRarity}";
            }
            else
            {
                logMessage += $"\nItem: EMPTY SLOT";
            }
        }
        
        // Check for UnifiedSlot component
        var inventorySlot = GetComponent<UnifiedSlot>();
        if (inventorySlot != null)
        {
            logMessage += $"\nType: INVENTORY SLOT";
            // Add inventory slot specific info if available
        }
        
        // Check for Image component and sprite info
        var image = GetComponent<Image>();
        if (image != null)
        {
            logMessage += $"\nSprite: {(image.sprite != null ? image.sprite.name : "None")}";
            logMessage += $"\nColor: {image.color}";
        }
        
        // Check parent objects for context
        Transform parent = transform.parent;
        if (parent != null)
        {
            logMessage += $"\nParent: {parent.name}";
            if (parent.parent != null)
            {
                logMessage += $"\nGrandparent: {parent.parent.name}";
            }
        }
        
        // Check for any Text components (item names, counts, etc.)
        var texts = GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        if (texts.Length > 0)
        {
            logMessage += $"\nText Components:";
            foreach (var text in texts)
            {
                if (!string.IsNullOrEmpty(text.text))
                {
                    logMessage += $"\n  - {text.name}: '{text.text}'";
                }
            }
        }
        
        logMessage += $"\n========================\n";
        
        Debug.Log(logMessage);
    }
    
    /// <summary>
    /// Get the full hierarchy path of a GameObject
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    [ContextMenu("Test Hover On")]
    public void TestHoverOn()
    {
        OnPointerEnter(null);
    }
    
    [ContextMenu("Test Hover Off")]
    public void TestHoverOff()
    {
        OnPointerExit(null);
    }
}
