// --- START OF FILE DeathPlane.cs ---
using UnityEngine;

public class DeathPlane : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Make sure your player GameObject is tagged "Player"
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.Die();
            }
            else
            {
                Debug.LogError("Player hit DeathPlane, but no PlayerHealth component was found on the Player!", other.gameObject);
            }
        }
    }
}
// --- END OF FILE DeathPlane.cs ---