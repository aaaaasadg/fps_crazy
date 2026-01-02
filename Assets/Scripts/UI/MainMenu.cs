using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject shopPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private LeaderboardUI leaderboardUI;
    
    [Header("Menu Text References")]
    [SerializeField] private TMPro.TextMeshProUGUI playButtonText;
    [SerializeField] private TMPro.TextMeshProUGUI shopButtonText;
    [SerializeField] private TMPro.TextMeshProUGUI quitButtonText;
    [SerializeField] private TMPro.TextMeshProUGUI settingsButtonText;
    [SerializeField] private UnityEngine.UI.Button shopBackButton; // Back button in shop

    private void Start()
    {
        // Initialize menu normally - menu must work regardless of SDK or any other system
        // Set these first to ensure menu is responsive
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Initialize UI panels first (most important)
        try
        {
            CloseShop();
            
            // Hide settings panel at start
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MainMenu] Error initializing panels: {e.Message}");
        }

        // Try to initialize other systems, but don't let them block
        try
        {
            // Subscribe to language changes
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += UpdateMenuText;
                UpdateMenuText();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MainMenu] Error initializing localization: {e.Message}");
        }

        try
        {
            // Auto-find LeaderboardUI if not assigned
            if (leaderboardUI == null)
            {
                leaderboardUI = FindFirstObjectByType<LeaderboardUI>();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MainMenu] Error finding LeaderboardUI: {e.Message}");
        }

        try
        {
            // Play Menu Music
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayMenuMusic();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MainMenu] Error playing menu music: {e.Message}");
        }

        // Try to signal loading stop - but don't block if SDK isn't ready
        // Do this in a coroutine so it doesn't block menu initialization
        StartCoroutine(TryCallLoadingStop());
    }

    private IEnumerator TryCallLoadingStop()
    {
        // Wait a frame to ensure everything is initialized
        yield return null;
        
        // Try to call loading stop, but don't wait for SDK
        // Menu works regardless of SDK state
        try
        {
            if (PlatformManager.Instance != null && PlatformManager.Instance.IsInitialized)
            {
                if (PlatformManager.Instance.SDK is CrazyGamesSDK crazySDK)
                {
                    crazySDK.CallLoadingStop();
                }
            }
        }
        catch (System.Exception e)
        {
            // Silently fail - menu should work even if SDK fails
            Debug.LogWarning($"[MainMenu] SDK call failed: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateMenuText;
        }
    }

    private void UpdateMenuText()
    {
        if (LocalizationManager.Instance == null) return;

        TMPro.TMP_FontAsset font = LocalizationManager.Instance.GetFontForCurrentLanguage();

        if (playButtonText != null)
        {
            playButtonText.text = LocalizationManager.Instance.GetLocalizedString("Play");
            if (font != null) LocalizationManager.Instance.ApplyFont(playButtonText, font);
        }

        if (shopButtonText != null)
        {
            shopButtonText.text = LocalizationManager.Instance.GetLocalizedString("Shop");
            if (font != null) LocalizationManager.Instance.ApplyFont(shopButtonText, font);
        }

        if (quitButtonText != null)
        {
            quitButtonText.text = LocalizationManager.Instance.GetLocalizedString("Quit");
            if (font != null) LocalizationManager.Instance.ApplyFont(quitButtonText, font);
        }
        
        if (settingsButtonText != null)
        {
            settingsButtonText.text = LocalizationManager.Instance.GetLocalizedString("Settings");
            if (font != null) LocalizationManager.Instance.ApplyFont(settingsButtonText, font);
        }

        // Update shop back button text
        if (shopBackButton != null)
        {
            TMPro.TextMeshProUGUI backButtonText = shopBackButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (backButtonText != null)
            {
                backButtonText.text = LocalizationManager.Instance.GetLocalizedString("Back");
                if (font != null) LocalizationManager.Instance.ApplyFont(backButtonText, font);
            }
        }
    }

    public void PlayGame()
    {
        // Safety: Ensure audio is resumed on the very first interaction
        AudioListener.pause = false;
        
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("Game");
    }

    public void OpenShop()
    {
        // Safety: Ensure audio is resumed on interaction
        AudioListener.pause = false;

        if (mainPanel != null) mainPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(true);
        
        // Update button texts when opening shop (ensures translations are correct)
        UpdateMenuText();
        
        // Play shop open sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayShopOpen();
        }

        // Hide leaderboard when opening shop
        if (leaderboardUI != null)
        {
            leaderboardUI.HideLeaderboard();
        }
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        // Show leaderboard when closing shop
        if (leaderboardUI != null)
        {
            leaderboardUI.ShowLeaderboard();
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }
        
        // Hide leaderboard when opening settings
        if (leaderboardUI != null)
        {
            leaderboardUI.HideLeaderboard();
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }
        
        // Show leaderboard when closing settings
        if (leaderboardUI != null)
        {
            leaderboardUI.ShowLeaderboard();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
