using UnityEngine;
using TMPro;
using System.Collections;

public class GemDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gemText;
    [SerializeField] private float highlightScale = 1.3f;
    [SerializeField] private float animationDuration = 0.3f;
    
    private InventoryManager inventoryManager;
    private int lastGemCount = 0;
    private Vector3 originalScale;
    private bool isAnimating = false;
    
    void Start()
    {
        if (gemText == null)
            gemText = GetComponent<TextMeshProUGUI>();
            
        originalScale = gemText.transform.localScale;
        inventoryManager = InventoryManager.Instance;
        
        if (inventoryManager != null)
            lastGemCount = inventoryManager.GetGemCount();
        
        InvokeRepeating("UpdateGemDisplay", 0f, 0.1f);
    }
    
    public void UpdateGemDisplay()
    {
        if (gemText != null && inventoryManager != null)
        {
            int currentGemCount = inventoryManager.GetGemCount();
            gemText.text = currentGemCount.ToString();
            
            // Check if gems increased and we're not already animating
            if (currentGemCount > lastGemCount && !isAnimating)
            {
                StartCoroutine(HighlightAnimation());
            }
            
            lastGemCount = currentGemCount;
        }
    }
    
    private IEnumerator HighlightAnimation()
    {
        isAnimating = true;
        
        // Scale up
        float elapsed = 0f;
        while (elapsed < animationDuration / 2)
        {
            float progress = elapsed / (animationDuration / 2);
            gemText.transform.localScale = Vector3.Lerp(originalScale, originalScale * highlightScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < animationDuration / 2)
        {
            float progress = elapsed / (animationDuration / 2);
            gemText.transform.localScale = Vector3.Lerp(originalScale * highlightScale, originalScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we're back to original scale
        gemText.transform.localScale = originalScale;
        isAnimating = false;
    }
}
