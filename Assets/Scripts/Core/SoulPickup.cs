using UnityEngine;

public class SoulPickup : MonoBehaviour
{
    [SerializeField] private int amount = 1;
    [SerializeField] private float speed = 10f;

    private Transform playerTransform;
    private bool isPooledInstance = false;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// Set the pickup amount at runtime.
    /// </summary>
    public void Setup(int amountValue)
    {
        amount = amountValue;
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
                Debug.LogWarning($"SoulPickup on {gameObject.name}: SpriteRenderer has no sprite assigned!");
            }
        }
        else
        {
            Debug.LogWarning($"SoulPickup on {gameObject.name}: No SpriteRenderer found!");
        }
        
        // Cache camera reference
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
    }

    private void OnEnable()
    {
        CachePlayerTransform();
        
        // Ensure sprite is visible
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    private void Update()
    {
        // Face the camera (billboard effect)
        if (mainCamera != null)
        {
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0); // Flip to face camera properly
        }
        
        // Player null check and reacquisition
        if (playerTransform == null)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                Transform playerTrans = gm.playerTransform;
                if (playerTrans != null)
                    playerTransform = playerTrans;
            }
            if (playerTransform == null)
                return;
        }

        Vector3 direction = playerTransform.position - transform.position;
        float distance = direction.magnitude;

        if (distance < 0.5f)
        {
            // --- SOUND: Item Pickup (Soul) ---
            if (SoundManager.Instance != null) SoundManager.Instance.PlayItemPickup();

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.AddSouls(amount);
            }
            Despawn();
            return;
        }

        // Movement logic
        Vector3 move = direction.normalized * speed * Time.deltaTime;
        if (move.magnitude > distance)
            move = direction;

        transform.position += move;
    }

    public void SetPooledInstance(bool pooled)
    {
        isPooledInstance = pooled;
    }

    private void CachePlayerTransform()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            Transform playerTrans = gm.playerTransform;
            if (playerTrans != null)
            {
                playerTransform = playerTrans;
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
