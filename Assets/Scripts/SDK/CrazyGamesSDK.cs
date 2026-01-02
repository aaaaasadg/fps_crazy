using System;
using UnityEngine;

/// <summary>
/// CrazyGames SDK implementation refactored to use the clean CrazyGamesHandler.
/// </summary>
public class CrazyGamesSDK : IPlatformSDK
{
    public string PlatformName => "CrazyGames";
    public bool IsInitialized => CrazyGamesHandler.Instance != null && CrazyGamesHandler.Instance.IsInitialized;

    // User data structure (required by UserInfoManager)
    [System.Serializable]
    public class CrazyGamesUser
    {
        public string id;
        public string username;
        public string avatarUrl;
        public bool isGuest;
    }

    public event Action<CrazyGamesUser> OnUserInfoReceived;

    public void Initialize(Action onSuccess, Action<string> onError)
    {
        if (CrazyGamesHandler.Instance == null)
        {
            GameObject go = new GameObject("CrazyGamesHandler");
            go.AddComponent<CrazyGamesHandler>();
        }
        
        // Connect to handler events
        CrazyGamesHandler.Instance.onUserInfoReceived += (user) => OnUserInfoReceived?.Invoke(user);

        onSuccess?.Invoke();
    }

    public void ShowInterstitialAd(Action onAdOpened, Action onAdClosed, Action<string> onAdError)
    {
        if (CrazyGamesHandler.Instance != null)
        {
            CrazyGamesHandler.Instance.RequestAd("midgame", onAdOpened, onAdClosed, onAdError);
        }
        else
        {
            onAdError?.Invoke("CrazyGamesHandler not found");
        }
    }

    public void ShowRewardedAd(Action onRewarded, Action onAdOpened, Action onAdClosed, Action<string> onAdError)
    {
        if (CrazyGamesHandler.Instance != null)
        {
            CrazyGamesHandler.Instance.RequestAd("rewarded", onAdOpened, () => {
                onRewarded?.Invoke();
                onAdClosed?.Invoke();
            }, onAdError);
        }
        else
        {
            onAdError?.Invoke("CrazyGamesHandler not found");
        }
    }

    public bool IsRewardedAdAvailable()
    {
        return IsInitialized;
    }

    public void SubmitScore(int score, Action onSuccess, Action<string> onError)
    {
        if (CrazyGamesHandler.Instance != null) {
            CrazyGamesHandler.Instance.AddScore(score);
            onSuccess?.Invoke();
        } else {
            onError?.Invoke("CrazyGamesHandler not found");
        }
    }

    public void GetPlayerRank(Action<int> onSuccess, Action<string> onError)
    {
        // CrazyGames doesn't provide a direct rank-fetching API in v3 yet.
        onError?.Invoke("Not supported");
    }

    public void SaveData(string key, string value, Action onSuccess, Action<string> onError)
    {
        if (CrazyGamesHandler.Instance != null)
            CrazyGamesHandler.Instance.SaveData(key, value, onSuccess, onError);
        else
            onError?.Invoke("CrazyGamesHandler not found");
    }

    public void LoadData(string key, Action<string> onSuccess, Action<string> onError)
    {
        if (CrazyGamesHandler.Instance != null)
            CrazyGamesHandler.Instance.LoadData(key, onSuccess, onError);
        else
            onError?.Invoke("CrazyGamesHandler not found");
    }

    public string GetPlayerLanguage()
    {
        return "en";
    }

    public void GameReady()
    {
        if (CrazyGamesHandler.Instance != null)
        {
            CrazyGamesHandler.Instance.LoadingStop();
        }
    }

    // Additional CrazyGames Specific methods used by other managers

    public void PrefetchAd(string type)
    {
        if (CrazyGamesHandler.Instance != null)
            CrazyGamesHandler.Instance.PrefetchAd(type);
    }

    public void CallLoadingStart()
    {
        if (CrazyGamesHandler.Instance != null)
            CrazyGamesHandler.Instance.LoadingStart();
    }

    public void CallLoadingStop()
    {
        if (CrazyGamesHandler.Instance != null)
            CrazyGamesHandler.Instance.LoadingStop();
    }

    public void CallHappyTime()
    {
        if (CrazyGamesHandler.Instance != null)
            CrazyGamesHandler.Instance.HappyTime();
    }

    public bool IsUserAccountAvailable()
    {
        if (CrazyGamesHandler.Instance != null)
            return CrazyGamesHandler.Instance.IsUserAccountAvailable();
        return false;
    }

    public void GetUser(Action<CrazyGamesUser> onSuccess, Action<string> onError)
    {
        if (CrazyGamesHandler.Instance != null)
            CrazyGamesHandler.Instance.GetUser(onSuccess, onError);
        else
            onError?.Invoke("CrazyGamesHandler not found");
    }

    public void ShowAuthPrompt(Action<CrazyGamesUser> onSuccess, Action<string> onError)
    {
        if (CrazyGamesHandler.Instance != null)
            CrazyGamesHandler.Instance.ShowAuthPrompt(onSuccess, onError);
        else
            onError?.Invoke("CrazyGamesHandler not found");
    }
}
