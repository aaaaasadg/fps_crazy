using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TextMeshProUGUI sensitivityLabelText;
    [SerializeField] private TextMeshProUGUI sensitivityValueText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeValueText;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeValueText;
    [SerializeField] private Button englishButton;
    [SerializeField] private Button russianButton;
    [SerializeField] private Button turkishButton;

    [SerializeField] private TextMeshProUGUI settingsTitleText;
    [SerializeField] private TextMeshProUGUI backButtonText;

    private PlayerController playerController;

    private void Start()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = 15f;
        }

        // Subscribe to language changes
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalizedText;
        }

        // Initial update
        UpdateLocalizedText();
        UpdateSensitivityLabel();
        
        // Setup language buttons
        SetupLanguageButtons();

        // Initialize audio sliders (don't override min/max - use inspector values)
        if (sfxVolumeSlider != null)
        {
            if (SoundManager.Instance != null)
            {
                // Slider value maps directly to volume multiplier (e.g. 1 = 100%, 5 = 500%)
                float savedVolume = SoundManager.Instance.GetSFXVolume();
                sfxVolumeSlider.value = savedVolume;
                UpdateSFXVolumeText(sfxVolumeSlider.value);
            }
        }

        if (musicVolumeSlider != null)
        {
            if (SoundManager.Instance != null)
            {
                // Slider value maps directly to volume multiplier
                float savedVolume = SoundManager.Instance.GetMusicVolume();
                musicVolumeSlider.value = savedVolume;
                UpdateMusicVolumeText(musicVolumeSlider.value);
            }
        }

        // Try to fetch the player controller safely
        if (GameManager.Instance != null)
        {
            // GameManager should provide a way to get player Transform or use the reference if public
            Transform playerTransform = GameManager.Instance.playerTransform;
            if (playerTransform != null)
            {
                playerController = playerTransform.GetComponent<PlayerController>();
                if (playerController != null && sensitivitySlider != null)
                {
                    sensitivitySlider.value = playerController.GetSensitivity();
                    UpdateSensitivityText(sensitivitySlider.value);
                }
            }
        }
        else
        {
            // Main menu: load sensitivity from PlayerPrefs
            if (sensitivitySlider != null)
            {
                float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2.0f);
                sensitivitySlider.value = savedSensitivity;
                UpdateSensitivityText(sensitivitySlider.value);
            }
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
        
        // Update Sensitivity Label
        UpdateSensitivityLabel();
        if (sensitivityLabelText != null && font != null) LocalizationManager.Instance.ApplyFont(sensitivityLabelText, font);
        if (sensitivityValueText != null && font != null) LocalizationManager.Instance.ApplyFont(sensitivityValueText, font);

        // Update Volume Labels - Force static label text instead of value percentage
        if (sfxVolumeValueText != null)
        {
            sfxVolumeValueText.text = LocalizationManager.Instance.GetLocalizedString("SFX Volume");
            if (font != null) LocalizationManager.Instance.ApplyFont(sfxVolumeValueText, font);
        }
        if (musicVolumeValueText != null)
        {
            musicVolumeValueText.text = LocalizationManager.Instance.GetLocalizedString("Music Volume");
            if (font != null) LocalizationManager.Instance.ApplyFont(musicVolumeValueText, font);
        }

        // Update Static Text
        if (settingsTitleText != null)
        {
            settingsTitleText.text = LocalizationManager.Instance.GetLocalizedString("Settings");
            if (font != null) LocalizationManager.Instance.ApplyFont(settingsTitleText, font);
        }
        
        if (backButtonText != null)
        {
            backButtonText.text = LocalizationManager.Instance.GetLocalizedString("Back");
            if (font != null) LocalizationManager.Instance.ApplyFont(backButtonText, font);
        }
    }

    public void OnSensitivityChanged(float value)
    {
        if (playerController != null)
        {
            playerController.SetSensitivity(value);
        }
        // Save to PlayerPrefs so it persists in main menu too
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
        UpdateSensitivityText(value);
    }

    private void UpdateSensitivityText(float value)
    {
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = value.ToString("F1");
        }
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (SoundManager.Instance != null)
        {
            // Pass raw value as multiplier (e.g. 2.5 = 250%)
            SoundManager.Instance.SetSFXVolume(value);
        }
        UpdateSFXVolumeText(value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        if (SoundManager.Instance != null)
        {
            // Pass raw value as multiplier
            SoundManager.Instance.SetMusicVolume(value);
        }
        UpdateMusicVolumeText(value);
    }

    private void UpdateSFXVolumeText(float value)
    {
        // Disabled percentage display as per request - static label is handled in UpdateLocalizedText
        // if (sfxVolumeValueText != null)
        // {
        //    sfxVolumeValueText.text = Mathf.RoundToInt(value * 100f).ToString() + "%";
        // }
    }

    private void UpdateMusicVolumeText(float value)
    {
        // Disabled percentage display as per request - static label is handled in UpdateLocalizedText
        // if (musicVolumeValueText != null)
        // {
        //    musicVolumeValueText.text = Mathf.RoundToInt(value * 100f).ToString() + "%";
        // }
    }

    private void SetupLanguageButtons()
    {
        if (englishButton != null)
        {
            englishButton.onClick.RemoveAllListeners();
            englishButton.onClick.AddListener(() => SetLanguage(LocalizationManager.Language.English));
        }

        if (russianButton != null)
        {
            russianButton.onClick.RemoveAllListeners();
            russianButton.onClick.AddListener(() => SetLanguage(LocalizationManager.Language.Russian));
        }

        if (turkishButton != null)
        {
            turkishButton.onClick.RemoveAllListeners();
            turkishButton.onClick.AddListener(() => SetLanguage(LocalizationManager.Language.Turkish));
        }
    }

    private void SetLanguage(LocalizationManager.Language lang)
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SetLanguage(lang);
            UpdateSensitivityLabel();
        }
    }

    private void UpdateSensitivityLabel()
    {
        if (sensitivityLabelText != null && LocalizationManager.Instance != null)
        {
            sensitivityLabelText.text = LocalizationManager.Instance.GetLocalizedString("Sensitivity");
        }
        else if (sensitivityLabelText != null)
        {
            sensitivityLabelText.text = "Sensitivity";
        }
    }

    public void Back()
    {
        // Try GameManager first (in-game settings)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CloseSettings();
        }
        // Try MainMenu (main menu settings)
        else
        {
            MainMenu mainMenu = FindFirstObjectByType<MainMenu>();
            if (mainMenu != null)
            {
                mainMenu.CloseSettings();
            }
        }
    }
}
