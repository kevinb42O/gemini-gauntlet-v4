using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages video intro playback and automatic transition to menu scene
/// Attach this to a GameObject with a VideoPlayer component
/// </summary>
public class VideoIntroManager : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip introVideoClip;
    
    [Header("Timing")]
    [SerializeField] private float videoDuration = 8.0f;
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private KeyCode skipKey = KeyCode.Space;
    
    [Header("Scene Transition")]
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private bool fadeToBlack = true;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool muteVideoAudio = false;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private bool hasVideoStarted = false;
    private bool isTransitioning = false;
    private CanvasGroup fadeCanvas;
    
    void Awake()
    {
        // Auto-find VideoPlayer if not assigned
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = FindFirstObjectByType<VideoPlayer>();
            }
        }
        
        // Auto-find AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        // Create fade canvas if fade is enabled
        if (fadeToBlack)
        {
            CreateFadeCanvas();
        }
    }
    
    void Start()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[VideoIntroManager] Starting video intro. Duration: {videoDuration}s, Menu Scene: {menuSceneName}");
        }
        
        SetupVideoPlayer();
        StartVideoPlayback();
    }
    
    void Update()
    {
        // Check for skip input
        if (allowSkip && !isTransitioning && Input.GetKeyDown(skipKey))
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[VideoIntroManager] Video skipped by user pressing {skipKey}");
            }
            TransitionToMenu();
        }
        
        // Check if video has finished naturally
        if (hasVideoStarted && videoPlayer != null && !videoPlayer.isPlaying && !isTransitioning)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[VideoIntroManager] Video finished playing naturally");
            }
            TransitionToMenu();
        }
    }
    
    private void SetupVideoPlayer()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("[VideoIntroManager] No VideoPlayer found! Please assign one or add VideoPlayer component.");
            TransitionToMenu(); // Fallback to menu if no video player
            return;
        }
        
        // Configure video player
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        
        // Set video clip if assigned
        if (introVideoClip != null)
        {
            videoPlayer.clip = introVideoClip;
        }
        
        // Configure audio
        if (muteVideoAudio)
        {
            videoPlayer.SetDirectAudioMute(0, true);
        }
        
        // Set up event callbacks
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.started += OnVideoStarted;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
    }
    
    private void StartVideoPlayback()
    {
        if (videoPlayer == null) return;
        
        if (enableDebugLogs)
        {
            Debug.Log("[VideoIntroManager] Preparing video playback...");
        }
        
        // Prepare and play video
        videoPlayer.Prepare();
        
        // Start timer as backup (in case video events fail)
        StartCoroutine(VideoTimeoutCoroutine());
    }
    
    private IEnumerator VideoTimeoutCoroutine()
    {
        yield return new WaitForSeconds(videoDuration + 1.0f); // Add 1 second buffer
        
        if (!isTransitioning)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[VideoIntroManager] Video timeout reached, forcing transition to menu");
            }
            TransitionToMenu();
        }
    }
    
    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (enableDebugLogs)
        {
            Debug.Log("[VideoIntroManager] Video prepared, starting playback");
        }
        vp.Play();
    }
    
    private void OnVideoStarted(VideoPlayer vp)
    {
        hasVideoStarted = true;
        if (enableDebugLogs)
        {
            Debug.Log("[VideoIntroManager] Video playback started");
        }
    }
    
    private void OnVideoFinished(VideoPlayer vp)
    {
        if (enableDebugLogs)
        {
            Debug.Log("[VideoIntroManager] Video reached loop point (finished)");
        }
        TransitionToMenu();
    }
    
    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"[VideoIntroManager] Video error: {message}");
        TransitionToMenu(); // Fallback to menu on error
    }
    
    private void TransitionToMenu()
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[VideoIntroManager] Transitioning to menu scene: {menuSceneName}");
        }
        
        // Stop video if playing
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        // Fade out if enabled
        if (fadeToBlack && fadeCanvas != null)
        {
            StartCoroutine(FadeOutAndLoadScene());
        }
        else
        {
            LoadMenuScene();
        }
    }
    
    private IEnumerator FadeOutAndLoadScene()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeOutDuration);
            fadeCanvas.alpha = alpha;
            yield return null;
        }
        
        fadeCanvas.alpha = 1f;
        yield return new WaitForSeconds(0.1f); // Brief pause on black
        
        LoadMenuScene();
    }
    
    private void LoadMenuScene()
    {
        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogError("[VideoIntroManager] Menu scene name is empty! Cannot transition.");
            return;
        }
        
        try
        {
            SceneManager.LoadScene(menuSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VideoIntroManager] Failed to load scene '{menuSceneName}': {e.Message}");
        }
    }
    
    private void CreateFadeCanvas()
    {
        // Create fade canvas GameObject
        GameObject fadeCanvasGO = new GameObject("FadeCanvas");
        fadeCanvasGO.transform.SetParent(transform);
        
        // Add Canvas component
        Canvas canvas = fadeCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // High order to be on top
        
        // Add CanvasScaler for responsive design
        var canvasScaler = fadeCanvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        
        // Add GraphicRaycaster
        fadeCanvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Create black image for fade
        GameObject fadeImageGO = new GameObject("FadeImage");
        fadeImageGO.transform.SetParent(fadeCanvasGO.transform);
        
        var rectTransform = fadeImageGO.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        var image = fadeImageGO.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        
        // Add CanvasGroup for fade control
        fadeCanvas = fadeCanvasGO.AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 0f;
        fadeCanvas.interactable = false;
        fadeCanvas.blocksRaycasts = false;
    }
    
    void OnDestroy()
    {
        // Cleanup event subscriptions
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.started -= OnVideoStarted;
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }
    
    // Public methods for external control
    public void SkipVideo()
    {
        if (!isTransitioning)
        {
            TransitionToMenu();
        }
    }
    
    public void SetMenuScene(string sceneName)
    {
        menuSceneName = sceneName;
    }
    
    public bool IsVideoPlaying()
    {
        return videoPlayer != null && videoPlayer.isPlaying;
    }
}
