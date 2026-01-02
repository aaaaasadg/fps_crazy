using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource; // Shared source for one-shot SFX (fallback)
    private AudioSource musicSource;
    private AudioSource loopingSource; // For looping sounds like pylon charging
    private AudioSource prioritySource; // Dedicated source for high-priority sounds (level up, item drop) - never interrupted
    
    [Header("Pitch Variation")]
    [SerializeField] private float pitchVariationRange = 0.15f; // ±0.15 pitch variation (subtle)
    [SerializeField] private int audioSourcePoolSize = 5; // Pool size for overlapping sounds with pitch
    
    [Header("Sound Throttling")]
    [SerializeField] private float enemyHitThrottleInterval = 0.05f; // Min time between enemy hit sounds (prevents AOE spam)
    
    private List<AudioSource> audioSourcePool; // Pool for overlapping sounds with pitch variation
    private int currentPoolIndex = 0;
    private float lastEnemyHitTime; // Throttle enemy hit sounds

    [Header("Volume Settings")]
    private float sfxVolumeMultiplier = 1f;
    private float musicVolumeMultiplier = 1f;

    [Header("Enemy & Combat")]
    [SerializeField] private AudioClip enemyHitClip;
    [SerializeField] [Range(0f, 1f)] private float enemyHitVolume = 1f;
    [SerializeField] private AudioClip shootClip;
    [SerializeField] [Range(0f, 1f)] private float shootVolume = 1f;
    [SerializeField] private AudioClip reloadClip;
    [SerializeField] [Range(0f, 1f)] private float reloadVolume = 1f;
    [SerializeField] private AudioClip critHitClip;
    [SerializeField] [Range(0f, 2f)] private float critHitVolume = 1.2f;

    [Header("Player")]
    [SerializeField] private AudioClip walkClip;
    [SerializeField] [Range(0f, 1f)] private float walkVolume = 0.5f;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] [Range(0f, 1f)] private float jumpVolume = 1f;
    [SerializeField] private AudioClip landClip;
    [SerializeField] [Range(0f, 1f)] private float landVolume = 1f;
    [SerializeField] private AudioClip playerTakeDamageClip;
    [SerializeField] [Range(0f, 1f)] private float playerTakeDamageVolume = 1f;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] [Range(0f, 1f)] private float deathVolume = 1f;

    [Header("Items & Progression")]
    [SerializeField] private AudioClip itemDropClip;
    [SerializeField] [Range(0f, 1f)] private float itemDropVolume = 1f;
    [SerializeField] private AudioClip itemPickupClip;
    [SerializeField] [Range(0f, 1f)] private float itemPickupVolume = 1f;
    [SerializeField] private AudioClip goldPickupClip;
    [SerializeField] [Range(0f, 1f)] private float goldPickupVolume = 1f;
    [SerializeField] private AudioClip xpPickupClip;
    [SerializeField] [Range(0f, 1f)] private float xpPickupVolume = 1f;
    [SerializeField] private AudioClip upgradeDropClip;
    [SerializeField] [Range(0f, 1f)] private float upgradeDropVolume = 1f;
    [SerializeField] private AudioClip upgradeChoiceClip; // Used for both "upgrade choice" requests
    [SerializeField] [Range(0f, 1f)] private float upgradeChoiceVolume = 1f;
    [SerializeField] private AudioClip pylonChargingClip; // Looping sound for pylon charging
    [SerializeField] [Range(0f, 1f)] private float pylonChargingVolume = 1f;
    [SerializeField] private AudioClip tombstoneInteractClip; // Sound for interacting with tombstone
    [SerializeField] [Range(0f, 1f)] private float tombstoneInteractVolume = 1f;

    [Header("UI")]
    [SerializeField] private AudioClip pauseOpenClip;
    [SerializeField] [Range(0f, 1f)] private float pauseOpenVolume = 1f;
    [SerializeField] private AudioClip pauseCloseClip;
    [SerializeField] [Range(0f, 1f)] private float pauseCloseVolume = 1f;
    [SerializeField] private AudioClip buttonHoverClip;
    [SerializeField] [Range(0f, 1f)] private float buttonHoverVolume = 1f;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] [Range(0f, 1f)] private float buttonClickVolume = 1f;
    [SerializeField] private AudioClip shopBuyClip;
    [SerializeField] [Range(0f, 1f)] private float shopBuyVolume = 1f;
    [SerializeField] private AudioClip shopOpenClip;
    [SerializeField] [Range(0f, 1f)] private float shopOpenVolume = 1f;

    [Header("Music Settings")]
    [SerializeField] private AudioClip menuMusicClip;
    [SerializeField] [Range(0f, 1f)] private float menuMusicVolume = 0.5f;
    [SerializeField] private AudioClip gameplayMusicClip;
    [Space(10)]
    [Tooltip("Volume for the main body of the track (between first and last 10s)")]
    [SerializeField] [Range(0f, 1f)] private float musicBodyVolume = 0.5f;
    [Tooltip("Volume for the first and last 10 seconds of the track")]
    [SerializeField] [Range(0f, 1f)] private float musicEdgeVolume = 0.5f;
    
    [Header("Mobile Settings")]
    [Tooltip("If enabled, background music will be disabled on mobile devices to prevent notification player issues")]
    [SerializeField] private bool disableMusicOnMobile = true;
    
    // Cache mobile detection
    private bool isMobileDevice;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return; // Important return to prevent executing the rest of Awake on the duplicate
        }

        // Detect if we're on a mobile device (WebGL on mobile)
        isMobileDevice = IsMobile();
        
        if (isMobileDevice && disableMusicOnMobile)
        {
            Debug.Log("[SoundManager] Mobile device detected - Background music will be disabled");
        }

        // Ensure AudioSource exists if not assigned
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Create dedicated AudioSource for music
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        // Create dedicated AudioSource for looping sounds
        loopingSource = gameObject.AddComponent<AudioSource>();
        loopingSource.loop = true;
        loopingSource.playOnAwake = false;
        
        // Create dedicated AudioSource for high-priority sounds (level up, item drop)
        // This source is never interrupted by regular sounds
        prioritySource = gameObject.AddComponent<AudioSource>();
        prioritySource.playOnAwake = false;
        
        // Create pool of AudioSources for overlapping sounds with pitch variation
        audioSourcePool = new List<AudioSource>();
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            audioSourcePool.Add(source);
        }

        // Load saved volume settings
        sfxVolumeMultiplier = PlayerPrefs.GetFloat("SFXVolume", 1f);
        musicVolumeMultiplier = PlayerPrefs.GetFloat("MusicVolume", 1f);
        
        // Apply music volume immediately
        if (musicSource != null)
        {
            musicSource.volume = musicSource.volume * musicVolumeMultiplier;
        }
    }

    public void PlayEnemyHit()
    {
        // Throttle enemy hit sounds to prevent audio spam from AOE damage
        if (Time.time >= lastEnemyHitTime + enemyHitThrottleInterval)
        {
            PlaySFX(enemyHitClip, enemyHitVolume);
            lastEnemyHitTime = Time.time;
        }
    }
    public void PlayShoot() => PlaySFX(shootClip, shootVolume);
    public void PlayReload() => PlaySFX(reloadClip, reloadVolume);
    public void PlayCritHit() => PlaySFX(critHitClip, critHitVolume);

    public void PlayWalk()
    {
        PlaySFX(walkClip, walkVolume);
    }
    public void PlayJump() => PlaySFX(jumpClip, jumpVolume);
    public void PlayLand() => PlaySFX(landClip, landVolume);
    public void PlayPlayerTakeDamage() => PlaySFX(playerTakeDamageClip, playerTakeDamageVolume);
    public void PlayDeath() => PlaySFX(deathClip, deathVolume);

    public void PlayItemDrop() => PlayPrioritySFX(itemDropClip, itemDropVolume); // High priority - always plays
    public void PlayItemPickup() => PlaySFX(itemPickupClip, itemPickupVolume);
    public void PlayGoldPickup() => PlaySFX(goldPickupClip, goldPickupVolume);
    public void PlayXPPickup() => PlaySFX(xpPickupClip, xpPickupVolume);
    public void PlayUpgradeDrop() => PlayPrioritySFX(upgradeDropClip, upgradeDropVolume); // High priority - always plays
    public void PlayUpgradeChoice() => PlayPrioritySFX(upgradeChoiceClip, upgradeChoiceVolume); // High priority - always plays (level up)

    public void PlayPauseOpen() => PlaySFX(pauseOpenClip, pauseOpenVolume);
    public void PlayPauseClose() => PlaySFX(pauseCloseClip, pauseCloseVolume);
    public void PlayButtonHover() => PlaySFX(buttonHoverClip, buttonHoverVolume);
    public void PlayButtonClick() => PlaySFX(buttonClickClip, buttonClickVolume);
    public void PlayShopBuy() => PlaySFX(shopBuyClip, shopBuyVolume);
    public void PlayShopOpen() => PlaySFX(shopOpenClip, shopOpenVolume);

    private void Update()
    {
        if (musicSource != null && musicSource.isPlaying && musicSource.clip != null)
        {
            // Only apply dynamic volume logic to gameplay music
            if (musicSource.clip == gameplayMusicClip)
            {
                float t = musicSource.time;
                float length = musicSource.clip.length;
                
                // Determine target volume based on time (first 10s or last 10s vs middle)
                float targetVolume = musicBodyVolume;
                
                // Check edge cases (start or end of track)
                // Note: If track is shorter than 20s, this logic prioritizes edge volume
                if (t < 10f || t > (length - 10f))
                {
                    targetVolume = musicEdgeVolume;
                }
                
                // Calculate transition speed to ensure the change happens over roughly 2 seconds
                float volDiff = Mathf.Abs(musicBodyVolume - musicEdgeVolume);
                // Avoid divide by zero or tiny speeds; default to 0.5f if diff is negligible
                float transitionSpeed = volDiff > 0.01f ? volDiff / 2f : 0.5f;

                // Apply volume (using simple MoveTowards with calculated speed) with music volume multiplier
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, targetVolume * musicVolumeMultiplier, transitionSpeed * Time.unscaledDeltaTime);
            }
        }
    }

    public void PlayMenuMusic()
    {
        // Skip music on mobile devices if disabled
        if (isMobileDevice && disableMusicOnMobile)
        {
            Debug.Log("[SoundManager] Skipping menu music on mobile device");
            return;
        }
        
        PlayMusic(menuMusicClip, menuMusicVolume);
    }

    public void PlayGameplayMusic()
    {
        // Skip music on mobile devices if disabled
        if (isMobileDevice && disableMusicOnMobile)
        {
            Debug.Log("[SoundManager] Skipping gameplay music on mobile device");
            return;
        }
        
        PlayMusic(gameplayMusicClip, musicEdgeVolume); // Start with edge volume
    }

    private void PlayMusic(AudioClip clip, float initialVolume)
    {
        if (musicSource == null || clip == null) return;

        // Don't restart if already playing the same clip
        if (musicSource.isPlaying && musicSource.clip == clip) return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = initialVolume * musicVolumeMultiplier; 
        musicSource.loop = true;
        musicSource.Play();
    }
    
    /// <summary>
    /// Detects if the game is running on a mobile device (WebGL on mobile)
    /// </summary>
    private bool IsMobile()
    {
        // Check Unity's built-in mobile detection
        if (Application.isMobilePlatform)
        {
            return true;
        }
        
#if UNITY_WEBGL && !UNITY_EDITOR
        // For WebGL, check user agent via JavaScript
        try
        {
            string userAgent = Application.platform.ToString().ToLower();
            // Unity WebGL reports as WebGLPlayer, so we need to check the actual platform
            // We'll use a simple heuristic: if we're on WebGL, assume mobile if touch is supported
            return Input.touchSupported;
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }

    public void PlayPylonCharging()
    {
        if (pylonChargingClip != null && loopingSource != null)
        {
            if (!loopingSource.isPlaying || loopingSource.clip != pylonChargingClip)
            {
                loopingSource.clip = pylonChargingClip;
                loopingSource.volume = pylonChargingVolume * sfxVolumeMultiplier;
                // Apply subtle pitch variation to looping sounds too
                float pitchVariation = UnityEngine.Random.Range(-pitchVariationRange, pitchVariationRange);
                loopingSource.pitch = 1f + pitchVariation;
                loopingSource.Play();
            }
        }
    }

    public void StopPylonCharging()
    {
        if (loopingSource != null && loopingSource.isPlaying)
        {
            loopingSource.Stop();
        }
    }
    
    /// <summary>
    /// Stops all looping sounds (pylon charging, etc.). Call this on game over/restart.
    /// </summary>
    public void StopAllLoopingSounds()
    {
        if (loopingSource != null)
        {
            loopingSource.Stop();
        }
    }

    public void PlayTombstoneInteract() => PlaySFX(tombstoneInteractClip, tombstoneInteractVolume);

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        
        // Get an available AudioSource from pool (round-robin)
        AudioSource sourceToUse = null;
        
        // Try to find an available source (not playing)
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            int index = (currentPoolIndex + i) % audioSourcePool.Count;
            AudioSource source = audioSourcePool[index];
            if (source != null && !source.isPlaying)
            {
                sourceToUse = source;
                currentPoolIndex = (index + 1) % audioSourcePool.Count;
                break;
            }
        }
        
        // If all sources are busy, use the first one (will interrupt)
        if (sourceToUse == null && audioSourcePool.Count > 0)
        {
            sourceToUse = audioSourcePool[currentPoolIndex];
            currentPoolIndex = (currentPoolIndex + 1) % audioSourcePool.Count;
        }
        
        // Fallback to main sfxSource if pool failed
        if (sourceToUse == null)
        {
            sourceToUse = sfxSource;
        }
        
        if (sourceToUse != null)
        {
            // Apply subtle random pitch variation
            float pitchVariation = UnityEngine.Random.Range(-pitchVariationRange, pitchVariationRange);
            sourceToUse.pitch = 1f + pitchVariation;
            sourceToUse.volume = volume * sfxVolumeMultiplier;
            sourceToUse.clip = clip;
            sourceToUse.Play();
        }
    }

    /// <summary>
    /// Plays a high-priority sound effect that will ALWAYS play regardless of other sounds.
    /// Used for level up and item drop sounds that must never be missed.
    /// </summary>
    private void PlayPrioritySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        
        // Priority source always plays - interrupt any current priority sound if needed
        if (prioritySource != null)
        {
            // Apply subtle random pitch variation
            float pitchVariation = UnityEngine.Random.Range(-pitchVariationRange, pitchVariationRange);
            prioritySource.pitch = 1f + pitchVariation;
            prioritySource.volume = volume * sfxVolumeMultiplier;
            prioritySource.clip = clip;
            prioritySource.Play();
        }
        else
        {
            // Fallback to regular SFX if priority source somehow doesn't exist
            PlaySFX(clip, volume);
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolumeMultiplier = Mathf.Max(0f, volume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeMultiplier);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolumeMultiplier = Mathf.Max(0f, volume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeMultiplier);
        PlayerPrefs.Save();
        
        if (musicSource != null && musicSource.isPlaying)
        {
            // Recalculate current volume with new multiplier
            float baseVolume = musicSource.clip == gameplayMusicClip ? 
                (musicSource.time < 10f || musicSource.time > (musicSource.clip.length - 10f) ? musicEdgeVolume : musicBodyVolume) :
                menuMusicVolume;
            musicSource.volume = baseVolume * musicVolumeMultiplier;
        }
    }

    public float GetSFXVolume()
    {
        return sfxVolumeMultiplier;
    }

    public float GetMusicVolume()
    {
        return musicVolumeMultiplier;
    }
}
