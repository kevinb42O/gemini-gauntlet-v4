using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemTooltip : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Main tooltip panel")]
    public GameObject tooltipPanel;
    [Tooltip("Item name text")]
    public TextMeshProUGUI itemNameText;
    [Tooltip("Item description text")]
    public TextMeshProUGUI itemDescriptionText;
    [Tooltip("Item type text")]
    public TextMeshProUGUI itemTypeText;
    [Tooltip("Item rarity text")]
    public TextMeshProUGUI itemRarityText;
    [Tooltip("Item icon")]
    public Image itemIcon;
    [Tooltip("Background image for rarity color")]
    public Image rarityBackground;
    
    [Header("Positioning")]
    [Tooltip("Offset from cursor position")]
    public Vector2 cursorOffset = new Vector2(10f, -10f);
    [Tooltip("Should tooltip follow mouse cursor?")]
    public bool followCursor = true;
    
    [Header("Animation")]
    [Tooltip("Fade in/out duration")]
    public float fadeDuration = 0.2f;
    
    // State
    private bool isVisible = false;
    private CanvasGroup canvasGroup;
    private RectTransform tooltipRect;
    private Canvas parentCanvas;
    
    // Coroutines
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // Get components
        canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
        }
        
        tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        
        // Start hidden
        HideTooltip();
    }

    private void Update()
    {
        if (isVisible && followCursor)
        {
            UpdatePosition();
        }
    }

    public void ShowTooltip(ChestItemData itemData, Vector3 worldPosition)
    {
        if (itemData == null) return;

        // Update tooltip content
        UpdateTooltipContent(itemData);
        
        // Position tooltip
        if (followCursor)
        {
            UpdatePosition();
        }
        else
        {
            SetWorldPosition(worldPosition);
        }
        
        // Show tooltip
        ShowTooltipPanel();
    }

    public void ShowTooltip(ChestItemData itemData)
    {
        ShowTooltip(itemData, Vector3.zero);
    }

    public void HideTooltip()
    {
        HideTooltipPanel();
    }

    private void UpdateTooltipContent(ChestItemData itemData)
    {
        // Item name
        if (itemNameText != null)
        {
            itemNameText.text = itemData.itemName;
            itemNameText.color = itemData.GetRarityColor();
        }
        
        // Item description
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = itemData.description;
        }
        
        // Item type
        if (itemTypeText != null)
        {
            itemTypeText.text = $"Type: {itemData.itemType}";
        }
        
        // Item rarity
        if (itemRarityText != null)
        {
            string rarityText = GetRarityText(itemData.itemRarity);
            itemRarityText.text = $"Rarity: {rarityText}";
            itemRarityText.color = itemData.GetRarityColor();
        }
        
        // Item icon
        if (itemIcon != null && itemData.itemIcon != null)
        {
            itemIcon.sprite = itemData.itemIcon;
            itemIcon.color = Color.white;
        }
        
        // Rarity background
        if (rarityBackground != null)
        {
            Color bgColor = itemData.GetRarityColor();
            bgColor.a = 0.3f; // Semi-transparent
            rarityBackground.color = bgColor;
        }
    }

    private string GetRarityText(int rarity)
    {
        switch (rarity)
        {
            case 1: return "Common";
            case 2: return "Uncommon";
            case 3: return "Rare";
            case 4: return "Epic";
            case 5: return "Legendary";
            default: return "Unknown";
        }
    }

    private void UpdatePosition()
    {
        if (tooltipRect == null || parentCanvas == null) return;

        // Get mouse position in screen space
        Vector2 mousePosition = Input.mousePosition;
        
        // Convert to canvas space
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            mousePosition,
            parentCanvas.worldCamera,
            out canvasPosition
        );
        
        // Apply offset
        canvasPosition += cursorOffset;
        
        // Clamp to screen bounds
        ClampToScreen(ref canvasPosition);
        
        // Set position
        tooltipRect.anchoredPosition = canvasPosition;
    }

    private void SetWorldPosition(Vector3 worldPosition)
    {
        if (tooltipRect == null || parentCanvas == null) return;

        // Convert world position to screen space
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        
        // Convert to canvas space
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPosition,
            parentCanvas.worldCamera,
            out canvasPosition
        );
        
        // Apply offset
        canvasPosition += cursorOffset;
        
        // Clamp to screen bounds
        ClampToScreen(ref canvasPosition);
        
        // Set position
        tooltipRect.anchoredPosition = canvasPosition;
    }

    private void ClampToScreen(ref Vector2 canvasPosition)
    {
        if (tooltipRect == null || parentCanvas == null) return;

        // Get canvas rect
        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Vector2 canvasSize = canvasRect.sizeDelta;
        
        // Get tooltip size
        Vector2 tooltipSize = tooltipRect.sizeDelta;
        
        // Clamp position to keep tooltip on screen
        float halfWidth = tooltipSize.x * 0.5f;
        float halfHeight = tooltipSize.y * 0.5f;
        
        canvasPosition.x = Mathf.Clamp(canvasPosition.x, -canvasSize.x * 0.5f + halfWidth, canvasSize.x * 0.5f - halfWidth);
        canvasPosition.y = Mathf.Clamp(canvasPosition.y, -canvasSize.y * 0.5f + halfHeight, canvasSize.y * 0.5f - halfHeight);
    }

    private void ShowTooltipPanel()
    {
        if (isVisible) return;

        isVisible = true;
        tooltipPanel.SetActive(true);
        
        // Fade in
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeTooltip(1f));
    }

    private void HideTooltipPanel()
    {
        if (!isVisible) return;

        isVisible = false;
        
        // Fade out
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeTooltip(0f));
    }

    private System.Collections.IEnumerator FadeTooltip(float targetAlpha)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        
        // Hide panel if fully faded out
        if (targetAlpha <= 0f)
        {
            tooltipPanel.SetActive(false);
        }
    }

    // Public methods for external control
    public bool IsVisible => isVisible;
    
    public void SetFollowCursor(bool follow)
    {
        followCursor = follow;
    }
    
    public void SetCursorOffset(Vector2 offset)
    {
        cursorOffset = offset;
    }
}
