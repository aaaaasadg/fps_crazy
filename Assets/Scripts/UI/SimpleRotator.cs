using UnityEngine;

// Simple script to rotate UI elements (like upgrade card auras)
// Use this on a UI Image prefab to fake a particle effect
public class SimpleRotator : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 40f;
    [SerializeField] private bool pulseScale = true;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseAmount = 0.1f;

    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        // Rotate around Z axis (2D rotation)
        transform.Rotate(0f, 0f, -rotateSpeed * Time.unscaledDeltaTime);

        // Optional Pulsing
        if (pulseScale)
        {
            float scaleOffset = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
            transform.localScale = initialScale * (1f + scaleOffset);
        }
    }
}

