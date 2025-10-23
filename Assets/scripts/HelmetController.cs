using UnityEngine;
using UnityEngine.UI;

public class HelmetController : MonoBehaviour
{
    [Header("Object References")]
    [Tooltip("The player's Rigidbody to get speed from.")]
    public Rigidbody playerRigidbody;
    [Tooltip("The Material of the FrostEffect UI Image.")]
    public Material frostMaterial;

    [Header("Cold Effect Parameters")]
    [Tooltip("Speed at which the frost is fully formed.")]
    public float maxSpeedForFrost = 100f;
    [Tooltip("How quickly the frost appears when over the speed limit.")]
    [Range(0.1f, 5f)] public float frostingSpeed = 1.0f;
    [Tooltip("How quickly the frost melts when under the speed limit.")]
    [Range(0.1f, 5f)] public float thawingSpeed = 2.0f;

    private float currentFrostAmount = 0f;
    private static readonly int FrostAmountID = Shader.PropertyToID("_FrostAmount");

    void Start() {
        if (playerRigidbody == null) Debug.LogError("Player Rigidbody not assigned in HelmetController!");
        if (frostMaterial == null) Debug.LogError("Frost Material not assigned in HelmetController!");
        if (frostMaterial != null) frostMaterial.SetFloat(FrostAmountID, 0);
    }

    void Update() {
        if (playerRigidbody == null || frostMaterial == null) return;
        float currentSpeed = playerRigidbody.linearVelocity.magnitude;
        float targetFrost = Mathf.InverseLerp(0, maxSpeedForFrost, currentSpeed);
        float transitionSpeed = (targetFrost > currentFrostAmount) ? frostingSpeed : thawingSpeed;
        currentFrostAmount = Mathf.MoveTowards(currentFrostAmount, targetFrost, transitionSpeed * Time.deltaTime);
        frostMaterial.SetFloat(FrostAmountID, currentFrostAmount);
    }

    public void ApplyUpgrade(float newMaxSpeed, float newFrostingSpeed, float newThawingSpeed) {
        this.maxSpeedForFrost = newMaxSpeed;
        this.frostingSpeed = newFrostingSpeed;
        this.thawingSpeed = newThawingSpeed;
    }
}
