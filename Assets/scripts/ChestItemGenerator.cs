#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ChestItemGenerator : EditorWindow
{
    private string itemSetName = "DefaultItemSet";
    private int numberOfItems = 10;
    private Texture2D defaultIcon;
    
    private string[] itemTypes = new string[] { "Weapon", "Material", "Consumable", "Relic", "Gem", "Tool" };
    private string[] itemPrefixes = new string[] { "Ancient", "Rusty", "Enchanted", "Mystic", "Broken", "Gleaming", "Forgotten", "Blessed", "Cursed", "Royal" };
    private string[] itemNouns = new string[] { "Sword", "Shield", "Hammer", "Crystal", "Orb", "Pendant", "Rune", "Statue", "Potion", "Ring", "Amulet", "Gem", "Dagger", "Helm", "Gauntlet", "Cloak", "Staff" };
    private string[] materialTypes = new string[] { "Iron", "Gold", "Silver", "Wood", "Stone", "Leather", "Cloth", "Glass", "Diamond", "Ruby", "Emerald", "Bone", "Dragon Scale" };
    private string[] descriptionFormats = new string[] 
    {
        "A {0} {1} made from {2}. It seems to hold mysterious power.",
        "This {0} {1} was found in the ruins of an ancient civilization. Made of {2}.",
        "The {0} {1} gleams with an otherworldly light. Crafted from rare {2}.",
        "Legends speak of this {0} {1}. Its {2} structure shows expert craftsmanship.",
        "A peculiar {0} {1} that seems to change weight when held. Made of strange {2}."
    };
    
    [MenuItem("Gemini Gauntlet/Chest Item Generator")]
    public static void ShowWindow()
    {
        GetWindow<ChestItemGenerator>("Chest Item Generator");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Chest Item Generator", EditorStyles.boldLabel);
        
        itemSetName = EditorGUILayout.TextField("Item Set Name", itemSetName);
        numberOfItems = EditorGUILayout.IntSlider("Number of Items", numberOfItems, 1, 50);
        defaultIcon = (Texture2D)EditorGUILayout.ObjectField("Default Icon", defaultIcon, typeof(Texture2D), false);
        
        if (GUILayout.Button("Generate Sample Items"))
        {
            GenerateSampleItems();
        }
    }
    
    private void GenerateSampleItems()
    {
        // Create folders if they don't exist
        string itemsFolder = "Assets/Resources/Items";
        if (!Directory.Exists(itemsFolder))
        {
            Directory.CreateDirectory(itemsFolder);
        }
        
        // Create default icon if needed
        Sprite defaultSprite = null;
        if (defaultIcon != null)
        {
            defaultSprite = Sprite.Create(defaultIcon, new Rect(0, 0, defaultIcon.width, defaultIcon.height), Vector2.one * 0.5f);
        }
        
        List<ChestItemData> createdItems = new List<ChestItemData>();
        
        // Generate items
        for (int i = 0; i < numberOfItems; i++)
        {
            // Generate random item properties
            string prefix = itemPrefixes[Random.Range(0, itemPrefixes.Length)];
            string noun = itemNouns[Random.Range(0, itemNouns.Length)];
            string material = materialTypes[Random.Range(0, materialTypes.Length)];
            string itemName = $"{prefix} {noun}";
            string itemType = itemTypes[Random.Range(0, itemTypes.Length)];
            
            // Create description
            string descFormat = descriptionFormats[Random.Range(0, descriptionFormats.Length)];
            string description = string.Format(descFormat, prefix.ToLower(), noun.ToLower(), material.ToLower());
            
            // Create ScriptableObject
            ChestItemData item = CreateInstance<ChestItemData>();
            item.itemName = itemName;
            item.description = description;
            item.itemIcon = defaultSprite;
            item.itemType = itemType;
            item.itemRarity = Random.Range(1, 6); // 1-5 rarity
            item.craftingCategory = material;
            
            // Set up random crafting components
            int componentCount = Random.Range(1, 4);
            item.craftingComponents = new string[componentCount];
            for (int j = 0; j < componentCount; j++)
            {
                item.craftingComponents[j] = materialTypes[Random.Range(0, materialTypes.Length)];
            }
            
            item.craftingValue = Random.Range(1, 10);
            
            // Save the asset
            string assetPath = $"{itemsFolder}/{itemName.Replace(" ", "")}.asset";
            AssetDatabase.CreateAsset(item, assetPath);
            createdItems.Add(item);
        }
        
        // Create item set reference
        string setPath = $"{itemsFolder}/{itemSetName}Set.asset";
        ChestItemSet itemSet = CreateInstance<ChestItemSet>();
        itemSet.items = createdItems.ToArray();
        AssetDatabase.CreateAsset(itemSet, setPath);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Generated {numberOfItems} sample items and saved them to {itemsFolder}");
        
        // Select the item set in the Project window
        Selection.activeObject = itemSet;
    }
}
#endif
