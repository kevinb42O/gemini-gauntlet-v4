using UnityEngine;
using UnityEditor;

/// <summary>
/// KeycardSetupHelper.cs - Editor utility to quickly create keycard pickups and doors
/// Access via: GameObject > Keycard System > [option]
/// </summary>
public class KeycardSetupHelper : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("GameObject/Keycard System/Create Keycard Pickup", false, 10)]
    static void CreateKeycardPickup()
    {
        // Create the main GameObject
        GameObject keycardPickup = new GameObject("Keycard_Pickup");
        
        // Add the KeycardItem script
        KeycardItem keycardItem = keycardPickup.AddComponent<KeycardItem>();
        
        // Set default values
        keycardItem.collectionDistance = 2.5f;
        keycardItem.autoCollect = true;
        keycardItem.collectionCooldown = 0.5f;
        keycardItem.enableBobbing = true;
        keycardItem.bobbingSpeed = 2f;
        keycardItem.bobbingHeight = 0.3f;
        keycardItem.enableRotation = true;
        keycardItem.rotationSpeed = 60f;
        keycardItem.playCollectionSound = true;
        
        // Create visual representation (card-shaped cube)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(keycardPickup.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(0.2f, 0.5f, 0.1f);
        
        // Remove the collider from visual (parent has the trigger collider)
        DestroyImmediate(visual.GetComponent<Collider>());
        
        // Create a basic material
        Material cardMaterial = new Material(Shader.Find("Standard"));
        cardMaterial.color = Color.yellow;
        visual.GetComponent<Renderer>().material = cardMaterial;
        
        // Position at scene view camera or at origin
        if (SceneView.lastActiveSceneView != null)
        {
            keycardPickup.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 5f;
        }
        
        // Select the new object
        Selection.activeGameObject = keycardPickup;
        
        // Mark scene as dirty
        EditorUtility.SetDirty(keycardPickup);
        
        Debug.Log("[KeycardSetupHelper] Created keycard pickup. Assign a keycard ScriptableObject to the 'Keycard Data' field.");
    }
    
    [MenuItem("GameObject/Keycard System/Create Keycard Door", false, 11)]
    static void CreateKeycardDoor()
    {
        // Create the door GameObject
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Keycard_Door";
        
        // Scale to door size
        door.transform.localScale = new Vector3(2f, 3f, 0.2f);
        
        // Remove the default collider (KeycardDoor will add a trigger collider)
        DestroyImmediate(door.GetComponent<Collider>());
        
        // Add the KeycardDoor script
        KeycardDoor keycardDoor = door.AddComponent<KeycardDoor>();
        
        // Set default values
        keycardDoor.openType = KeycardDoor.DoorOpenType.SlideUp;
        keycardDoor.openDistance = 3f;
        keycardDoor.openSpeed = 2f;
        keycardDoor.autoClose = false;
        keycardDoor.autoCloseDelay = 5f;
        keycardDoor.interactionDistance = 3f;
        keycardDoor.lockedColor = Color.red;
        keycardDoor.unlockedColor = Color.green;
        
        // Set material to red (locked state)
        Material doorMaterial = new Material(Shader.Find("Standard"));
        doorMaterial.color = Color.red;
        door.GetComponent<Renderer>().material = doorMaterial;
        
        // Position at scene view camera or at origin
        if (SceneView.lastActiveSceneView != null)
        {
            door.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 5f;
        }
        
        // Select the new object
        Selection.activeGameObject = door;
        
        // Mark scene as dirty
        EditorUtility.SetDirty(door);
        
        Debug.Log("[KeycardSetupHelper] Created keycard door. Assign a keycard ScriptableObject to the 'Required Keycard' field.");
    }
    
    [MenuItem("GameObject/Keycard System/Create Complete Keycard Set", false, 12)]
    static void CreateCompleteKeycardSet()
    {
        // Create parent container
        GameObject container = new GameObject("Keycard_System");
        GameObject pickupsContainer = new GameObject("Keycard_Pickups");
        GameObject doorsContainer = new GameObject("Keycard_Doors");
        
        pickupsContainer.transform.SetParent(container.transform);
        doorsContainer.transform.SetParent(container.transform);
        
        // Keycard data (name, color)
        var keycards = new[]
        {
            ("Building21", new Color(1f, 0.5f, 0f)),  // Orange
            ("Green", Color.green),
            ("Blue", new Color(0f, 0.5f, 1f)),
            ("Black", new Color(0.1f, 0.1f, 0.1f)),
            ("Red", Color.red)
        };
        
        float spacing = 5f;
        Vector3 basePosition = Vector3.zero;
        
        if (SceneView.lastActiveSceneView != null)
        {
            basePosition = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 10f;
        }
        
        for (int i = 0; i < keycards.Length; i++)
        {
            var (name, color) = keycards[i];
            
            // Create pickup
            GameObject pickup = new GameObject($"{name}_Keycard_Pickup");
            pickup.transform.SetParent(pickupsContainer.transform);
            pickup.transform.position = basePosition + new Vector3(i * spacing, 1f, 0f);
            
            KeycardItem keycardItem = pickup.AddComponent<KeycardItem>();
            keycardItem.collectionDistance = 2.5f;
            keycardItem.autoCollect = true;
            keycardItem.enableBobbing = true;
            keycardItem.enableRotation = true;
            
            GameObject pickupVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickupVisual.name = "Visual";
            pickupVisual.transform.SetParent(pickup.transform);
            pickupVisual.transform.localPosition = Vector3.zero;
            pickupVisual.transform.localScale = new Vector3(0.2f, 0.5f, 0.1f);
            DestroyImmediate(pickupVisual.GetComponent<Collider>());
            
            Material pickupMaterial = new Material(Shader.Find("Standard"));
            pickupMaterial.color = color;
            pickupVisual.GetComponent<Renderer>().material = pickupMaterial;
            
            // Create door
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = $"{name}_Keycard_Door";
            door.transform.SetParent(doorsContainer.transform);
            door.transform.position = basePosition + new Vector3(i * spacing, 1.5f, 5f);
            door.transform.localScale = new Vector3(2f, 3f, 0.2f);
            
            DestroyImmediate(door.GetComponent<Collider>());
            
            KeycardDoor keycardDoor = door.AddComponent<KeycardDoor>();
            keycardDoor.openType = KeycardDoor.DoorOpenType.SlideUp;
            keycardDoor.openDistance = 3f;
            keycardDoor.openSpeed = 2f;
            keycardDoor.interactionDistance = 3f;
            keycardDoor.lockedColor = Color.red;
            keycardDoor.unlockedColor = color;
            
            Material doorMaterial = new Material(Shader.Find("Standard"));
            doorMaterial.color = Color.red;
            door.GetComponent<Renderer>().material = doorMaterial;
        }
        
        // Select the container
        Selection.activeGameObject = container;
        
        Debug.Log("[KeycardSetupHelper] Created complete keycard set with 5 pickups and 5 doors. Assign keycard ScriptableObjects to each pickup and door.");
    }
    
    [MenuItem("Assets/Create/Keycard System/All 5 Keycards", false, 1)]
    static void CreateAllKeycardScriptableObjects()
    {
        string path = "Assets/";
        
        // Get selected folder path if any
        if (Selection.activeObject != null)
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (System.IO.Directory.Exists(selectedPath))
            {
                path = selectedPath + "/";
            }
            else if (System.IO.File.Exists(selectedPath))
            {
                path = System.IO.Path.GetDirectoryName(selectedPath) + "/";
            }
        }
        
        // Create folder for keycards if it doesn't exist
        string keycardFolder = path + "Keycards/";
        if (!AssetDatabase.IsValidFolder(keycardFolder.TrimEnd('/')))
        {
            string parentFolder = path.TrimEnd('/');
            AssetDatabase.CreateFolder(parentFolder, "Keycards");
        }
        
        // Keycard data
        var keycards = new[]
        {
            ("Building21_Keycard", "Building21 Keycard", "A security keycard that grants access to Building 21. Handle with care.", 3, new Color(1f, 0.5f, 0f)),
            ("Green_Keycard", "Green Keycard", "A green security keycard. Opens doors marked with green access panels.", 2, Color.green),
            ("Blue_Keycard", "Blue Keycard", "A blue security keycard. Opens doors marked with blue access panels.", 3, new Color(0f, 0.5f, 1f)),
            ("Black_Keycard", "Black Keycard", "A high-security black keycard. Opens the most restricted areas.", 5, new Color(0.1f, 0.1f, 0.1f)),
            ("Red_Keycard", "Red Keycard", "A red security keycard. Opens emergency access doors.", 4, Color.red)
        };
        
        foreach (var (fileName, itemName, description, rarity, color) in keycards)
        {
            ChestItemData keycard = ScriptableObject.CreateInstance<ChestItemData>();
            keycard.itemName = itemName;
            keycard.description = description;
            keycard.itemType = "Keycard";
            keycard.itemRarity = rarity;
            keycard.craftingCategory = "Key Items";
            keycard.rarityColor = color;
            
            string assetPath = keycardFolder + fileName + ".asset";
            AssetDatabase.CreateAsset(keycard, assetPath);
            
            Debug.Log($"[KeycardSetupHelper] Created {itemName} at {assetPath}");
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[KeycardSetupHelper] Created all 5 keycard ScriptableObjects in " + keycardFolder);
        
        // Select the folder
        Object folder = AssetDatabase.LoadAssetAtPath<Object>(keycardFolder.TrimEnd('/'));
        Selection.activeObject = folder;
        EditorGUIUtility.PingObject(folder);
    }
#endif
}
