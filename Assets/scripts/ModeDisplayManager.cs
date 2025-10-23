using UnityEngine;

public static class ModeDisplayManager
{
    public static void DisplayModeText(string message)
    {
        // Log the message
        Debug.Log($"[MODE CHANGE] {message}");
        
        // Use the same direct approach that works in other scripts
        CognitiveFeedManager.Instance?.QueueMessage(message);
    }
}
