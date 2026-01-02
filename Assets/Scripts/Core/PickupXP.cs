using UnityEngine;

public class PickupXP : MonoBehaviour
{
    [SerializeField] private float xpAmount = 10f;
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float magnetMoveSpeed = 30f; // Faster speed when magnetized
    
    [Header("Levitation Animation")]
    [SerializeField] private float levitationHeight = 0.2f; // How much it bobs up/down
    [SerializeField] private float levitationSpeed = 2f; // Speed of bobbing
    [SerializeField] private float rotationSpeed = 90f; // Degrees per second
    
    [Header("Pickup Effect")]
    [SerializeField] private string pickupVfxTag = "XPPickupVFX"; // Tag for particle effect in ObjectPool

    private Transform playerTransform;
    private PlayerStats playerStats;
    private bool isPooledInstance = false;
    private SpriteRenderer spriteRenderer;
    private float baseYPosition;
    private float animationTimer = 0f;
    private Vector3 initialAttractionPosition; // Position when orb first starts moving to player
    private bool hasStartedMoving = false; // Track if we've started moving toward player

    public void Setup(float amount)
    {
        xpAmount = amount;
    }

    private void Awake()
    {
        // Get or add SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        // Ensure sprite renderer is set up correctly
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 10; // Make sure it renders on top
            if (spriteRenderer.sprite == null)
            {
                Debug.LogWarning($"PickupXP on {gameObject.name}: SpriteRenderer has no sprite assigned!");
            }
        }
        else
        {
            Debug.LogWarning($"PickupXP on {gameObject.name}: No SpriteRenderer found!");
        }
    }

    private void OnEnable()
    {
        CachePlayerReferences();
        
        // Ensure sprite is visible
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        
        // Set base position 0.5f higher for levitation
        baseYPosition = transform.position.y;
        transform.position = new Vector3(transform.position.x, baseYPosition + 0.5f, transform.position.z);
        animationTimer = 0f;
        hasStartedMoving = false;
    }

    private void Update()
    {
        if (playerTransform == null || playerStats == null)
            return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Check if magnet is active
        bool isMagnetized = playerStats.IsMagnetActive();
        
        // When magnetized, all XP orbs are attracted from any distance
        // Otherwise use normal pickup range
        float pickupRadius;
        float currentMoveSpeed;
        
        if (isMagnetized)
        {
            pickupRadius = float.MaxValue; // Unlimited range when magnetized
            currentMoveSpeed = magnetMoveSpeed;
        }
        else
        {
            pickupRadius = playerStats.GetStatBonus(StatType.PickupRange);
            pickupRadius += 3f; // Default base pickup range
            currentMoveSpeed = moveSpeed;
        }

        if (distance < pickupRadius)
        {
            // Store initial position when first starting to move toward player
            if (!hasStartedMoving)
            {
                initialAttractionPosition = transform.position;
                hasStartedMoving = true;
            }
            
            // Move towards player (faster when magnetized)
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, currentMoveSpeed * Time.deltaTime);
            
            // Continue rotating even while moving
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

            if (distance < 0.5f)
            {
                // --- PARTICLE EFFECT: Spawn pickup VFX at initial attraction position (where orb started moving) ---
                if (ObjectPool.Instance != null)
                {
                    if (!string.IsNullOrEmpty(pickupVfxTag))
                    {
                        GameObject vfxObj = ObjectPool.Instance.SpawnFromPool(pickupVfxTag, initialAttractionPosition, Quaternion.identity);
                        if (vfxObj == null)
                        {
                            Debug.LogWarning($"PickupXP: Failed to spawn particle effect with tag '{pickupVfxTag}'. Check ObjectPool configuration!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("PickupXP: pickupVfxTag is empty! Assign a tag in the Inspector.");
                    }
                }
                else
                {
                    Debug.LogWarning("PickupXP: ObjectPool.Instance is null! Make sure ObjectPool exists in the scene.");
                }
                
                playerStats.AddXP(xpAmount);
                Despawn();
            }
        }
        else
        {
            // Reset movement flag when outside pickup radius
            hasStartedMoving = false;
            // Levitation animation when not moving to player
            AnimateLevitation();
        }
    }
    
    private void AnimateLevitation()
    {
        animationTimer += Time.deltaTime;
        
        // Smooth up/down movement using sine wave
        float verticalOffset = Mathf.Sin(animationTimer * levitationSpeed) * levitationHeight;
        float currentY = baseYPosition + 0.5f + verticalOffset;
        
        // Update Y position while keeping X and Z
        transform.position = new Vector3(transform.position.x, currentY, transform.position.z);
        
        // Rotate around its own origin (Y axis)
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }

    public void SetPooledInstance(bool pooled)
    {
        isPooledInstance = pooled;
    }
    
    /// <summary>
    /// Public method to get XP amount (for magnet pickup)
    /// </summary>
    public float GetXPAmount()
    {
        return xpAmount;
    }
    
    /// <summary>
    /// Force collect this XP orb instantly (used by magnet pickup)
    /// </summary>
    public void ForceCollect()
    {
        if (playerStats != null)
        {
            playerStats.AddXP(xpAmount);
        }
        Despawn();
    }

    private void CachePlayerReferences()
    {
        if (GameManager.Instance != null)
        {
            playerTransform = GameManager.Instance.GetPlayer();
            if (playerTransform != null)
            {
                playerStats = playerTransform.GetComponent<PlayerStats>();
            }
        }
    }

    private void Despawn()
    {
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
