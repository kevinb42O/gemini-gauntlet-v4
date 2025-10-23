using UnityEngine;
using System.Collections.Generic;

public class ObjectManager : MonoBehaviour
{
    [Header("Object Prefabs")]
    public GameObject objectKind1;
    public GameObject objectKind2;

    [Header("Spawning Settings")]
    public int numberOfEachKind = 20;
    public float roamingRadius = 30f;
    
    [Header("Swarming Behavior")]
    public float bubbleRadius = 15f;
    public float separationRadius = 3f;
    public float cohesionStrength = 1f;
    public float separationStrength = 2f;
    
    [Header("Camera Interaction")]
    public float cameraAttackChance = 0.02f; // 2% chance per second
    public float cameraAttackSpeed = 15f;
    public float dodgeDistance = 5f;
    public float mouseDetectionRadius = 10f;
    public float mouseAvoidanceStrength = 3f;
    
    [Header("Mouse Attack Behavior")]
    public float mouseAttackSpeed = 10f; // Speed when attacking mouse pointer
    public float mouseAttackDuration = 15f; // How long to attack mouse (seconds)
    public float mouseChaseRestDuration = 2f; // Rest period between attacks (seconds)
    
    // Static references for all roaming objects to access
    public static ObjectManager Instance;
    public static List<RoamingObject> AllObjects = new List<RoamingObject>();
    public static Camera MainCamera;
    
    // Simple state tracking
    private bool isRelocatingSwarm = false;
    
    void Awake()
    {
        Instance = this;
        MainCamera = Camera.main;
        if (MainCamera == null) MainCamera = FindObjectOfType<Camera>();
    }

    void Start()
    {
        // Spawn the first kind of object
        SpawnObjects(objectKind1, "TypeA");

        // Spawn the second kind of object
        SpawnObjects(objectKind2, "TypeB");
        
        // Simple initialization - no migration tracking needed
    }
    
    void Update()
    {
        HandleMouseInteraction();
    }
    
    // Removed all migration methods - keeping only essential swarming

    void SpawnObjects(GameObject prefab, string tag)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab for tag " + tag + " is not assigned!");
            return;
        }

        for (int i = 0; i < numberOfEachKind; i++)
        {
            // Get a random position within the bubble radius
            Vector3 randomPosition = transform.position + Random.insideUnitSphere * bubbleRadius;

            // Instantiate the object
            GameObject newObject = Instantiate(prefab, randomPosition, Quaternion.identity);

            // Assign the tag
            newObject.tag = tag;

            // Get the RoamingObject script and initialize its roaming area
            RoamingObject roamingScript = newObject.GetComponent<RoamingObject>();
            if (roamingScript != null)
            {
                roamingScript.Initialize(transform.position, bubbleRadius);
                AllObjects.Add(roamingScript); // Add to global list for swarming
            }
        }
    }
    
    // Removed camera movement detection - no longer needed
    
    // Removed camera relocation - no longer needed
    
    void HandleMouseInteraction()
    {
        if (MainCamera == null) return;
        
        // Cast ray from camera through mouse position
        Ray mouseRay = MainCamera.ScreenPointToRay(Input.mousePosition);
        
        // Get world position at swarm distance for more accurate detection
        Vector3 swarmCenter = transform.position;
        float distanceToCamera = Vector3.Distance(swarmCenter, MainCamera.transform.position);
        Vector3 mouseWorldPos = mouseRay.origin + mouseRay.direction * distanceToCamera;
        
        // CHECK FOR MOUSE PUSH-AWAY TRIGGER
        foreach (var obj in AllObjects)
        {
            if (obj != null)
            {
                float distanceToMouse = Vector3.Distance(obj.transform.position, mouseWorldPos);
                
                // If mouse gets close enough, trigger push-away and attack sequence
                if (distanceToMouse < mouseDetectionRadius)
                {
                    float avoidanceStrength = 1f - (distanceToMouse / mouseDetectionRadius);
                    
                    // If pushed away strongly enough, start 15-second attack sequence
                    if (avoidanceStrength > 0.7f)
                    {
                        obj.StartMouseAttackSequence(mouseWorldPos, mouseAttackSpeed, mouseAttackDuration, mouseChaseRestDuration);
                    }
                    else
                    {
                        // Light avoidance without triggering attack
                        obj.ReactToMouseThreat(mouseWorldPos, avoidanceStrength);
                    }
                }
                else
                {
                    // Let object know mouse is not threatening anymore
                    obj.StopMouseAvoidance();
                }
            }
        }
    }
    
    // Helper method to get all nearby objects for swarming calculations
    public static List<RoamingObject> GetNearbyObjects(Vector3 position, float radius)
    {
        List<RoamingObject> nearby = new List<RoamingObject>();
        
        foreach (var obj in AllObjects)
        {
            if (obj != null && Vector3.Distance(obj.transform.position, position) <= radius)
            {
                nearby.Add(obj);
            }
        }
        
        return nearby;
    }
    
    // Removed staggered migration - no longer needed

    // Draw a visual representation of the roaming radius in the editor
    private void OnDrawGizmosSelected()
    {
        // Main roaming area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, roamingRadius);
        
        // Bubble radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, bubbleRadius);
    }
}