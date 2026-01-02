using UnityEngine;
using TMPro;

/// <summary>
/// Lightweight FPS readout for temporary debugging.
/// Attach to any GameObject and assign a TMP text label.
/// </summary>
public class DebugFPSDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsLabel;
    [SerializeField] private float refreshInterval = 0.25f;

    private float timeAccumulator;
    private int frameCount;

    private void Awake()
    {
        if (fpsLabel != null)
        {
            fpsLabel.text = string.Empty;
        }
    }

    private void Update()
    {
        if (fpsLabel == null || refreshInterval <= 0f)
            return;

        timeAccumulator += Time.unscaledDeltaTime;
        frameCount++;

        if (timeAccumulator >= refreshInterval)
        {
            float fps = frameCount / timeAccumulator;
            fpsLabel.text = $"{fps:0} FPS";

            timeAccumulator = 0f;
            frameCount = 0;
        }
    }
}

