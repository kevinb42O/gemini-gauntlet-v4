using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quick debug UI to display real-time movement metrics.
/// Shows: Current speed (walk/sprint), jump height, and jump duration.
/// TEMPORARY - Remove after testing.
/// </summary>
public class MovementDebugUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AAAMovementController movementController;
    [SerializeField] private CharacterController characterController;
    
    [Header("UI Settings")]
    [SerializeField] private bool showUI = true;
    [SerializeField] private int fontSize = 18;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
    
    // Jump tracking
    private bool wasGrounded = true;
    private float jumpStartTime = 0f;
    private float jumpStartHeight = 0f;
    private float maxJumpHeight = 0f;
    private float lastJumpDuration = 0f;
    private float lastJumpHeight = 0f;
    
    // Speed tracking
    private float currentSpeed = 0f;
    private float currentHorizontalSpeed = 0f;
    private bool isSprinting = false;
    
    // UI Style
    private GUIStyle labelStyle;
    private GUIStyle boxStyle;
    
    void Awake()
    {
        // Auto-find references if not assigned
        if (movementController == null)
            movementController = GetComponent<AAAMovementController>();
        
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }
    
    void Update()
    {
        if (!showUI) return;
        
        TrackSpeed();
        TrackJump();
    }
    
    void TrackSpeed()
    {
        if (characterController == null) return;
        
        // Get current velocity
        Vector3 velocity = characterController.velocity;
        
        // Calculate horizontal speed (ignoring vertical)
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        currentHorizontalSpeed = horizontalVelocity.magnitude;
        
        // Calculate total speed
        currentSpeed = velocity.magnitude;
        
        // Detect sprint (simple heuristic: if moving faster than walk speed threshold)
        // You can adjust this threshold based on your movement settings
        isSprinting = currentHorizontalSpeed > 1000f; // Adjust threshold as needed
    }
    
    void TrackJump()
    {
        if (movementController == null) return;
        
        bool isGrounded = movementController.IsGrounded;
        float currentHeight = transform.position.y;
        
        // Detect jump start (transition from grounded to airborne)
        if (wasGrounded && !isGrounded)
        {
            jumpStartTime = Time.time;
            jumpStartHeight = currentHeight;
            maxJumpHeight = 0f;
        }
        
        // Track max height while airborne
        if (!isGrounded)
        {
            float currentJumpHeight = currentHeight - jumpStartHeight;
            if (currentJumpHeight > maxJumpHeight)
            {
                maxJumpHeight = currentJumpHeight;
            }
        }
        
        // Detect landing (transition from airborne to grounded)
        if (!wasGrounded && isGrounded)
        {
            lastJumpDuration = Time.time - jumpStartTime;
            lastJumpHeight = maxJumpHeight;
        }
        
        wasGrounded = isGrounded;
    }
    
    void OnGUI()
    {
        if (!showUI) return;
        
        // Initialize styles
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = fontSize;
            labelStyle.normal.textColor = textColor;
            labelStyle.padding = new RectOffset(10, 10, 5, 5);
        }
        
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, backgroundColor);
        }
        
        // Position in top-left corner
        float x = 10f;
        float y = 10f;
        float width = 400f;
        float height = 180f;
        
        // Draw background box
        GUI.Box(new Rect(x, y, width, height), "", boxStyle);
        
        // Draw text
        float lineHeight = fontSize + 8;
        float textY = y + 10;
        
        GUI.Label(new Rect(x, textY, width, lineHeight), 
            "<b>=== MOVEMENT DEBUG ===</b>", labelStyle);
        textY += lineHeight;
        
        // Speed info
        string speedType = isSprinting ? "<color=yellow>SPRINT</color>" : "<color=cyan>WALK</color>";
        GUI.Label(new Rect(x, textY, width, lineHeight), 
            $"Speed Type: {speedType}", labelStyle);
        textY += lineHeight;
        
        GUI.Label(new Rect(x, textY, width, lineHeight), 
            $"Horizontal Speed: <b>{currentHorizontalSpeed:F1}</b> units/s", labelStyle);
        textY += lineHeight;
        
        GUI.Label(new Rect(x, textY, width, lineHeight), 
            $"Total Speed: <b>{currentSpeed:F1}</b> units/s", labelStyle);
        textY += lineHeight;
        
        // Jump info
        bool isGrounded = movementController != null && movementController.IsGrounded;
        string groundedStatus = isGrounded ? "<color=lime>GROUNDED</color>" : "<color=orange>AIRBORNE</color>";
        GUI.Label(new Rect(x, textY, width, lineHeight), 
            $"Status: {groundedStatus}", labelStyle);
        textY += lineHeight;
        
        if (!isGrounded)
        {
            float currentJumpTime = Time.time - jumpStartTime;
            float currentJumpHeight = transform.position.y - jumpStartHeight;
            
            GUI.Label(new Rect(x, textY, width, lineHeight), 
                $"Jump Height: <b>{currentJumpHeight:F1}</b> units (Max: {maxJumpHeight:F1})", labelStyle);
            textY += lineHeight;
            
            GUI.Label(new Rect(x, textY, width, lineHeight), 
                $"Jump Duration: <b>{currentJumpTime:F2}</b> seconds", labelStyle);
        }
        else
        {
            GUI.Label(new Rect(x, textY, width, lineHeight), 
                $"Last Jump Height: <b>{lastJumpHeight:F1}</b> units", labelStyle);
            textY += lineHeight;
            
            GUI.Label(new Rect(x, textY, width, lineHeight), 
                $"Last Jump Duration: <b>{lastJumpDuration:F2}</b> seconds", labelStyle);
        }
    }
    
    // Helper to create colored texture for background
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
