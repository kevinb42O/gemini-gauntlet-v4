// --- UniverseTrigger.cs ---
using UnityEngine;

public class UniverseTrigger : MonoBehaviour
{
    public SnakeController snake; // Drag your Snake_Head object here in the inspector
    private string _playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (snake != null && other.CompareTag(_playerTag))
        {
            snake.EngagePlayer(other.transform);
        }
    }
}