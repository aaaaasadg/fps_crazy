using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float minSpawnRadius = 15f;
    [SerializeField] private float maxSpawnRadius = 22f;
    [SerializeField] private int maxTotalEnemies = 250; // GLOBAL CAP (increased for VAT Horde)

    [Header("Goblin Grunt")]
    [SerializeField] private GameObject goblinGruntPrefab;
    [SerializeField] private int goblinGruntPoolSize = 150; // increased
    [SerializeField] private float goblinGruntBaseSpawnRate = 0.8f; // weight for selection

    [Header("Orc Brute")]
    [SerializeField] private GameObject orcBrutePrefab;
    [SerializeField] private int orcBrutePoolSize = 50; // increased
    [SerializeField] private float orcBruteBaseSpawnRate = 0.15f; // weight for selection

    [Header("Goblin Runner")]
    [SerializeField] private GameObject goblinRunnerPrefab;
    [SerializeField] private int goblinRunnerPoolSize = 50; // increased
    [SerializeField] private float goblinRunnerBaseSpawnRate = 0.5f; // weight for selection

    [Header("Stone Golem (Mini-Boss/Boss)")]
    [SerializeField] private GameObject stoneGolemPrefab;
    [SerializeField] private int stoneGolemPoolSize = 5; // slightly increased
    // Removed unused 'stoneGolemBaseSpawnRate' to fix warning CS0414

    [Header("Balance Settings")]
    [SerializeField] private float bal_minSpawnInterval = 0.12f;
    [SerializeField] private float bal_rageMinInterval = 0.05f;
    [SerializeField] private float bal_baseSpawnInterval = 0.85f;
    [SerializeField] private float bal_spawnAcceleration = 0.18f;
    [SerializeField] private float bal_baseMaxEnemies = 28f;
    [SerializeField] private float bal_maxEnemiesGrowth = 6f;
    [SerializeField] private float bal_maxEnemiesRamp = 0.15f;
    [SerializeField] private float bal_difficultyGrowth = 0.08f;
    [SerializeField] private float bal_extremeStartMinutes = 10f;
    [SerializeField] private float bal_extremeIntervalFactor = 0.65f;
    [SerializeField] private float bal_extremeEnemyBonusPerMinute = 8f;
    [SerializeField] private float bal_extremeDifficultyBoost = 0.45f;

    // --- Private fields (object pools, timers, state)
    private Queue<Enemy> goblinGruntPool = new Queue<Enemy>();
    private Queue<Enemy> orcBrutePool = new Queue<Enemy>();
    private Queue<Enemy> goblinRunnerPool = new Queue<Enemy>();
    private Queue<Enemy> stoneGolemPool = new Queue<Enemy>();

    private bool isSpawning = false;
    private bool boss3MinSpawned = false;
    private bool boss7MinSpawned = false;
    private bool boss10MinSpawned = false;

    private void Awake()
    {
        if (player == null)
        {
            Debug.LogWarning("EnemySpawner: Player Transform not assigned.");
        }
        // Pre-instantiate pools
        PrewarmPool(goblinGruntPrefab, goblinGruntPool, goblinGruntPoolSize);
        PrewarmPool(orcBrutePrefab, orcBrutePool, orcBrutePoolSize);
        PrewarmPool(goblinRunnerPrefab, goblinRunnerPool, goblinRunnerPoolSize);
        PrewarmPool(stoneGolemPrefab, stoneGolemPool, stoneGolemPoolSize);
    }

    private void Start()
    {
        // Don't auto-start spawning - let GameManager.StartGame() control this
        // This prevents enemies from spawning during weapon select screen
    }

    private float spawnTimer = 0f;

    private void Update()
    {
        if (!isSpawning || player == null)
            return;

        // Use GameManager.runTime instead of Time.time to ensure proper reset on game restart
        // Time.time doesn't reset on scene reload, causing boss spawn issues
        float minutes = 0f;
        if (GameManager.Instance != null)
        {
            minutes = GameManager.Instance.runTime / 60f;
        }
        else
        {
            // Fallback if GameManager not available (shouldn't happen in normal gameplay)
            minutes = Time.time / 60f;
        }
        // Check game mode for Madness multipliers
        bool isMadnessMode = GameManager.Instance != null && GameManager.Instance.CurrentGameMode == GameMode.Madness;
        float spawnRateMultiplier = isMadnessMode ? 2f : 1f; // 2x spawn rate in Madness
        float difficultyGrowthMultiplier = isMadnessMode ? 1.5f : 1f; // 1.5x difficulty scaling in Madness
        
        float spawnAcceleration = (1f + minutes * bal_spawnAcceleration) * spawnRateMultiplier;
        float difficultyScalar = (1f + minutes * bal_difficultyGrowth) * difficultyGrowthMultiplier;
        
        // maxEnemies ramp with time and acceleration
        int maxEnemies = Mathf.RoundToInt(bal_baseMaxEnemies + (bal_maxEnemiesGrowth * minutes * spawnAcceleration) + minutes * minutes * bal_maxEnemiesRamp);
        
        // Never spawn if currentEnemies >= maxEnemies
        if (GetActiveEnemyCount() >= maxEnemies)
            return;
        
        if (GetActiveEnemyCount() >= maxTotalEnemies) // Hard cap
            return;

        // spawnInterval shrinks aggressively with time
        float spawnInterval = Mathf.Max(bal_minSpawnInterval, bal_baseSpawnInterval / spawnAcceleration);

        // Extreme rage mode past boss kill timer
        if (minutes >= bal_extremeStartMinutes)
        {
            float rageMinutes = minutes - bal_extremeStartMinutes;
            float rageFactor = Mathf.Pow(bal_extremeIntervalFactor, rageMinutes);
            spawnInterval = Mathf.Max(bal_rageMinInterval, spawnInterval * rageFactor);
            maxEnemies += Mathf.RoundToInt(bal_extremeEnemyBonusPerMinute * rageMinutes);
            difficultyScalar *= 1f + rageMinutes * bal_extremeDifficultyBoost;
        }

        spawnTimer += Time.deltaTime;

        while (spawnTimer >= spawnInterval)
        {
            spawnTimer -= spawnInterval;
            SpawnRandomEnemy(difficultyScalar);
        }

        // --- Boss Spawning at 3, 7, and 10 minutes ---
        // Boss at 3 minutes
        if (minutes >= 3f && !boss3MinSpawned)
        {
            SpawnBossStoneGolem(difficultyScalar, false); // Not map unlock boss
            boss3MinSpawned = true;
        }
        
        // Boss at 7 minutes
        if (minutes >= 7f && !boss7MinSpawned)
        {
            SpawnBossStoneGolem(difficultyScalar, false); // Not map unlock boss
            boss7MinSpawned = true;
        }
        
        // Boss at 10 minutes (map unlock boss)
        if (minutes >= 10f && !boss10MinSpawned)
        {
            SpawnBossStoneGolem(difficultyScalar, true); // This is the map unlock boss
            boss10MinSpawned = true;
        }
    }

    private void SpawnRandomEnemy(float difficulty)
    {
        // Randomly choose enemy type based on adjustable relative spawn rates
        float totalWeight = goblinGruntBaseSpawnRate + orcBruteBaseSpawnRate + goblinRunnerBaseSpawnRate;
        if (totalWeight <= 0f)
        {
            totalWeight = 1f; // safety fallback
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = goblinGruntBaseSpawnRate;
        
        if (roll < cumulative)
        {
                TrySpawnFromPool(goblinGruntPool, difficulty);
        }
        else
        {
            cumulative += orcBruteBaseSpawnRate;
            if (roll < cumulative)
            {
                TrySpawnFromPool(orcBrutePool, difficulty);
            }
            else
            {
                TrySpawnFromPool(goblinRunnerPool, difficulty);
            }
        }
    }

    // Helper to spawn and buff the Stone Golem boss
    // isMapUnlockBoss: true only for 10-minute boss (unlocks map)
    private void SpawnBossStoneGolem(float difficulty, bool isMapUnlockBoss)
    {
        Enemy golemEnemy = null;

        // Try get from pool
        int poolCount = stoneGolemPool.Count;
        for (int i = 0; i < poolCount; ++i)
        {
            Enemy enemy = stoneGolemPool.Dequeue();
            if (!enemy.gameObject.activeInHierarchy)
            {
                golemEnemy = enemy;
                stoneGolemPool.Enqueue(enemy);
                break;
            }
            stoneGolemPool.Enqueue(enemy);
        }

        // If none available, instantiate one
        if (golemEnemy == null)
        {
            GameObject obj = Instantiate(stoneGolemPrefab, Vector3.zero, Quaternion.identity, this.transform);
            golemEnemy = obj.GetComponent<Enemy>();
            if (golemEnemy == null)
            {
                Debug.LogError("EnemySpawner: Stone Golem prefab is missing Enemy component! Boss spawn failed.");
                return;
            }
            // Pool the new enemy for further reuse
            stoneGolemPool.Enqueue(golemEnemy);
        }

        // Set spawn position (2 metres above ground)
        Vector3 pos = GetSpawnPositionAroundPlayer();
        pos.y += 2f; // Spawn boss 2 metres above ground
        Transform t = golemEnemy.transform;
        t.position = pos;
        t.rotation = Quaternion.identity;

        // Mark as boss BEFORE ResetEnemy so HP calculation uses boss multipliers
        // ResetEnemy resets boss flags to false, so we set them after
        golemEnemy.ResetEnemy(difficulty * 2.5f, player);
        
        // Now set boss mode (after reset) - this triggers HP recalculation
        golemEnemy.SetBossMode(true);
        
        // Only 10-minute boss unlocks the map
        if (isMapUnlockBoss)
        {
            golemEnemy.SetMapUnlockBoss(true);
        }
        
        // Recalculate boss HP now that boss mode is set
        golemEnemy.RecalculateBossHP(difficulty * 2.5f);

        // Try a quick visual tweak to make the boss obvious:
        Renderer rend = golemEnemy.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            rend.material.color = Color.yellow;
            rend.material.SetColor("_EmissionColor", Color.yellow * 2.0f);
        }
        golemEnemy.transform.localScale = Vector3.one * 3f;

        golemEnemy.gameObject.SetActive(true);

        #if UNITY_EDITOR
        string bossType = isMapUnlockBoss ? "Map Unlock" : "Regular";
        Debug.Log($"EnemySpawner: Spawned {bossType} Boss Stone Golem at {t.position} with difficulty {difficulty * 2.5f}");
        #endif
    }

    // Helper: Get count of all active enemies across all pools (GLOBAL CAP)
    private int GetActiveEnemyCount()
    {
        int count = 0;

        foreach (Enemy enemy in goblinGruntPool)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
                count++;
        }
        foreach (Enemy enemy in orcBrutePool)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
                count++;
        }
        foreach (Enemy enemy in goblinRunnerPool)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
                count++;
        }
        foreach (Enemy enemy in stoneGolemPool)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
                count++;
        }

        return count;
    }

    // Helper: Prewarm object pool
    private void PrewarmPool(GameObject prefab, Queue<Enemy> pool, int amount)
    {
        for (int i = 0; i < amount; ++i)
        {
            GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, this.transform);
            obj.SetActive(false);
            Enemy enemyComponent = obj.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                pool.Enqueue(enemyComponent);
            }
            else
            {
                Debug.LogWarning("EnemySpawner: Enemy prefab is missing an Enemy component!");
            }
        }
    }

    // Helper: Find and return a pooled enemy if available and spawn it in the world
    private bool TrySpawnFromPool(Queue<Enemy> pool, float difficulty)
    {
        int safety = pool.Count;
        for (int i = 0; i < safety; ++i)
        {
            Enemy enemy = pool.Dequeue();
            if (!enemy.gameObject.activeInHierarchy)
            {
                Vector3 pos = GetSpawnPositionAroundPlayer();
                Transform t = enemy.transform;
                t.position = pos;
                t.rotation = Quaternion.identity;

                enemy.ResetEnemy(difficulty, player);

                enemy.gameObject.SetActive(true);

                #if UNITY_EDITOR
                Debug.Log($"EnemySpawner: Activated {enemy.gameObject.name} at {t.position}");
                #endif

                pool.Enqueue(enemy);
                return true;
            }
            pool.Enqueue(enemy);
        }
        // No available enemy, all active
        return false;
    }

    // Compute random spawn position around player in ring and set Y using MapGenerator or ground height
    // Ensures position is within playable map bounds
    private Vector3 GetSpawnPositionAroundPlayer()
    {
        if (player == null) return Vector3.zero;
        
        const int maxAttempts = 10;
        Vector3 pos = Vector3.zero;
        bool validPosition = false;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
            float radius = UnityEngine.Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            pos = player.position + offset;

            // Check if position is within playable bounds
            if (MapGenerator.Instance != null)
            {
                if (MapGenerator.Instance.IsWithinPlayableBounds(pos))
                {
                    validPosition = true;
                    break;
                }
            }
            else
            {
                // No MapGenerator, assume position is valid
                validPosition = true;
                break;
            }
        }
        
        // If no valid position found after attempts, clamp to playable bounds
        if (!validPosition && MapGenerator.Instance != null)
        {
            pos = MapGenerator.Instance.ClampToPlayableBounds(pos);
        }

        // Snap to ground height using MapGenerator if available, else fallback to Raycast
        float newY = pos.y;
        bool snapped = false;

        // Attempt MapGenerator first
        if (MapGenerator.Instance != null)
        {
            newY = MapGenerator.Instance.GetHeight(pos);
            snapped = true;
        }
        else
        {
            // Fallback to raycast
            Ray ray = new Ray(new Vector3(pos.x, 100f, pos.z), Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200f))
            {
                newY = hit.point.y;
                snapped = true;
            }
        }

        // Set Y to ground height (spawn on ground)
        if (snapped)
        {
            pos.y = newY;
        }
        else
        {
            // Could not snap, use player Y as fallback
            pos.y = player.position.y;
        }

        return pos;
    }

    // --- Public Controls ---
    public void StartSpawning()
    {
        isSpawning = true;
    }
    public void StopSpawning()
    {
        isSpawning = false;
    }
    public void ResetSpawner()
    {
        StopSpawning();
        boss3MinSpawned = false;
        boss7MinSpawned = false;
        boss10MinSpawned = false;
        ResetAllEnemies();
    }

    /// <summary>
    /// Spawns a random non-boss enemy at the specified position. Used by Tombstone and other systems.
    /// Validates that the position is within playable bounds before spawning.
    /// </summary>
    /// <param name="spawnPosition">World position to spawn the enemy at.</param>
    /// <param name="difficulty">Difficulty multiplier for enemy stats.</param>
    /// <returns>True if enemy was spawned successfully, false otherwise.</returns>
    public bool SpawnRandomNonBossEnemyAtPosition(Vector3 spawnPosition, float difficulty)
    {
        if (player == null)
        {
            Debug.LogWarning("EnemySpawner: Cannot spawn enemy - player transform is null!");
            return false;
        }

        // Validate and clamp position to playable bounds
        if (MapGenerator.Instance != null)
        {
            if (!MapGenerator.Instance.IsWithinPlayableBounds(spawnPosition))
            {
                spawnPosition = MapGenerator.Instance.ClampToPlayableBounds(spawnPosition);
            }
            // Also snap Y to ground height
            spawnPosition.y = MapGenerator.Instance.GetHeight(spawnPosition);
        }

        // Randomly choose non-boss enemy type based on spawn rates
        float totalWeight = goblinGruntBaseSpawnRate + orcBruteBaseSpawnRate + goblinRunnerBaseSpawnRate;
        if (totalWeight <= 0f)
        {
            totalWeight = 1f; // safety fallback
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = goblinGruntBaseSpawnRate;
        
        bool spawned = false;
        if (roll < cumulative)
        {
            spawned = TrySpawnFromPoolAtPosition(goblinGruntPool, spawnPosition, difficulty);
        }
        else
        {
            cumulative += orcBruteBaseSpawnRate;
            if (roll < cumulative)
            {
                spawned = TrySpawnFromPoolAtPosition(orcBrutePool, spawnPosition, difficulty);
            }
            else
            {
                spawned = TrySpawnFromPoolAtPosition(goblinRunnerPool, spawnPosition, difficulty);
            }
        }

        return spawned;
    }

    /// <summary>
    /// Helper method to spawn an enemy from a pool at a specific position.
    /// </summary>
    private bool TrySpawnFromPoolAtPosition(Queue<Enemy> pool, Vector3 position, float difficulty)
    {
        int safety = pool.Count;
        for (int i = 0; i < safety; ++i)
        {
            Enemy enemy = pool.Dequeue();
            if (!enemy.gameObject.activeInHierarchy)
            {
                Transform t = enemy.transform;
                t.position = position;
                t.rotation = Quaternion.identity;

                enemy.ResetEnemy(difficulty, player);

                enemy.gameObject.SetActive(true);

                pool.Enqueue(enemy);
                return true;
            }
            pool.Enqueue(enemy);
        }
        return false;
    }

    // Deactivate all enemies in all pools
    private void ResetAllEnemies()
    {
        ResetPool(goblinGruntPool);
        ResetPool(orcBrutePool);
        ResetPool(goblinRunnerPool);
        ResetPool(stoneGolemPool);
    }

    private void ResetPool(Queue<Enemy> pool)
    {
        foreach (Enemy enemy in pool)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
                enemy.gameObject.SetActive(false);
        }
    }
}
