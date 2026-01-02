using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeathScreenUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject deathScreenPanel;
    [SerializeField] private RectTransform blackCoverImage; // 1920x1080 image that moves from top to bottom
    [SerializeField] private GameObject statsContainer; // Container for all text and buttons (hide during animation)
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI soulsEarnedText;
    [SerializeField] private TextMeshProUGUI goldEarnedText;
    [SerializeField] private TextMeshProUGUI timeSurvivedText;
    [SerializeField] private TextMeshProUGUI leaderboardPlaceText; // Shows "Your place on leaderboard: X"
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button doubleSoulsButton; // Rewarded ad button
    [SerializeField] private TextMeshProUGUI doubleSoulsButtonText;

    private int leaderboardPlace = -1;
    private int currentSoulsEarned = 0;
    private int currentKills = 0;
    private bool hasDoubledSouls = false;

    private void Awake()
    {
        // Hide death screen by default
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(false);
        }

        // Hide stats container initially
        if (statsContainer != null)
        {
            statsContainer.SetActive(false);
        }
        else
        {
            Debug.LogWarning("DeathScreenUI: statsContainer is not assigned! Stats and buttons won't appear.");
        }

        // Setup button listeners
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        if (doubleSoulsButton != null)
        {
            doubleSoulsButton.onClick.AddListener(OnDoubleSoulsButtonClicked);
            doubleSoulsButton.gameObject.SetActive(false); // Hidden initially
        }

        UpdateDoubleSoulsButtonText();
        UpdateButtonTexts();
    }

    private void Start()
    {
        // Subscribe to language changes
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateButtonTexts;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from language changes
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateButtonTexts;
        }
    }

    private void UpdateButtonTexts()
    {
        // Update Restart button text
        if (restartButton != null)
        {
            TextMeshProUGUI textComponent = restartButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                string restartText = "Restart";
                if (LocalizationManager.Instance != null)
                {
                    restartText = LocalizationManager.Instance.GetLocalizedString("Restart");
                }
                textComponent.text = restartText;
            }
        }

        // Update Main Menu button text
        if (mainMenuButton != null)
        {
            TextMeshProUGUI textComponent = mainMenuButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                string mainMenuText = "Main Menu";
                if (LocalizationManager.Instance != null)
                {
                    mainMenuText = LocalizationManager.Instance.GetLocalizedString("Main Menu");
                }
                textComponent.text = mainMenuText;
            }
        }

        // Update Double Souls button text (already has its own method)
        UpdateDoubleSoulsButtonText();
    }

    public void ShowDeathScreen(int level, int kills, int soulsEarned, float goldEarned, float timeSurvived)
    {
        // Store values for double souls feature
        currentSoulsEarned = soulsEarned;
        currentKills = kills;
        hasDoubledSouls = false;

        // Show cursor for button interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(true);
            Debug.Log("DeathScreenUI: Death screen panel activated.");
        }
        else
        {
            Debug.LogError("DeathScreenUI: deathScreenPanel is null! Make sure it's assigned in the Inspector.");
            return;
        }

        // Update button texts with current language
        UpdateButtonTexts();

        // Update stats display (but hide until animation completes)
        string levelLabel = "Level: ";
        string killsLabel = "Kills: ";
        string soulsLabel = "Souls Earned: ";
        string goldLabel = "Gold Earned: ";
        string timeLabel = "Time Survived: ";

        if (LocalizationManager.Instance != null)
        {
            levelLabel = LocalizationManager.Instance.GetLocalizedString("Level") + ": ";
            killsLabel = LocalizationManager.Instance.GetLocalizedString("Kills") + ": ";
            soulsLabel = LocalizationManager.Instance.GetLocalizedString("Souls Earned") + ": ";
            goldLabel = LocalizationManager.Instance.GetLocalizedString("Gold Earned") + ": ";
            timeLabel = LocalizationManager.Instance.GetLocalizedString("Time Survived") + ": ";
        }

        if (levelText != null)
        {
            levelText.text = levelLabel + level;
        }

        if (killsText != null)
        {
            killsText.text = killsLabel + kills;
        }

        if (soulsEarnedText != null)
        {
            soulsEarnedText.text = soulsLabel + soulsEarned;
        }

        if (goldEarnedText != null)
        {
            goldEarnedText.text = goldLabel + Mathf.FloorToInt(goldEarned);
        }

        if (timeSurvivedText != null)
        {
            int minutes = Mathf.FloorToInt(timeSurvived / 60f);
            int seconds = Mathf.FloorToInt(timeSurvived % 60f);
            timeSurvivedText.text = timeLabel + $"{minutes:00}:{seconds:00}";
        }

        // Submit score to platform SDK leaderboard (Yandex Games)
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsInitialized)
        {
            PlatformManager.Instance.SubmitScore(kills, 
                onSuccess: () => {
                    Debug.Log("[DeathScreenUI] Score submitted successfully to SDK leaderboard");
                },
                onError: (error) => {
                    Debug.LogWarning($"[DeathScreenUI] Failed to submit score to SDK: {error}");
                }
            );
        }

        // Also submit to local leaderboard (for display)
        leaderboardPlace = -1;
        if (LeaderboardManager.Instance != null)
        {
            string playerName = GetPlayerName();
            leaderboardPlace = LeaderboardManager.Instance.SubmitScore(playerName, kills);
        }

        // Update leaderboard place text
        if (leaderboardPlaceText != null)
        {
            if (leaderboardPlace > 0)
            {
                if (LocalizationManager.Instance != null)
                    leaderboardPlaceText.text = LocalizationManager.Instance.GetLocalizedString("Your place on leaderboard", leaderboardPlace);
                else
                    leaderboardPlaceText.text = "Your place on leaderboard: " + leaderboardPlace;
            }
            else
            {
                leaderboardPlaceText.text = "";
            }
        }

        // Show double souls button if SDK is available and player earned souls
        if (doubleSoulsButton != null && soulsEarned > 0 && PlatformManager.Instance != null && PlatformManager.Instance.IsInitialized)
        {
            doubleSoulsButton.gameObject.SetActive(true);
            doubleSoulsButton.interactable = true;
        }

        // Start death animation
        StartCoroutine(PlayDeathAnimation());
    }

    private string GetPlayerName()
    {
        // Simple default player name (can be replaced with SDK implementation later)
        return "Player";
    }

    private IEnumerator PlayDeathAnimation()
    {
        if (blackCoverImage == null)
        {
            // If no black cover, just show stats immediately
            if (statsContainer != null)
            {
                statsContainer.SetActive(true);
            }
            yield break;
        }

        // Make sure image stretches across full screen
        blackCoverImage.anchorMin = new Vector2(0f, 0f); // Bottom-left
        blackCoverImage.anchorMax = new Vector2(1f, 1f); // Top-right
        blackCoverImage.offsetMin = Vector2.zero; // No offset from bottom-left
        blackCoverImage.offsetMax = Vector2.zero; // No offset from top-right
        
        // Get the rect height (should be screen height)
        float rectHeight = blackCoverImage.rect.height;
        
        // Start: image above screen (anchored position Y = rectHeight, so it's above)
        blackCoverImage.anchoredPosition = new Vector2(0f, rectHeight);
        
        // Make sure image is visible
        blackCoverImage.gameObject.SetActive(true);

        // Animate from top (rectHeight) to center (0) over 1 second
        float startY = rectHeight;
        float endY = 0f;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time since game is paused
            float progress = Mathf.Clamp01(elapsed / duration);

            // Lerp from startY to endY
            float currentY = Mathf.Lerp(startY, endY, progress);
            blackCoverImage.anchoredPosition = new Vector2(0f, currentY);

            yield return null;
        }

        // Ensure final position (centered, covering screen)
        blackCoverImage.anchoredPosition = new Vector2(0f, endY);

        // Wait a tiny bit
        yield return new WaitForSecondsRealtime(0.1f);

        // Show stats and buttons
        if (statsContainer != null)
        {
            statsContainer.SetActive(true);
            // Make sure it's above the black image (bring to front)
            statsContainer.transform.SetAsLastSibling();
            Debug.Log("DeathScreenUI: Stats container activated.");
        }
        else
        {
            Debug.LogError("DeathScreenUI: statsContainer is null! Make sure it's assigned in the Inspector.");
        }
    }

    public void HideDeathScreen()
    {
        // Stop any running animation
        StopAllCoroutines();

        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(false);
        }

        // Reset black cover position for next time (back to top)
        if (blackCoverImage != null)
        {
            // Reset anchors to full screen
            blackCoverImage.anchorMin = new Vector2(0f, 0f);
            blackCoverImage.anchorMax = new Vector2(1f, 1f);
            blackCoverImage.offsetMin = Vector2.zero;
            blackCoverImage.offsetMax = Vector2.zero;
            // Position above screen
            blackCoverImage.anchoredPosition = new Vector2(0f, blackCoverImage.rect.height);
        }

        // Hide stats container
        if (statsContainer != null)
        {
            statsContainer.SetActive(false);
        }
    }

    public bool IsShowing()
    {
        return deathScreenPanel != null && deathScreenPanel.activeInHierarchy;
    }

    private void OnRestartClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    private void OnMainMenuClicked()
    {
        // Show interstitial ad before returning to main menu (after gameplay session)
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsInitialized)
        {
            PlatformManager.Instance.ShowInterstitialAd(
                onAdClosed: () => {
                    Time.timeScale = 1f;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(0);
                },
                onAdError: (error) => {
                    // If ad fails, still return to menu
                    Debug.LogWarning($"[DeathScreenUI] Interstitial ad error: {error}, returning to menu anyway");
                    Time.timeScale = 1f;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(0);
                }
            );
        }
        else
        {
            // SDK not available, return to menu directly
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    // ===== DOUBLE SOULS FUNCTIONALITY =====

    private void OnDoubleSoulsButtonClicked()
    {
        if (hasDoubledSouls)
        {
            Debug.LogWarning("[DeathScreenUI] Already doubled souls!");
            return;
        }

        if (PlatformManager.Instance == null || !PlatformManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[DeathScreenUI] SDK not ready for rewarded ad");
            return;
        }

        // Disable button while showing ad
        if (doubleSoulsButton != null)
            doubleSoulsButton.interactable = false;

        // Show rewarded ad
        PlatformManager.Instance.ShowRewardedAd(
            onRewarded: OnDoubleSoulsAdWatched,
            onAdClosed: OnDoubleSoulsAdClosed,
            onAdError: OnDoubleSoulsAdError
        );
    }

    private void OnDoubleSoulsAdWatched()
    {
        Debug.Log("[DeathScreenUI] Double souls ad watched - doubling souls");
        
        hasDoubledSouls = true;

        // Double the souls
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AddSouls(currentSoulsEarned); // Add the same amount again to double
        }

        // Update UI to show doubled amount
        int totalSouls = currentSoulsEarned * 2;
        string soulsLabel = "Souls Earned: ";
        if (LocalizationManager.Instance != null)
        {
            soulsLabel = LocalizationManager.Instance.GetLocalizedString("Souls Earned") + ": ";
        }

        if (soulsEarnedText != null)
        {
            soulsEarnedText.text = soulsLabel + totalSouls;
        }

        // Hide the double souls button
        if (doubleSoulsButton != null)
            doubleSoulsButton.gameObject.SetActive(false);
    }

    private void OnDoubleSoulsAdClosed()
    {
        // Re-enable button if ad was closed without reward
        if (!hasDoubledSouls && doubleSoulsButton != null)
        {
            doubleSoulsButton.interactable = true;
        }
    }

    private void OnDoubleSoulsAdError(string error)
    {
        Debug.LogWarning($"[DeathScreenUI] Double souls ad error: {error}");
        
        // Re-enable button if ad failed
        if (!hasDoubledSouls && doubleSoulsButton != null)
        {
            doubleSoulsButton.interactable = true;
        }
    }

    private void UpdateDoubleSoulsButtonText()
    {
        // First try to use the assigned reference
        TextMeshProUGUI textComponent = doubleSoulsButtonText;
        
        // If not assigned, try to find it in the button
        if (textComponent == null && doubleSoulsButton != null)
        {
            textComponent = doubleSoulsButton.GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (textComponent != null)
        {
            string text = "Double Souls";
            if (LocalizationManager.Instance != null)
            {
                text = LocalizationManager.Instance.GetLocalizedString("Double Souls");
            }
            textComponent.text = text;
        }
    }
}

