using UnityEngine;
using System.Collections;

public class ObjectDropController : MonoBehaviour
{
    [Header("Objects to Drop")]
    public GameObject[] objectsToDrop = new GameObject[7];

    [Header("Movement Settings")]
    public float dropDistance = 5f; // How far down each object moves
    public float moveSpeed = 2f; // How fast objects move down

    [Header("Delay Settings")]
    public float[] objectDelays = new float[7]; // Delay before each object starts moving

    private bool playerInRange = false;
    private bool objectsDropped = false;
    private Vector3[] originalPositions = new Vector3[7];
    private Coroutine dropCoroutine;

    private void Start()
    {
        // Store original positions
        for (int i = 0; i < objectsToDrop.Length; i++)
        {
            if (objectsToDrop[i] != null)
            {
                originalPositions[i] = objectsToDrop[i].transform.position;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !objectsDropped)
        {
            objectsDropped = true;
            dropCoroutine = StartCoroutine(DropObjects());
        }
    }

    private IEnumerator DropObjects()
    {
        // Drop objects down with delays
        for (int i = 0; i < objectsToDrop.Length; i++)
        {
            if (objectsToDrop[i] != null)
            {
                // Wait for the specified delay for this object
                yield return new WaitForSeconds(objectDelays[i]);

                // Start moving this object down
                StartCoroutine(MoveObjectDown(objectsToDrop[i], i));
            }
        }

        // Wait 5 seconds after all objects start dropping
        yield return new WaitForSeconds(5f);

        // Restore objects back to original positions with same delays
        for (int i = 0; i < objectsToDrop.Length; i++)
        {
            if (objectsToDrop[i] != null)
            {
                // Wait for the specified delay for this object (same as drop delay)
                yield return new WaitForSeconds(objectDelays[i]);

                // Start moving this object back up
                StartCoroutine(MoveObjectUp(objectsToDrop[i], i));
            }
        }

        objectsDropped = false; // Reset for next use
    }

    private IEnumerator MoveObjectDown(GameObject obj, int index)
    {
        Vector3 startPos = originalPositions[index];
        Vector3 endPos = startPos - new Vector3(0, dropDistance, 0);

        float elapsedTime = 0f;

        while (elapsedTime < 1f / moveSpeed)
        {
            if (obj != null)
            {
                obj.transform.position = Vector3.Lerp(startPos, endPos, elapsedTime * moveSpeed);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure object reaches final position
        if (obj != null)
        {
            obj.transform.position = endPos;
        }
    }

    private IEnumerator MoveObjectUp(GameObject obj, int index)
    {
        Vector3 startPos = originalPositions[index] - new Vector3(0, dropDistance, 0);
        Vector3 endPos = originalPositions[index];

        float elapsedTime = 0f;

        while (elapsedTime < 1f / moveSpeed)
        {
            if (obj != null)
            {
                obj.transform.position = Vector3.Lerp(startPos, endPos, elapsedTime * moveSpeed);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure object reaches final position
        if (obj != null)
        {
            obj.transform.position = endPos;
        }
    }
}
