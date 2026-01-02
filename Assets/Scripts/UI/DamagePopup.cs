using UnityEngine;
using TMPro;

/// <summary>
/// DamagePopup displays floating damage text over enemies and supports stacking and billboarding.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer;
    private float accumulatedDamage;
    private Color baseColor; // Stores Red or White
    private Vector3 moveVector;

    private const float DISAPPEAR_TIMER_MAX = 1f;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(float amount, bool isCrit)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();

        // --- Accumulation Logic ---
        if (gameObject.activeSelf && disappearTimer > 0.5f)
        {
            accumulatedDamage += amount;
        }
        else
        {
            accumulatedDamage = amount;
            transform.position += Vector3.up * 0.5f;
        }

        // Text Value
        textMesh.text = accumulatedDamage.ToString("0");

        // Color (Instant Override based on THIS hit)
        baseColor = isCrit ? Color.red : Color.white;

        // Force Update Visuals
        textMesh.fontSize = isCrit ? 10 : 6;
        textMesh.color = baseColor;
        textMesh.faceColor = baseColor;

        // Reset Timer
        disappearTimer = DISAPPEAR_TIMER_MAX;
    }

    private void Update()
    {
        // Billboard towards camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }

        // Float Upwards
        transform.position += Vector3.up * 2f * Time.deltaTime;

        // Fade Logic
        disappearTimer -= Time.deltaTime;
        float currentAlpha = 1f;

        if (disappearTimer < 0)
        {
            currentAlpha = Mathf.Clamp01(1f + (disappearTimer * 3f)); // Fast fade out
            if (currentAlpha <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        // Color logic per frame -- brute force as instructed
        Color displayColor = new Color(baseColor.r, baseColor.g, baseColor.b, currentAlpha);
        textMesh.color = displayColor;
        textMesh.faceColor = displayColor;
    }
}
