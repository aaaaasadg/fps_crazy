using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LeaderboardUI : MonoBehaviour
{
    [System.Serializable]
    public struct LanguageVisuals
    {
        public Sprite sprite;
        public Vector2 anchoredPosition;
    }

    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform entriesContainer; // Parent container for leaderboard entries
    [SerializeField] private GameObject entryPrefab; // Prefab for a single leaderboard entry (TextMeshPro text)
    [SerializeField] private TextMeshProUGUI titleText; // "Leaderboard" title

    [Header("Localization Visuals")]
    [SerializeField] private Image dynamicImage;
    [SerializeField] private LanguageVisuals englishVisuals;
    [SerializeField] private LanguageVisuals russianVisuals;
    [SerializeField] private LanguageVisuals turkishVisuals;

    private void Start()
    {
        // Subscribe to language changes
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += RefreshLeaderboard;
        }

        // Show leaderboard panel and refresh it
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
            RefreshLeaderboard();
        }
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= RefreshLeaderboard;
        }
    }

    public void ShowLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
            RefreshLeaderboard();
        }
    }

    public void HideLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }

    private void RefreshLeaderboard()
    {
        if (LocalizationManager.Instance != null)
        {
            if (titleText != null)
            {
                titleText.text = LocalizationManager.Instance.GetLocalizedString("Leaderboard");
                TMP_FontAsset font = LocalizationManager.Instance.GetFontForCurrentLanguage();
                if (font != null) LocalizationManager.Instance.ApplyFont(titleText, font);
            }

            if (dynamicImage != null)
            {
                LanguageVisuals visuals = englishVisuals; // Default
                switch (LocalizationManager.Instance.GetCurrentLanguage())
                {
                    case LocalizationManager.Language.English: visuals = englishVisuals; break;
                    case LocalizationManager.Language.Russian: visuals = russianVisuals; break;
                    case LocalizationManager.Language.Turkish: visuals = turkishVisuals; break;
                }

                if (visuals.sprite != null) dynamicImage.sprite = visuals.sprite;
                dynamicImage.rectTransform.anchoredPosition = visuals.anchoredPosition;
            }
        }

        if (entriesContainer == null || entryPrefab == null)
        {
            Debug.LogWarning("LeaderboardUI: entriesContainer or entryPrefab is not assigned!");
            return;
        }

        // Clear existing entries
        foreach (Transform child in entriesContainer)
        {
            Destroy(child.gameObject);
        }

        // Get leaderboard data
        if (LeaderboardManager.Instance == null)
        {
            Debug.LogWarning("LeaderboardUI: LeaderboardManager.Instance is null!");
            return;
        }

        List<LeaderboardManager.LeaderboardEntry> entries = LeaderboardManager.Instance.GetLeaderboard();

        // Create UI entries
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            GameObject entryObj = Instantiate(entryPrefab, entriesContainer);
            
            TextMeshProUGUI textComponent = entryObj.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                string killsText = "kills";
                if (LocalizationManager.Instance != null)
                {
                    killsText = LocalizationManager.Instance.GetLocalizedString("kills");
                    TMP_FontAsset font = LocalizationManager.Instance.GetFontForCurrentLanguage();
                    if (font != null) LocalizationManager.Instance.ApplyFont(textComponent, font);
                }
                textComponent.text = $"{i + 1}. {entry.playerName} - {entry.kills} {killsText}";
            }
        }
    }
}

