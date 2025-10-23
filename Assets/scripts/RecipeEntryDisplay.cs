using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for displaying individual recipe entries in the recipe info panel
/// Shows ingredients and output item with icons and names
/// </summary>
public class RecipeEntryDisplay : MonoBehaviour
{
    [Header("Recipe Entry UI")]
    [Tooltip("Images for ingredient icons (should have 4 slots)")]
    public Image[] ingredientIcons = new Image[4];
    [Tooltip("Text fields for ingredient names (should have 4 slots)")]
    public TextMeshProUGUI[] ingredientNames = new TextMeshProUGUI[4];
    [Tooltip("Image for output item icon")]
    public Image outputIcon;
    [Tooltip("Text field for output item name")]
    public TextMeshProUGUI outputName;
    [Tooltip("Text field for output item count")]
    public TextMeshProUGUI outputCount;
    [Tooltip("Arrow or separator image between ingredients and output")]
    public Image arrowImage;
    
    [Header("Visual Settings")]
    [Tooltip("Alpha for empty ingredient slots")]
    [Range(0f, 1f)]
    public float emptySlotAlpha = 0.3f;
    [Tooltip("Color for empty ingredient slot backgrounds")]
    public Color emptySlotColor = Color.gray;
    [Tooltip("Default sprite for empty ingredient slots")]
    public Sprite emptySlotSprite;
    
    /// <summary>
    /// Setup this recipe entry with the given recipe data
    /// </summary>
    public void SetupRecipe(ForgeRecipe recipe)
    {
        if (recipe == null)
        {
            Debug.LogWarning("RecipeEntryDisplay: Cannot setup with null recipe");
            return;
        }
        
        Debug.Log($"RecipeEntryDisplay: Setting up recipe for {recipe.outputItem?.itemName}");
        
        // Setup ingredient slots
        SetupIngredientSlots(recipe);
        
        // Setup output slot
        SetupOutputSlot(recipe);
        
        Debug.Log($"RecipeEntryDisplay: Recipe setup completed for {recipe.outputItem?.itemName}");
    }
    
    /// <summary>
    /// Setup ingredient slots with recipe data
    /// </summary>
    void SetupIngredientSlots(ForgeRecipe recipe)
    {
        for (int i = 0; i < ingredientIcons.Length; i++)
        {
            bool hasIngredient = i < recipe.requiredIngredients.Length && recipe.requiredIngredients[i] != null;
            
            if (hasIngredient)
            {
                ChestItemData ingredient = recipe.requiredIngredients[i];
                
                // Set ingredient icon
                if (ingredientIcons[i] != null)
                {
                    ingredientIcons[i].sprite = ingredient.itemIcon;
                    ingredientIcons[i].color = Color.white;
                }
                
                // Set ingredient name
                if (ingredientNames[i] != null)
                {
                    ingredientNames[i].text = ingredient.itemName;
                    ingredientNames[i].color = Color.white;
                }
                
                Debug.Log($"RecipeEntryDisplay: Set ingredient {i}: {ingredient.itemName}");
            }
            else
            {
                // Empty slot
                if (ingredientIcons[i] != null)
                {
                    ingredientIcons[i].sprite = emptySlotSprite;
                    Color emptyColor = emptySlotColor;
                    emptyColor.a = emptySlotAlpha;
                    ingredientIcons[i].color = emptyColor;
                }
                
                if (ingredientNames[i] != null)
                {
                    ingredientNames[i].text = "";
                }
                
                Debug.Log($"RecipeEntryDisplay: Set ingredient {i}: Empty");
            }
        }
    }
    
    /// <summary>
    /// Setup output slot with recipe data
    /// </summary>
    void SetupOutputSlot(ForgeRecipe recipe)
    {
        if (recipe.outputItem == null) return;
        
        // Set output icon
        if (outputIcon != null)
        {
            outputIcon.sprite = recipe.outputItem.itemIcon;
            outputIcon.color = Color.white;
        }
        
        // Set output name
        if (outputName != null)
        {
            outputName.text = recipe.outputItem.itemName;
            outputName.color = Color.white;
        }
        
        // Set output count
        if (outputCount != null)
        {
            if (recipe.outputCount > 1)
            {
                outputCount.text = $"x{recipe.outputCount}";
                outputCount.gameObject.SetActive(true);
            }
            else
            {
                outputCount.gameObject.SetActive(false);
            }
        }
        
        Debug.Log($"RecipeEntryDisplay: Set output: {recipe.outputItem.itemName} x{recipe.outputCount}");
    }
    
    /// <summary>
    /// Validate that all required UI components are assigned
    /// </summary>
    void Start()
    {
        ValidateComponents();
    }
    
    /// <summary>
    /// Validate UI component assignments
    /// </summary>
    void ValidateComponents()
    {
        bool isValid = true;
        
        // Check ingredient arrays
        if (ingredientIcons.Length != 4)
        {
            Debug.LogError($"RecipeEntryDisplay: ingredientIcons array should have 4 elements, has {ingredientIcons.Length}");
            isValid = false;
        }
        
        if (ingredientNames.Length != 4)
        {
            Debug.LogError($"RecipeEntryDisplay: ingredientNames array should have 4 elements, has {ingredientNames.Length}");
            isValid = false;
        }
        
        // Check for null components
        for (int i = 0; i < ingredientIcons.Length && i < 4; i++)
        {
            if (ingredientIcons[i] == null)
            {
                Debug.LogWarning($"RecipeEntryDisplay: ingredientIcons[{i}] is null");
            }
            
            if (i < ingredientNames.Length && ingredientNames[i] == null)
            {
                Debug.LogWarning($"RecipeEntryDisplay: ingredientNames[{i}] is null");
            }
        }
        
        if (outputIcon == null)
        {
            Debug.LogWarning("RecipeEntryDisplay: outputIcon is null");
        }
        
        if (outputName == null)
        {
            Debug.LogWarning("RecipeEntryDisplay: outputName is null");
        }
        
        if (isValid)
        {
            Debug.Log("RecipeEntryDisplay: All components validated successfully");
        }
    }
    
    /// <summary>
    /// Context menu method for testing recipe display
    /// </summary>
    [ContextMenu("Test Recipe Display")]
    void TestRecipeDisplay()
    {
        // This would need actual ChestItemData references to work
        Debug.Log("RecipeEntryDisplay: Test method called - assign actual recipe data in ForgeManager");
    }
}
