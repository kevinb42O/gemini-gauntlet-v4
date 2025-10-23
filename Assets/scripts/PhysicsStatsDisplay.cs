using UnityEngine;

/// <summary>
/// Display physics stats in real-time to verify arena calculations
/// Add to player to see speed, height, and momentum data
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PhysicsStatsDisplay : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 lastPosition;
    private float currentSpeed;
    private float peakSpeed;
    private float currentHeight;
    private float peakHeight;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }
    
    void Update()
    {
        // Calculate current speed
        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        currentSpeed = velocity.magnitude;
        
        if (currentSpeed > peakSpeed)
            peakSpeed = currentSpeed;
        
        // Track height
        currentHeight = transform.position.y;
        if (currentHeight > peakHeight)
            peakHeight = currentHeight;
        
        lastPosition = transform.position;
        
        // Reset peak speed on ground
        if (controller.isGrounded && Input.GetKeyDown(KeyCode.R))
        {
            peakSpeed = 0;
            peakHeight = 0;
            Debug.Log("🔄 Stats reset");
        }
    }
    
    void OnGUI()
    {
        int x = Screen.width - 320;
        int y = 10;
        
        GUI.Box(new Rect(x, y, 310, 260), "⚡ PHYSICS STATS");
        
        // Current stats
        GUI.Label(new Rect(x + 10, y + 25, 290, 20), $"Speed: {currentSpeed:F0} u/s");
        GUI.Label(new Rect(x + 10, y + 45, 290, 20), $"Peak Speed: {peakSpeed:F0} u/s");
        GUI.Label(new Rect(x + 10, y + 65, 290, 20), $"Height: {currentHeight:F0} units");
        GUI.Label(new Rect(x + 10, y + 85, 290, 20), $"Peak Height: {peakHeight:F0} units");
        
        // Reference values
        GUI.Label(new Rect(x + 10, y + 110, 290, 20), "━━━ REFERENCE ━━━");
        GUI.Label(new Rect(x + 10, y + 130, 290, 20), "Sprint: 1485 u/s");
        GUI.Label(new Rect(x + 10, y + 150, 290, 20), "Sprint Jump: 1522 units");
        GUI.Label(new Rect(x + 10, y + 170, 290, 20), "Wall Jump Height: 321 units");
        GUI.Label(new Rect(x + 10, y + 190, 290, 20), "Character Height: 320 units");
        
        // Speed indicators
        if (currentSpeed > 2000)
            GUI.Label(new Rect(x + 10, y + 215, 290, 20), "🔥 MOMENTUM CHAINING!");
        else if (currentSpeed > 1485)
            GUI.Label(new Rect(x + 10, y + 215, 290, 20), "⚡ ABOVE SPRINT!");
        else if (currentSpeed > 900)
            GUI.Label(new Rect(x + 10, y + 215, 290, 20), "✓ Sprinting");
        
        GUI.Label(new Rect(x + 10, y + 235, 290, 20), "Press R to reset peaks");
    }
}
