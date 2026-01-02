using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;
    [SerializeField] private string[] formatArgs;

    private TextMeshProUGUI textComponent;
    private TMP_FontAsset originalFont;
    private float originalFontSize;
    private FontStyles originalFontStyle;
    private FontWeight originalFontWeight;
    private Material originalMaterial;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            // Store original font settings to preserve them
            originalFont = textComponent.font;
            originalFontSize = textComponent.fontSize;
            originalFontStyle = textComponent.fontStyle;
            originalFontWeight = textComponent.fontWeight;
            originalMaterial = textComponent.material;
        }
    }

    private void Start()
    {
        UpdateText();
    }

    public void UpdateText()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        if (textComponent == null || string.IsNullOrEmpty(localizationKey))
            return;

        if (LocalizationManager.Instance == null)
            return;

        // Store current font settings if not already stored
        if (originalFont == null)
        {
            originalFont = textComponent.font;
            originalFontSize = textComponent.fontSize;
            originalFontStyle = textComponent.fontStyle;
            originalFontWeight = textComponent.fontWeight;
            originalMaterial = textComponent.material;
        }

        object[] args = formatArgs != null && formatArgs.Length > 0 ? formatArgs : null;
        string localizedText = LocalizationManager.Instance.GetLocalizedString(localizationKey, args);
        
        // Get appropriate font for current language
        TMP_FontAsset languageFont = LocalizationManager.Instance.GetFontForCurrentLanguage();
        
        // Update text while preserving font settings
        textComponent.text = localizedText;
        
        // Apply language-specific font using helper that handles outline thickness
        if (languageFont != null && languageFont != TMP_Settings.defaultFontAsset)
        {
            LocalizationManager.Instance.ApplyFont(textComponent, languageFont);
        }
        else if (originalFont != null)
        {
            LocalizationManager.Instance.ApplyFont(textComponent, originalFont);
        }
        
        // Preserve all other font settings
        textComponent.fontSize = originalFontSize;
        textComponent.fontStyle = originalFontStyle;
        textComponent.fontWeight = originalFontWeight;
        
        // Do NOT restore originalMaterial as it may be incompatible with the new font asset
        // The ApplyFont method handles setting the correct outline thickness on the new material instance
    }

    public void SetKey(string key)
    {
        localizationKey = key;
        UpdateText();
    }

    public void SetFormatArgs(params string[] args)
    {
        formatArgs = args;
        UpdateText();
    }
}

