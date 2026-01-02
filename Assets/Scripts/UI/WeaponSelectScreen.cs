using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class WeaponSelectScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    private const float hoverScale = 1.1f;
    private const float scaleDuration = 0.15f;

    private void Start()
    {
        // Ensure cursor is unlocked for selection
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Set up hover scale effects for all buttons
        SetupButtonHoverEffects();
    }
    
    private void SetupButtonHoverEffects()
    {
        // Find all buttons in the panel or this gameObject
        GameObject targetObject = panel != null ? panel : gameObject;
        Button[] buttons = targetObject.GetComponentsInChildren<Button>(true);
        
        foreach (Button button in buttons)
        {
            if (button == null) continue;
            
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect == null) continue;
            
            // Add smooth scale component to handle hover animations
            ButtonHoverScale hoverComponent = button.gameObject.GetComponent<ButtonHoverScale>();
            if (hoverComponent == null)
            {
                hoverComponent = button.gameObject.AddComponent<ButtonHoverScale>();
            }
            hoverComponent.Initialize(buttonRect, hoverScale, scaleDuration);
        }
    }

    public void SelectWeapon(int weaponIndex)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetStartingWeapon(weaponIndex);
            GameManager.Instance.StartGame();
        }

        // Hide this screen
        if (panel != null)
            panel.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void SelectClass(int classIndex)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetClass(classIndex);
            GameManager.Instance.SetStartingWeapon(classIndex);
            GameManager.Instance.StartGame();
        }

        // Disable the panel
        if (panel != null)
            panel.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}

// Helper component for smooth button hover scale animation (similar to UpgradeUI)
public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private Coroutine scaleCoroutine;
    private float hoverScaleMultiplier = 1.1f;
    private float animationDuration = 0.15f;

    public void Initialize(RectTransform rect, float hoverScale, float duration)
    {
        rectTransform = rect;
        baseScale = rectTransform.localScale;
        hoverScaleMultiplier = hoverScale;
        animationDuration = duration;
    }

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        
        if (rectTransform != null && baseScale == Vector3.one)
            baseScale = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale(hoverScaleMultiplier));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale(1.0f));
    }

    private IEnumerator AnimateScale(float targetScaleMultiplier)
    {
        if (rectTransform == null) yield break;

        Vector3 target = baseScale * targetScaleMultiplier;
        float elapsed = 0f;
        Vector3 start = rectTransform.localScale;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            rectTransform.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }
        rectTransform.localScale = target;
    }
}
