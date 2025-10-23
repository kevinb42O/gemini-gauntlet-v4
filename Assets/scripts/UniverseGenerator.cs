using UnityEngine;
using System.Collections.Generic;

// This class defines the layout for a single orbital system that we want to generate.
[System.Serializable]
public class SystemLayoutConfig
{
    public string systemName = "New Orbital System";
    public Vector3 position = Vector3.zero;
    
    [Header("Orbital Plane Control")]
    [Tooltip("Tilt angle of the entire orbital system. 0° = horizontal orbit, 90° = vertical orbit")]
    [Range(0f, 90f)]
    public float orbitalTiltAngle = 0f;
    
    [Tooltip("Direction of the tilt. X-axis tilt makes platforms orbit up/down, Z-axis tilt makes them orbit left/right")]
    public enum TiltAxis { XAxis, ZAxis }
    public TiltAxis tiltAxis = TiltAxis.XAxis;
    
    [Tooltip("The orbital tiers for this specific system.")]
    public List<OrbitalTierConfig> orbitalTiers = new List<OrbitalTierConfig>();
}

// This manager will create all the orbital systems in the scene at startup.
public class UniverseGenerator : MonoBehaviour
{
    [Tooltip("A list of all the orbital systems you want to generate in the scene.")]
    public List<SystemLayoutConfig> systemsToGenerate = new List<SystemLayoutConfig>();

    void Awake()
    {
        GenerateUniverses();
    }

    void GenerateUniverses()
    {
        if (systemsToGenerate == null || systemsToGenerate.Count == 0)
        {
            Debug.LogWarning("UniverseGenerator: No systems are configured to be generated.", this);
            return;
        }

        foreach (var systemConfig in systemsToGenerate)
        {
            // Create a new parent GameObject for the system at the specified position.
            GameObject systemGO = new GameObject(systemConfig.systemName);
            systemGO.transform.position = systemConfig.position;
            // Parent it to this generator to keep the hierarchy clean.
            systemGO.transform.SetParent(this.transform);

            // Add the OrbitalSystem component to our new GameObject.
            OrbitalSystem newSystem = systemGO.AddComponent<OrbitalSystem>();

            // Copy the tier configurations from our layout to the new system.
            // We create a new list to ensure it's a copy, not a reference.
            newSystem.orbitalTiers = new List<OrbitalTierConfig>(systemConfig.orbitalTiers);
            
            // Apply orbital tilt settings to the new system
            newSystem.orbitalTiltAngle = systemConfig.orbitalTiltAngle;
            newSystem.tiltAxis = systemConfig.tiltAxis;

            // The OrbitalSystem's Start() method will handle the rest of the setup.
            Debug.Log($"<color=cyan>Generated System:</color> '{systemConfig.systemName}' at position {systemConfig.position}", systemGO);
        }
    }
}
