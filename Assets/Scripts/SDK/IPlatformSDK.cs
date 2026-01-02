using System;
using UnityEngine;

/// <summary>
/// Platform SDK abstraction interface.
/// All platform-specific SDKs (Yandex, CrazyGames, etc.) must implement this interface.
/// </summary>
public interface IPlatformSDK
{
    /// <summary>
    /// Platform name for debugging/logging
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// Is the SDK initialized and ready to use?
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initialize the SDK. Must be called before any other SDK methods.
    /// </summary>
    void Initialize(Action onSuccess, Action<string> onError);

    // ===== AD METHODS =====

    /// <summary>
    /// Show interstitial (fullscreen) ad between gameplay moments
    /// </summary>
    void ShowInterstitialAd(Action onAdOpened, Action onAdClosed, Action<string> onAdError);

    /// <summary>
    /// Show rewarded video ad. Player must watch to completion to get reward.
    /// </summary>
    void ShowRewardedAd(Action onRewarded, Action onAdOpened, Action onAdClosed, Action<string> onAdError);

    /// <summary>
    /// Check if rewarded ads are available (some platforms may have limits)
    /// </summary>
    bool IsRewardedAdAvailable();

    // ===== LEADERBOARD METHODS =====

    /// <summary>
    /// Submit score to leaderboard
    /// </summary>
    void SubmitScore(int score, Action onSuccess, Action<string> onError);

    /// <summary>
    /// Get player's rank on leaderboard (optional, may not be supported by all platforms)
    /// </summary>
    void GetPlayerRank(Action<int> onSuccess, Action<string> onError);

    // ===== DATA PERSISTENCE (Optional, may be needed for cross-device sync) =====

    /// <summary>
    /// Save player data to cloud (optional feature)
    /// </summary>
    void SaveData(string key, string value, Action onSuccess, Action<string> onError);

    /// <summary>
    /// Load player data from cloud (optional feature)
    /// </summary>
    void LoadData(string key, Action<string> onSuccess, Action<string> onError);

    /// <summary>
    /// Get player's language from platform SDK (e.g., "en", "ru", "tr")
    /// </summary>
    string GetPlayerLanguage();

    /// <summary>
    /// Signal to the platform that the game has finished loading and is ready to play.
    /// Should be called when the game is fully loaded and playable.
    /// </summary>
    void GameReady();
}

