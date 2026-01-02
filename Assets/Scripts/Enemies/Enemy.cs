using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float baseSpeed = 2.25f;

    [Header("Runtime Stats")]
    [SerializeField] private float currentHP;
    [SerializeField] private float maxHP; // Track max HP for boss health UI
    [SerializeField] private float moveSpeed;
    [SerializeField] private float damage;

    [Header("Pickups")]
    [SerializeField] private GameObject xpPickupPrefab;
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private float chestDropChance = 0.035f;
    [SerializeField] private GameObject soulPrefab;
    [SerializeField] private GameObject magnetPickupPrefab;
    [SerializeField] private GameObject ragePickupPrefab;
    [SerializeField] private float specialPickupDropChance = 0.01f; // 1% chance for magnet or rage

    [Header("Type Flags")]
    [SerializeField] private bool isBoss = false;
    [SerializeField] private bool isElite = false;
    [SerializeField] private bool isMapUnlockBoss = false; // Only 10-minute boss unlocks map

    [Header("VFX")]
    [SerializeField] private string hitVfxTag = "HitVFX";
    [SerializeField] private string aoeHitVfxTag = "AoEHitVFX";
    [SerializeField] private float hitVfxHeightOffset = 1.2f;
    [SerializeField] private float hitVfxForwardOffset = 0.35f;
    // [SerializeField] private GameObject goldPickupPrefab; // REMOVED

    [Header("Balance Settings")]
    [SerializeField] private float bal_normalBaseHP = 40f;
    [SerializeField] private float bal_hpGrowth = 0.24f;
    [SerializeField] private float bal_normalBaseDmg = 8f;
    [SerializeField] private float bal_dmgGrowth = 0.16f;
    [SerializeField] private float bal_baseXP = 16f;
    [SerializeField] private float bal_xpGrowth = 0.08f;
    [SerializeField] private float bal_speedGrowth = 0.05f;
    [SerializeField] private float bal_extremeSpeedMultiplier = 2.5f;

    public bool IsBoss
    {
        get { return isBoss; }
        set { SetBossMode(value); }
    }

    public bool IsEliteo
    {
        get { return isElite; }
        set { isElite = value; }
    }

    // Public properties for boss health UI
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsDead => isDead;

    private Transform target;
    private bool isDead = false;
    private Rigidbody rb;
    private Transform _transform;
    private PlayerStats cachedPlayerStats;
    private UpgradeSystem cachedUpgradeSystem;

    // Knockback
    private Vector3 knockbackVel = Vector3.zero;
    private float knockbackResistTimer = 0f;

    // Player damage cooldown
    private float nextAttackTime = 0f;
    private const float attackCooldown = 0.2f;

    // Tracks if player is currently in range for continuous damage
    private bool isPlayerInRange = false;

    // --- OBSTACLE CLIMBING ---
    [Header("Climbing Settings")]
    [SerializeField] private float obstacleCheckDistance = 1.5f;
    [SerializeField] private float climbForce = 8f;
    [SerializeField] private float maxClimbHeight = 3f;
    [SerializeField] private float climbForwardBoost = 3f; // Forward velocity boost when climbing
    private float lastObstacleCheckTime = 0f;
    private const float obstacleCheckInterval = 0.2f; // Check every 0.2s to avoid per-frame raycasts
    private Vector3 lastPosition; // For stuck detection
    private float stuckTimer = 0f;
    private const float stuckCheckInterval = 0.5f; // Check for stuck every 0.5s

    // --- SPAWN IN ANIMATION ---
    private float spawnTime;
    private const float spawnDuration = 0.3f;
    private bool isSpawning = false;
    private float targetSpawnY;

    // ======= OPENVAT HIT FLASH FIELDS =======
    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;
    private float _hitFlashTimer = 0f;
    private const float HitFlashDuration = 0.2f;
    private static readonly int HitFlashID = Shader.PropertyToID("_HitFlash");
    private const string XpPickupPoolTag = "PickupXP";
    private const string SoulPickupPoolTag = "SoulPickup";
    private const string ChestPoolTag = "ChestPickup";
    private const string MagnetPickupPoolTag = "PickupMagnet";
    private const string RagePickupPoolTag = "PickupRage";
    // ========================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _transform = transform;

        // --- HIT FLASH INIT FOR openVAT (_HitFlash float param style) ---
        _renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (_renderer == null)
            _renderer = GetComponentInChildren<MeshRenderer>();

        if (_renderer != null)
        {
            if (_mpb == null)
                _mpb = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(HitFlashID, 0f);
            _renderer.SetPropertyBlock(_mpb);
        }
        else
        {
            Debug.LogWarning($"{name}: No mesh renderer found on Enemy! openVAT flash will not work.");
        }
        // ---------------------------------

        if (rb)
        {
            rb.mass = 500f;
            rb.linearDamping = 1f;
            rb.angularDamping = 1f;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            Debug.LogWarning($"{name}: No Rigidbody found on Enemy! Please add a Rigidbody component.");
        }

        CacheUpgradeSystemReference();
    }

    // Called by Spawner when reusing object from pool
    public void ResetEnemy(float difficulty, Transform playerTarget)
    {
        // Reset boss/elite flags to default when reusing from pool
        // These will be set explicitly by SpawnBossStoneGolem if needed
        isBoss = false;
        isElite = false;
        isMapUnlockBoss = false;
        
        // Reset scale to default (boss scale is set by SetBossMode)
        if (_transform != null)
        {
            _transform.localScale = Vector3.one;
        }
        
        // Use GameManager.runTime for proper reset on game restart
        float minutes = 0f;
        if (GameManager.Instance != null)
        {
            minutes = GameManager.Instance.runTime / 60f;
        }
        else
        {
            minutes = Time.time / 60f;
        }
        
        float difficultyMultiplier = Mathf.Max(1f, difficulty);
        float rageT = Mathf.Clamp01((minutes - 10f) / 5f);

        // 1) ENEMY STATS FORMULAS
        float normalHP = bal_normalBaseHP * Mathf.Pow(1f + bal_hpGrowth, minutes) * difficultyMultiplier;
        float normalDamage = Mathf.Min(bal_normalBaseDmg * Mathf.Pow(1f + bal_dmgGrowth, minutes), 80f) * difficultyMultiplier;

        if (isBoss)
        {
            currentHP = normalHP * 15f;
            damage = normalDamage * 3f;
        }
        else if (isElite)
        {
            currentHP = normalHP * 4f;
            damage = normalDamage * 1.8f;
        }
        else
        {
            currentHP = normalHP;
            damage = normalDamage;
        }

        maxHP = currentHP; // Track max HP for boss health UI

        float speedScale = 1f + minutes * bal_speedGrowth;
        float extremeSpeed = Mathf.Lerp(1f, bal_extremeSpeedMultiplier, rageT);
        moveSpeed = baseSpeed * speedScale * extremeSpeed * Mathf.Lerp(1f, 1.25f, difficultyMultiplier - 1f);
        target = playerTarget;
        CachePlayerStatsReference();
        isDead = false;
        knockbackResistTimer = 0f;
        nextAttackTime = 0f;
        knockbackVel = Vector3.zero;
        isPlayerInRange = false;
        lastPosition = _transform.position;
        stuckTimer = 0f;
        // --- SPAWN IN ANIMATION ---
        isSpawning = true;
        spawnTime = 0f;
        if (_transform != null)
        {
            _transform.localScale = Vector3.one * 0.1f; // Start tiny

            // FIX: Raycast down to snap target Y to the ground
            Vector3 rayOrigin = _transform.position + Vector3.up * 2f;
            RaycastHit hit;
            float groundY;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 20f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                groundY = hit.point.y;
            }
            else
            {
                // Fallback: use current position as ground level
                groundY = _transform.position.y;
            }
            
            // If enemy is already positioned high (boss spawn), preserve that height offset
            float currentHeightAboveGround = _transform.position.y - groundY;
            if (currentHeightAboveGround > 0.5f)
            {
                // Boss spawn: spawn 2m above ground
                targetSpawnY = groundY + 2f;
            }
            else
            {
                // Regular spawn: snap to ground
                targetSpawnY = groundY;
            }
            // Start spawn animation 2m below target position (for pop-up effect)
            _transform.position = new Vector3(_transform.position.x, targetSpawnY - 2.0f, _transform.position.z);
        }
        // Disable physics while spawning ("underground")
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
        // Reset openVAT _HitFlash on object reuse
        if (_renderer != null && _mpb != null)
        {
            _hitFlashTimer = 0f;
            _mpb.SetFloat(HitFlashID, 0f);
            _renderer.SetPropertyBlock(_mpb);
        }
    }

    // Public method to set Boss mode
    public void SetBossMode(bool status)
    {
        isBoss = status;
        if (_transform != null)
        {
            if (status)
            {
                _transform.localScale = Vector3.one * 3f;
                // (Optional: add color/other visuals here for boss feedback.)
                
                // Show boss health UI when boss mode is enabled
                if (GameManager.Instance != null && GameManager.Instance.hudManager != null && maxHP > 0f)
                {
                    GameManager.Instance.hudManager.ShowBossHealth();
                    GameManager.Instance.hudManager.UpdateBossHealth(currentHP, maxHP);
                }
            }
            else
            {
                _transform.localScale = Vector3.one;
                
                // Hide boss health UI when boss mode is disabled
                if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
                {
                    GameManager.Instance.hudManager.HideBossHealth();
                }
            }
        }
    }

    public void SetMapUnlockBoss(bool status)
    {
        isMapUnlockBoss = status;
    }
    
    /// <summary>
    /// Recalculates HP for a boss after boss mode has been set.
    /// Call this after SetBossMode(true) to apply boss HP multiplier.
    /// </summary>
    public void RecalculateBossHP(float difficulty)
    {
        if (!isBoss) return;
        
        // Use GameManager.runTime for proper calculation
        float minutes = 0f;
        if (GameManager.Instance != null)
        {
            minutes = GameManager.Instance.runTime / 60f;
        }
        
        float difficultyMultiplier = Mathf.Max(1f, difficulty);
        
        // Recalculate HP with boss multiplier
        float normalHP = bal_normalBaseHP * Mathf.Pow(1f + bal_hpGrowth, minutes) * difficultyMultiplier;
        float normalDamage = Mathf.Min(bal_normalBaseDmg * Mathf.Pow(1f + bal_dmgGrowth, minutes), 80f) * difficultyMultiplier;
        
        currentHP = normalHP * 15f;
        maxHP = currentHP;
        damage = normalDamage * 3f;
        
        // Update boss health UI
        if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
        {
            GameManager.Instance.hudManager.ShowBossHealth();
            GameManager.Instance.hudManager.UpdateBossHealth(currentHP, maxHP);
        }
    }

    // Move all movement/knockback logic to FixedUpdate for physics correctness and performance
    private void FixedUpdate()
    {
        if (isDead || rb == null) return;

        // --- SPAWN IN ANIMATION ---
        if (isSpawning)
        {
            spawnTime += Time.fixedDeltaTime;
            float progress = spawnTime / spawnDuration;

            if (progress >= 1f)
            {
                isSpawning = false;
                _transform.localScale = isBoss ? Vector3.one * 3f : Vector3.one;
                // Snap to final Y position
                _transform.position = new Vector3(_transform.position.x, targetSpawnY, _transform.position.z);
                // Enable physics when finished spawning
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.detectCollisions = true;
                }
            }
            else
            {
                float currentScale = Mathf.Lerp(0.1f, isBoss ? 3f : 1f, progress);
                _transform.localScale = Vector3.one * currentScale;

                float currentY = Mathf.Lerp(targetSpawnY - 2.0f, targetSpawnY, progress);
                _transform.position = new Vector3(_transform.position.x, currentY, _transform.position.z);
            }
            // Return while spawning to pause normal movement until fully spawned
            if (isSpawning)
            {
                return;
            }
        }

        bool isKnockedBack = knockbackVel.sqrMagnitude > 0.04f;
        if (isKnockedBack)
        {
            // Apply Knockback Movement
            _transform.position += knockbackVel * Time.fixedDeltaTime;

            // Simple arithmetic decay (much cheaper than Lerp for WebGL)
            knockbackVel -= knockbackVel * (5f * Time.fixedDeltaTime);
        }

        // Legacy knockbackResistTimer (very rare, can be refactored out later)
        if (knockbackResistTimer > 0f)
        {
            knockbackResistTimer -= Time.fixedDeltaTime;
            return;
        }

        // --- OBSTACLE CLIMBING CHECK ---
        if (target != null && Time.time > lastObstacleCheckTime + obstacleCheckInterval)
        {
            lastObstacleCheckTime = Time.time;
            TryClimbObstacle();
        }

        // --- STUCK DETECTION AND RECOVERY ---
        if (target != null && !isDead && Time.time > stuckTimer + stuckCheckInterval)
        {
            stuckTimer = Time.time;
            CheckAndRecoverFromStuck();
        }

        // Only execute AI movement if we have a target
        if (target != null)
        {
            Vector3 direction = target.position - _transform.position;
            direction.y = 0f;
            float distSqr = direction.sqrMagnitude;
            if (distSqr > 0.001f)
            {
                Vector3 moveDir = direction / Mathf.Sqrt(distSqr); // normalized, but faster than direction.normalized
                if (!isKnockedBack)
                {
                    Vector3 moveVel = moveDir * moveSpeed;
                    rb.linearVelocity = new Vector3(moveVel.x, rb.linearVelocity.y, moveVel.z);
                }
                else
                {
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                }

                // Rotate to face player
                if (moveDir.sqrMagnitude > 0.00001f)
                {
                    _transform.rotation = Quaternion.LookRotation(moveDir);
                }
            }
            else
            {
                // Stop movement if very close to target
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }

        // --- Continuous Player Damage Logic ---
        if (isPlayerInRange && Time.time > nextAttackTime)
        {
            PlayerStats playerStats = GetCachedPlayerStats();
            if (playerStats != null)
            {
                playerStats.TakeDamage(damage, this.transform);
                nextAttackTime = Time.time + attackCooldown;
            }
        }

        // --- Check if player is on top of enemy (for head-jumping damage) ---
        if (target != null && Time.time > nextAttackTime)
        {
            Vector3 toPlayer = target.position - _transform.position;
            float horizontalDist = new Vector3(toPlayer.x, 0f, toPlayer.z).magnitude;
            float verticalDist = toPlayer.y;

            // Player is directly above enemy (within 1.5m horizontally) and between 0.3m to 3m above
            if (horizontalDist < 1.5f && verticalDist > 0.3f && verticalDist < 3.0f)
            {
                PlayerStats playerStats = GetCachedPlayerStats();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(damage, this.transform);
                    nextAttackTime = Time.time + attackCooldown;
                }
            }
        }

        // Update position tracking for stuck detection
        lastPosition = _transform.position;
    }

    // Check if enemy is stuck on edge of obstacle and help them get unstuck (very restrictive)
    private void CheckAndRecoverFromStuck()
    {
        if (target == null || rb == null) return;

        // Check horizontal movement - if moved less than 0.2m in last check, might be stuck
        Vector3 horizontalMove = _transform.position - lastPosition;
        horizontalMove.y = 0f;
        float moveDist = horizontalMove.magnitude;

        if (moveDist < 0.2f)
        {
            // Enemy might be stuck on wall edge - check if player is ahead and there's a wall
            Vector3 toPlayer = target.position - _transform.position;
            toPlayer.y = 0f;
            float horizontalDist = toPlayer.magnitude;

            if (horizontalDist > 3.0f) // Player is ahead but enemy not moving
            {
                // Check if there's actually a wall blocking (same check as climbing)
                Vector3 forwardDir = toPlayer.normalized;
                Vector3 rayOrigin = _transform.position + Vector3.up * 0.5f;
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, forwardDir, out hit, obstacleCheckDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                {
                    // Only help if it's a vertical wall (not slope, not enemy, not player)
                    if (!hit.collider.TryGetComponent<Enemy>(out _) && hit.collider.gameObject != target.gameObject)
                    {
                        float surfaceAngle = Vector3.Angle(Vector3.up, hit.normal);
                        if (surfaceAngle > 45f) // It's a wall
                        {
                            // Apply forward boost with slight vertical to clear lips/edges
                            Vector3 currentVel = rb.linearVelocity;
                            Vector3 forwardBoost = forwardDir * (climbForwardBoost * 0.5f);
                            rb.linearVelocity = new Vector3(forwardBoost.x, climbForce * 0.5f, forwardBoost.z);
                        }
                    }
                }
            }
        }
    }

    // Climbing: climb when player is above AND there's a wall/obstacle blocking the path (but not slopes/enemies)
    private void TryClimbObstacle()
    {
        if (target == null || rb == null) return;

        // Don't climb if already in range to attack player
        if (isPlayerInRange) return;

        // Check if player is above enemy (at least 0.25m higher to avoid minor elevation differences)
        float heightDiff = target.position.y - _transform.position.y;
        if (heightDiff < 0.25f) return; // Player not above, no climbing needed

        // Check horizontal distance
        Vector3 toPlayer = target.position - _transform.position;
        toPlayer.y = 0f;
        float horizontalDist = toPlayer.magnitude;
        // REMOVED: if (horizontalDist < 2.0f) return; - caused enemies to not climb when player is on the obstacle

        // Raycast forward toward player to detect obstacles
        Vector3 forwardDir = toPlayer.normalized;
        Vector3 rayOrigin = _transform.position + Vector3.up * 0.3f;
        
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, forwardDir, out hit, obstacleCheckDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            // Don't climb if the obstacle is another enemy
            if (hit.collider.TryGetComponent<Enemy>(out _))
            {
                return; // Ignore other enemies - don't jump on them
            }

            // Don't climb if hitting the player directly and too close
            if (hit.collider.gameObject == target.gameObject && horizontalDist < 3.0f)
            {
                return; // Too close to player, don't climb
            }

            // Check if it's a slope or wall
            float surfaceAngle = Vector3.Angle(Vector3.up, hit.normal);
            
            // Don't climb gentle slopes (angle < 30 degrees means it's walkable)
            if (surfaceAngle < 30f)
            {
                return; // It's a gentle slope, walk over it instead
            }

            // For walls (angle >= 30 degrees) or any obstacle when player is above
            // Check if there's something blocking the path upward
            float obstacleTopY = hit.point.y;
            if (hit.collider.bounds.size.y > 0.1f)
            {
                obstacleTopY = hit.collider.bounds.max.y;
            }

            // Check if obstacle is climbable height (between 0.3m and maxClimbHeight)
            float obstacleHeight = obstacleTopY - _transform.position.y;
            if (obstacleHeight < 0.3f || obstacleHeight > maxClimbHeight)
            {
                return; // Too low to matter or too high to climb
            }

            // If we get here: player is above, there's a wall/obstacle blocking, and it's climbable
            // Climb with forward boost to get over the obstacle
            Vector3 currentVel = rb.linearVelocity;
            Vector3 forwardBoost = forwardDir * climbForwardBoost;
            rb.linearVelocity = new Vector3(forwardBoost.x, climbForce, forwardBoost.z);
        }
    }

    // Refactored knockback application
    public void TakeKnockback(Vector3 direction, float force)
    {
        // Bosses are immune to knockback
        if (isBoss) return;
        
        knockbackVel = direction.normalized * force;
    }

    // Overload for legacy knockback logic (deprecated, no-op)
    public void ApplyKnockback(Vector3 force)
    {
        knockbackResistTimer = 0.2f;
    }

    // --- openVAT flash: trigger method ---
    private void TriggerHitFlash()
    {
        if (_renderer == null) return;
        _hitFlashTimer = HitFlashDuration;
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(HitFlashID, 1f);
        _renderer.SetPropertyBlock(_mpb);
    }

    // --- openVAT flash: update method ---
    private void UpdateHitFlash(float deltaTime)
    {
        if (_hitFlashTimer <= 0f) return;
        _hitFlashTimer -= deltaTime;

        float flashAmount = Mathf.Clamp01(_hitFlashTimer / HitFlashDuration);

        if (_renderer != null && _mpb != null)
        {
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(HitFlashID, flashAmount);
            _renderer.SetPropertyBlock(_mpb);
        }
    }

    // --- Modified TakeDamage(): openVAT flash when taking ANY damage ---
    // Overload that supports crit flag and correct visual popup logic, modified for AoE VFX handling
    public void TakeDamage(float amount, bool isCrit, float aoeRadius = 0f)
    {
        // Only try to flash if active in hierarchy
        if (gameObject.activeInHierarchy)
        {
            TriggerHitFlash();
        }

        // Track damage for DPS counter
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterDamage(amount);
        }

        // Tutorial Hook
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnDamageDealt();
        }

        // --- HP LOGIC ---
        currentHP -= amount;

        // Update boss health UI if this is a boss
        if (isBoss && GameManager.Instance != null && GameManager.Instance.hudManager != null)
        {
            GameManager.Instance.hudManager.UpdateBossHealth(currentHP, maxHP);
        }

        // VISUALS: Always spawn the single-target (direct hit) hitVfx. 
        // Also, if aoeRadius > 0.1f, spawn aoeHitVfx on top.
        if (ObjectPool.Instance != null)
        {
            // Always spawn hitVfxTag slightly in front of the enemy so it isn't hidden inside the mesh
            Vector3 vfxPos = _transform.position + Vector3.up * hitVfxHeightOffset;
            if (target != null)
            {
                Vector3 towardPlayer = target.position - _transform.position;
                towardPlayer.y = 0f;
                if (towardPlayer.sqrMagnitude > 0.001f)
                    vfxPos += towardPlayer.normalized * hitVfxForwardOffset;
            }
            else
            {
                vfxPos += _transform.forward * hitVfxForwardOffset;
            }
            ObjectPool.Instance.SpawnFromPool(hitVfxTag, vfxPos, Quaternion.identity);

            // If AoE radius is meaningful, ALSO spawn AoE VFX
            if (aoeRadius > 0.1f)
            {
                GameObject aoeVfx = ObjectPool.Instance.SpawnFromPool(aoeHitVfxTag, _transform.position + Vector3.up * 0.5f, Quaternion.identity);
                if (aoeVfx != null)
                {
                    aoeVfx.transform.localScale = Vector3.one * (aoeRadius * 2f);
                }
            }
        }

        // --- PLAY SOUND ---
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEnemyHit();
        }

        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.ShowDamage(amount, _transform.position + Vector3.up * 1.5f, isCrit, this.gameObject);
        }

        if (isDead) return;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // Maintains compatibility for calls that use only (amount, isCrit)
    public void TakeDamage(float amount, bool isCrit)
    {
        TakeDamage(amount, isCrit, 0f);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, false, 0f);
    }

    private void Die()
    {
        // Ensure dead flag set immediately
        isDead = true;

        // Register kill with GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterKill();
        }

        try
        {
            // === Boss Unlock Logic ===
            // Only the 10-minute boss (map unlock boss) unlocks the map
            if (isBoss)
            {
                // Hide boss health UI when boss dies
                if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
                {
                    GameManager.Instance.hudManager.HideBossHealth();
                }

                // Only unlock map if this is the map unlock boss (10-minute boss)
                if (isMapUnlockBoss && SaveManager.Instance != null)
                {
                    SaveManager.Instance.UnlockMap(1);
                    Debug.Log("[Boss] Map 2 Unlocked by 10-minute boss!");
                }

                // --- CrazyGames SDK: Call happytime when boss is killed (exciting moment) ---
                if (PlatformManager.Instance != null && PlatformManager.Instance.IsInitialized)
                {
                    if (PlatformManager.Instance.SDK is CrazyGamesSDK crazySDK)
                    {
                        crazySDK.CallHappyTime();
                    }
                }
            }
            // Also call happytime for elite kills (exciting moments)
            else if (isElite)
            {
                if (PlatformManager.Instance != null && PlatformManager.Instance.IsInitialized)
                {
                    if (PlatformManager.Instance.SDK is CrazyGamesSDK crazySDK)
                    {
                        crazySDK.CallHappyTime();
                    }
                }
            }

            // Use GameManager.runTime for proper reset on game restart
            float minutes = 0f;
            if (GameManager.Instance != null)
            {
                minutes = GameManager.Instance.runTime / 60f;
            }

            // 3) XP SYSTEM FORMULAS
            float difficultyMultiplier = 1f + minutes * 0.1f;
            float normalXP = bal_baseXP * Mathf.Pow(1f + bal_xpGrowth, minutes) * difficultyMultiplier;

            float xpToDrop = normalXP;
            if (isBoss) xpToDrop = normalXP * 12f;
            else if (isElite) xpToDrop = normalXP * 4f;
            
            // Madness mode: increase XP drops
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameMode == GameMode.Madness)
            {
                xpToDrop *= 1.5f; // 1.5x more XP in Madness mode
            }

            // --- XP Pickup Spawn ---
            if (xpPickupPrefab != null)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-0.3f, 0.3f),
                    0.5f,
                    UnityEngine.Random.Range(-0.3f, 0.3f)
                );
                Vector3 spawnPos = _transform.position + offset;
            GameObject xpObj = SpawnPickupFromPool(XpPickupPoolTag, xpPickupPrefab, spawnPos, Quaternion.identity);
            if (xpObj != null)
                {
                PickupXP pickupScript = xpObj.GetComponent<PickupXP>();
                if (pickupScript != null)
                {
                    pickupScript.Setup(xpToDrop);
                }
                }
            }

            // --- GOLD DROP (100% Chance) ---
            int goldAmount = 1; // Base 1 gold
            if (isElite) goldAmount += 2;
            if (isBoss) goldAmount += 10;

            // Find PlayerStats and add gold directly
            PlayerStats stats = GetCachedPlayerStats();
            if (stats != null)
            {
                stats.AddGold(goldAmount);
            }

            // --- SOUL DROP (4% Chance) ---
        if (soulPrefab != null && UnityEngine.Random.value <= 0.04f)
            {
            GameObject soulObj = SpawnPickupFromPool(SoulPickupPoolTag, soulPrefab, _transform.position, Quaternion.identity);
            if (soulObj != null)
                {
                var soulPickup = soulObj.GetComponent<SoulPickup>();
                if (soulPickup != null)
                {
                    soulPickup.Setup(1); // 1 Soul per pickup
                }
                }
            }

            // --- Special Pickup Drop (Magnet or Rage) - 1% chance ---
            if (UnityEngine.Random.value < specialPickupDropChance)
            {
                Vector3 specialOffset = new Vector3(
                    UnityEngine.Random.Range(-0.3f, 0.3f),
                    0.5f,
                    UnityEngine.Random.Range(-0.3f, 0.3f)
                );
                Vector3 specialSpawnPos = _transform.position + specialOffset;
                
                // Randomly choose between magnet or rage (50/50)
                bool isMagnet = UnityEngine.Random.value < 0.5f;
                
                if (isMagnet && magnetPickupPrefab != null)
                {
                    GameObject magnetObj = SpawnPickupFromPool(MagnetPickupPoolTag, magnetPickupPrefab, specialSpawnPos, Quaternion.identity);
                    if (magnetObj != null)
                    {
                        PickupMagnet magnetScript = magnetObj.GetComponent<PickupMagnet>();
                        if (magnetScript != null)
                        {
                            magnetScript.SetPooledInstance(true);
                        }
                    }
                }
                else if (!isMagnet && ragePickupPrefab != null)
                {
                    GameObject rageObj = SpawnPickupFromPool(RagePickupPoolTag, ragePickupPrefab, specialSpawnPos, Quaternion.identity);
                    if (rageObj != null)
                    {
                        PickupRage rageScript = rageObj.GetComponent<PickupRage>();
                        if (rageScript != null)
                        {
                            rageScript.SetPooledInstance(true);
                        }
                    }
                }
            }

            // --- Chest Drop ---
            // Bosses always drop chests (guaranteed)
            bool shouldDropChest = isBoss || (chestPrefab != null && UnityEngine.Random.value < chestDropChance);
            if (chestPrefab != null && shouldDropChest)
            {
                Vector3 chestOffset = new Vector3(
                    UnityEngine.Random.Range(-0.3f, 0.3f),
                    0f,
                    UnityEngine.Random.Range(-0.3f, 0.3f)
                );
                Vector3 chestSpawnPos = _transform.position + chestOffset;
                
                // Snap to ground height using MapGenerator if available (same as enemy spawning)
                float groundY = chestSpawnPos.y;
                bool snapped = false;
                
                if (MapGenerator.Instance != null)
                {
                    groundY = MapGenerator.Instance.GetHeight(chestSpawnPos);
                    snapped = true;
                }
                else
                {
                    // Fallback to raycast from high above
                    Ray ray = new Ray(new Vector3(chestSpawnPos.x, 100f, chestSpawnPos.z), Vector3.down);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 200f))
                    {
                        // Only use if not hitting enemy/player
                        if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Enemy"))
                        {
                            groundY = hit.point.y;
                            snapped = true;
                        }
                    }
                }
                
                if (snapped)
                {
                    chestSpawnPos.y = groundY;
                }
                else
                {
                    // Final fallback: use enemy Y position
                    chestSpawnPos.y = _transform.position.y;
                }
                
                GameObject chestObj = null;
                
                // Bosses use their assigned prefab directly (not from pool) and scale to 2
                if (isBoss)
                {
                    // Mark as boss chest BEFORE instantiation so OnEnable sees it
                    // Note: We can't set it before instantiation, so we set it immediately after
                    // The animation will check the flag dynamically
                    chestObj = Instantiate(chestPrefab, chestSpawnPos, _transform.rotation);
                    if (chestObj != null)
                    {
                        // Mark as boss chest first (guarantees legendary item)
                        // This must be set before or during the spawn animation
                        ChestPickup chestPickup = chestObj.GetComponent<ChestPickup>();
                        if (chestPickup != null)
                        {
                            chestPickup.SetBossChest(true);
                        }
                        // Note: Don't set scale directly - let the spawn animation handle it
                        // The animation will scale to 2 for boss chests
                    }
                }
                else
                {
                    // Regular enemies use pool system
                    chestObj = SpawnPickupFromPool(ChestPoolTag, chestPrefab, chestSpawnPos, _transform.rotation);
                }

                // Play chest drop sound
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayItemDrop();
                }
            }

            // Try to drop a stat item using UpgradeSystem if setup
        UpgradeSystem upgradeSystem = GetCachedUpgradeSystem();
            if (upgradeSystem != null)
            {
                var statDrop = upgradeSystem.GenerateRandomStatItemDrop();
                if (statDrop != null)
                {
                    // TODO: Spawn visual StatItem pickup prefab here if implemented
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Enemy drop error: " + ex.Message);
        }

        // Always ensure this enemy gets deactivated (returns to pool)
        gameObject.SetActive(false);
    }

    private void TryDealDamage(GameObject targetObj)
    {
        if (targetObj.CompareTag("Player"))
        {
            if (Time.time > nextAttackTime)
            {
                PlayerStats playerStats;
                if (targetObj.TryGetComponent<PlayerStats>(out playerStats) && playerStats != null)
                {
                    playerStats.TakeDamage(damage, this.transform);
                }
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
        TryDealDamage(other.gameObject);
        // Projectiles handle their own logic
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
        // No action needed for non-player
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
        TryDealDamage(collision.gameObject);
        // (REMOVED PUSHBACK LOGIC)
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
        // No action needed for non-player
    }

    // Per frame update of openVAT hit flash, cheap: no allocations/coroutines. 
    private void Update()
    {
        UpdateHitFlash(Time.deltaTime);
    }

    // Removed OnTriggerStay/OnCollisionStay for perf
    private void CachePlayerStatsReference()
    {
        cachedPlayerStats = null;

        if (target != null)
        {
            cachedPlayerStats = target.GetComponent<PlayerStats>();
        }

        if (cachedPlayerStats == null)
        {
            cachedPlayerStats = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        }
    }

    private PlayerStats GetCachedPlayerStats()
    {
        if (cachedPlayerStats == null)
        {
            CachePlayerStatsReference();
        }

        return cachedPlayerStats;
    }

    private void CacheUpgradeSystemReference()
    {
        if (cachedUpgradeSystem == null)
        {
            cachedUpgradeSystem = UnityEngine.Object.FindFirstObjectByType<UpgradeSystem>();
        }
    }

    private UpgradeSystem GetCachedUpgradeSystem()
    {
        if (cachedUpgradeSystem == null)
        {
            CacheUpgradeSystemReference();
        }

        return cachedUpgradeSystem;
    }

    private GameObject SpawnPickupFromPool(string poolTag, GameObject fallbackPrefab, Vector3 position, Quaternion rotation)
    {
        GameObject spawned = null;
        bool spawnedFromPool = false;

        if (ObjectPool.Instance != null)
        {
            spawned = ObjectPool.Instance.SpawnFromPool(poolTag, position, rotation);
            spawnedFromPool = spawned != null;
        }

        if (spawned == null && fallbackPrefab != null)
        {
            spawned = Instantiate(fallbackPrefab, position, rotation);
            spawnedFromPool = false;
        }

        MarkPickupPooledState(spawned, spawnedFromPool);

        return spawned;
    }

    private void MarkPickupPooledState(GameObject pickup, bool isFromPool)
    {
        if (pickup == null)
            return;

        if (pickup.TryGetComponent<PickupXP>(out var xpPickup))
        {
            xpPickup.SetPooledInstance(isFromPool);
        }
        else if (pickup.TryGetComponent<SoulPickup>(out var soulPickup))
        {
            soulPickup.SetPooledInstance(isFromPool);
        }
        else if (pickup.TryGetComponent<ChestPickup>(out var chestPickup))
        {
            chestPickup.SetPooledInstance(isFromPool);
            // Note: Boss chest flag is set separately when chest is spawned by a boss
        }
    }
}
