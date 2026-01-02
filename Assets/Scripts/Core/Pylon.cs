using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Pylon : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float chargeRadius = 5f;
    [SerializeField] private float chargeHeight = 10f; // Height of the charging zone box
    [SerializeField] private float chargeDuration = 4f; // Charging duration in seconds (must be 4 seconds)
    
    private float chargeProgress = 0f;
    private bool isCharging = false;
    private bool hasBeenUsed = false;
    private Transform playerTransform;
    private BoxCollider chargeTriggerCollider;
    private LevelUpScreen levelUpScreen;
    private HUDManager hudManager;

    private void Awake()
    {
        // Align pylon with ground slope
        AlignWithGround();
        
        // BoxCollider is for the pylon's main collider (physics)
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = false; // Not a trigger, for physics
        }
        
        // Create or get BoxCollider for charging zone
        BoxCollider[] allBoxColliders = GetComponents<BoxCollider>();
        if (allBoxColliders.Length > 1)
        {
            // Use the second BoxCollider (first is the main pylon collider)
            chargeTriggerCollider = allBoxColliders[1];
        }
        else
        {
            // Create a second BoxCollider for charging zone
            chargeTriggerCollider = gameObject.AddComponent<BoxCollider>();
        }
        
        if (chargeTriggerCollider != null)
        {
            // Set box size: width/depth = radius * 2, height = chargeHeight
            chargeTriggerCollider.size = new Vector3(chargeRadius * 2f, chargeHeight, chargeRadius * 2f);
            // Position center so bottom of box is at origin (pylon's lowest point)
            chargeTriggerCollider.center = new Vector3(0f, chargeHeight * 0.5f, 0f);
            chargeTriggerCollider.isTrigger = true;
        }
        
        // Cache LevelUpScreen reference
        if (levelUpScreen == null)
        {
            levelUpScreen = FindFirstObjectByType<LevelUpScreen>();
        }
        
        // Cache HUDManager reference
        if (hudManager == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
            {
                hudManager = GameManager.Instance.hudManager;
            }
            else
            {
                hudManager = FindFirstObjectByType<HUDManager>();
            }
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
                // This will tilt the pylon to match the slope while preserving Y rotation
                transform.rotation = Quaternion.LookRotation(forward, groundNormal);
            }
        }
    }
    
    private void OnEnable()
    {
        chargeProgress = 0f;
        isCharging = false;
        hasBeenUsed = false;
        
        // Align with ground when enabled (in case position changed)
        AlignWithGround();
        
        // Refresh charging box collider settings
        if (chargeTriggerCollider != null)
        {
            chargeTriggerCollider.size = new Vector3(chargeRadius * 2f, chargeHeight, chargeRadius * 2f);
            chargeTriggerCollider.center = new Vector3(0f, chargeHeight * 0.5f, 0f);
            chargeTriggerCollider.isTrigger = true;
        }
        
        // Hide slider
        if (hudManager != null)
        {
            hudManager.HidePylonProgress();
        }
        
        // Stop charging sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopPylonCharging();
        }
    }
    
    private void OnDisable()
    {
        // Stop charging sound when pylon is disabled (e.g., player dies, scene changes)
        if (isCharging && SoundManager.Instance != null)
        {
            SoundManager.Instance.StopPylonCharging();
        }
        
        isCharging = false;
        chargeProgress = 0f;
        
        // Hide slider
        if (hudManager != null)
        {
            hudManager.HidePylonProgress();
        }
    }

    private void Update()
    {
        if (hasBeenUsed) return;
        
        // Check if player is in range
        if (playerTransform == null)
        {
            if (GameManager.Instance != null)
            {
                playerTransform = GameManager.Instance.GetPlayer();
            }
        }
        
        if (playerTransform == null) return;
        
        // Check if player is within the box collider (charging zone)
        bool isInRange = false;
        if (chargeTriggerCollider != null)
        {
            Vector3 playerPos = playerTransform.position;
            // Convert player position to local space relative to collider
            Vector3 localPos = transform.InverseTransformPoint(playerPos);
            // Check if player is within the collider's local bounds
            Bounds bounds = new Bounds(chargeTriggerCollider.center, chargeTriggerCollider.size);
            isInRange = bounds.Contains(localPos);
        }
        
        if (isInRange)
        {
            if (!isCharging)
            {
                isCharging = true;
                // Start playing charging sound from the beginning
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayPylonCharging();
                }
            }
            
            // Charge progress
            chargeProgress += Time.deltaTime;
            
            // Update slider via HUDManager
            if (hudManager != null)
            {
                hudManager.ShowPylonProgress(chargeProgress / chargeDuration);
            }
            
            if (chargeProgress >= chargeDuration)
            {
                ActivatePylon();
            }
        }
        else
        {
            // Player left range - reset charge
            if (isCharging)
            {
                isCharging = false;
                chargeProgress = 0f;
                // Hide slider when player leaves
                if (hudManager != null)
                {
                    hudManager.HidePylonProgress();
                }
                // Stop charging sound
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.StopPylonCharging();
                }
            }
        }
    }

    private void ActivatePylon()
    {
        if (hasBeenUsed) return;
        
        hasBeenUsed = true;
        isCharging = false;
        
        // Hide slider immediately
        if (hudManager != null)
        {
            hudManager.HidePylonProgress();
        }
        
        // Stop charging sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopPylonCharging();
        }
        
        // --- SOUND: Pylon Charged (Upgrade Choice) ---
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayUpgradeChoice();
        }
        
        // Deactivate pylon immediately (it will work again when a new one spawns)
        gameObject.SetActive(false);
        
        // Show upgrade choice screen (same as level up)
        if (levelUpScreen == null)
        {
            levelUpScreen = FindFirstObjectByType<LevelUpScreen>();
        }
        
        if (levelUpScreen != null)
        {
            // Pause game and show upgrade screen (same as level up)
            Time.timeScale = 0f;
            levelUpScreen.Show();

            // Hide weapon when pylon upgrade screen shows
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetWeaponVisible(false);
            }
        }
        else
        {
            Debug.LogWarning("Pylon: LevelUpScreen not found! Cannot show upgrade choices.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize charge zone (box) in editor
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position + new Vector3(0f, chargeHeight * 0.5f, 0f);
        Vector3 size = new Vector3(chargeRadius * 2f, chargeHeight, chargeRadius * 2f);
        Gizmos.DrawWireCube(center, size);
    }
}

