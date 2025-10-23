// --- IDamageable.cs (CORRECTED) ---
using UnityEngine;

// Contract: any object that can take damage via raycast hits must implement this.
public interface IDamageable
{
    // amount: damage value applied
    // hitPoint: exact world-space point hit
    // hitDirection: direction the ray came from (normally weapon forward)
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection);
}