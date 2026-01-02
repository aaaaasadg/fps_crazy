using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Tombstone : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionRadius = 5f;
    [SerializeField] private int enemyCount = 10; // Number of enemies to spawn
    [SerializeField] private float spawnRadius = 8f; // Radius around tombstone to spawn enemies
    
    private bool isPlayerInRange = false;
    private Transform playerTransform;
    private SphereCollider triggerCollider;
    private EnemySpawner enemySpawner;
    private HUDManager hudManager;
    private bool hasBeenUsed = false;

    private void Awake()
    {
        // Align tombstone with ground slope
        AlignWithGround();
        
        triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider != null)
        {
            triggerCollider.radius = interactionRadius;
            triggerCollider.isTrigger = true;
        }
        
        // Cache EnemySpawner reference
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
        
        // Cache HUDManager reference
        if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
        {
            hudManager = GameManager.Instance.hudManager;
        }
        else
        {
            hudManager = FindFirstObjectByType<HUDManager>();
        }
    }

    private void AlignWithGround()
    {
        // Raycast downward to get ground normal
        Ray ray = new Ray(transform.position + Vector3.up * 2f, Vector3.down);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 10f))
        {
            // Only align if not hitting player or enemy
            if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Enemy"))
            {
                // Get the ground normal
                Vector3 groundNormal = hit.normal;
                
                // Preserve current Y rotation (forward direction)
                float currentYRotation = transform.eulerAngles.y;
                Vector3 forward = Quaternion.Euler(0, currentYRotation, 0) * Vector3.forward;
                
                // Calculate rotation using LookRotation with ground normal as up
                // This will tilt the tombstone to match the slope while preserving Y rotation
                transform.rotation = Quaternion.LookRotation(forward, groundNormal);
            }
        }
    }

    private void OnEnable()
    {
        hasBeenUsed = false;
        isPlayerInRange = false;
        
        // Align with ground when enabled (in case position changed)
        AlignWithGround();
    }

    private void Update()
    {
        if (hasBeenUsed) return;
        
        // Check if game is paused
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            if (hudManager != null && isPlayerInRange)
            {
                hudManager.HideInteractionText();
            }
            return;
        }
        
        // Get player transform
        if (playerTransform == null)
        {
            if (GameManager.Instance != null)
            {
                playerTransform = GameManager.Instance.GetPlayer();
            }
        }
        
        if (playerTransform == null) return;
        
        // Check if player is in range
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool wasInRange = isPlayerInRange;
        isPlayerInRange = distance <= interactionRadius;
        
        if (isPlayerInRange)
        {
            // Show interaction text (white text with yellow "E")
            if (hudManager != null)
            {
                string msg = "Press <color=yellow>\"E\"</color> to spawn a horde";
                if (LocalizationManager.Instance != null)
                {
                    msg = LocalizationManager.Instance.GetLocalizedString("Interaction_Tombstone");
                    // Inject color tag for "E" manually if the localized string doesn't have it, 
                    // or assume localized strings might want to format it differently.
                    // For now, simple replacement of "E" with colored E if it exists in translation
                    if (msg.Contains("E"))
                    {
                        msg = msg.Replace("E", "<color=yellow>\"E\"</color>");
                    }
                }
                hudManager.ShowInteractionText(msg, Color.white);
            }
            
            // Check for E key or mobile interact button
            bool interactPressed = Input.GetKeyDown(KeyCode.E);
            if (!interactPressed && MobileInputController.IsMobile && MobileInputController.Instance != null)
            {
                interactPressed = MobileInputController.Instance.GetInteractInput();
            }
            
            if (interactPressed)
            {
                ActivateTombstone();
            }
        }
        else if (wasInRange)
        {
            // Player left range - hide text
            if (hudManager != null)
            {
                hudManager.HideInteractionText();
            }
        }
    }

    private void ActivateTombstone()
    {
        if (hasBeenUsed) return;
        
        hasBeenUsed = true;
        
        // Hide interaction text
        if (hudManager != null)
        {
            hudManager.HideInteractionText();
        }
        
        // --- SOUND: Tombstone Interaction ---
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayTombstoneInteract();
        }
        
        // Spawn enemies
        SpawnEnemies();
        
        // Destroy tombstone
        Destroy(gameObject);
    }

    private void SpawnEnemies()
    {
        if (enemySpawner == null)
        {
            Debug.LogWarning("Tombstone: EnemySpawner not found! Cannot spawn enemies.");
            return;
        }
        
        if (playerTransform == null)
        {
            Debug.LogWarning("Tombstone: Player transform not found! Cannot spawn enemies.");
            return;
        }
        
        // Get current difficulty (same as EnemySpawner uses) - use runTime for proper reset
        float minutes = 0f;
        if (GameManager.Instance != null)
        {
            minutes = GameManager.Instance.runTime / 60f;
        }
        float difficultyScalar = 1f + minutes * 0.08f; // Same as bal_difficultyGrowth
        
        // Spawn 10 random non-boss enemies around the tombstone
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnRandomNonBossEnemy(difficultyScalar);
        }
    }

    private void SpawnRandomNonBossEnemy(float difficulty)
    {
        if (enemySpawner == null) return;
        
        // Calculate spawn position around tombstone
        float angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
        float radius = UnityEngine.Random.Range(3f, spawnRadius);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
        Vector3 spawnPos = transform.position + offset;
        
        // Snap to ground
        if (MapGenerator.Instance != null)
        {
            spawnPos.y = MapGenerator.Instance.GetHeight(spawnPos);
        }
        else
        {
            Ray ray = new Ray(new Vector3(spawnPos.x, 100f, spawnPos.z), Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200f))
            {
                spawnPos.y = hit.point.y;
            }
        }
        
        // Spawn enemy using EnemySpawner's public method
        enemySpawner.SpawnRandomNonBossEnemyAtPosition(spawnPos, difficulty);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize interaction radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
        
        // Visualize spawn radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}

