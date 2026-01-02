using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Serializable]
    private class StatIconEntry
    {
        public StatType statType;
        public Sprite icon;
    }

    private const int ItemsPerPage = 10;

    [Header("References")]
    [SerializeField] private Transform listContainer; // The content of the scroll view
    [SerializeField] private GameObject shopItemPrefab; // Should have ShopItem component
    [SerializeField] private TextMeshProUGUI totalSoulsText; // Shows Souls total
    [SerializeField] private Button nextPageButton; // Advances the shop page
    [SerializeField] private Button previousPageButton; // Goes back one page
    [SerializeField] private Button resetAllButton; // QA: Reset all upgrades to 0

    [Header("Visuals")]
    [SerializeField] private StatIconEntry[] statIconEntries;
    [SerializeField] private Vector2 iconSize = new Vector2(128f, 128f);

    private readonly Dictionary<StatType, Sprite> statIconLookup = new Dictionary<StatType, Sprite>();
    private readonly List<StatType> statTypes = new List<StatType>();
    private int currentPage;

    private void Awake()
    {
        BuildIconLookup();
        CacheStatTypes();
        SetupButtons();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateUI;
        }
        UpdateUI();
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateUI;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        BuildIconLookup();
        CacheStatTypes();
        SetupButtons();
    }
#endif

    // Start removed as OnEnable handles initial UpdateUI
    // private void Start() { UpdateUI(); }

    public void UpdateUI()
    {
        // Update Souls Display
        if (totalSoulsText != null && SaveManager.Instance != null && SaveManager.Instance.data != null)
        {
            string soulsLabel = "Souls: ";
            if (LocalizationManager.Instance != null)
                soulsLabel = LocalizationManager.Instance.GetLocalizedString("Souls") + ": ";
            totalSoulsText.text = soulsLabel + SaveManager.Instance.data.souls;
            
            // Apply font
            if (LocalizationManager.Instance != null)
            {
                TMP_FontAsset font = LocalizationManager.Instance.GetFontForCurrentLanguage();
                if (font != null) LocalizationManager.Instance.ApplyFont(totalSoulsText, font);
            }
        }

        if (listContainer == null || shopItemPrefab == null || statTypes.Count == 0)
            return;

        int totalPages = Mathf.CeilToInt(statTypes.Count / (float)ItemsPerPage);
        if (totalPages <= 0)
            return;

        currentPage = Mathf.Clamp(currentPage, 0, Mathf.Max(totalPages - 1, 0));

        // Clear shop list
        for (int i = listContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(listContainer.GetChild(i).gameObject);
        }

        int startIndex = currentPage * ItemsPerPage;
        int endIndex = Mathf.Min(startIndex + ItemsPerPage, statTypes.Count);

        TMP_FontAsset languageFont = null;
        if (LocalizationManager.Instance != null)
        {
            languageFont = LocalizationManager.Instance.GetFontForCurrentLanguage();
        }

        for (int i = startIndex; i < endIndex; i++)
        {
            StatType type = statTypes[i];
            GameObject itemGO = Instantiate(shopItemPrefab, listContainer);
            ShopItem shopItem = itemGO.GetComponent<ShopItem>();

            int currentLevel = (SaveManager.Instance != null) ? SaveManager.Instance.GetStatLevel(type) : 0;
            int cost = 10 * (currentLevel + 1);

            int level = currentLevel;
            float bonus = StatMetaProgression.GetMetaBonus(type, level);

            if (shopItem == null)
                continue;

            // Ensure item and button are active/visible
            shopItem.gameObject.SetActive(true);
            if (shopItem.BuyButton != null)
                shopItem.BuyButton.gameObject.SetActive(true);

            // Set separate name and level labels
            if (shopItem.NameText != null)
            {
                string displayName = GetDisplayName(type);
                if (LocalizationManager.Instance != null)
                {
                    // Try to localize the stat name
                    displayName = LocalizationManager.Instance.GetLocalizedString(displayName);
                }
                
                shopItem.NameText.text = displayName;
                if (languageFont != null) LocalizationManager.Instance.ApplyFont(shopItem.NameText, languageFont);

#if TMP_TEXT_WRAPPING_MODE_ENUM
                shopItem.NameText.textWrappingMode = TextWrappingModes.NoWrap;
#else
                shopItem.NameText.overflowMode = TextOverflowModes.Overflow;
#endif
            }
            if (shopItem.LevelText != null)
            {
                string lvlText = "Lvl ";
                if (LocalizationManager.Instance != null)
                    lvlText = LocalizationManager.Instance.GetLocalizedString("Lvl") + " ";
                shopItem.LevelText.text = lvlText + level;
                if (languageFont != null) LocalizationManager.Instance.ApplyFont(shopItem.LevelText, languageFont);
            }

            // 2. Set boost value text using shared meta progression formatting
            if (shopItem.BoostValueText != null)
            {
                shopItem.BoostValueText.text = StatMetaProgression.FormatBonusText(type, bonus);
                shopItem.BoostValueText.color = Color.green;
                if (languageFont != null) LocalizationManager.Instance.ApplyFont(shopItem.BoostValueText, languageFont);
            }

            // 3. Assign icon per stat type
            shopItem.SetIcon(GetIconForStat(type));
            shopItem.SetIconSize(iconSize);

            // Set cost text: "{cost} Souls"
            if (shopItem.CostText != null)
            {
                string soulsText = "Souls";
                if (LocalizationManager.Instance != null)
                    soulsText = LocalizationManager.Instance.GetLocalizedString("Souls");
                shopItem.CostText.text = $"{cost} {soulsText}";
                if (languageFont != null) LocalizationManager.Instance.ApplyFont(shopItem.CostText, languageFont);
            }

            // Setup Buy Button
            if (shopItem.BuyButton != null)
            {
                shopItem.BuyButton.onClick.RemoveAllListeners();
                StatType capturedType = type; // Prevent closure issues
                int capturedCost = cost;
                shopItem.BuyButton.onClick.AddListener(() => BuyUpgrade(capturedType, capturedCost));
            }
        }

        UpdateNextButtonState(totalPages);
    }

    private void BuildIconLookup()
    {
        statIconLookup.Clear();

        if (statIconEntries == null)
            return;

        for (int i = 0; i < statIconEntries.Length; i++)
        {
            StatIconEntry entry = statIconEntries[i];
            if (entry == null)
                continue;

            statIconLookup[entry.statType] = entry.icon;
        }
    }

    private Sprite GetIconForStat(StatType type)
    {
        return statIconLookup.TryGetValue(type, out Sprite icon) ? icon : null;
    }

    private string GetDisplayName(StatType type)
    {
        if (type == StatType.DamageReduction)
            return "Armor";
        return type.ToString();
    }

    private void CacheStatTypes()
    {
        statTypes.Clear();
        Array values = Enum.GetValues(typeof(StatType));
        for (int i = 0; i < values.Length; i++)
        {
            StatType type = (StatType)values.GetValue(i);
            statTypes.Add(type);
        }
    }

    private void SetupButtons()
    {
        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveAllListeners();
            nextPageButton.onClick.AddListener(NextPage);
        }

        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveAllListeners();
            previousPageButton.onClick.AddListener(PreviousPage);
        }

        if (resetAllButton != null)
        {
            resetAllButton.onClick.RemoveAllListeners();
            resetAllButton.onClick.AddListener(ResetAllUpgrades);
        }
    }

    private void NextPage()
    {
        if (statTypes.Count == 0)
            return;

        int totalPages = Mathf.CeilToInt(statTypes.Count / (float)ItemsPerPage);
        if (totalPages <= 1 || currentPage >= totalPages - 1)
            return;

        currentPage++;
        UpdateUI();
    }

    private void PreviousPage()
    {
        if (statTypes.Count == 0)
            return;

        if (currentPage <= 0)
            return;

        currentPage--;
        UpdateUI();
    }

    private void UpdateNextButtonState(int totalPages)
    {
        bool hasMultiplePages = totalPages > 1;
        SetButtonState(previousPageButton, hasMultiplePages && currentPage > 0);
        SetButtonState(nextPageButton, hasMultiplePages && currentPage < totalPages - 1);
    }

    private void SetButtonState(Button button, bool visible)
    {
        if (button == null)
            return;

        if (button.gameObject.activeSelf != visible)
            button.gameObject.SetActive(visible);
        button.interactable = visible;
    }

    public void BuyUpgrade(StatType type, int cost)
    {
        if (SaveManager.Instance == null || SaveManager.Instance.data == null)
            return;

        if (SaveManager.Instance.data.souls >= cost)
        {
            SaveManager.Instance.data.souls -= cost;
            SaveManager.Instance.UpgradeStat(type);
            
            // Play purchase sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayShopBuy();
            }

            UpdateUI();
        }
    }

    /// <summary>
    /// QA: Resets all purchased upgrades to 0. Does not affect souls.
    /// </summary>
    public void ResetAllUpgrades()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.data == null)
            return;

        // Reset all stat levels to 0
        if (SaveManager.Instance.data.statLevels != null)
        {
            for (int i = 0; i < SaveManager.Instance.data.statLevels.Length; i++)
            {
                SaveManager.Instance.data.statLevels[i] = 0;
            }
        }

        // Save the changes
        SaveManager.Instance.Save();

        // Refresh the UI
        UpdateUI();
    }
}
