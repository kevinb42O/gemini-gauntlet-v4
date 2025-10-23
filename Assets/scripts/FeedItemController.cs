// --- FeedItemController.cs (NEW SCRIPT - Add this to your project) ---
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FeedItemController : MonoBehaviour
{
    [Header("UI Element References")]
    [Tooltip("The TextMeshProUGUI component that displays the message.")]
    public TextMeshProUGUI notificationText;
    [Tooltip("The Image component for the icon.")]
    public Image iconImage;

    [Header("Padding Control")]
    [Tooltip("The LayoutElement used for top padding. This object's Preferred Height will be changed.")]
    public LayoutElement topPaddingElement;

    [Header("Padding & Sizing Values")]
    [Tooltip("The height of the top padding for MAJOR events.")]
    public float majorEventTopPadding = 12f;
    [Tooltip("The height of the top padding for MINOR events.")]
    public float minorEventTopPadding = 2f;

    [Header("Animation")]
    [Tooltip("The CanvasGroup for fading the item in and out.")]
    public CanvasGroup canvasGroup;
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.4f;
    public float fadeOutDelay = 0.5f; // Extra delay after the main message duration before fading

    private float _currentDuration;
    private Coroutine _lifecycleCoroutine;
    private bool _skipNextFadeIn = false;

    public void UpdateMessage(string newMessage)
    {
        if (notificationText != null)
        {
            notificationText.text = newMessage;
        }
    }

    public void ResetDuration(float newDuration)
    {
        _currentDuration = newDuration;
        // Avoid replaying fade-in when extending the lifetime repeatedly (prevents flicker)
        _skipNextFadeIn = true;
        if (_lifecycleCoroutine != null)
        {
            StopCoroutine(_lifecycleCoroutine);
        }
        _lifecycleCoroutine = StartCoroutine(LifecycleRoutine());
    }

    public void Initialize(string message, Color color, Sprite icon, float iconSize, float duration, bool isMajor)
    {
        // Set text content & style
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.color = color;
        }

        // Set icon
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
            if (icon != null)
            {
                iconImage.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            }
        }

        // Set padding based on event type
        if (topPaddingElement != null)
        {
            topPaddingElement.preferredHeight = isMajor ? majorEventTopPadding : minorEventTopPadding;
        }

        // Start lifecycle
        _currentDuration = duration;
        if (_lifecycleCoroutine != null)
        {
            StopCoroutine(_lifecycleCoroutine);
        }
        _lifecycleCoroutine = StartCoroutine(LifecycleRoutine());
    }

    private IEnumerator LifecycleRoutine()
    {
        // Fade in (optionally skipped when extending duration to prevent flicker)
        if (canvasGroup != null)
        {
            if (_skipNextFadeIn)
            {
                _skipNextFadeIn = false;
                canvasGroup.alpha = 1f;
            }
            else
            {
                canvasGroup.alpha = 0f;
                float timer = 0f;
                while (timer < fadeInDuration)
                {
                    timer += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeInDuration);
                    yield return new WaitForEndOfFrame();
                }
                canvasGroup.alpha = 1f;
            }
        }

        // Wait for duration
        yield return new WaitForSeconds(_currentDuration);

        // Wait for fade out delay
        yield return new WaitForSeconds(fadeOutDelay);

        // Fade out
        if (canvasGroup != null)
        {
            float timer = 0f;
            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeOutDuration);
                yield return new WaitForEndOfFrame();
            }
            canvasGroup.alpha = 0f;
        }

        // Destroy
        Destroy(gameObject);
    }

    public void ForceFadeOutAndDestroy()
    {
        if (_lifecycleCoroutine != null)
        {
            StopCoroutine(_lifecycleCoroutine);
        }
        StartCoroutine(ForceFadeOutRoutine());
    }

    private IEnumerator ForceFadeOutRoutine()
    {
        if (canvasGroup != null)
        {
            float timer = 0f;
            float startAlpha = canvasGroup.alpha;
            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeOutDuration);
                yield return new WaitForEndOfFrame();
            }
            canvasGroup.alpha = 0f;
        }
        Destroy(gameObject);
    }
}