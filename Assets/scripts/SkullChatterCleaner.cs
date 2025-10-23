using UnityEngine;

/// <summary>
/// Empty placeholder class - all skull sound cleaning functionality removed
/// This file can be safely deleted
/// </summary>
public class SkullChatterCleaner : MonoBehaviour
{
    public static SkullChatterCleaner Instance { get; private set; }
    
    // Placeholder methods to prevent compile errors
    public void RegisterSkull(Transform skullTransform) { }
    public void RegisterSkull(SkullEnemy skullEnemy) { }
    public void UnregisterSkull(Transform skullTransform) { }
    public void UnregisterSkull(SkullEnemy skullEnemy) { }
    public void ForceStopSkullSounds(Transform skullTransform) { }
    public void ForceCleanupAllSkullSounds() { }
}
