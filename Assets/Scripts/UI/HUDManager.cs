using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class HUDManager : MonoBehaviour
{
    [Header("XP UI")]
    [SerializeField] private Slider xpSlider;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("HP UI")]
    [SerializeField] private Slider hpSlider;

    [Header("Gold UI")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("DPS UI")]
    [SerializeField] private TextMeshProUGUI dpsText;

    [Header("Timer UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Souls UI")]
    [SerializeField] private TextMeshProUGUI soulsText;

    [Header("Kills UI")]
    [SerializeField] private Image killsIcon;
    [SerializeField] private TextMeshProUGUI killsText;

    [Header("Boss Health UI")]
    [SerializeField] private GameObject bossHealthPanel; // Parent panel to show/hide
    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private TextMeshProUGUI bossHealthText;

    [Header("Interaction UI")]
    [SerializeField] private TextMeshProUGUI interactionText; // Assign in inspector

    [Header("Milestone UI")]
    [SerializeField] private TextMeshProUGUI milestoneText;

    [Header("Ammo UI")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image reloadCircle;

    [Header("Pylon Progress UI")]
    [SerializeField] private Slider pylonProgressSlider; // Assign slider in Inspector

    [Header("Crosshair UI")]
    [SerializeField] private GameObject crosshairObject; // The crosshair GameObject to show/hide

    [Header("Stats Panel (shows on pause)")]
    [SerializeField] private GameObject statsPanelObject; // The stats panel GameObject
    [SerializeField] private StatsPanel statsPanel; // The StatsPanel component for refreshing stats

    [Header("Damage Indicators")]
    [SerializeField] private GameObject indicatorPrefab; // The red arrow prefab (image inside a pivot wrapper)
    [SerializeField] private Transform indicatorContainer; // Parent object (center of screen)
    [SerializeField] private int indicatorPoolSize = 10;
    [SerializeField] private float indicatorFadeTime = 2f;
    [SerializeField] private float indicatorRadius = 400f; // Distance from center (default 400, was 300 in prefab)

    // Damage Flash UI REMOVED

    private Coroutine milestoneRoutine;
    // private Coroutine damageFlashRoutine; // Removed

    private List<DamageIndicatorInstance> activeIndicators = new List<DamageIndicatorInstance>();
    private Queue<DamageIndicatorInstance> indicatorPool = new Queue<DamageIndicatorInstance>();
    private Transform playerTransform;

    private class DamageIndicatorInstance
    {
        public GameObject root;       // The Pivot object
        public CanvasGroup group;     // Canvas Group for fading
        public Transform attacker;    // Who caused this?
        public float timer;           // Fade timer
    }

    [Header("Localization")]
    [SerializeField] private LocalizedString levelPrefix = new LocalizedString { english = "Lv. ", russian = "Ур. ", turkish = "Sev. " };
    [SerializeField] private LocalizedString dpsPrefix = new LocalizedString { english = "DPS: ", russian = "УВС: ", turkish = "SPS: " };

    [System.Serializable]
    public class LocalizedString
    {
        public string english;
        public string russian;
        public string turkish;

        public string GetText()
        {
            if (LocalizationManager.Instance == null) return english;
            switch (LocalizationManager.Instance.GetCurrentLanguage())
            {
                case LocalizationManager.Language.Russian: return russian;
                case LocalizationManager.Language.Turkish: return turkish;
                default: return english;
            }
        }
    }

    private void Awake()
    {
        // Ensure Souls Text is always updated with correct initial value from SaveManager
        if (SaveManager.Instance != null && SaveManager.Instance.data != null)
        {
            UpdateSouls(SaveManager.Instance.data.souls);
        }

        // Ensure interaction text is hidden at start
        if (interactionText != null)
        {
            interactionText.enabled = false;
        }
        // Ensure milestone text is hidden at start
        if (milestoneText != null)
        {
            milestoneText.enabled = false;
        }

        // If reloadCircle exists, hide by default
        if (reloadCircle != null)
        {
            reloadCircle.enabled = false;
        }

        // Hide boss health UI at start
        if (bossHealthPanel != null)
        {
            bossHealthPanel.SetActive(false);
        }
        
        // Hide pylon progress slider at start
        if (pylonProgressSlider != null && pylonProgressSlider.gameObject != null)
        {
            pylonProgressSlider.gameObject.SetActive(false);
        }
        
        // Show crosshair at start (will be hidden when UI shows)
        if (crosshairObject != null)
        {
            crosshairObject.SetActive(true);
        }
        
        // Hide stats panel at start (only shows when paused)
        if (statsPanelObject != null)
        {
            statsPanelObject.SetActive(false);
        }
        
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalizedFonts;
            UpdateLocalizedFonts();
        }
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalizedFonts;
        }
    }

    private void UpdateLocalizedFonts()
    {
        if (LocalizationManager.Instance == null) return;
        
        TMP_FontAsset font = LocalizationManager.Instance.GetFontForCurrentLanguage();
        if (font == null) return;

        if (levelText != null) LocalizationManager.Instance.ApplyFont(levelText, font);
        if (goldText != null) LocalizationManager.Instance.ApplyFont(goldText, font);
        if (dpsText != null) LocalizationManager.Instance.ApplyFont(dpsText, font);
        if (timerText != null) LocalizationManager.Instance.ApplyFont(timerText, font);
        if (soulsText != null) LocalizationManager.Instance.ApplyFont(soulsText, font);
        if (killsText != null) LocalizationManager.Instance.ApplyFont(killsText, font);
        if (bossHealthText != null) LocalizationManager.Instance.ApplyFont(bossHealthText, font);
        if (interactionText != null) LocalizationManager.Instance.ApplyFont(interactionText, font);
        if (milestoneText != null) LocalizationManager.Instance.ApplyFont(milestoneText, font);
        if (ammoText != null) LocalizationManager.Instance.ApplyFont(ammoText, font);
        
        // Refresh dynamic text content to apply translations
        if (levelText != null)
        {
            // Extract level number from current text if possible, or wait for next update
            // Safer to just update the static part if we can, but UpdateXP handles the full string.
            // For now, just updating the font is enough for numbers/names, but "Lv." needs translation.
            // Ideally UpdateXP is called or we re-apply the current state if we knew it.
            // Let's trigger a refresh of the current labels where possible.
        }
    }

    /// <summary>
    /// Updates XP bar and level display.
    /// </summary>
    public void UpdateXP(float current, float max, int level)
    {
        if (xpSlider != null && max > 0f)
            xpSlider.value = current / max;
        if (levelText != null)
        {
            string prefix = levelPrefix.GetText();
            levelText.text = prefix + level;
        }
    }

    /// <summary>
    /// Updates HP bar.
    /// </summary>
    public void UpdateHP(float current, float max)
    {
        if (hpSlider != null && max > 0f)
            hpSlider.value = current / max;
    }

    /// <summary>
    /// Updates the gold amount display.
    /// </summary>
    public void UpdateGold(float amount)
    {
        if (goldText != null)
            goldText.text = Mathf.FloorToInt(amount).ToString();
    }

    /// <summary>
    /// Updates the DPS display.
    /// </summary>
    public void UpdateDPS(float dps)
    {
        if (dpsText != null)
        {
            string prefix = dpsPrefix.GetText();
            dpsText.text = prefix + Mathf.RoundToInt(dps);
        }
    }

    public void UpdateTimer(float time)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60F);
            int seconds = Mathf.FloorToInt(time % 60F);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void UpdateSouls(int amount)
    {
        if (soulsText != null)
            soulsText.text = amount.ToString();
    }

    /// <summary>
    /// Updates the kill counter display.
    /// </summary>
    public void UpdateKills(int count)
    {
        if (killsText != null)
            killsText.text = count.ToString();
    }

    /// <summary>
    /// Updates the boss health bar and text.
    /// </summary>
    public void UpdateBossHealth(float current, float max)
    {
        if (bossHealthSlider != null && max > 0f)
        {
            float healthPercent = current / max;
            // Clamp to ensure full fill when at max health (fixes floating point precision issues)
            if (current >= max)
            {
                bossHealthSlider.value = 1f;
            }
            else
            {
                bossHealthSlider.value = Mathf.Clamp01(healthPercent);
            }
        }
        if (bossHealthText != null)
        {
            bossHealthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    /// <summary>
    /// Shows the boss health UI.
    /// </summary>
    public void ShowBossHealth()
    {
        if (bossHealthPanel != null && !bossHealthPanel.activeSelf)
        {
            // Cancel any pending hide delay when showing
            bossHealthHideDelay = 0f;
            bossHealthPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the boss health UI.
    /// </summary>
    public void HideBossHealth()
    {
        if (bossHealthPanel != null && bossHealthPanel.activeSelf)
        {
            bossHealthPanel.SetActive(false);
            // Clear cached boss reference
            cachedBossEnemy = null;
        }
        // Cancel any pending hide delay since we're hiding now
        bossHealthHideDelay = 0f;
    }

    /// <summary>
    /// Updates the ammo amount display.
    /// </summary>
    /// <param name="current">Current ammo.</param>
    /// <param name="max">Maximum ammo.</param>
    public void UpdateAmmo(int current, int max)
    {
        if (ammoText != null)
            ammoText.text = max > 0 ? $"{current} / {max}" : $"{current}";
    }

    /// <summary>
    /// Updates the reload UI; shows or hides the reloadCircle, and animates its fill.
    /// </summary>
    /// <param name="progress">0=just started, 1=done</param>
    /// <param name="isReloading">If true, shows reloadCircle. If false, hides it.</param>
    public void UpdateReload(float progress, bool isReloading)
    {
        if (reloadCircle != null)
        {
            reloadCircle.enabled = isReloading;
            if (isReloading)
            {
                // Animate from full (1) to empty (0) as progress increases
                reloadCircle.fillAmount = 1.0f - Mathf.Clamp01(progress);
            }
        }
    }

    private float bossHealthCheckTimer = 0f;
    private const float bossHealthCheckInterval = 0.2f; // Check every 0.2 seconds (less frequent)
    private float bossHealthHideDelay = 0f; // Delay before hiding boss health UI to prevent flashing
    private const float bossHealthHideDelayTime = 0.5f; // Wait 0.5 seconds before hiding (increased for stability)
    private Enemy cachedBossEnemy = null; // Cache the current boss to avoid repeated searches

    private void Update()
    {
        // Update Damage Indicators
        if (EnsurePlayerTransform())
        {
            for (int i = activeIndicators.Count - 1; i >= 0; i--)
            {
                var ind = activeIndicators[i];
                if (ind == null || ind.root == null) continue;
                
                ind.timer -= Time.deltaTime;

                // Fade alpha
                if (ind.group != null)
                {
                    ind.group.alpha = Mathf.Clamp01(ind.timer / indicatorFadeTime);
                }

                // Update Rotation (track the enemy dynamically while indicator is alive)
                if (ind.attacker != null && playerTransform != null)
                {
                    Vector3 dir = ind.attacker.position - playerTransform.position;
                    dir.y = 0;
                    float angle = Vector3.SignedAngle(playerTransform.forward, dir, Vector3.up);
                    if (ind.root != null)
                    {
                        ind.root.transform.localEulerAngles = new Vector3(0, 0, -angle);
                    }
                }

                // Return to pool if faded
                if (ind.timer <= 0f)
                {
                    if (ind.root != null)
                        ind.root.SetActive(false);
                    indicatorPool.Enqueue(ind);
                    activeIndicators.RemoveAt(i);
                }
            }
        }

        // Update boss health hide delay timer
        if (bossHealthHideDelay > 0f)
        {
            bossHealthHideDelay -= Time.deltaTime;
            if (bossHealthHideDelay <= 0f)
            {
                bossHealthHideDelay = 0f;
                // Only hide if no boss exists after delay
                if (bossHealthPanel != null && bossHealthPanel.activeSelf)
                {
                    // Check cached boss first (faster)
                    bool hasBoss = cachedBossEnemy != null && cachedBossEnemy.gameObject.activeInHierarchy && cachedBossEnemy.IsBoss && !cachedBossEnemy.IsDead;
                    
                    // If cached boss is invalid, do a quick check
                    if (!hasBoss)
                    {
                        UpdateBossHealthIfExists();
                        hasBoss = cachedBossEnemy != null;
                    }
                    
                    if (!hasBoss)
                    {
                        bossHealthPanel.SetActive(false);
                        cachedBossEnemy = null;
                    }
                }
            }
        }

        // Check for active boss and update health UI (less frequently)
        bossHealthCheckTimer += Time.deltaTime;
        if (bossHealthCheckTimer >= bossHealthCheckInterval)
        {
            bossHealthCheckTimer = 0f;
            UpdateBossHealthIfExists();
        }

        // Removed damage flash fade-out block

        // Simple check to keep souls synced if they change elsewhere (e.g. global save updates)
        // Or better: call UpdateSouls from wherever AddSouls is called.
        // For robustness in this setup, we can check periodically or just once in Start if static.
        // Let's rely on AddSouls calling this, but ensure Start does it too.
    }

    private void UpdateBossHealthIfExists()
    {
        // Check if cached boss is still valid
        if (cachedBossEnemy != null && cachedBossEnemy.gameObject.activeInHierarchy && cachedBossEnemy.IsBoss && !cachedBossEnemy.IsDead)
        {
            // Use cached boss - update UI
            bossHealthHideDelay = 0f; // Cancel any pending hide
            
            if (bossHealthPanel != null && !bossHealthPanel.activeSelf)
            {
                bossHealthPanel.SetActive(true);
            }
            
            if (cachedBossEnemy.MaxHP > 0f)
            {
                UpdateBossHealth(cachedBossEnemy.CurrentHP, cachedBossEnemy.MaxHP);
            }
            return;
        }

        // Cached boss is invalid or null - search for a new one
        cachedBossEnemy = null; // Clear invalid cache
        Enemy activeBoss = null;
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.IsBoss && !enemy.IsDead)
            {
                activeBoss = enemy;
                break;
            }
        }
        
        // Update cache
        cachedBossEnemy = activeBoss;

        if (activeBoss != null)
        {
            // Boss exists - show and update UI
            bossHealthHideDelay = 0f; // Cancel any pending hide delay
            
            if (bossHealthPanel != null && !bossHealthPanel.activeSelf)
            {
                bossHealthPanel.SetActive(true);
            }
            
            // Update boss health UI with current values
            if (activeBoss.MaxHP > 0f)
            {
                UpdateBossHealth(activeBoss.CurrentHP, activeBoss.MaxHP);
            }
        }
        else
        {
            // No active boss - start delay before hiding to prevent flashing
            if (bossHealthPanel != null && bossHealthPanel.activeSelf)
            {
                if (bossHealthHideDelay <= 0f)
                {
                    bossHealthHideDelay = bossHealthHideDelayTime;
                }
            }
        }
    }

    private void Start()
    {
        // Initialize indicator pool
        if (indicatorPrefab != null && indicatorContainer != null)
        {
            for (int i = 0; i < indicatorPoolSize; i++)
            {
                GameObject obj = Instantiate(indicatorPrefab, indicatorContainer);
                obj.SetActive(false);
                CanvasGroup cg = obj.GetComponent<CanvasGroup>();
                if (cg == null) cg = obj.AddComponent<CanvasGroup>();

                // Setup indicator: disable Images without sprites and set radius
                SetupIndicator(obj);

                indicatorPool.Enqueue(new DamageIndicatorInstance { root = obj, group = cg });
            }

            // Force Indicators to render behind other HUD elements
            indicatorContainer.SetAsFirstSibling();
        }

        if (SaveManager.Instance != null && SaveManager.Instance.data != null)
        {
            UpdateSouls(SaveManager.Instance.data.souls);
        }
        // No damage flash to initialize
    }

    /// <summary>
    /// Shows the interaction text with custom message and color.
    /// </summary>
    /// <param name="message">The text to display.</param>
    /// <param name="color">The color of the text.</param>
    public void ShowInteractionText(string message, Color color)
    {
        bool isMobile = Application.isMobilePlatform || UnityEngine.Device.Application.isMobilePlatform;
        
        // On mobile, show interact button instead of text
        if (isMobile && MobileInputController.Instance != null)
        {
            MobileInputController.Instance.ShowInteractButton();
            // Ensure text is hidden on mobile
            if (interactionText != null && interactionText.enabled)
            {
                interactionText.enabled = false;
            }
        }
        else
        {
            // On PC, show text - ensure mobile button is hidden first
            if (isMobile && MobileInputController.Instance != null)
            {
                MobileInputController.Instance.HideInteractButton();
            }
            
            // Show text on PC
            if (interactionText != null)
            {
                interactionText.text = message;
                interactionText.color = color;
                interactionText.enabled = true;
            }
        }
    }

    /// <summary>
    /// Hides the interaction text.
    /// </summary>
    public void HideInteractionText()
    {
        bool isMobile = Application.isMobilePlatform || UnityEngine.Device.Application.isMobilePlatform;
        
        // Hide mobile interact button
        if (isMobile && MobileInputController.Instance != null)
        {
            MobileInputController.Instance.HideInteractButton();
        }
        
        // Hide text (works for both PC and mobile)
        if (interactionText != null && interactionText.enabled)
        {
            interactionText.enabled = false;
        }
    }

    /// <summary>
    /// Shows the milestone text with a fade in/out effect.
    /// </summary>
    /// <param name="message">The milestone message to display.</param>
    public void ShowMilestone(string message)
    {
        if (milestoneText != null)
        {
            // Stop existing animation
            if (milestoneRoutine != null)
            {
                StopCoroutine(milestoneRoutine);
            }
            milestoneText.text = message;

            // Set alpha to 0 initially
            Color c = milestoneText.color;
            c.a = 0f;
            milestoneText.color = c;

            milestoneText.enabled = true;
            milestoneRoutine = StartCoroutine(AnimateMilestone());
        }
    }

    private IEnumerator AnimateMilestone()
    {
        if (milestoneText == null)
            yield break;

        // Fade In
        float fadeInTime = 0.5f;
        float t = 0f;
        Color c = milestoneText.color;
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(t / fadeInTime);
            c.a = alpha;
            milestoneText.color = c;
            yield return null;
        }
        c.a = 1f;
        milestoneText.color = c;

        // Show for 4 seconds
        yield return new WaitForSecondsRealtime(4.0f);

        // Fade Out
        float fadeOutTime = 0.5f;
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeOutTime);
            c.a = alpha;
            milestoneText.color = c;
            yield return null;
        }
        c.a = 0f;
        milestoneText.color = c;

        milestoneText.enabled = false;
    }

    /// <summary>
    /// Triggers the damage direction indicator for the given attacker.
    /// </summary>
    /// <param name="attacker">Transform of the enemy, or whatever damaged the player.</param>
    public void ShowDamageIndicator(Transform attacker)
    {
        if (attacker == null) return;
        if (indicatorPrefab == null || indicatorContainer == null) return;

        // 1. Check if we already have an indicator for this specific attacker
        foreach (var active in activeIndicators)
        {
            if (active.attacker == attacker)
            {
                // Just refresh this one
                active.timer = indicatorFadeTime;
                active.group.alpha = 1f;
                return;
            }
        }

        // 2. If no existing indicator for this enemy, spawn new from pool
        if (indicatorPool.Count > 0)
        {
            var newInd = indicatorPool.Dequeue();

            newInd.attacker = attacker;
            newInd.timer = indicatorFadeTime;
            newInd.group.alpha = 1f;
            
            // Setup indicator from pool (ensure radius is correct even if changed in Inspector)
            SetupIndicator(newInd.root);
            
            // Set initial rotation immediately BEFORE activating
            if (playerTransform == null)
            {
                EnsurePlayerTransform();
            }

            if (playerTransform != null)
            {
                Vector3 dir = attacker.position - playerTransform.position;
                dir.y = 0;
                float angle = Vector3.SignedAngle(playerTransform.forward, dir, Vector3.up);
                newInd.root.transform.localEulerAngles = new Vector3(0, 0, -angle);
            }
            
            newInd.root.SetActive(true);

            activeIndicators.Add(newInd);
        }
    }

    private bool EnsurePlayerTransform()
    {
        if (playerTransform != null)
            return true;

        if (GameManager.Instance == null)
            return false;

        var player = GameManager.Instance.GetPlayer();
        if (player == null)
            return false;

        playerTransform = player.transform;
        return playerTransform != null;
    }

    /// <summary>
    /// Sets up a damage indicator: sets the radius for all children.
    /// Called both during pool initialization and when reusing indicators from the pool.
    /// </summary>
    private void SetupIndicator(GameObject indicatorRoot)
    {
        if (indicatorRoot == null) return;

        RectTransform[] allRects = indicatorRoot.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform rect in allRects)
        {
            // Skip the root pivot itself
            if (rect == indicatorRoot.transform) continue;
            
            // Set the radius for child elements
            rect.anchoredPosition = new Vector2(0f, indicatorRadius);
        }
    }

    // ShowDamageFlash() removed, see instructions. If other game code calls it, add an empty stub here.

    /// <summary>
    /// Shows the pylon progress slider and sets its value.
    /// </summary>
    /// <param name="progress">Progress value from 0 to 1.</param>
    public void ShowPylonProgress(float progress)
    {
        if (pylonProgressSlider != null)
        {
            if (pylonProgressSlider.gameObject != null && !pylonProgressSlider.gameObject.activeSelf)
            {
                pylonProgressSlider.gameObject.SetActive(true);
            }
            pylonProgressSlider.value = Mathf.Clamp01(progress);
        }
    }

    /// <summary>
    /// Hides the pylon progress slider.
    /// </summary>
    public void HidePylonProgress()
    {
        if (pylonProgressSlider != null && pylonProgressSlider.gameObject != null)
        {
            pylonProgressSlider.gameObject.SetActive(false);
            pylonProgressSlider.value = 0f;
        }
    }

    /// <summary>
    /// Shows the crosshair (during gameplay).
    /// </summary>
    public void ShowCrosshair()
    {
        if (crosshairObject != null)
        {
            crosshairObject.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the crosshair (when UI is showing).
    /// </summary>
    public void HideCrosshair()
    {
        if (crosshairObject != null)
        {
            crosshairObject.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the stats panel and refreshes all stat values.
    /// </summary>
    public void ShowStatsPanel()
    {
        if (statsPanelObject != null)
        {
            statsPanelObject.SetActive(true);
        }
        
        if (statsPanel != null)
        {
            statsPanel.RefreshStats();
        }
    }

    /// <summary>
    /// Hides the stats panel.
    /// </summary>
    public void HideStatsPanel()
    {
        if (statsPanelObject != null)
        {
            statsPanelObject.SetActive(false);
        }
    }
}
