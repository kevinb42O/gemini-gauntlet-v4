using UnityEngine;
using System.Collections.Generic;

public class ProximityActivator : MonoBehaviour
{
    [Header("Activation Settings")]
    [Tooltip("The distance from the player at which the enemy will activate.")]
    public float activationRange = 50f;

    [Tooltip("The tag of the player GameObject.")]
    public string playerTag = "Player";

    [Header("Components to Activate")]
    [Tooltip("A list of components (e.g., AI scripts, Animators) that will be enabled on activation.")]
    public List<MonoBehaviour> scriptsToActivate = new List<MonoBehaviour>();

    [Tooltip("A list of GameObjects (e.g., weapon models, visual effects) that will be set to active.")]
    public List<GameObject> objectsToActivate = new List<GameObject>();

    private Transform playerTransform;
    private bool isActivated = false;

    void Start()
    {
        // Initially, deactivate all specified components and objects
        SetComponentsActive(false);

        // Find the player by tag
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError($"ProximityActivator: Could not find a GameObject with tag '{playerTag}'. The activator will not work.", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (isActivated || playerTransform == null) return;

        // Check the distance to the player
        if (Vector3.Distance(transform.position, playerTransform.position) <= activationRange)
        {
            Activate();
        }
    }

    private void Activate()
    {
        isActivated = true;
        SetComponentsActive(true);
        Debug.Log($"<color=yellow>ACTIVATED:</color> {gameObject.name} has emerged.", this);

        // Optionally, disable this script after activation to save a tiny bit of performance
        enabled = false;
    }

    private void SetComponentsActive(bool isActive)
    {
        foreach (var script in scriptsToActivate)
        {
            if (script != null) script.enabled = isActive;
        }

        foreach (var obj in objectsToActivate)
        {
            if (obj != null) obj.SetActive(isActive);
        }
    }

    // Draw a gizmo in the editor to visualize the activation range
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRange);
    }
}
