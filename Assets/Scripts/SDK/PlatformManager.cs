using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Central manager for platform SDK. 
/// Detects which platform the game is running on and provides the appropriate SDK instance.
/// </summary>
public class PlatformManager : MonoBehaviour
{
    public static PlatformManager Instance { get; private set; }

    [Header("Platform Detection")]
    [SerializeField] private PlatformType forcePlatform = PlatformType.Auto;

    private IPlatformSDK currentSDK;
    private PlatformType detectedPlatform;
    private bool isInitialized = false;
    private bool gameReadyCalled = false;

    public enum PlatformType
    {
        Auto,       // Detect automatically
        CrazyGames,
        Editor      // For testing in Unity Editor
    }

    // Public API
    public IPlatformSDK SDK => currentSDK;
    public bool IsInitialized => isInitialized;
    public PlatformType CurrentPlatform => detectedPlatform;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Initialize platform in coroutine to avoid blocking
        StartCoroutine(InitializePlatformCoroutine());
    }

    private IEnumerator InitializePlatformCoroutine()
    {
        // Wait a frame to ensure everything is loaded (non-blocking)
        yield return null;

        // Detect platform
        if (forcePlatform != PlatformType.Auto)
        {
            detectedPlatform = forcePlatform;
            Debug.Log($"[PlatformManager] Platform forced to: {detectedPlatform}");
        }
        else
        {
            detectedPlatform = DetectPlatform();
            Debug.Log($"[PlatformManager] Platform detected: {detectedPlatform}");
        }

        // Create appropriate SDK instance
        switch (detectedPlatform)
        {
            case PlatformType.CrazyGames:
                currentSDK = new CrazyGamesSDK();
                Debug.Log("[PlatformManager] Using CrazyGames SDK");
                break;

            case PlatformType.Editor:
                Debug.Log("[PlatformManager] Running in Editor mode with dummy SDK.");
                currentSDK = new DummySDK("Editor");
                break;

            default:
                Debug.LogWarning("[PlatformManager] Unknown platform. Using dummy SDK.");
                currentSDK = new DummySDK("Unknown");
                break;
        }

        // Wait another frame before initializing SDK (ensures game is responsive)
        yield return null;

        // Initialize the SDK (non-blocking, uses callbacks)
        currentSDK.Initialize(
            onSuccess: OnSDKInitialized,
            onError: OnSDKInitializationError
        );
    }

    private PlatformType DetectPlatform()
    {
        // If not running on WebGL, assume Editor
        if (!Application.platform.ToString().Contains("WebGL"))
        {
            return PlatformType.Editor;
        }

        // Try to detect platform, but don't block if URL check fails
        try
        {
            string url = Application.absoluteURL.ToLower();
            if (url.Contains("crazygames"))
            {
                return PlatformType.CrazyGames;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlatformManager] URL detection failed: {e.Message}, defaulting to Editor");
        }

        // Default to Editor if on WebGL but can't detect
        return PlatformType.Editor;
    }

    private void OnSDKInitialized()
    {
        isInitialized = true;
        Debug.Log($"[PlatformManager] ===== SDK INITIALIZED: {currentSDK.PlatformName} =====");

        // Language detection is now handled by browser language in LocalizationManager
        // No SDK language detection needed for CrazyGames

        // Prefetch ads for faster loading (non-blocking, deferred)
        if (currentSDK is CrazyGamesSDK crazySDK)
        {
            // Defer prefetch to avoid blocking
            StartCoroutine(DeferredPrefetchAds(crazySDK));
        }

        // Signal that game is ready (CrazyGames uses gameplayStart instead, but GameReady is part of interface)
        SignalGameReady();
    }

    private IEnumerator DeferredPrefetchAds(CrazyGamesSDK crazySDK)
    {
        // Wait a few frames before prefetching to ensure game is responsive
        yield return null;
        yield return null;
        
        try
        {
            crazySDK.PrefetchAd("midgame");
            crazySDK.PrefetchAd("rewarded");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlatformManager] Ad prefetch failed: {e.Message}");
        }
    }

    /// <summary>
    /// Signal to the platform that the game is ready to play.
    /// Called automatically after SDK initialization, but can also be called manually if needed.
    /// </summary>
    public void SignalGameReady()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[PlatformManager] Cannot signal GameReady - SDK not initialized yet");
            return;
        }

        if (gameReadyCalled)
        {
            Debug.LogWarning("[PlatformManager] GameReady already called, skipping");
            return;
        }

        Debug.Log("[PlatformManager] ===== SIGNALING GAME READY TO PLATFORM =====");
        gameReadyCalled = true;
        
        if (currentSDK != null)
        {
            currentSDK.GameReady();
        }
    }

    private void OnSDKInitializationError(string error)
    {
        Debug.LogError($"[PlatformManager] SDK initialization failed: {error}");
        // Fallback to dummy SDK
        currentSDK = new DummySDK("Fallback");
        isInitialized = true;
    }

    // ===== CONVENIENCE METHODS (Wrapper around SDK) =====

    /// <summary>
    /// Shows interstitial ad (kept for SDK compatibility, but not used in-game).
    /// </summary>
    public void ShowInterstitialAd(Action onAdClosed = null, Action<string> onAdError = null, bool showCountdown = true)
    {
        if (CrazyGamesHandler.Instance != null)
        {
            CrazyGamesHandler.Instance.RequestAd("midgame", 
                onStarted: OnAdOpened, 
                onFinished: () => { OnAdClosed(); onAdClosed?.Invoke(); }, 
                onError: (error) => { OnAdError(error); onAdError?.Invoke(error); }
            );
            return;
        }

        if (!isInitialized)
        {
            Debug.LogWarning("[PlatformManager] SDK not initialized yet!");
            onAdError?.Invoke("SDK not initialized");
            return;
        }

        currentSDK.ShowInterstitialAd(
            onAdOpened: OnAdOpened,
            onAdClosed: () => { 
                OnAdClosed();
                onAdClosed?.Invoke();
            },
            onAdError: (error) => { 
                OnAdError(error); 
                onAdError?.Invoke(error); 
            }
        );
    }

    public void ShowRewardedAd(Action onRewarded, Action onAdClosed = null, Action<string> onAdError = null)
    {
        if (CrazyGamesHandler.Instance != null)
        {
            CrazyGamesHandler.Instance.RequestAd("rewarded", 
                onStarted: OnAdOpened, 
                onFinished: () => { 
                    onRewarded?.Invoke();
                    OnAdClosed(); 
                    onAdClosed?.Invoke(); 
                }, 
                onError: (error) => { OnAdError(error); onAdError?.Invoke(error); }
            );
            return;
        }

        if (!isInitialized)
        {
            Debug.LogWarning("[PlatformManager] SDK not initialized yet!");
            onAdError?.Invoke("SDK not initialized");
            return;
        }

        currentSDK.ShowRewardedAd(
            onRewarded: onRewarded,
            onAdOpened: OnAdOpened,
            onAdClosed: () => { OnAdClosed(); onAdClosed?.Invoke(); },
            onAdError: (error) => { OnAdError(error); onAdError?.Invoke(error); }
        );
    }

    public void SubmitScore(int score, Action onSuccess = null, Action<string> onError = null)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[PlatformManager] SDK not initialized yet!");
            onError?.Invoke("SDK not initialized");
            return;
        }

        currentSDK.SubmitScore(score, onSuccess, onError);
    }

    /// <summary>
    /// Gets the player's language from the platform SDK.
    /// Returns language code: "en", "ru", "tr", "be", "kk", "uk", "uz"
    /// </summary>
    public string GetPlayerLanguage()
    {
        if (!isInitialized || currentSDK == null)
        {
            Debug.LogWarning("[PlatformManager] SDK not initialized, returning default language");
            return "en";
        }

        return currentSDK.GetPlayerLanguage();
    }

    // ===== AD EVENT HANDLERS (Pause/Resume Game) =====

    private void OnAdOpened()
    {
        Debug.Log("[PlatformManager] Ad opened");
        // Logic handled by CrazyGamesHandler for CrazyGames
        // If not CrazyGames, we can add platform-specific logic here
    }

    private void OnAdClosed()
    {
        Debug.Log("[PlatformManager] Ad closed");
    }

    private void OnAdError(string error)
    {
        Debug.LogWarning($"[PlatformManager] Ad error: {error}");
    }
}

