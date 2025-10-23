// --- KeepUpright.cs ---
using UnityEngine;

/// <summary>
/// Attach this script to a child object to force it to ignore its
/// parent's rotation and remain perfectly upright relative to the world.
/// </summary>
public class KeepUpright : MonoBehaviour
{
    // Using LateUpdate ensures that we modify the rotation *after* all physics
    // and parent movements have been calculated for the frame, preventing jitter.
    void LateUpdate()
    {
        // Force this object's rotation to be the default world rotation (no rotation).
        transform.rotation = Quaternion.identity;
    }
}