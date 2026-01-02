using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private Image tutorialImage;

    [Header("Tutorial Content")]
    [SerializeField] private Sprite moveImage;
    private string moveKey = "Tutorial_Move";
    private string moveKeyMobile = "Tutorial_Move_Mobile";
    
    [SerializeField] private Sprite shootImage;
    private string shootKey = "Tutorial_Shoot";
    private string shootKeyMobile = "Tutorial_Shoot_Mobile";
    
    [SerializeField] private Sprite damageImage;
    private string damageKey = "Tutorial_Damage";
    
    [SerializeField] private Sprite xpImage;
    private string xpKey = "Tutorial_XP";
    
    [SerializeField] private Sprite levelUpImage;
    private string levelUpKey = "Tutorial_LevelUp";
    
    [SerializeField] private Sprite surviveImage;
    private string surviveKey = "Tutorial_Survive";

    // Mobile detection
    private bool isMobile;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayDelay = 0.5f;

    // State flags
    public bool IsTutorialActive { get; private set; } = false;
    private int currentStep = 0;
    private string currentTextKey; // Track current key for dynamic language update
    
    // Step completion flags
    private bool hasMoved = false;
    private bool hasJumped = false;
    private bool hasShot = false;
    private bool hasDealtDamage = false;
    private bool hasPickedUpXP = false;
    private bool hasLeveledUp = false;

    private const string TUTORIAL_KEY = "TutorialCompleted";

    private void Awake()
    {
        // Check if tutorial has already been completed - do this FIRST in Awake
        if (PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 1)
        {
            // Tutorial already completed - don't even create this instance
            Destroy(gameObject);
            return;
        }

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Setup initial UI state
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateCurrentText;
        }
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateCurrentText;
        }
    }

    private void UpdateCurrentText()
    {
        if (IsTutorialActive && !string.IsNullOrEmpty(currentTextKey))
        {
            string text = LocalizationManager.Instance.GetLocalizedString(currentTextKey);
            if (tutorialText != null) 
            {
                tutorialText.text = text;
                // Apply font
                TMP_FontAsset font = LocalizationManager.Instance.GetFontForCurrentLanguage();
                if (font != null) LocalizationManager.Instance.ApplyFont(tutorialText, font);
            }
        }
    }

    private void Start()
    {
        // Check if tutorial has already been completed FIRST - before anything else
        if (PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 1)
        {
            // Tutorial already completed - destroy this instance immediately
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            Destroy(gameObject);
            return;
        }

        // Detect mobile platform
        isMobile = Application.isMobilePlatform || UnityEngine.Device.Application.isMobilePlatform;

        // Only start tutorial in the game scene (GameManager exists and game is active/starting)
        if (GameManager.Instance != null)
        {
            StartCoroutine(StartTutorialFlow());
        }
    }

    private void Update()
    {
        if (!IsTutorialActive) return;

        // Check inputs for Step 0 (Move/Jump)
        if (currentStep == 0)
        {
            if (isMobile && MobileInputController.Instance != null)
            {
                Vector2 mobileInput = MobileInputController.Instance.GetMovementInput();
                if (Mathf.Abs(mobileInput.x) > 0.1f || Mathf.Abs(mobileInput.y) > 0.1f) hasMoved = true;
                if (MobileInputController.Instance.GetJumpInput()) hasJumped = true;
            }
            else
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f) hasMoved = true;
                if (Input.GetButtonDown("Jump")) hasJumped = true;
            }
        }

        // Check inputs for Step 1 (Shoot)
        if (currentStep == 1)
        {
            if (isMobile && MobileInputController.Instance != null)
            {
                if (MobileInputController.Instance.IsShooting()) hasShot = true;
            }
            else
            {
                if (Input.GetMouseButtonDown(0)) hasShot = true;
            }
        }
    }

    // Hooks for external events
    public void OnDamageDealt()
    {
        if (currentStep == 2) hasDealtDamage = true;
    }

    public void OnXPGained()
    {
        if (currentStep == 3) hasPickedUpXP = true;
    }

    public void OnLevelUp()
    {
        if (currentStep == 4) hasLeveledUp = true;
    }

    private IEnumerator StartTutorialFlow()
    {
        // Wait a moment after scene load
        yield return new WaitForSeconds(1f);
        
        IsTutorialActive = true;
        if (tutorialPanel != null) tutorialPanel.SetActive(true);

        // --- STEP 0: MOVE & JUMP ---
        currentStep = 0;
        string moveKeyToUse = isMobile ? moveKeyMobile : moveKey;
        UpdateUI(moveKeyToUse, moveImage);
        yield return FadeIn();
        yield return new WaitUntil(() => hasMoved && hasJumped);
        yield return FadeOut();
        yield return new WaitForSeconds(displayDelay);

        // --- STEP 1: SHOOT ---
        currentStep = 1;
        string shootKeyToUse = isMobile ? shootKeyMobile : shootKey;
        UpdateUI(shootKeyToUse, shootImage);
        yield return FadeIn();
        yield return new WaitUntil(() => hasShot);
        yield return FadeOut();
        yield return new WaitForSeconds(displayDelay);

        // --- STEP 2: DEAL DAMAGE ---
        currentStep = 2;
        UpdateUI(damageKey, damageImage);
        yield return FadeIn();
        yield return new WaitUntil(() => hasDealtDamage);
        yield return FadeOut();
        yield return new WaitForSeconds(displayDelay);

        // --- STEP 3: PICKUP XP ---
        currentStep = 3;
        UpdateUI(xpKey, xpImage);
        yield return FadeIn();
        yield return new WaitUntil(() => hasPickedUpXP);
        yield return FadeOut();
        yield return new WaitForSeconds(displayDelay);

        // --- STEP 4: LEVEL UP ---
        currentStep = 4;
        UpdateUI(levelUpKey, levelUpImage);
        yield return FadeIn();
        // If player already leveled up (rare), this will pass immediately
        yield return new WaitUntil(() => hasLeveledUp);
        yield return FadeOut();
        yield return new WaitForSeconds(displayDelay);

        // --- STEP 5: SURVIVE (Final) ---
        currentStep = 5;
        UpdateUI(surviveKey, surviveImage);
        yield return FadeIn();
        yield return new WaitForSeconds(4f); // Show for 4 seconds
        yield return FadeOut();

        // Complete - mark tutorial as completed and save immediately
        PlayerPrefs.SetInt(TUTORIAL_KEY, 1);
        PlayerPrefs.Save(); // Ensure it's saved to disk
        IsTutorialActive = false;
        currentTextKey = null;
        
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        
        // Destroy after a short delay to ensure everything is cleaned up
        Destroy(gameObject, 0.5f);
    }

    private void UpdateUI(string key, Sprite sprite)
    {
        currentTextKey = key;
        UpdateCurrentText();

        if (tutorialImage != null)
        {
            tutorialImage.sprite = sprite;
            tutorialImage.gameObject.SetActive(sprite != null);
        }
    }

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            canvasGroup.alpha = t;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

        float t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / fadeDuration;
            canvasGroup.alpha = t;
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}

