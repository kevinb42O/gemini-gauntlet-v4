using UnityEngine;
using System.Collections;

public class SkyboxChanger : MonoBehaviour
{
    [Header("Skybox Settings")]
    // Array to hold your skybox textures (cube textures/cubemaps)
    public Texture[] skyboxTextures;


    [Header("Trigger Zone Settings")]
    // Array of trigger zones that pause/resume skybox changing when player enters
    public Collider[] triggerZones;

    // Tag to identify the player (defaults to "Player")
    public string playerTag = "Player";

    // The current skybox index
    private int currentIndex = 0;

    // Array to store generated skybox materials
    private Material[] skyboxMaterials;

    void Start()
    {
        // Check if there are any skybox textures assigned
        if (skyboxTextures.Length == 0)
        {
            Debug.LogError("No skybox textures assigned to the SkyboxChanger script!");
            return;
        }

        // Create materials from textures
        CreateSkyboxMaterials();

        // Set the initial skybox
        if (skyboxMaterials.Length > 0)
        {
            RenderSettings.skybox = skyboxMaterials[currentIndex];
        }

        // Set up trigger zones if assigned
        if (triggerZones != null && triggerZones.Length > 0)
        {
            foreach (Collider triggerZone in triggerZones)
            {
                if (triggerZone != null)
                {
                    // Ensure the trigger zone is set as a trigger
                    triggerZone.isTrigger = true;
                    
                    // Add trigger detection component if it doesn't exist
                    SkyboxTriggerDetector detector = triggerZone.GetComponent<SkyboxTriggerDetector>();
                    if (detector == null)
                    {
                        detector = triggerZone.gameObject.AddComponent<SkyboxTriggerDetector>();
                    }
                    detector.skyboxChanger = this;
                }
                else
                {
                    Debug.LogWarning("One of the trigger zones in the array is null!");
                }
            }
        }
    }

    private void CreateSkyboxMaterials()
    {
        skyboxMaterials = new Material[skyboxTextures.Length];

        for (int i = 0; i < skyboxTextures.Length; i++)
        {
            if (skyboxTextures[i] != null)
            {
                // Create a new skybox material
                Material skyboxMat = new Material(Shader.Find("Skybox/Cubemap"));
                skyboxMat.SetTexture("_Tex", skyboxTextures[i]);
                skyboxMaterials[i] = skyboxMat;
            }
            else
            {
                Debug.LogWarning($"Skybox texture at index {i} is null!");
            }
        }
    }

    public void NextSkybox()
    {
        if (skyboxMaterials != null && skyboxMaterials.Length > 0)
        {
            // Increment the index, and loop back to the beginning if necessary
            currentIndex = (currentIndex + 1) % skyboxMaterials.Length;

            // Change the skybox material
            if (skyboxMaterials[currentIndex] != null)
            {
                RenderSettings.skybox = skyboxMaterials[currentIndex];
                Debug.Log($"Skybox changed to index {currentIndex}");
            }
        }
    }

    // Called by the trigger detector
    public void OnPlayerEnteredTrigger()
    {
        NextSkybox();
    }
}

// Helper component for trigger detection
public class SkyboxTriggerDetector : MonoBehaviour
{
    [HideInInspector]
    public SkyboxChanger skyboxChanger;

    private void OnTriggerEnter(Collider other)
    {
        if (skyboxChanger != null && other.CompareTag(skyboxChanger.playerTag))
        {
            skyboxChanger.OnPlayerEnteredTrigger();
        }
    }
}
