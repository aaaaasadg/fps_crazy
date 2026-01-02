using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MapSelectScreen : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button map1Button;
    [SerializeField] private Button map2Button;
    [SerializeField] private Button map3Button; // Madness mode button
    [SerializeField] private TextMeshProUGUI map2LockText; // Displays map name or locked message
    [SerializeField] private TextMeshProUGUI map1ButtonText;
    [SerializeField] private TextMeshProUGUI map2ButtonText;
    [SerializeField] private TextMeshProUGUI map3ButtonText; // Madness mode button text
    [SerializeField] private TextMeshProUGUI backButtonText;
    [SerializeField] private Button backButton;

    [Header("Scene Names")]
    [SerializeField] private string map1SceneName = "Game"; // Default scene
    [SerializeField] private string map2SceneName = "Game 2"; // Second map scene
    [SerializeField] private string map3SceneName = "Game 3"; // Madness mode scene

    private void Start()
    {
        // Map 1 is always unlocked (Normal mode)
        if (map1Button != null)
            map1Button.onClick.AddListener(() => LoadMapWithMode(map1SceneName, GameMode.Normal));

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        // Subscribe to language changes
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalizedText;
            UpdateLocalizedText();
        }

        // Check if Map 2 is unlocked via SaveManager
        bool map2Unlocked = false;
        if (SaveManager.Instance != null)
        {
            map2Unlocked = SaveManager.Instance.IsMapUnlocked(1); // Index 1 = Map 2
        }

        // Configure Map 2 Button interactability (Normal mode)
        if (map2Button != null)
        {
            map2Button.interactable = map2Unlocked;
            map2Button.onClick.AddListener(() => LoadMapWithMode(map2SceneName, GameMode.Normal));
        }

        // Text update handled in UpdateLocalizedText -> RefreshMap2Text
        RefreshMap2Text();

        // Map 3 (Madness) - always unlocked
        bool map3Unlocked = true; // Madness is always unlocked
        if (SaveManager.Instance != null)
        {
            map3Unlocked = SaveManager.Instance.IsMapUnlocked(2); // Index 2 = Map 3 (Madness)
        }
        
        if (map3Button != null)
        {
            map3Button.interactable = map3Unlocked;
            map3Button.onClick.AddListener(() => LoadMapWithMode(map3SceneName, GameMode.Madness));
        }
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalizedText;
        }
    }

    private void UpdateLocalizedText()
    {
        if (LocalizationManager.Instance == null) return;

        TMP_FontAsset font = LocalizationManager.Instance.GetFontForCurrentLanguage();

        if (map1ButtonText != null)
        {
            map1ButtonText.text = LocalizationManager.Instance.GetLocalizedString("Stone Island");
            if (font != null) LocalizationManager.Instance.ApplyFont(map1ButtonText, font);
        }

        if (map2ButtonText != null)
        {
            map2ButtonText.text = LocalizationManager.Instance.GetLocalizedString("Wild Forest");
            if (font != null) LocalizationManager.Instance.ApplyFont(map2ButtonText, font);
        }

        if (map3ButtonText != null)
        {
            map3ButtonText.text = LocalizationManager.Instance.GetLocalizedString("Madness");
            if (font != null) LocalizationManager.Instance.ApplyFont(map3ButtonText, font);
        }

        if (backButtonText != null)
        {
            backButtonText.text = LocalizationManager.Instance.GetLocalizedString("Back");
            if (font != null) LocalizationManager.Instance.ApplyFont(backButtonText, font);
        }

        if (map2LockText != null)
        {
            if (font != null) LocalizationManager.Instance.ApplyFont(map2LockText, font);
            // Text content logic is handled in Start/Update based on unlock status,
            // but we should refresh it here too if possible.
            // Since Start runs once, let's extract the unlock logic to a method we can call here.
            RefreshMap2Text();
        }
    }

    private void RefreshMap2Text()
    {
        bool map2Unlocked = false;
        if (SaveManager.Instance != null)
        {
            map2Unlocked = SaveManager.Instance.IsMapUnlocked(1);
        }

        if (map2LockText != null)
        {
            map2LockText.gameObject.SetActive(true);
            if (map2Unlocked)
            {
                if (LocalizationManager.Instance != null)
                    map2LockText.text = LocalizationManager.Instance.GetLocalizedString("Wild Forest");
                else
                    map2LockText.text = "Wild Forest";
            }
            else
            {
                // Specify which boss unlocks the map (3rd boss at 10 minutes)
                if (LocalizationManager.Instance != null)
                    map2LockText.text = LocalizationManager.Instance.GetLocalizedString("Map2_Locked_Kill3rdBoss");
                else
                    map2LockText.text = "LOCKED: Kill 3rd Boss (10 min)";
            }
        }
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void LoadMapWithMode(string sceneName, GameMode mode)
    {
        // Set game mode before loading scene
        GameManager.selectedGameMode = mode;
        
        // Signal loading start when loading a new scene
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsInitialized)
        {
            if (PlatformManager.Instance.SDK is CrazyGamesSDK crazySDK)
            {
                crazySDK.CallLoadingStart();
            }
        }
        
        // Ensure Time Scale is 1 before loading
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
