// OPT: cache GameManager.Instance reference in Awake
// OPT: early-return helpers to reduce nesting on singleton checks
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI pauseTitleText;
    [SerializeField] private TextMeshProUGUI resumeButtonText;
    [SerializeField] private TextMeshProUGUI settingsButtonText;
    [SerializeField] private TextMeshProUGUI mainMenuButtonText;
    [SerializeField] private TextMeshProUGUI restartButtonText; // New Reference

    private GameManager cachedGameManager;

    private void Start()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalizedText;
            UpdateLocalizedText();
        }
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalizedText;
        }
    }

    private void OnEnable()
    {
        UpdateLocalizedText();
    }

    private void UpdateLocalizedText()
    {
        if (LocalizationManager.Instance == null) return;

        TMP_FontAsset font = LocalizationManager.Instance.GetFontForCurrentLanguage();

        if (pauseTitleText != null)
        {
            pauseTitleText.text = LocalizationManager.Instance.GetLocalizedString("Paused");
            if (font != null) LocalizationManager.Instance.ApplyFont(pauseTitleText, font);
        }

        if (resumeButtonText != null)
        {
            resumeButtonText.text = LocalizationManager.Instance.GetLocalizedString("Resume");
            if (font != null) LocalizationManager.Instance.ApplyFont(resumeButtonText, font);
        }

        if (settingsButtonText != null)
        {
            settingsButtonText.text = LocalizationManager.Instance.GetLocalizedString("Settings");
            if (font != null) LocalizationManager.Instance.ApplyFont(settingsButtonText, font);
        }

        if (mainMenuButtonText != null)
        {
            mainMenuButtonText.text = LocalizationManager.Instance.GetLocalizedString("Main Menu");
            if (font != null) LocalizationManager.Instance.ApplyFont(mainMenuButtonText, font);
        }

        if (restartButtonText != null)
        {
            restartButtonText.text = LocalizationManager.Instance.GetLocalizedString("Restart");
            if (font != null) LocalizationManager.Instance.ApplyFont(restartButtonText, font);
        }
    }

    private void Awake()
    {
        CacheGameManager();
    }

    private void CacheGameManager()
    {
        if (cachedGameManager == null)
        {
            cachedGameManager = GameManager.Instance;
        }
    }

    public void Resume()
    {
        if (cachedGameManager == null)
        {
            CacheGameManager();
            if (cachedGameManager == null)
            {
                return;
            }
        }

        cachedGameManager.SetPaused(false);
    }

    public void Settings()
    {
        if (cachedGameManager == null)
        {
            CacheGameManager();
            if (cachedGameManager == null)
            {
                return;
            }
        }

        cachedGameManager.OpenSettings();
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(0);
    }

    public void Restart()
    {
        if (cachedGameManager == null)
        {
            CacheGameManager();
            if (cachedGameManager == null)
            {
                return;
            }
        }

        cachedGameManager.RestartGame();
    }
}
