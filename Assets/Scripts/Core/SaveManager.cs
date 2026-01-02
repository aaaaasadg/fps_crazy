using UnityEngine;
using System.Collections;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public GameData data;

    private const string SAVE_KEY = "SaveData_v1";
    private bool isLoadingFromCloud = false;
    private bool isSavingToCloud = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize data immediately (don't block game start)
        if (data == null)
        {
            data = new GameData();
        }
        
        // Load from PlayerPrefs immediately (fast, non-blocking)
        LoadFromPlayerPrefs();
        
        // Then try to load from cloud in background (non-blocking)
        StartCoroutine(InitializeAndLoadCloud());
    }

    private IEnumerator InitializeAndLoadCloud()
    {
        // Wait several frames first to ensure game is responsive
        yield return null;
        yield return null;
        yield return null;

        // Wait for PlatformManager to initialize (max 5 seconds, but don't block game)
        float timeout = 5f;
        float elapsed = 0f;
        
        while (PlatformManager.Instance == null || !PlatformManager.Instance.IsInitialized)
        {
            if (elapsed >= timeout)
            {
                Debug.LogWarning("[SaveManager] PlatformManager initialization timeout, using PlayerPrefs only");
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Wait another frame before loading (ensures game stays responsive)
        yield return null;

        // Try to load from cloud (non-blocking, won't freeze game)
        if (!isLoadingFromCloud)
        {
            Load();
        }
    }

    public void Save()
    {
        if (data == null) data = new GameData();
        string json = JsonUtility.ToJson(data);
        
        // Always save to PlayerPrefs as backup
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        
        // Also save to cloud if SDK is available and supports cloud save
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsInitialized)
        {
            IPlatformSDK sdk = PlatformManager.Instance.SDK;
            if (sdk != null && !isSavingToCloud)
            {
                StartCoroutine(SaveToCloud(json));
            }
        }
    }

    private IEnumerator SaveToCloud(string json)
    {
        isSavingToCloud = true;
        bool saveComplete = false;

        IPlatformSDK sdk = PlatformManager.Instance.SDK;
        
        // Check if SDK supports cloud save (CrazyGames SDK does)
        if (sdk is CrazyGamesSDK)
        {
            sdk.SaveData(
                key: SAVE_KEY,
                value: json,
                onSuccess: () =>
                {
                    saveComplete = true;
                    Debug.Log("[SaveManager] Data saved to cloud successfully");
                },
                onError: (error) =>
                {
                    saveComplete = true;
                    Debug.LogWarning($"[SaveManager] Cloud save failed: {error}. Using PlayerPrefs backup.");
                }
            );

            // Wait for save to complete (max 3 seconds)
            float timeout = 3f;
            float elapsed = 0f;
            while (!saveComplete && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!saveComplete)
            {
                Debug.LogWarning("[SaveManager] Cloud save timeout, PlayerPrefs backup is available");
            }
        }

        isSavingToCloud = false;
    }

    public void Load()
    {
        if (isLoadingFromCloud)
        {
            Debug.LogWarning("[SaveManager] Load already in progress, skipping");
            return;
        }

        // Try loading from cloud first if SDK is available
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsInitialized)
        {
            IPlatformSDK sdk = PlatformManager.Instance.SDK;
            if (sdk is CrazyGamesSDK)
            {
                StartCoroutine(LoadFromCloud());
                return;
            }
        }

        // Fallback to PlayerPrefs
        LoadFromPlayerPrefs();
    }

    private IEnumerator LoadFromCloud()
    {
        isLoadingFromCloud = true;
        bool loadComplete = false;
        bool loadSuccess = false;
        string loadedJson = "";

        IPlatformSDK sdk = PlatformManager.Instance.SDK;
        
        // Initialize data to default if null (prevents null reference)
        if (data == null) data = new GameData();
        
        sdk.LoadData(
            key: SAVE_KEY,
            onSuccess: (json) =>
            {
                loadedJson = json;
                loadSuccess = true;
                loadComplete = true;
                Debug.Log("[SaveManager] Data loaded from cloud successfully");
            },
            onError: (error) =>
            {
                loadSuccess = false;
                loadComplete = true;
                Debug.Log($"[SaveManager] Cloud load failed: {error}. Trying PlayerPrefs fallback.");
            }
        );

        // Wait for load to complete (max 3 seconds)
        // Note: SDK callbacks may fire immediately (synchronous) or asynchronously
        float timeout = 3f;
        float elapsed = 0f;
        while (!loadComplete && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (loadSuccess && !string.IsNullOrEmpty(loadedJson))
        {
            // Successfully loaded from cloud
            try
            {
                JsonUtility.FromJsonOverwrite(loadedJson, data);
                
                // Also update PlayerPrefs as backup
                PlayerPrefs.SetString(SAVE_KEY, loadedJson);
                PlayerPrefs.Save();
                
                Debug.Log("[SaveManager] Loaded from cloud and synced to PlayerPrefs");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to parse cloud data: {e.Message}. Using PlayerPrefs fallback.");
                LoadFromPlayerPrefs();
            }
        }
        else
        {
            // Cloud load failed or timed out, use PlayerPrefs
            LoadFromPlayerPrefs();
        }

        isLoadingFromCloud = false;
    }

    private void LoadFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            if (data == null) data = new GameData();
            string json = PlayerPrefs.GetString(SAVE_KEY);
            JsonUtility.FromJsonOverwrite(json, data);
            Debug.Log("[SaveManager] Loaded from PlayerPrefs");
        }
        else
        {
            data = new GameData();
            Debug.Log("[SaveManager] No save data found, using default GameData");
        }
    }

    // AddSouls is correct: updates data, saves, and updates HUD.
    public void AddSouls(int amount)
    {
        if (data == null) data = new GameData();
        data.souls += amount;
        Save();

        if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
        {
            GameManager.Instance.hudManager.UpdateSouls(data.souls);
        }
    }

    public void UpgradeStat(StatType type)
    {
        if (data == null) data = new GameData();
        data.statLevels[(int)type]++;
        Save();
    }

    public int GetStatLevel(StatType type)
    {
        if (data == null) data = new GameData();
        return data.statLevels[(int)type];
    }

    public void UnlockMap(int mapIndex)
    {
        if (data == null) data = new GameData();
        if (data.mapsUnlocked != null && mapIndex >= 0 && mapIndex < data.mapsUnlocked.Length)
        {
            data.mapsUnlocked[mapIndex] = true;
            Save();
        }
    }

    public bool IsMapUnlocked(int mapIndex)
    {
        if (data == null) data = new GameData();
        if (data.mapsUnlocked != null && mapIndex >= 0 && mapIndex < data.mapsUnlocked.Length)
        {
            return data.mapsUnlocked[mapIndex];
        }
        // Default: not unlocked if index out of range or mapsUnlocked is null
        return false;
    }
}
