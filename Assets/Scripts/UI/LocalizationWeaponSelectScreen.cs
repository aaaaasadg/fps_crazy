using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LocalizationWeaponSelectScreen : MonoBehaviour
{
    [System.Serializable]
    public class LocalizedField
    {
        public TextMeshProUGUI targetText;
        [TextArea(1, 3)] public string englishText;
        [TextArea(1, 3)] public string russianText;
        [TextArea(1, 3)] public string turkishText;
    }

    [System.Serializable]
    public class WeaponCardData
    {
        public string name; // Just for editor organization
        public LocalizedField title;
        public LocalizedField description;
        public LocalizedField bonus;
        public LocalizedField milestones;
    }

    [Header("Panel Title")]
    [SerializeField] private LocalizedField screenTitle;

    [Header("Weapon Cards")]
    [SerializeField] private WeaponCardData[] cards;

    private void Start()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateTranslations;
            UpdateTranslations();
        }
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateTranslations;
        }
    }

    private void OnValidate()
    {
        // Update in editor when changing values
        if (!Application.isPlaying)
        {
            UpdateTranslations();
        }
    }

    private void UpdateTranslations()
    {
        if (LocalizationManager.Instance == null) return;

        LocalizationManager.Language lang = LocalizationManager.Instance.GetCurrentLanguage();
        TMP_FontAsset font = LocalizationManager.Instance.GetFontForCurrentLanguage();

        // Update Screen Title
        ApplyText(screenTitle, lang, font, isTitle: true);

        // Update Cards
        if (cards != null)
        {
            foreach (var card in cards)
            {
                ApplyText(card.title, lang, font, isTitle: true);
                ApplyText(card.description, lang, font, isTitle: false);
                ApplyText(card.bonus, lang, font, isTitle: false);
                ApplyText(card.milestones, lang, font, isTitle: false);
            }
        }
    }

    private void ApplyText(LocalizedField field, LocalizationManager.Language lang, TMP_FontAsset font, bool isTitle)
    {
        if (field == null || field.targetText == null) return;

        // 1. Select text based on language
        string textToUse = field.englishText; // Default
        switch (lang)
        {
            case LocalizationManager.Language.English:
                textToUse = field.englishText;
                break;
            case LocalizationManager.Language.Russian:
                textToUse = field.russianText;
                break;
            case LocalizationManager.Language.Turkish:
                textToUse = field.turkishText;
                break;
        }

        // 2. Apply Font & Outline
        if (font != null && LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.ApplyFont(field.targetText, font);
        }

        // 3. Set Text
        field.targetText.text = textToUse;

        // 4. Auto-size settings (Force fit but respect original size)
        field.targetText.textWrappingMode = TextWrappingModes.Normal;
        
        // If auto-size is not already enabled, enable it but set max size to current size
        if (!field.targetText.enableAutoSizing)
        {
            field.targetText.enableAutoSizing = true;
            // Use the current editor font size as the maximum preference
            field.targetText.fontSizeMax = field.targetText.fontSize; 
            field.targetText.fontSizeMin = 14f; // Don't let it get unreadable
        }
        else
        {
            // If already auto-sizing, ensure min size isn't too small
            if (field.targetText.fontSizeMin < 14f) field.targetText.fontSizeMin = 14f;
        }
    }
}

