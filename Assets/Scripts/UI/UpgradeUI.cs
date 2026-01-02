using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UpgradeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button selectButton;

    [Header("Rarity VFX")]
    [SerializeField] private GameObject[] rarityAuras; // Index 0=Common, 1=Rare, 2=Epic, 3=Legendary
    [SerializeField] private Transform auraVfxContainer; // Assign a RectTransform or empty Transform in the inspector for VFX parent
    [SerializeField] private float vfxScale = 100f; // NEW FIELD: Scale for VFX (100 for Camera mode, 1 for Overlay)
    private Vector3 baseScale = Vector3.one;
    private Coroutine scaleCoroutine;
    private GameObject[] auraInstanceCache;

    private void Awake()
    {
        InitializeAuraCache();
    }

    private void OnEnable()
    {
        baseScale = Vector3.one; // Ensure base scale is known
        // Start small for pop-in animation
        transform.localScale = Vector3.one * 0.1f;
        StartCoroutine(AnimatePopIn());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale(1.1f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale(1.0f));
    }

    private System.Collections.IEnumerator AnimateScale(float targetScaleMultiplier)
    {
        Vector3 target = baseScale * targetScaleMultiplier;
        float duration = 0.15f; // Fast smooth transition
        float elapsed = 0f;
        Vector3 start = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }
        transform.localScale = target;
    }

    private System.Collections.IEnumerator AnimatePopIn()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.1f;
        Vector3 targetScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time since game is likely paused
            float t = Mathf.Clamp01(elapsed / duration);
            
            // SmoothStep for nice ease-in-out feel
            float ease = Mathf.SmoothStep(0f, 1f, t);

            transform.localScale = Vector3.Lerp(startScale, targetScale, ease);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    /// <summary>
    /// Sets up the upgrade card UI elements based on the given UpgradeChoice data.
    /// </summary>
    /// <param name="choice">UpgradeChoice data (stat, rarity, icon, etc.)</param>
    /// <param name="onSelect">Callback invoked when this card is selected</param>
    public void Setup(UpgradeSystem.UpgradeChoice choice, System.Action<UpgradeSystem.UpgradeChoice> onSelect)
    {
        if (titleText != null)
        {
            string defaultText = "Upgrade";
            if (LocalizationManager.Instance != null)
                defaultText = LocalizationManager.Instance.GetLocalizedString("Upgrade");
            
            // Preserve font weight and other properties
            FontWeight originalWeight = titleText.fontWeight;
            
            // Get language-specific font
            TMP_FontAsset languageFont = null;
            if (LocalizationManager.Instance != null)
            {
                languageFont = LocalizationManager.Instance.GetFontForCurrentLanguage();
            }
            
            titleText.text = !string.IsNullOrEmpty(choice.Name) ? choice.Name : defaultText;
            
            // Apply language-specific font, preserving weight
            if (languageFont != null && languageFont != TMP_Settings.defaultFontAsset)
            {
                LocalizationManager.Instance.ApplyFont(titleText, languageFont);
            }
            titleText.fontWeight = originalWeight;
        }

        if (descriptionText != null)
        {
            // Preserve font weight and other properties
            FontWeight originalWeight = descriptionText.fontWeight;
            
            // Get language-specific font
            TMP_FontAsset languageFont = null;
            if (LocalizationManager.Instance != null)
            {
                languageFont = LocalizationManager.Instance.GetFontForCurrentLanguage();
            }
            
            descriptionText.text = !string.IsNullOrEmpty(choice.Desc) ? choice.Desc : "";
            
            // Apply language-specific font, preserving weight
            if (languageFont != null && languageFont != TMP_Settings.defaultFontAsset)
            {
                LocalizationManager.Instance.ApplyFont(descriptionText, languageFont);
            }
            descriptionText.fontWeight = originalWeight;
        }

        if (rarityText != null)
        {
            string rarityString = choice.Rarity.ToString();
            if (LocalizationManager.Instance != null)
            {
                string localized = LocalizationManager.Instance.GetLocalizedString(rarityString);
                if (localized != rarityString)
                    rarityString = localized;
            }
            
            // Preserve font weight and other properties
            FontWeight originalWeight = rarityText.fontWeight;
            
            // Get language-specific font
            TMP_FontAsset languageFont = null;
            if (LocalizationManager.Instance != null)
            {
                languageFont = LocalizationManager.Instance.GetFontForCurrentLanguage();
            }
            
            rarityText.text = rarityString;
            
            // Apply language-specific font, preserving weight
            if (languageFont != null && languageFont != TMP_Settings.defaultFontAsset)
            {
                LocalizationManager.Instance.ApplyFont(rarityText, languageFont);
            }
            rarityText.fontWeight = originalWeight;
        }

        if (iconImage != null)
            iconImage.sprite = choice.Icon;

        // Set rarity color for background and rarity text
        if (backgroundImage != null)
        {
            backgroundImage.color = GetColorForRarity(choice.Rarity);
        }
        if (rarityText != null)
        {
            rarityText.color = GetBrightTextColorForRarity(choice.Rarity);
        }

        // === VFX Implementation ===
        // 1. Clear old VFX
        DeactivateAllAuraInstances();

        // 2. Spawn new VFX
        if (auraVfxContainer != null && rarityAuras != null)
        {
            int index = (int)choice.Rarity;
            if (index >= 0 && index < rarityAuras.Length && rarityAuras[index] != null)
            {
                GameObject aura = GetOrCreateAuraInstance(index);

                // Camera Mode Settings:
                // Z = 10 (Push behind card)
                // Scale = vfxScale (100 for particles)
                // Position at bottom of card
                if (aura != null)
                {
                    aura.transform.localRotation = Quaternion.identity;
                    aura.transform.localScale = Vector3.one * vfxScale;

                    // Position VFX at the bottom of the card
                    RectTransform cardRect = GetComponent<RectTransform>();
                    RectTransform rect = aura.GetComponent<RectTransform>();
                    
                    if (rect != null && cardRect != null)
                    {
                        // Position at bottom of card (center horizontally, bottom vertically)
                        // For center-anchored cards: bottom Y = -(cardHeight / 2)
                        float cardHeight = cardRect.rect.height;
                        float bottomY = -(cardHeight / 2f);
                        // Keep Z at 10 for depth (push behind card)
                        rect.anchoredPosition3D = new Vector3(0f, bottomY, 10f);
                    }
                    else if (rect != null)
                    {
                        // Fallback: if no card RectTransform, position relative to container
                        RectTransform containerRect = auraVfxContainer as RectTransform;
                        if (containerRect != null)
                        {
                            float containerHeight = containerRect.rect.height;
                            float bottomY = -(containerHeight / 2f);
                            rect.anchoredPosition3D = new Vector3(0f, bottomY, 10f);
                        }
                        else
                        {
                            rect.anchoredPosition3D = new Vector3(0f, 0f, 10f);
                        }
                    }
                    else
                    {
                        // Final fallback: use local position (non-UI VFX)
                        if (auraVfxContainer is RectTransform containerRect)
                        {
                            float containerHeight = containerRect.rect.height;
                            aura.transform.localPosition = new Vector3(0f, -(containerHeight / 2f), 10f);
                        }
                        else
                        {
                            aura.transform.localPosition = new Vector3(0f, 0f, 10f);
                        }
                    }
                }
            }
        }

        // Set up the select button
        if (selectButton != null)
        {
            // Add EventTrigger for Hover sound (Button Hover)
            EventTrigger trigger = selectButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = selectButton.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((data) => { 
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonHover(); 
            });
            trigger.triggers.Add(entry);

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() =>
            {
                // --- SOUND: Upgrade Choice (Click) ---
                if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeChoice();

                if (onSelect != null) onSelect(choice);
            });
        }
    }

    private Color GetColorForRarity(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return new Color(0.65f, 0.65f, 0.65f, 1f); // Grey
            case Rarity.Rare:
                return new Color(0.3f, 0.45f, 1f, 1f);     // Blue
            case Rarity.Epic:
                return new Color(0.6f, 0.25f, 0.95f, 1f);  // Purple
            case Rarity.Legendary:
                return new Color(1f, 0.55f, 0.12f, 1f);    // Orange
            default:
                return Color.white;
        }
    }

    private Color GetBrightTextColorForRarity(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return new Color(1f, 1f, 1f, 1f);          // Bright white for contrast
            case Rarity.Rare:
                return new Color(0.6f, 0.8f, 1f, 1f);     // Bright cyan-blue
            case Rarity.Epic:
                return new Color(0.9f, 0.5f, 1f, 1f);      // Bright magenta-purple
            case Rarity.Legendary:
                return new Color(1f, 0.85f, 0.4f, 1f);    // Bright gold-yellow
            default:
                return Color.white;
        }
    }

    private void InitializeAuraCache()
    {
        if (rarityAuras == null || rarityAuras.Length == 0)
            return;

        if (auraInstanceCache == null || auraInstanceCache.Length != rarityAuras.Length)
        {
            auraInstanceCache = new GameObject[rarityAuras.Length];
        }
    }

    private void DeactivateAllAuraInstances()
    {
        if (auraInstanceCache == null)
            return;

        for (int i = 0; i < auraInstanceCache.Length; i++)
        {
            if (auraInstanceCache[i] != null)
            {
                auraInstanceCache[i].SetActive(false);
            }
        }
    }

    private GameObject GetOrCreateAuraInstance(int index)
    {
        if (rarityAuras == null || index < 0 || index >= rarityAuras.Length)
            return null;

        InitializeAuraCache();

        if (auraInstanceCache[index] == null && rarityAuras[index] != null)
        {
            auraInstanceCache[index] = Instantiate(rarityAuras[index], auraVfxContainer);
        }

        GameObject aura = auraInstanceCache[index];
        if (aura != null)
        {
            if (aura.transform.parent != auraVfxContainer)
            {
                aura.transform.SetParent(auraVfxContainer);
            }
            aura.SetActive(true);
        }
        return aura;
    }
}
