// --- SnakeSegment.cs ---
using UnityEngine;

public class SnakeSegment : MonoBehaviour
{
    public bool isLethal = true;
    private string _playerTag = "Player";

    private void OnCollisionEnter(Collision collision)
    {
        if (isLethal && collision.gameObject.CompareTag(_playerTag))
        {
            Debug.Log("Player was killed by the snake!");
            // --- ADD YOUR PLAYER DEATH LOGIC HERE ---
            // Example:
            // collision.gameObject.GetComponent<PlayerHealth>().Die();
            // or for simplicity:
            // Destroy(collision.gameObject);
        }
    }
}