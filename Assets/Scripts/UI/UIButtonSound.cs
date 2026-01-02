using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    private Button cachedButton;
    private RectTransform rectTransform;
    private Coroutine scaleCoroutine;
    
    [Header("Scale Settings")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float scaleSpeed = 8f; // How fast the scaling happens

    private Vector3 originalScale = Vector3.one;

    private void Awake()
    {
        cachedButton = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalScale = rectTransform.localScale;
        }
    }

    private void OnDisable()
    {
        // Stop coroutine and reset scale when object is disabled
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonHover();
        }

        // Scale to hover size
        ScaleTo(originalScale * hoverScale);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cachedButton != null && !cachedButton.interactable) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        // Scale back to normal on click
        ScaleTo(originalScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Scale back to normal when mouse leaves
        ScaleTo(originalScale);
    }

    private void ScaleTo(Vector3 targetScale)
    {
        if (rectTransform == null) return;

        // Stop existing coroutine if running
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }

        // If GameObject is inactive, set scale instantly (can't run coroutines on inactive objects)
        if (!gameObject.activeInHierarchy)
        {
            rectTransform.localScale = targetScale;
            return;
        }

        // Start smooth scaling coroutine
        scaleCoroutine = StartCoroutine(ScaleCoroutine(targetScale));
    }

    private IEnumerator ScaleCoroutine(Vector3 targetScale)
    {
        if (rectTransform == null) yield break;

        Vector3 startScale = rectTransform.localScale;
        float t = 0f;

        while (t < 1f)
        {
            // Check if object became inactive or destroyed during animation
            if (!gameObject.activeInHierarchy || rectTransform == null)
            {
                scaleCoroutine = null;
                yield break;
            }

            t += Time.unscaledDeltaTime * scaleSpeed;
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        // Final check before setting final scale
        if (rectTransform != null && gameObject.activeInHierarchy)
        {
            rectTransform.localScale = targetScale;
        }
        
        scaleCoroutine = null;
    }
}