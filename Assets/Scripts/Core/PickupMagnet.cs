using UnityEngine;

public class PickupMagnet : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 15f;
    
    [Header("Levitation Animation")]
    [SerializeField] private float levitationHeight = 0.2f;
    [SerializeField] private float levitationSpeed = 2f;
    [SerializeField] private float rotationSpeed = 90f;
    
    [Header("Magnet Settings")]
    [SerializeField] private float magnetDuration = 15f; // How long magnet effect lasts (pulls ALL XP from map)
    
    [Header("Pickup Effect")]
    [SerializeField] private string pickupVfxTag = "XPPickupVFX";

    private Transform playerTransform;
    private bool isPooledInstance = false;
    private SpriteRenderer spriteRenderer;
    private float baseYPosition;
    private float animationTimer = 0f;
    private Vector3 initialAttractionPosition;
    private bool hasStartedMoving = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 10;
        }
    }

    private void OnEnable()
    {
        CachePlayerTransform();
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        
        baseYPosition = transform.position.y;
        transform.position = new Vector3(transform.position.x, baseYPosition + 0.5f, transform.position.z);
        animationTimer = 0f;
        hasStartedMoving = false;
    }

    private void Update()
    {
        if (playerTransform == null)
            return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Use pickup range (same as XP orbs)
        float pickupRadius = 3f;
        PlayerStats playerStats = playerTransform.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            pickupRadius += playerStats.GetStatBonus(StatType.PickupRange);
        }

        if (distance < pickupRadius)
        {
            if (!hasStartedMoving)
            {
                initialAttractionPosition = transform.position;
                hasStartedMoving = true;
            }
            
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

            if (distance < 0.5f)
            {
                // Spawn pickup VFX
                if (ObjectPool.Instance != null && !string.IsNullOrEmpty(pickupVfxTag))
                {
                    ObjectPool.Instance.SpawnFromPool(pickupVfxTag, initialAttractionPosition, Quaternion.identity);
                }
                
                // Activate magnet mode - makes all XP orbs fly to player
                ActivateMagnetEffect();
                
                Despawn();
            }
        }
        else
        {
            hasStartedMoving = false;
            AnimateLevitation();
        }
    }
    
    private void AnimateLevitation()
    {
        animationTimer += Time.deltaTime;
        
        float verticalOffset = Mathf.Sin(animationTimer * levitationSpeed) * levitationHeight;
        float currentY = baseYPosition + 0.5f + verticalOffset;
        
        transform.position = new Vector3(transform.position.x, currentY, transform.position.z);
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }

    private void ActivateMagnetEffect()
    {
        if (playerTransform == null) return;
        
        PlayerStats playerStats = playerTransform.GetComponent<PlayerStats>();
        if (playerStats == null) return;
        
        // Activate magnet mode for the specified duration
        playerStats.ActivateMagnet(magnetDuration);
    }

    public void SetPooledInstance(bool pooled)
    {
        isPooledInstance = pooled;
    }

    private void CachePlayerTransform()
    {
        if (GameManager.Instance != null)
        {
            playerTransform = GameManager.Instance.GetPlayer();
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

