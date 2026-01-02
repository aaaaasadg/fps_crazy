using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class CrazyGamesHandler : MonoBehaviour
{
    public static CrazyGamesHandler Instance { get; private set; }

    public bool IsInitialized { get; private set; }
    public bool HasAdblock { get; private set; }

    public event Action<CrazyGamesSDK.CrazyGamesUser> onUserInfoReceived;
    public event Action<bool> onAdblockDetected;

    private Action currentAdFinished;
    private Action<string> currentAdError;
    private Action currentAdStarted;

    private Action<CrazyGamesSDK.CrazyGamesUser> pendingUserSuccess;
    private Action<string> pendingUserError;
    private Action<CrazyGamesSDK.CrazyGamesUser> pendingAuthSuccess;
    private Action<string> pendingAuthError;

    [DllImport("__Internal")] private static extern void InitSDK();
    [DllImport("__Internal")] private static extern void RequestAdSDK(string type);
    [DllImport("__Internal")] private static extern void PrefetchAdSDK(string type);
    [DllImport("__Internal")] private static extern void RequestBannersSDK(string bannersJSON);
    [DllImport("__Internal")] private static extern void HappyTimeSDK();
    [DllImport("__Internal")] private static extern void GameplayStartSDK();
    [DllImport("__Internal")] private static extern void GameplayStopSDK();
    [DllImport("__Internal")] private static extern void LoadingStartSDK();
    [DllImport("__Internal")] private static extern void LoadingStopSDK();
    [DllImport("__Internal")] private static extern void GetUserSDK();
    [DllImport("__Internal")] private static extern void ShowAuthPromptSDK();
    [DllImport("__Internal")] private static extern int IsUserAccountAvailableSDK();
    [DllImport("__Internal")] private static extern void AddScoreSDK(int score);
    [DllImport("__Internal")] private static extern void DataSetItemSDK(string key, string value);
    [DllImport("__Internal")] private static extern string DataGetItemSDK(string key);
    [DllImport("__Internal")] private static extern void SyncUnityGameDataSDK();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            name = "CrazyGamesHandler"; 
        }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        InitSDK();
#else
        IsInitialized = true;
#endif
    }

    public void RequestAd(string type, Action onStarted = null, Action onFinished = null, Action<string> onError = null)
    {
        if (!IsInitialized) { onError?.Invoke("SDK Not Ready"); return; }
        currentAdStarted = onStarted; currentAdFinished = onFinished; currentAdError = onError;
#if UNITY_WEBGL && !UNITY_EDITOR
        RequestAdSDK(type);
#else
        onStarted?.Invoke(); onFinished?.Invoke();
#endif
    }

    public void PrefetchAd(string type) { if (IsInitialized) PrefetchAdSDK(type); }
    public void RequestBanners(string json) { if (IsInitialized) RequestBannersSDK(json); }
    public void HappyTime() { if (IsInitialized) HappyTimeSDK(); }
    public void GameplayStart() { if (IsInitialized) GameplayStartSDK(); }
    public void GameplayStop() { if (IsInitialized) GameplayStopSDK(); }
    public void LoadingStart() { if (IsInitialized) LoadingStartSDK(); }
    public void LoadingStop() { if (IsInitialized) LoadingStopSDK(); }
    public void AddScore(int score) { if (IsInitialized) AddScoreSDK(score); }

    public void GetUser(Action<CrazyGamesSDK.CrazyGamesUser> s, Action<string> e) {
        if (!IsInitialized) { e?.Invoke("SDK Not Ready"); return; }
        pendingUserSuccess = s; pendingUserError = e;
        GetUserSDK();
    }

    public void ShowAuthPrompt(Action<CrazyGamesSDK.CrazyGamesUser> s, Action<string> e) {
        if (!IsInitialized) { e?.Invoke("SDK Not Ready"); return; }
        pendingAuthSuccess = s; pendingAuthError = e;
        ShowAuthPromptSDK();
    }

    public bool IsUserAccountAvailable() {
        return IsInitialized && IsUserAccountAvailableSDK() == 1;
    }

    public void SaveData(string k, string v, Action s, Action<string> e) {
        if (!IsInitialized) { e?.Invoke("SDK Not Ready"); return; }
        try { 
#if UNITY_WEBGL && !UNITY_EDITOR
            DataSetItemSDK(k, v); SyncUnityGameDataSDK(); 
#endif
            s?.Invoke(); 
        }
        catch (Exception ex) { e?.Invoke(ex.Message); }
    }

    public void LoadData(string k, Action<string> s, Action<string> e) {
        if (!IsInitialized) { e?.Invoke("SDK Not Ready"); return; }
#if UNITY_WEBGL && !UNITY_EDITOR
        string val = DataGetItemSDK(k);
        if (string.IsNullOrEmpty(val)) e?.Invoke("No data"); else s?.Invoke(val);
#else
        e?.Invoke("Not available in Editor");
#endif
    }

    public void OnSDKInitSuccess() { IsInitialized = true; LoadingStart(); }
    public void OnSDKInitError(string error) { IsInitialized = false; Debug.LogError("CG SDK Fail: " + error); }
    public void OnAdblockDetected(int detected) { HasAdblock = detected == 1; onAdblockDetected?.Invoke(HasAdblock); }
    
    public void OnAdStarted() {
        GameplayStop();
        Time.timeScale = 0f; AudioListener.pause = true;
        currentAdStarted?.Invoke();
    }
    public void OnAdFinished() {
        bool inMenu = GameManager.Instance != null && GameManager.Instance.IsAnyUIDisplaying();
        if (!inMenu) { Time.timeScale = 1f; GameplayStart(); }
        AudioListener.pause = false;
        currentAdFinished?.Invoke();
    }
    public void OnAdError(string error) { OnAdFinished(); currentAdError?.Invoke(error); }

    public void OnAuthListener(string json) {
        var user = JsonUtility.FromJson<CrazyGamesSDK.CrazyGamesUser>(json);
        onUserInfoReceived?.Invoke(user);
    }
    public void OnGetUser(string json) { var u = JsonUtility.FromJson<CrazyGamesSDK.CrazyGamesUser>(json); pendingUserSuccess?.Invoke(u); onUserInfoReceived?.Invoke(u); }
    public void OnGetUserError(string err) { pendingUserError?.Invoke(err); }
    public void OnShowAuthPrompt(string json) { var u = JsonUtility.FromJson<CrazyGamesSDK.CrazyGamesUser>(json); pendingAuthSuccess?.Invoke(u); onUserInfoReceived?.Invoke(u); }
    public void OnShowAuthPromptError(string err) { pendingAuthError?.Invoke(err); }

    private void Update() {
        if (IsInitialized && (Input.GetMouseButtonDown(0) || Input.anyKeyDown)) {
            if (AudioListener.pause == false && Time.timeScale > 0) {
                AudioListener.pause = true; AudioListener.pause = false;
            }
        }
    }
}
