using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SphereCollider))]
public class ChestPickup : MonoBehaviour
{
    [Header("Balance Settings")]
    [SerializeField] private float bal_basePrice = 15f; // Base price for first chest
    [SerializeField] private float bal_priceGrowth = 1.25f; // Price multiplier per chest opened
    
    // Static price - all chests share the same price based on how many have been opened
    private static float sharedCurrentPrice = 15f;
    private static int lastKnownChestsOpened = 0;
    private bool isPlayerInRange = false;
    private PlayerStats playerStatsInRange = null;
    private HUDManager hudManager = null;

    private bool hasOpened = false;
    private bool isPooledInstance = false;
    private Coroutine spawnAnimationCoroutine;
    private bool isBossChest = false; // True if this chest was dropped by a boss (guarantees legendary)
    private bool isSpawned = false; // True after spawn animation completes

    private void Awake()
    {
        // Fix: Disable any AudioSource that might be playing automatically
        AudioSource source = GetComponent<AudioSource>();
        if (source != null)
        {
            source.playOnAwake = false;
            source.Stop();
        }

        // Store base scale once in Awake (runs before OnEnable)
        if (transform.localScale == Vector3.zero || transform.localScale.magnitude < 0.01f)
        {
            transform.localScale = Vector3.one;
        }
    }

    private void OnEnable()
    {
        InitializeCollider();
        RefreshCost();
        ResetState();
        
        // Start spawn animation (will check isBossChest dynamically)
        // Note: SetBossChest may be called after OnEnable for boss chests,
        // but the animation checks the flag each frame so it will adjust
        if (spawnAnimationCoroutine != null)
        {
            StopCoroutine(spawnAnimationCoroutine);
        }
        spawnAnimationCoroutine = StartCoroutine(AnimateSpawn());
    }

    /// <summary>
    /// Marks this chest as a boss chest (guarantees legendary item, free to open).
    /// </summary>
    public void SetBossChest(bool isBoss)
    {
        isBossChest = isBoss;
        // Refresh cost since boss chests are free
        RefreshCost();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerStatsInRange = other.GetComponent<PlayerStats>();

            if (hudManager == null)
            {
                hudManager = UnityEngine.Object.FindFirstObjectByType<HUDManager>();
            }
            UpdateHUDText();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerStatsInRange = null;
            if (hudManager == null)
            {
                hudManager = UnityEngine.Object.FindFirstObjectByType<HUDManager>();
            }
            if (hudManager != null)
            {
                hudManager.HideInteractionText();
            }
        }
    }

    private void Update()
    {
        // At the very start, handle game pause (upgrade/levelup/etc. screens)
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            // If game paused (upgrade screen open), hide prompt and return.
            if (hudManager != null) hudManager.HideInteractionText();
            return;
        }

        // Existing distance check logic below
        if (isPlayerInRange)
        {
            UpdateHUDText();

            // Check for E key or mobile interact button
            bool interactPressed = Input.GetKeyDown(KeyCode.E);
            if (!interactPressed && MobileInputController.IsMobile && MobileInputController.Instance != null)
            {
                interactPressed = MobileInputController.Instance.GetInteractInput();
            }
            
            if (interactPressed)
            {
                TryOpenChest();
            }
        }
    }

    private void UpdateHUDText()
    {
        // Don't show interaction UI until spawn animation is complete
        if (!isSpawned)
            return;
            
        if (hudManager == null)
        {
            // Cache it once if possible, or try finding it
            hudManager = UnityEngine.Object.FindFirstObjectByType<HUDManager>();
        }

        if (hudManager == null || playerStatsInRange == null)
            return;

        // Boss chests are free
        if (isBossChest)
        {
            string freeMsg = "Press E to Open (FREE)";
            if (LocalizationManager.Instance != null)
            {
                freeMsg = LocalizationManager.Instance.GetLocalizedString("Interaction_OpenChest_Free");
            }
            hudManager.ShowInteractionText(freeMsg, Color.green);
            return;
        }

        // Use shared price for all chests
        int displayCost = GetCurrentPrice();
            
        // Check affordability using exact values (no floor/ceiling mismatch)
        bool canAfford = playerStatsInRange.CurrentGold >= displayCost;
        Color color = canAfford ? Color.yellow : Color.red;
        
        string msg = $"Press E to Open ({displayCost} G)";
        if (LocalizationManager.Instance != null)
        {
            // Use format arg for cost
            msg = LocalizationManager.Instance.GetLocalizedString("Interaction_OpenChest", displayCost);
        }
        
        hudManager.ShowInteractionText(msg, color);
    }

    private void TryOpenChest()
    {
        // Don't allow opening until spawn animation is complete
        if (!isSpawned) return;
        if (hasOpened) return;
        if (playerStatsInRange == null)
            return;

        // Boss chests are free - open immediately
        if (isBossChest)
        {
            hasOpened = true;

            if (hudManager == null)
            {
                hudManager = UnityEngine.Object.FindFirstObjectByType<HUDManager>();
            }
            if (hudManager != null)
            {
                hudManager.HideInteractionText();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerChestReward(isBossChest);
            }

            Despawn();
            return;
        }

        // Use the shared price (same for all chests)
        int requiredCost = GetCurrentPrice();

        // Check if player has enough gold (using exact gold value, no floor/ceiling mismatch)
        bool canAfford = playerStatsInRange.CurrentGold >= requiredCost;

        if (canAfford)
        {
            hasOpened = true;

            // Deduct the exact cost amount (AddGold with negative value will NOT apply multiplier)
            playerStatsInRange.AddGold(-requiredCost);

            if (hudManager == null)
            {
                hudManager = UnityEngine.Object.FindFirstObjectByType<HUDManager>();
            }
            if (hudManager != null)
            {
                hudManager.HideInteractionText();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerChestReward(isBossChest);
            }
            else
            {
                Debug.LogWarning("GameManager.Instance is null in ChestPickup!");
            }

            Despawn();
        }
        else
        {
            Debug.Log($"Not enough gold! Have: {playerStatsInRange.CurrentGold}, Need: {requiredCost}");
            // For later UI: Show a "Not enough gold!" message with cost here.
        }
    }

    public void SetPooledInstance(bool pooled)
    {
        isPooledInstance = pooled;
    }

    private void InitializeCollider()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            sphere.radius = 4f;
            sphere.isTrigger = true;
        }
    }

    private void RefreshCost()
    {
        // Boss chests are always free - no need to update shared price
        if (isBossChest)
        {
            return;
        }
        
        // Recalculate shared price only if ChestsOpenedCount has changed
        int chestsOpened = 0;
        if (GameManager.Instance != null)
        {
            chestsOpened = GameManager.Instance.ChestsOpenedCount;
        }
        
        // Update shared price if chests opened count changed (new game or chest was opened)
        if (chestsOpened != lastKnownChestsOpened || sharedCurrentPrice < bal_basePrice)
        {
            lastKnownChestsOpened = chestsOpened;
            float price = bal_basePrice * Mathf.Pow(bal_priceGrowth, chestsOpened);
            sharedCurrentPrice = Mathf.Max(1f, Mathf.RoundToInt(price));
        }
    }
    
    /// <summary>
    /// Gets the current chest price (shared across all chests).
    /// </summary>
    private int GetCurrentPrice()
    {
        // Boss chests are free
        if (isBossChest) return 0;
        
        // Check if we need to update the shared price
        int chestsOpened = GameManager.Instance != null ? GameManager.Instance.ChestsOpenedCount : 0;
        if (chestsOpened != lastKnownChestsOpened)
        {
            lastKnownChestsOpened = chestsOpened;
            float price = bal_basePrice * Mathf.Pow(bal_priceGrowth, chestsOpened);
            sharedCurrentPrice = Mathf.Max(1f, Mathf.RoundToInt(price));
        }
        
        return Mathf.RoundToInt(sharedCurrentPrice);
    }
    
    /// <summary>
    /// Reset static price tracking (call when starting a new run)
    /// </summary>
    public static void ResetSharedPrice()
    {
        sharedCurrentPrice = 15f; // Default base price
        lastKnownChestsOpened = 0;
    }

    private void ResetState()
    {
        hasOpened = false;
        isPlayerInRange = false;
        playerStatsInRange = null;
        // Reset boss chest flag for pooled chests (will be set to true for boss chests after instantiation)
        isBossChest = false;
        isSpawned = false; // Reset spawn flag
        if (hudManager == null)
        {
            hudManager = UnityEngine.Object.FindFirstObjectByType<HUDManager>();
        }
        if (hudManager != null)
        {
            hudManager.HideInteractionText();
        }
    }

    private void Despawn()
    {
        if (hudManager != null)
        {
            hudManager.HideInteractionText();
        }
        
        if (spawnAnimationCoroutine != null)
        {
            StopCoroutine(spawnAnimationCoroutine);
            spawnAnimationCoroutine = null;
        }

        if (isPooledInstance)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private IEnumerator AnimateSpawn()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        
        // Determine target scale: 2 for boss chests, 1 for normal chests
        // Check isBossChest inside the coroutine in case it's set after OnEnable
        float targetScaleMultiplier = isBossChest ? 2f : 1f;
        Vector3 targetScale = Vector3.one * targetScaleMultiplier;
        
        // Start at 0.1 scale (relative to target)
        transform.localScale = targetScale * 0.1f;
        
        // Animate: 0.1 -> 1.1 -> 1.0 (relative to target scale) in 0.3 seconds
        while (elapsed < duration)
        {
            // Re-check isBossChest each frame in case it's set during animation
            float currentTargetMultiplier = isBossChest ? 2f : 1f;
            if (Mathf.Abs(currentTargetMultiplier - targetScaleMultiplier) > 0.01f)
            {
                // Boss chest flag was set/cleared during animation, recalculate target
                targetScaleMultiplier = currentTargetMultiplier;
                targetScale = Vector3.one * targetScaleMultiplier;
            }
            
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            float scale;
            if (t < 0.6f) // First 60%: 0.1 to 1.1
            {
                float phaseT = t / 0.6f;
                scale = Mathf.Lerp(0.1f, 1.1f, phaseT);
            }
            else // Last 40%: 1.1 to 1.0
            {
                float phaseT = (t - 0.6f) / 0.4f;
                scale = Mathf.Lerp(1.1f, 1.0f, phaseT);
            }
            
            transform.localScale = targetScale * scale;
            yield return null;
        }
        
        // Ensure final scale matches target (2 for boss, 1 for normal)
        float finalTargetMultiplier = isBossChest ? 2f : 1f;
        transform.localScale = Vector3.one * finalTargetMultiplier;
        spawnAnimationCoroutine = null;
        
        // Mark as spawned - now interaction UI can be shown
        isSpawned = true;
        
        // If player was already in range, update UI now
        if (isPlayerInRange && hudManager != null && playerStatsInRange != null)
        {
            UpdateHUDText();
        }
    }
}
