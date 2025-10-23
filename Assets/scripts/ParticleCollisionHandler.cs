using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to VFX prefabs to create persistent particle decals on collision.
/// Works with both shotgun and stream VFX.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ParticleCollisionHandler : MonoBehaviour
{
    [Header("Decal Settings")]
    [Tooltip("Enable persistent decals on collision")]
    public bool createDecalsOnCollision = true;
    
    [Tooltip("Size multiplier for decal particles")]
    [Range(0.1f, 5f)]
    public float decalSizeMultiplier = 1f;
    
    [Tooltip("Only create decals on these layers")]
    public LayerMask decalLayers = -1; // All layers by default
    
    [Header("Performance")]
    [Tooltip("Maximum decals to create per frame (prevents lag spikes)")]
    public int maxDecalsPerFrame = 10;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private ParticleSystem ps;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    
    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        
        // Ensure collision is enabled
        var collision = ps.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.sendCollisionMessages = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ParticleCollisionHandler] Initialized on {name}");
        }
    }
    
    void OnParticleCollision(GameObject other)
    {
        if (!createDecalsOnCollision) return;
        if (ParticleDecalManager.Instance == null) return;
        
        // Get collision events
        int numCollisionEvents = ps.GetCollisionEvents(other, collisionEvents);
        
        // Limit decals per frame for performance
        int decalsToCreate = Mathf.Min(numCollisionEvents, maxDecalsPerFrame);
        
        for (int i = 0; i < decalsToCreate; i++)
        {
            ParticleCollisionEvent collisionEvent = collisionEvents[i];
            
            // Check layer mask
            int hitLayer = collisionEvent.colliderComponent.gameObject.layer;
            if (((1 << hitLayer) & decalLayers) == 0)
            {
                continue; // Skip this layer
            }
            
            // Get particle color
            Color particleColor = GetParticleColor();
            
            // Spawn persistent decal
            ParticleDecalManager.Instance.SpawnDecal(
                collisionEvent.intersection,
                collisionEvent.normal,
                particleColor,
                decalSizeMultiplier
            );
            
            if (enableDebugLogs)
            {
                Debug.Log($"[ParticleCollisionHandler] Created decal at {collisionEvent.intersection} on {other.name}");
            }
        }
    }
    
    private Color GetParticleColor()
    {
        // Get color from particle system
        var main = ps.main;
        
        if (main.startColor.mode == ParticleSystemGradientMode.Color)
        {
            return main.startColor.color;
        }
        else if (main.startColor.mode == ParticleSystemGradientMode.TwoColors)
        {
            // Return average of two colors
            return Color.Lerp(main.startColor.colorMin, main.startColor.colorMax, 0.5f);
        }
        else if (main.startColor.mode == ParticleSystemGradientMode.Gradient)
        {
            // Sample gradient at middle
            return main.startColor.gradient.Evaluate(0.5f);
        }
        
        // Fallback to white
        return Color.white;
    }
}
