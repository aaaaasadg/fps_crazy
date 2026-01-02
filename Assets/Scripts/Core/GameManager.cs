using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Static field to pass game mode from MapSelectScreen to Game scene
    public static GameMode selectedGameMode { get; set; } = GameMode.Normal;

    [Header("Game Settings")]
    [field: SerializeField] public bool isGameActive { get; private set; } = false;
    [field: SerializeField] public float runTime { get; private set; } = 0f;
    public GameMode CurrentGameMode { get; private set; } = GameMode.Normal;

    [Header("References")]
    [FormerlySerializedAs("playerTransform")]
    [FormerlySerializedAs("<playerTransform>k__BackingField")]
    [SerializeField] private Transform playerTransformReference;
    public Transform playerTransform
    {
        get => playerTransformReference;
        private set => playerTransformReference = value;
    }
    // public UIManager uiManager; // Uncomment when UIManager is created

    [FormerlySerializedAs("hudManager")]
    [FormerlySerializedAs("<hudManager>k__BackingField")]
    [SerializeField] private HUDManager hudManagerReference; // <-- Added HUDManager reference
    public HUDManager hudManager
    {
        get => hudManagerReference;
        private set => hudManagerReference = value;
    }

    [Header("Screens")]
    [FormerlySerializedAs("levelUpScreen")]
    [FormerlySerializedAs("<levelUpScreen>k__BackingField")]
    [SerializeField] private LevelUpScreen levelUpScreenReference;
    public LevelUpScreen levelUpScreen
    {
        get => levelUpScreenReference;
        private set => levelUpScreenReference = value;
    }

    [FormerlySerializedAs("weaponSelectScreen")]
    [FormerlySerializedAs("<weaponSelectScreen>k__BackingField")]
    [SerializeField] private WeaponSelectScreen weaponSelectScreenReference;
    public WeaponSelectScreen weaponSelectScreen
    {
        get => weaponSelectScreenReference;
        private set => weaponSelectScreenReference = value;
    }

    [SerializeField] private DeathScreenUI deathScreenUI; // Assign Death Screen UI in Inspector
    // public PauseMenu pauseMenu;
    [SerializeField] private GameObject pauseMenuObject; // Assign PauseMenu GameObject in Inspector
    [Header("Screens")]
    [FormerlySerializedAs("settingsPanel")]
    [FormerlySerializedAs("<settingsPanel>k__BackingField")]
    [SerializeField] private GameObject settingsPanelReference; // Assign Settings Panel GameObject in Inspector
    public GameObject settingsPanel
    {
        get => settingsPanelReference;
        private set => settingsPanelReference = value;
    }

    [Header("Player Weapons")]
    [SerializeField] private GameObject[] playerWeapons; // Assign 4 player weapon GameObjects in Inspector

    [Header("Enemy Spawner")]
    [SerializeField] private EnemySpawner enemySpawner;

    private int selectedWeaponIndex = 0;
    public int SelectedWeaponIndex => selectedWeaponIndex;
    public int SelectedClassIndex { get; private set; } = 0;

    public int ChestsOpenedCount { get; private set; }

    public int KillCount { get; private set; }

    // --- Run stats tracking for death screen ---
    private int soulsAtRunStart = 0;
    private float goldAtRunStart = 0f;

    // --- DPS tracking fields ---
    private float damageInInterval = 0f;
    private float dpsIntervalTimer = 0f;

    // --- Pause system fields ---
    private bool isPaused = false;

    // --- Weapon Camera reference for hiding/showing ---
    private Camera weaponCamera;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Defer FindObjectOfType calls to avoid blocking (lazy initialization)
        // These will be found when needed, not during Awake
    }

    private const string TUTORIAL_KEY = "TutorialCompleted";
    private const string HAS_PLAYED_BEFORE_KEY = "HasPlayedBefore";

    private void Start()
    {
        // --- Begin mandatory start state ---
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // --- End mandatory start state ---

        // Signal loading stop when game scene is ready
        if (CrazyGamesHandler.Instance != null)
        {
            CrazyGamesHandler.Instance.LoadingStop();
        }

        // Set current game mode from static field (set by MapSelectScreen)
        CurrentGameMode = selectedGameMode;

        isPaused = false;

        ResetRun(); // Ensure ChestsOpenedCount reset on every fresh start

        // Check if this is the first time playing (tutorial not completed)
        bool isFirstTime = PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 0;

        if (isFirstTime)
        {
            // First time ever: skip weapon select, auto-select default weapon (index 0), and start game directly
            // Tutorial will automatically start via TutorialManager
            selectedWeaponIndex = 0;
            SelectedClassIndex = 0;
            
            // Hide weapon select screen if it exists
            if (weaponSelectScreen != null)
            {
                weaponSelectScreen.gameObject.SetActive(false);
            }
            
            // Start game immediately
            StartGame();
        }
        else if (weaponSelectScreen != null && weaponSelectScreen.gameObject.activeInHierarchy)
        {
            // Not first time: show weapon select screen as normal
            isGameActive = false;
            Time.timeScale = 1f; // Keep time scale at 1 for UI animations to work
            // Make sure weapon select screen is actually visible
            weaponSelectScreen.gameObject.SetActive(true);
            // Ensure cursor is free
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Play Menu Music during weapon selection
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayMenuMusic();
            }
        }
        else
        {
            // No weapon select screen, start game directly
            StartGame();
        }

        // Hide PauseMenu UI if it's assigned and enabled
        if (pauseMenuObject != null)
        {
            pauseMenuObject.SetActive(false);
        }
        // Hide Settings Panel at start if assigned
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Don't allow pause if death screen is showing
        bool isDeathScreenShowing = deathScreenUI != null && deathScreenUI.IsShowing();
        
        // Always check P at the very start (but not if death screen is showing)
        if (Input.GetKeyDown(KeyCode.P) && !isDeathScreenShowing)
        {
            TogglePause();
        }

        // Add: if game active, not paused, no UI showing, and cursor not locked, clicking should lock the cursor
        bool isAnyUIDisplaying = isPaused
            || (weaponSelectScreen != null && weaponSelectScreen.gameObject.activeInHierarchy)
            || (levelUpScreen != null && levelUpScreen.gameObject.activeInHierarchy)
            || (settingsPanel != null && settingsPanel.activeInHierarchy)
            || (deathScreenUI != null && deathScreenUI.IsShowing());

        if (isGameActive && !isPaused && !isAnyUIDisplaying && Cursor.lockState != CursorLockMode.Locked)
        {
            // Mouse button pressed: left(0), right(1), middle(2) - lock on any click
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (isGameActive)
        {
            runTime += Time.deltaTime;
            
            // Update Timer
            if (hudManager != null)
                hudManager.UpdateTimer(runTime);

            // DPS calculation and update
            dpsIntervalTimer += Time.deltaTime;
            if (dpsIntervalTimer > 0.5f)
            {
                if (hudManager != null)
                {
                    float dpsValue = (dpsIntervalTimer > 0f) ? (damageInInterval / dpsIntervalTimer) : 0f;
                    hudManager.UpdateDPS(dpsValue);
                }
                damageInInterval = 0f;
                dpsIntervalTimer = 0f;
            }
        }

        // Video-style cursor control: handled every frame
        // Check if any UI screen that requires cursor is active
        bool isUIDisplaying = isPaused
            || (weaponSelectScreen != null && weaponSelectScreen.gameObject.activeInHierarchy)
            || (levelUpScreen != null && levelUpScreen.IsPanelActive())
            || (settingsPanel != null && settingsPanel.activeInHierarchy)
            || (deathScreenUI != null && deathScreenUI.IsShowing());

        if (isUIDisplaying || !isGameActive)
        {
            // UI is showing or game is inactive - unlock cursor and hide crosshair
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Hide crosshair when any UI is showing
            if (hudManager != null)
            {
                hudManager.HideCrosshair();
            }
        }
        else if (isGameActive && !isPaused)
        {
            // Game is active and no UI - lock cursor and show crosshair
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Show crosshair during gameplay
            if (hudManager != null)
            {
                hudManager.ShowCrosshair();
            }
        }

        // Update weapon visibility based on UI state
        UpdateWeaponVisibility();
    }

    /// <summary>
    /// Opens the settings panel and hides the pause menu.
    /// </summary>
    public void OpenSettings()
    {
        Debug.Log("GameManager: OpenSettings called.");
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            Debug.Log("GameManager: Settings Panel activated.");
        }
        else
        {
            Debug.LogError("GameManager: Settings Panel reference is MISSING in Inspector!");
        }

        if (pauseMenuObject != null)
        {
            pauseMenuObject.SetActive(false);
        }
    }

    /// <summary>
    /// Closes the settings panel and shows the pause menu.
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        if (pauseMenuObject != null)
        {
            pauseMenuObject.SetActive(true);
        }
    }

    /// <summary>
    /// Toggles the pause state of the game.
    /// </summary>
    public void TogglePause()
    {
        SetPaused(!isPaused);
    }

    /// <summary>
    /// Returns true if any UI screen is currently displaying (pause, level up, settings, etc.)
    /// </summary>
    public bool IsAnyUIDisplaying()
    {
        return isPaused
            || (weaponSelectScreen != null && weaponSelectScreen.gameObject.activeInHierarchy)
            || (levelUpScreen != null && levelUpScreen.IsPanelActive())
            || (settingsPanel != null && settingsPanel.activeInHierarchy)
            || (deathScreenUI != null && deathScreenUI.IsShowing())
            || !isGameActive;
    }

    /// <summary>
    /// Sets the pause state explicitly.
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (isPaused == paused) return;

        isPaused = paused;

        // --- SOUND: Pause Open/Close ---
        if (SoundManager.Instance != null)
        {
            if (isPaused) SoundManager.Instance.PlayPauseOpen();
            else SoundManager.Instance.PlayPauseClose();
        }

        if (isPaused)
        {
            isGameActive = false;
            Time.timeScale = 0f;
            if (pauseMenuObject != null) pauseMenuObject.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            
            // --- CrazyGames SDK: Signal gameplay stop when paused ---
            if (CrazyGamesHandler.Instance != null)
            {
                CrazyGamesHandler.Instance.GameplayStop();
            }
            
            // Show stats panel when paused
            if (hudManager != null)
            {
                hudManager.ShowStatsPanel();
            }

            // Hide weapon when paused
            SetWeaponVisible(false);
        }
        else
        {
            if (pauseMenuObject != null) pauseMenuObject.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            
            // Hide stats panel when unpaused
            if (hudManager != null)
            {
                hudManager.HideStatsPanel();
            }

            // Check if other menus are active before resuming fully
            bool inMenu = (weaponSelectScreen != null && weaponSelectScreen.gameObject.activeInHierarchy) ||
                          (levelUpScreen != null && levelUpScreen.gameObject.activeInHierarchy);

            if (!inMenu)
            {
                isGameActive = true;
                Time.timeScale = 1f;
                
                // --- CrazyGames SDK: Signal gameplay start when resuming ---
                if (CrazyGamesHandler.Instance != null)
                {
                    CrazyGamesHandler.Instance.GameplayStart();
                }
                
                // Show weapon when game resumes and no menus are active
                SetWeaponVisible(true);
            }
        }
        // Cursor logic is handled by Update()
    }

    /// <summary>
    /// Register damage dealt for DPS calculation.
    /// </summary>
    public void RegisterDamage(float amount)
    {
        damageInInterval += amount;
    }

    /// <summary>
    /// Register an enemy kill and update the HUD.
    /// </summary>
    public void RegisterKill()
    {
        KillCount++;
        if (hudManager != null)
        {
            hudManager.UpdateKills(KillCount);
        }
    }

    /// <summary>
    /// Set the player's starting weapon based on button/index selection.
    /// Call this from WeaponSelectScreen before starting the game.
    /// </summary>
    public void SetStartingWeapon(int index)
    {
        selectedWeaponIndex = index;
    }

    public void SetClass(int index)
    {
        SelectedClassIndex = index;
    }

    public void StartGame()
    {
        isGameActive = true;
        runTime = 0f;
        Time.timeScale = 1f;
        isPaused = false;

        // Reset run stats and track starting values
        ResetRun();

        // Hide pause menu if for some reason it is active
        if (pauseMenuObject != null)
        {
            pauseMenuObject.SetActive(false);
        }
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        // Hide death screen if it's somehow active
        if (deathScreenUI != null)
        {
            deathScreenUI.HideDeathScreen();
        }

        // Do not start any cursor coroutine. Cursor logic handled in Update().

        Debug.Log("Game Started!");

        // --- PlayerStats class bonus application ---
        PlayerStats playerStats = null;
        if (playerTransform != null)
        {
            playerStats = playerTransform.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.InitializeClassStats();
            }
            else
            {
                Debug.LogWarning("GameManager: Could not find PlayerStats on playerTransform to initialize class stats!");
            }
        }
        else
        {
            Debug.LogWarning("GameManager: playerTransform is not assigned!");
        }

        // --- Weapon selection/activation logic with Debug.Log if null/not null ---
        if (playerWeapons != null && playerWeapons.Length > 0)
        {
            Debug.Log($"Activating weapon index: {selectedWeaponIndex}");
            for (int i = 0; i < playerWeapons.Length; i++)
            {
                if (playerWeapons[i] == null)
                {
                    Debug.LogWarning($"playerWeapons[{i}] is NULL");
                }
                else
                {
                    Debug.Log($"playerWeapons[{i}] is assigned: {playerWeapons[i].name}");
                }

                bool isActive = (i == selectedWeaponIndex);
                if (playerWeapons[i] != null)
                {
                    playerWeapons[i].SetActive(isActive);
                    if (isActive)
                    {
                        SetWeaponLayer(playerWeapons[i]);

                        WeaponStats weaponStats = playerWeapons[i].GetComponent<WeaponStats>();
                        if (weaponStats != null)
                        {
                            // intentionally left blank
                        }
                        else
                        {
                            Debug.LogWarning($"Weapon GameObject at index {i} is missing a WeaponStats component!");
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("GameManager: playerWeapons array not assigned or empty!");
        }

        // --- Reposition the player after weapons are set up ---
        if (playerTransform != null && MapGenerator.Instance != null)
        {
            float y = MapGenerator.Instance.GetHeight(Vector3.zero) + 2f;
            playerTransform.position = new Vector3(0, y, 0);
        }
        else
        {
            Debug.LogWarning("GameManager: Cannot reposition player; playerTransform or MapGenerator.Instance is null!");
        }

        // Start Spawning
        if (enemySpawner != null)
        {
            enemySpawner.StartSpawning();
        }
        else
        {
            if (enemySpawner == null) enemySpawner = FindFirstObjectByType<EnemySpawner>();
            if (enemySpawner != null) enemySpawner.StartSpawning();
        }
        
        // Show crosshair when game starts
        if (hudManager != null)
        {
            hudManager.ShowCrosshair();
        }

        // Play Gameplay Music
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayGameplayMusic();
        }

        // Show weapon when game starts
        SetWeaponVisible(true);

        // --- CrazyGames SDK: Signal gameplay start ---
        if (CrazyGamesHandler.Instance != null)
        {
            CrazyGamesHandler.Instance.GameplayStart();
        }
    }

    public void GameOver()
    {
        isGameActive = false;
        Time.timeScale = 0f; // Freeze the game
        
        // --- CrazyGames SDK: Signal gameplay stop ---
        if (CrazyGamesHandler.Instance != null)
        {
            CrazyGamesHandler.Instance.GameplayStop();
        }
        
        // Stop enemy spawning
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }
        
        // Stop all looping sounds (pylon charging, etc.)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAllLoopingSounds();
        }
        
        // Show cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Game Over!");

        // Calculate stats for death screen
        int level = 1;
        float goldEarned = 0f;
        int soulsEarned = 0;

        if (playerTransform != null)
        {
            PlayerStats playerStats = playerTransform.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                level = playerStats.CurrentLevel;
                goldEarned = playerStats.CurrentGold - goldAtRunStart;
            }
        }

        if (SaveManager.Instance != null && SaveManager.Instance.data != null)
        {
            soulsEarned = SaveManager.Instance.data.souls - soulsAtRunStart;
        }

        // Show death screen (lazy-find if not assigned, but don't block)
        if (deathScreenUI == null)
        {
            // Try to find, but don't block if it fails
            try
            {
                deathScreenUI = FindFirstObjectByType<DeathScreenUI>();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameManager] Failed to find DeathScreenUI: {e.Message}");
            }
        }

        if (deathScreenUI != null)
        {
            deathScreenUI.ShowDeathScreen(level, KillCount, soulsEarned, goldEarned, runTime);
            Debug.Log($"GameManager: Death screen shown. Level: {level}, Kills: {KillCount}, Souls: {soulsEarned}, Gold: {goldEarned}, Time: {runTime}");
        }
        else
        {
            Debug.LogError("GameManager: DeathScreenUI is not assigned and could not be found in scene! Death screen will not show. Make sure DeathScreenUI component exists in the scene.");
        }

        // Hide weapon when death screen shows
        SetWeaponVisible(false);
    }

    public void RestartGame()
    {
        // Hide death screen
        if (deathScreenUI != null)
        {
            deathScreenUI.HideDeathScreen();
        }

        // Show interstitial ad before restarting (after gameplay session)
        if (CrazyGamesHandler.Instance != null && CrazyGamesHandler.Instance.IsInitialized)
        {
            CrazyGamesHandler.Instance.RequestAd("midgame", 
                onStarted: () => {
                    // Game is paused by Handler
                },
                onFinished: () => {
                    Time.timeScale = 1f;
                    isPaused = false;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                },
                onError: (error) => {
                    Debug.LogWarning($"[GameManager] Interstitial ad error: {error}, restarting anyway");
                    Time.timeScale = 1f;
                    isPaused = false;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
            );
        }
        else
        {
            // SDK not available, restart directly
            Time.timeScale = 1f;
            isPaused = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    /// <summary>
    /// Reset all run-specific data; call on new run.
    /// </summary>
    public void ResetRun()
    {
        ChestsOpenedCount = 0;
        KillCount = 0;
        
        // Reset shared chest price for new run
        ChestPickup.ResetSharedPrice();
        
        // Track starting stats for death screen
        if (SaveManager.Instance != null && SaveManager.Instance.data != null)
        {
            soulsAtRunStart = SaveManager.Instance.data.souls;
        }
        
        if (playerTransform != null)
        {
            PlayerStats playerStats = playerTransform.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                goldAtRunStart = playerStats.CurrentGold;
            }
        }
        
        if (hudManager != null)
        {
            hudManager.UpdateKills(0);
        }
    }

    public Transform GetPlayer()
    {
        return playerTransform;
    }

    /// <summary>
    /// Call this when the player levels up to pause gameplay and show the upgrade selection screen.
    /// </summary>
    public void TriggerLevelUp()
    {
        isGameActive = false;
        Time.timeScale = 0f;
        
        // --- CrazyGames SDK: Call happytime on level up (exciting moment) ---
        if (CrazyGamesHandler.Instance != null)
        {
            CrazyGamesHandler.Instance.HappyTime();
            CrazyGamesHandler.Instance.GameplayStop();
        }
        
        // Tutorial Hook
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnLevelUp();
        }

        // No cursor logic here, handled in Update()

        Debug.Log("Triggering Level Up Screen...");

        if (levelUpScreen == null)
        {
            // Try to find, but don't block
            try
            {
                levelUpScreen = FindFirstObjectByType<LevelUpScreen>();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameManager] Failed to find LevelUpScreen: {e.Message}");
            }
            
            if (levelUpScreen == null)
            {
                Debug.LogWarning("LevelUpScreen reference not assigned in GameManager and could not be found in the scene!");
                return;
            }
        }

        levelUpScreen.Show();

        // Hide weapon when level up screen shows
        SetWeaponVisible(false);

        // --- SOUND: Upgrade Drop (Level Up) ---
        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeDrop();
    }

    /// <summary>
    /// Call this after the player has selected an upgrade to resume gameplay.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuObject != null) pauseMenuObject.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        bool inMenu = (weaponSelectScreen != null && weaponSelectScreen.gameObject.activeInHierarchy) ||
                      (levelUpScreen != null && levelUpScreen.IsPanelActive());

        if (!inMenu)
        {
            isGameActive = true;
            Time.timeScale = 1f;
        }
        
        // Update weapon visibility based on current UI state
        UpdateWeaponVisibility();
        
        // Cursor logic removed; handled by Update()
    }

    // Removed LockCursorAfterFrame coroutine entirely.

    /// <summary>
    /// Call this when a chest is picked up and chest reward should be shown.
    /// </summary>
    /// <param name="guaranteeLegendary">If true, the chest will always give a legendary item (boss chests).</param>
    public void TriggerChestReward(bool guaranteeLegendary = false)
    {
        ChestsOpenedCount++;

        isGameActive = false;
        Time.timeScale = 0f;

        // No cursor logic here, handled in Update()

        Debug.Log("Triggering Chest Reward Screen...");

        if (levelUpScreen == null)
        {
            // Try to find, but don't block
            try
            {
                levelUpScreen = FindFirstObjectByType<LevelUpScreen>();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameManager] Failed to find LevelUpScreen: {e.Message}");
            }
            
            if (levelUpScreen == null)
            {
                Debug.LogWarning("LevelUpScreen reference not assigned in GameManager and could not be found in the scene!");
                return;
            }
        }

        levelUpScreen.ShowChestReward(guaranteeLegendary);

        // Hide weapon when chest reward screen shows
        SetWeaponVisible(false);
    }

    private void SetWeaponLayer(GameObject weaponObj)
    {
        int weaponLayer = LayerMask.NameToLayer("Weapon");
        if (weaponLayer != -1)
        {
            SetLayerRecursively(weaponObj, weaponLayer);
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Shows or hides the weapon camera based on visibility state.
    /// </summary>
    public void SetWeaponVisible(bool visible)
    {
        // Find weapon camera if not cached (lazy, non-blocking)
        if (weaponCamera == null)
        {
            try
            {
                GameObject weaponCamObj = GameObject.FindGameObjectWithTag("WeaponCamera");
                if (weaponCamObj != null)
                {
                    weaponCamera = weaponCamObj.GetComponent<Camera>();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameManager] Failed to find WeaponCamera: {e.Message}");
            }
        }

        // Show/hide weapon camera
        if (weaponCamera != null)
        {
            weaponCamera.enabled = visible;
        }
    }

    /// <summary>
    /// Updates weapon visibility based on current UI state.
    /// Weapon should be hidden when: paused, level up screen, settings panel, death screen, or weapon select screen is active.
    /// Weapon should be visible when: game is active and no UI is showing.
    /// </summary>
    private void UpdateWeaponVisibility()
    {
        // Check if any UI that should hide weapon is active
        bool shouldHideWeapon = isPaused
            || (weaponSelectScreen != null && weaponSelectScreen.gameObject.activeInHierarchy)
            || (levelUpScreen != null && levelUpScreen.IsPanelActive())
            || (settingsPanel != null && settingsPanel.activeInHierarchy)
            || (deathScreenUI != null && deathScreenUI.IsShowing());

        // Show weapon if game is active and no UI is hiding it
        bool shouldShowWeapon = isGameActive && !shouldHideWeapon;

        SetWeaponVisible(shouldShowWeapon);
    }
}
