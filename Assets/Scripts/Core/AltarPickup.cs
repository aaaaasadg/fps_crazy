using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class AltarPickup : MonoBehaviour
{
    [Header("Balance Settings")]
    [SerializeField] private float hpCostPercentage = 0.10f; // 10% of current HP

    private bool isPlayerInRange = false;
    private PlayerStats playerStatsInRange = null;
    private HUDManager hudManager = null;

    private bool hasBeenUsed = false;
    private bool isPooledInstance = false;

    private void Awake()
    {
        // Ensure scale is always 1
        transform.localScale = Vector3.one;
    }

    private void OnEnable()
    {
        InitializeCollider();
        ResetState();
        
        // Ensure scale is always 1 when enabled
        transform.localScale = Vector3.one;
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
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            if (hudManager != null) hudManager.HideInteractionText();
            return;
        }

        if (isPlayerInRange && !hasBeenUsed)
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
                TryActivateAltar();
            }
        }
    }

    private void UpdateHUDText()
    {
        if (hudManager == null)
        {
            hudManager = UnityEngine.Object.FindFirstObjectByType<HUDManager>();
        }

        if (hudManager == null || playerStatsInRange == null || hasBeenUsed)
            return;

        float hpCost = playerStatsInRange.CurrentHP * hpCostPercentage;
        float remainingHP = playerStatsInRange.CurrentHP - hpCost;
        
        bool canUse = remainingHP >= 1f;
        Color color = canUse ? Color.yellow : Color.red;
        string text = "";

        if (canUse)
        {
            if (LocalizationManager.Instance != null)
                text = LocalizationManager.Instance.GetLocalizedString("Interaction_Altar", Mathf.RoundToInt(hpCostPercentage * 100f));
            else
                text = $"Press E to Sacrifice {Mathf.RoundToInt(hpCostPercentage * 100f)}% HP for Level Up";
        }
        else
        {
            if (LocalizationManager.Instance != null)
                text = LocalizationManager.Instance.GetLocalizedString("Interaction_Altar_Fail");
            else
                text = "Cannot use: Would reduce HP below 1";
        }
        
        hudManager.ShowInteractionText(text, color);
    }

    private void TryActivateAltar()
    {
        if (hasBeenUsed) return;
        if (playerStatsInRange == null) return;

        float hpCost = playerStatsInRange.CurrentHP * hpCostPercentage;
        float remainingHP = playerStatsInRange.CurrentHP - hpCost;

        if (remainingHP >= 1f)
        {
            hasBeenUsed = true;

            float actualDamage = playerStatsInRange.ReduceHPByPercentage(hpCostPercentage);

            if (hudManager == null)
            {
                hudManager = UnityEngine.Object.FindFirstObjectByType<HUDManager>();
            }
            if (hudManager != null)
            {
                hudManager.HideInteractionText();
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayItemPickup();
            }

            playerStatsInRange.ForceLevelUp();

            Despawn();
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

    private void ResetState()
    {
        hasBeenUsed = false;
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

    private void Despawn()
    {
        if (hudManager != null)
        {
            hudManager.HideInteractionText();
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
}

