using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

/// <summary>
/// Manages CrazyGames user info display (username, avatar).
/// Handles automatic login and user authentication state.
/// </summary>
public class UserInfoManager : MonoBehaviour
{
    public static UserInfoManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private GameObject userInfoPanel; // Optional panel to show/hide

    [Header("Settings")]
    [SerializeField] private Sprite defaultAvatarSprite; // Fallback avatar if user has none
    [SerializeField] private bool showGuestAsAnonymous = true;

    private CrazyGamesSDK.CrazyGamesUser currentUser;
    private bool isLoadingAvatar = false;

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
        
        // Initialize as guest immediately (don't block game start)
        ShowGuestUser();
    }

    private void Start()
    {
        // Wait for PlatformManager to initialize, then check user (non-blocking)
        StartCoroutine(InitializeUserInfo());
    }

    private IEnumerator InitializeUserInfo()
    {
        // Wait for PlatformManager to initialize (max 5 seconds)
        float timeout = 5f;
        float elapsed = 0f;

        while (PlatformManager.Instance == null || !PlatformManager.Instance.IsInitialized)
        {
            if (elapsed >= timeout)
            {
                Debug.LogWarning("[UserInfoManager] PlatformManager initialization timeout");
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Check if we're on CrazyGames platform
        if (PlatformManager.Instance.CurrentPlatform == PlatformManager.PlatformType.CrazyGames)
        {
            var sdk = PlatformManager.Instance.SDK as CrazyGamesSDK;
            if (sdk != null)
            {
                // Subscribe to user info updates
                sdk.OnUserInfoReceived += OnUserInfoReceived;

                // Check if user account is available
                if (sdk.IsUserAccountAvailable())
                {
                    // Try to get current user (automatic login check)
                    sdk.GetUser(
                        onSuccess: (user) =>
                        {
                            Debug.Log($"[UserInfoManager] User loaded: {user.username}");
                            OnUserInfoReceived(user);
                        },
                        onError: (error) =>
                        {
                            Debug.Log($"[UserInfoManager] No user logged in: {error}");
                            // User not logged in - show as guest/anonymous
                            ShowGuestUser();
                        }
                    );
                }
                else
                {
                    Debug.Log("[UserInfoManager] User account feature not available");
                    ShowGuestUser();
                }
            }
        }
        else
        {
            // Not on CrazyGames, show as guest
            ShowGuestUser();
        }
    }

    private void OnUserInfoReceived(CrazyGamesSDK.CrazyGamesUser user)
    {
        currentUser = user;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Update username
        if (usernameText != null)
        {
            if (currentUser != null && !string.IsNullOrEmpty(currentUser.username))
            {
                usernameText.text = currentUser.username;
            }
            else if (showGuestAsAnonymous)
            {
                usernameText.text = "Guest";
            }
            else
            {
                usernameText.text = "";
            }
        }

        // Update avatar
        if (avatarImage != null && currentUser != null && !string.IsNullOrEmpty(currentUser.avatarUrl))
        {
            StartCoroutine(LoadAvatar(currentUser.avatarUrl));
        }
        else if (avatarImage != null && defaultAvatarSprite != null)
        {
            avatarImage.sprite = defaultAvatarSprite;
        }
    }

    private IEnumerator LoadAvatar(string avatarUrl)
    {
        if (isLoadingAvatar) yield break;
        isLoadingAvatar = true;

        UnityEngine.Networking.UnityWebRequest www = null;

        // Create request outside try-catch
        try
        {
            www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(avatarUrl);
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserInfoManager] Error creating avatar request: {e.Message}");
            if (avatarImage != null && defaultAvatarSprite != null)
            {
                avatarImage.sprite = defaultAvatarSprite;
            }
            isLoadingAvatar = false;
            yield break;
        }

        // Send request (yield cannot be in try-catch)
        if (www != null)
        {
            yield return www.SendWebRequest();

            // Handle response (can use try-catch here since no yield)
            try
            {
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                    if (texture != null && avatarImage != null)
                    {
                        Sprite sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );
                        avatarImage.sprite = sprite;
                        Debug.Log("[UserInfoManager] Avatar loaded successfully");
                    }
                }
                else
                {
                    Debug.LogWarning($"[UserInfoManager] Failed to load avatar: {www.error}");
                    if (avatarImage != null && defaultAvatarSprite != null)
                    {
                        avatarImage.sprite = defaultAvatarSprite;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserInfoManager] Error processing avatar: {e.Message}");
                if (avatarImage != null && defaultAvatarSprite != null)
                {
                    avatarImage.sprite = defaultAvatarSprite;
                }
            }
            finally
            {
                // Dispose of the request
                if (www != null)
                {
                    www.Dispose();
                }
            }
        }
        else
        {
            if (avatarImage != null && defaultAvatarSprite != null)
            {
                avatarImage.sprite = defaultAvatarSprite;
            }
        }

        isLoadingAvatar = false;
    }

    private void ShowGuestUser()
    {
        currentUser = new CrazyGamesSDK.CrazyGamesUser
        {
            id = "",
            username = showGuestAsAnonymous ? "Guest" : "",
            avatarUrl = "",
            isGuest = true
        };
        UpdateUI();
    }

    /// <summary>
    /// Manually trigger login prompt (can be called from UI button)
    /// </summary>
    public void ShowLoginPrompt()
    {
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsInitialized)
        {
            var sdk = PlatformManager.Instance.SDK as CrazyGamesSDK;
            if (sdk != null)
            {
                sdk.ShowAuthPrompt(
                    onSuccess: (user) =>
                    {
                        Debug.Log($"[UserInfoManager] User logged in: {user.username}");
                        OnUserInfoReceived(user);
                    },
                    onError: (error) =>
                    {
                        Debug.LogWarning($"[UserInfoManager] Login failed: {error}");
                    }
                );
            }
        }
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    public CrazyGamesSDK.CrazyGamesUser GetCurrentUser()
    {
        return currentUser;
    }

    /// <summary>
    /// Check if user is logged in (not guest)
    /// </summary>
    public bool IsUserLoggedIn()
    {
        return currentUser != null && !currentUser.isGuest && !string.IsNullOrEmpty(currentUser.id);
    }

    private void OnDestroy()
    {
        // Unsubscribe from SDK events
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsInitialized)
        {
            var sdk = PlatformManager.Instance.SDK as CrazyGamesSDK;
            if (sdk != null)
            {
                sdk.OnUserInfoReceived -= OnUserInfoReceived;
            }
        }
    }
}

