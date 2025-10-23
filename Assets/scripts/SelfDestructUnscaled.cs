// --- SelfDestructUnscaled.cs (NEW SCRIPT) ---
using UnityEngine;
using System.Collections;

public class SelfDestructUnscaled : MonoBehaviour
{
    public void StartSelfDestruct(float delay)
    {
        StartCoroutine(DestroyAfterUnscaledDelay(delay));
    }

    private IEnumerator DestroyAfterUnscaledDelay(float delay)
    {
        float timer = 0f;
        while (timer < delay)
        {
            timer += Time.unscaledDeltaTime; // Use unscaled time
            yield return new WaitForEndOfFrame(); // Wait for next frame
        }
        Destroy(gameObject);
    }
}