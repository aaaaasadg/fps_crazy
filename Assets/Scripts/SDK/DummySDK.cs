using System;
using UnityEngine;

/// <summary>
/// Dummy SDK implementation for testing in Unity Editor or as fallback.
/// Simulates SDK behavior without actual platform calls.
/// </summary>
public class DummySDK : IPlatformSDK
{
    private string platformName;
    private bool isInitialized;

    public string PlatformName => platformName;
    public bool IsInitialized => isInitialized;

    public DummySDK(string name)
    {
        platformName = name;
        isInitialized = false;
    }

    public void Initialize(Action onSuccess, Action<string> onError)
    {
        Debug.Log($"[{platformName} DummySDK] Initialize called");
        isInitialized = true;
        onSuccess?.Invoke();
    }

    public void ShowInterstitialAd(Action onAdOpened, Action onAdClosed, Action<string> onAdError)
    {
        Debug.Log($"[{platformName} DummySDK] ShowInterstitialAd called (simulating ad)");
        onAdOpened?.Invoke();
        // Simulate ad duration
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            Debug.Log($"[{platformName} DummySDK] Interstitial ad finished");
            onAdClosed?.Invoke();
        }, 1f);
    }

    public void ShowRewardedAd(Action onRewarded, Action onAdOpened, Action onAdClosed, Action<string> onAdError)
    {
        Debug.Log($"[{platformName} DummySDK] ShowRewardedAd called (simulating ad + reward)");
        onAdOpened?.Invoke();
        // Simulate ad duration and reward
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            Debug.Log($"[{platformName} DummySDK] Rewarded ad finished - Granting reward");
            onRewarded?.Invoke();
            onAdClosed?.Invoke();
        }, 1.5f);
    }

    public bool IsRewardedAdAvailable()
    {
        return true; // Always available in dummy
    }

    public void SubmitScore(int score, Action onSuccess, Action<string> onError)
    {
        Debug.Log($"[{platformName} DummySDK] SubmitScore called: {score}");
        onSuccess?.Invoke();
    }

    public void GetPlayerRank(Action<int> onSuccess, Action<string> onError)
    {
        Debug.Log($"[{platformName} DummySDK] GetPlayerRank called (returning dummy rank 42)");
        onSuccess?.Invoke(42);
    }

    public void SaveData(string key, string value, Action onSuccess, Action<string> onError)
    {
        Debug.Log($"[{platformName} DummySDK] SaveData called: {key} = {value}");
        PlayerPrefs.SetString($"Dummy_{key}", value);
        onSuccess?.Invoke();
    }

    public void LoadData(string key, Action<string> onSuccess, Action<string> onError)
    {
        Debug.Log($"[{platformName} DummySDK] LoadData called: {key}");
        string value = PlayerPrefs.GetString($"Dummy_{key}", "");
        onSuccess?.Invoke(value);
    }

    public string GetPlayerLanguage()
    {
        Debug.Log($"[{platformName} DummySDK] GetPlayerLanguage called (returning 'en')");
        return "en"; // Default to English in Editor
    }

    public void GameReady()
    {
        Debug.Log($"[{platformName} DummySDK] GameReady called (no-op in dummy)");
    }
}

