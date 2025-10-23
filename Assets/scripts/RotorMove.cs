using UnityEngine;

// CORRECTED: The class name now matches the filename "RotorMove.cs"
public class RotorMove : MonoBehaviour
{
    [Tooltip("Speed of rotation in degrees per second.")]
    public float spinSpeed = 900.0f; // e.g., 900 degrees per second

    [Tooltip("The local axis around which the rotor will spin. For a typical top rotor, this is Y-axis (0,1,0). For a tail rotor, it might be X-axis (1,0,0) or Z-axis (0,0,1) depending on orientation.")]
    public Vector3 rotationAxis = Vector3.up; // Default to spinning around the local Y-axis

    // Update is called once per frame
    void Update()
    {
        // Ensure the rotationAxis is normalized if you want the speed to be strictly degrees/sec
        // Though for simple axes like Vector3.up, it's already normalized.
        Vector3 axisToRotateAround = rotationAxis.normalized;

        // Calculate rotation for this frame
        // Time.deltaTime ensures that the rotation is smooth and independent of the frame rate.
        float rotationThisFrame = spinSpeed * Time.deltaTime;

        // Rotate the transform of the GameObject this script is attached to.
        // Space.Self means the rotation is applied around the GameObject's local axes.
        transform.Rotate(axisToRotateAround, rotationThisFrame, Space.Self);
    }
}