using UnityEngine;
using TMPro;

/// <summary>
/// Debug panel to test and verify language detection.
/// Shows detected language, current language, and allows testing.
/// REMOVE or disable this before final release.
/// </summary>
public class LanguageDebugPanel : MonoBehaviour
{
    [Header("Debug UI")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private bool showOnStart = true;

    private void Start()
    {
        if (debugPanel != null && !showOnStart)
        {
            debugPanel.SetActive(false);
        }

        UpdateDebugInfo();
        InvokeRepeating(nameof(UpdateDebugInfo), 1f, 1f);
    }

    private void Update()
    {
        // Press F1 to toggle debug panel
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (debugPanel != null)
            {
                debugPanel.SetActive(!debugPanel.activeSelf);
            }
        }
    }

    private void UpdateDebugInfo()
    {
        if (debugText == null) return;

        string info = "=== LANGUAGE DEBUG ===\n\n";

        // Platform Manager status
        if (PlatformManager.Instance != null)
        {
            info += $"Platform: {PlatformManager.Instance.CurrentPlatform}\n";
            info += $"SDK Init: {PlatformManager.Instance.IsInitialized}\n";

            if (PlatformManager.Instance.IsInitialized)
            {
                string sdkLang = PlatformManager.Instance.GetPlayerLanguage();
                info += $"SDK Lang: '{sdkLang}'\n";
                
                // Show what this SHOULD convert to
                LocalizationManager.Language expectedLang;
                switch (sdkLang.ToLower())
                {
                    case "ru": expectedLang = LocalizationManager.Language.Russian; break;
                    case "tr": expectedLang = LocalizationManager.Language.Turkish; break;
                    default: expectedLang = LocalizationManager.Language.English; break;
                }
                info += $"Expected: {expectedLang}\n";
            }
            else
            {
                info += $"SDK Lang: (Waiting...)\n";
            }
        }
        else
        {
            info += $"❌ PlatformManager: NULL\n";
        }

        info += "\n";

        // Current language
        if (LocalizationManager.Instance != null)
        {
            var currentLang = LocalizationManager.Instance.GetCurrentLanguage();
            info += $"CURRENT LANGUAGE:\n";
            info += $">>> {currentLang} <<<\n";
            
            // Show sample translation to verify
            string playText = LocalizationManager.Instance.GetLocalizedString("Play");
            info += $"\n'Play' = '{playText}'\n";
            
            string mainMenuText = LocalizationManager.Instance.GetLocalizedString("Main Menu");
            info += $"'Main Menu' = '{mainMenuText}'\n";
        }
        else
        {
            info += $"❌ LocalizationManager: NULL\n";
        }

        info += "\n";
        info += $"URL: {Application.absoluteURL.Substring(0, Mathf.Min(50, Application.absoluteURL.Length))}\n";

        info += "\nF1=Toggle F2=EN F3=RU F4=TR\n";

        debugText.text = info;

        // Hotkeys for testing
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ForceLanguage(LocalizationManager.Language.English);
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            ForceLanguage(LocalizationManager.Language.Russian);
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            ForceLanguage(LocalizationManager.Language.Turkish);
        }
    }

    private void ForceLanguage(LocalizationManager.Language lang)
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SetLanguage(lang);
            Debug.Log($"[LanguageDebug] Forced language to: {lang}");
        }
    }
}

