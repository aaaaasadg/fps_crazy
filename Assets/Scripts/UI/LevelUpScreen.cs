using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelUpScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform container;
    [SerializeField] private GameObject upgradeCardPrefab;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollButtonText;

    private UpgradeSystem upgradeSystem;
    private PlayerStats playerStats;
    private bool hasRerolledThisLevel = false;

    /// <summary>
    /// Returns true if the level up panel is currently showing.
    /// </summary>
    public bool IsPanelActive()
    {
        return panel != null && panel.activeInHierarchy;
    }

    private void Awake()
    {
        upgradeSystem = FindFirstObjectByType<UpgradeSystem>();
        playerStats = FindFirstObjectByType<PlayerStats>();
        
        if (panel != null)
            panel.SetActive(false);

        // Setup reroll button
        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(OnRerollButtonClicked);
            rerollButton.gameObject.SetActive(false); // Hidden initially
        }

        UpdateRerollButtonText();
    }

    public void Show()
    {
        // Reset reroll flag for new level
        hasRerolledThisLevel = false;

        // 1. Ensure panel.SetActive(true) is called.
        if (panel != null)
        {
            panel.SetActive(true);
        }

        // 2. Ensure container is not null.
        if (container == null)
        {
            Debug.LogWarning("LevelUpScreen: container reference is null!");
            return;
        }

        // Show reroll button
        if (rerollButton != null)
        {
            rerollButton.gameObject.SetActive(true);
            rerollButton.interactable = true;
        }

        ShowUpgradeChoices();
    }

    private void ShowUpgradeChoices()
    {
        // Clear old upgrade cards
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }

        if (upgradeSystem != null)
        {
            var upgrades = upgradeSystem.GetRandomUpgrades(3);

            // 3. Debug.Log with number of choices
            Debug.Log("Showing Level Up Screen with " + upgrades.Count + " choices.");

            foreach (var choice in upgrades)
            {
                GameObject cardObj = Instantiate(upgradeCardPrefab, container);
                UpgradeUI upgradeUI = cardObj.GetComponent<UpgradeUI>();
                if (upgradeUI != null)
                {
                    upgradeUI.Setup(choice, OnUpgradeSelected);
                }
            }
        }
    }

    /// <summary>
    /// Shows the chest reward UI using a single random item from UpgradeSystem.
    /// </summary>
    /// <param name="guaranteeLegendary">If true, the chest will always give a legendary item (boss chests).</param>
    public void ShowChestReward(bool guaranteeLegendary = false)
    {
        // 1. Call upgradeSystem.GetRandomItem()
        if (upgradeSystem == null)
        {
            Debug.LogWarning("LevelUpScreen: UpgradeSystem reference is null!");
            return;
        }

        var reward = upgradeSystem.GetRandomItem(guaranteeLegendary);

        // 2. Clear container.
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }

        // 3. Spawn ONE card using the result.
        if (reward != null)
        {
            GameObject cardObj = Instantiate(upgradeCardPrefab, container);
            UpgradeUI upgradeUI = cardObj.GetComponent<UpgradeUI>();
            if (upgradeUI != null)
            {
                // For chest, auto-apply on card click (optional: auto-apply immediately)
                upgradeUI.Setup(reward, OnChestRewardSelected);
            }
        }

        // 4. Show panel.
        if (panel != null)
            panel.SetActive(true);
    }

    private void OnUpgradeSelected(UpgradeSystem.UpgradeChoice choice)
    {
        if (upgradeSystem != null)
            upgradeSystem.ApplyUpgrade(choice);

        // Hide pylon progress slider when upgrade is selected
        if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
        {
            GameManager.Instance.hudManager.HidePylonProgress();
        }

        if (panel != null)
            panel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();
    }

    private void OnChestRewardSelected(UpgradeSystem.UpgradeChoice choice)
    {
        if (upgradeSystem != null)
            upgradeSystem.ApplyUpgrade(choice);

        if (panel != null)
            panel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();
    }

    // ===== REROLL FUNCTIONALITY =====

    private void OnRerollButtonClicked()
    {
        if (hasRerolledThisLevel)
        {
            Debug.LogWarning("[LevelUpScreen] Already rerolled this level!");
            return;
        }

        if (PlatformManager.Instance == null || !PlatformManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[LevelUpScreen] SDK not ready for rewarded ad");
            return;
        }

        // Disable button while showing ad
        if (rerollButton != null)
            rerollButton.interactable = false;

        // Show rewarded ad
        PlatformManager.Instance.ShowRewardedAd(
            onRewarded: OnRerollAdWatched,
            onAdClosed: OnRerollAdClosed,
            onAdError: OnRerollAdError
        );
    }

    private void OnRerollAdWatched()
    {
        Debug.Log("[LevelUpScreen] Reroll ad watched - granting reroll");
        
        hasRerolledThisLevel = true;

        // Regenerate upgrade choices
        ShowUpgradeChoices();

        // Hide reroll button after use
        if (rerollButton != null)
            rerollButton.gameObject.SetActive(false);
    }

    private void OnRerollAdClosed()
    {
        // Re-enable button if ad was closed without reward
        if (!hasRerolledThisLevel && rerollButton != null)
        {
            rerollButton.interactable = true;
        }
    }

    private void OnRerollAdError(string error)
    {
        Debug.LogWarning($"[LevelUpScreen] Reroll ad error: {error}");
        
        // Re-enable button if ad failed
        if (!hasRerolledThisLevel && rerollButton != null)
        {
            rerollButton.interactable = true;
        }
    }

    private void UpdateRerollButtonText()
    {
        if (rerollButtonText != null)
        {
            string rerollText = "Reroll";
            if (LocalizationManager.Instance != null)
            {
                rerollText = LocalizationManager.Instance.GetLocalizedString("Reroll");
            }
            rerollButtonText.text = rerollText;
        }
    }
}
