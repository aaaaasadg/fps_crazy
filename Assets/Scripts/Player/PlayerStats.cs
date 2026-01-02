using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float baseMaxHP = 100f;
    [SerializeField] private float baseMoveSpeed = 6f;
    // [SerializeField] private float basePickupRadius = 3f; // Unused field - commented out to fix CS0414 warning

    [Header("Balance Settings")]
    [SerializeField] private float bal_baseXPLevel1 = 60f;
    [SerializeField] private float bal_xpLevelGrowth = 1.15f;

    private Dictionary<StatType, float> statModifiers;
    
    // Temporary buff system (for rage, etc.)
    private Dictionary<StatType, float> temporaryBuffs; // Stores the bonus amount
    private Dictionary<StatType, float> temporaryBuffTimers; // Stores time remaining
    
    // Magnet state (makes all XP orbs fly to player)
    private float magnetTimer = 0f;

    private float currentHP;
    private float maxHP;
    private float currentXP;
    private float requiredXP;
    private int currentLevel = 1;
    private float currentGold;

    // Removed redundant local souls variable

    // --- Properties for UI access ---
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public int CurrentLevel => currentLevel;
    public float CurrentXP => currentXP;
    public float RequiredXP => requiredXP;
    public float CurrentGold => currentGold;

    private HUDManager hudManager;

    private void Awake()
    {
        // Find HUDManager in the scene
        hudManager = UnityEngine.Object.FindFirstObjectByType<HUDManager>();

        statModifiers = new Dictionary<StatType, float>();
        temporaryBuffs = new Dictionary<StatType, float>();
        temporaryBuffTimers = new Dictionary<StatType, float>();
        
        foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
        {
            statModifiers[type] = 0f;
            temporaryBuffs[type] = 0f;
            temporaryBuffTimers[type] = 0f;
        }

        maxHP = GetCurrentStat(StatType.MaxHP, baseMaxHP);
        // Ensure currentHP is set after maxHP calculation
        currentHP = maxHP;
        requiredXP = CalculateRequiredXP(currentLevel);
        currentXP = 0f;
        currentGold = 0f;
    }

    private void Start()
    {
        // Initialize HUD with current values
        if (hudManager != null)
        {
            hudManager.UpdateXP(currentXP, requiredXP, currentLevel);
            hudManager.UpdateHP(currentHP, maxHP);
            hudManager.UpdateGold(currentGold);
        }
        // Souls UI will be set by HUDManager using SaveManager in its own initialization logic
    }

    // AddSouls is correct: delegates to SaveManager.
    public void AddSouls(int amount)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AddSouls(amount);
        }
    }

    /// <summary>
    /// Returns the current accumulated bonus for the specified stat (raw float value),
    /// including meta-progression bonuses from SaveManager and temporary buffs.
    /// </summary>
    public float GetStatBonus(StatType type)
    {
        float metaBonus = 0f;
        if (SaveManager.Instance != null)
        {
            int level = SaveManager.Instance.GetStatLevel(type);
            metaBonus = StatMetaProgression.GetMetaBonus(type, level);
        }

        float permanentBonus = 0f;
        if (statModifiers != null && statModifiers.ContainsKey(type))
            permanentBonus = statModifiers[type];
        
        float tempBonus = 0f;
        if (temporaryBuffs != null && temporaryBuffs.ContainsKey(type) && temporaryBuffTimers[type] > 0f)
            tempBonus = temporaryBuffs[type];

        return permanentBonus + metaBonus + tempBonus;
    }

    /// <summary>
    /// Returns the actual movement speed stat value, combining baseMoveSpeed with modifiers.
    /// Fully robust to live/inspector base value changes.
    /// </summary>
    public float GetMovementSpeed()
    {
        // Use StatType.MoveSpeed strictly per enum
        return baseMoveSpeed * (1f + GetStatBonus(StatType.MoveSpeed));
    }

    /// <summary>
    /// Returns the actual stat value, combining baseValue with modifiers.
    /// MaxHP and Luck are additive, all others are multiplicative.
    /// Fully robust to live/inspector base value changes.
    /// </summary>
    public float GetCurrentStat(StatType type, float baseValue)
    {
        if (statModifiers != null && statModifiers.ContainsKey(type))
        {
            // Additive for MaxHP and Luck
            if (type == StatType.MaxHP || type == StatType.Luck)
            {
                return baseValue + GetStatBonus(type);
            }
            // Multiplicative for everything else
            return baseValue * (1f + GetStatBonus(type));
        }
        return baseValue;
    }

    // Reload speed cap: -0.20 means max 20% reload time reduction
    private const float RELOAD_SPEED_CAP = -0.20f;
    // Crit chance cap: 1.0 means 100%
    private const float CRIT_CHANCE_CAP = 1.0f;
    
    /// <summary>
    /// Applies an upgrade bonus to the stat dictionary.
    /// Also handles HP scaling if MaxHP is raised.
    /// </summary>
    public void ApplyUpgrade(StatType type, float value)
    {
        float prevMaxHP = maxHP;

        if (!statModifiers.ContainsKey(type))
            statModifiers[type] = 0f;

        statModifiers[type] += value;
        
        // Cap ReloadSpeed at -20%
        if (type == StatType.ReloadSpeed && statModifiers[type] < RELOAD_SPEED_CAP)
        {
            statModifiers[type] = RELOAD_SPEED_CAP;
        }
        
        // Cap CritChance at 100%
        if (type == StatType.CritChance && statModifiers[type] > CRIT_CHANCE_CAP)
        {
            statModifiers[type] = CRIT_CHANCE_CAP;
        }

        // Handle MaxHP change: increase MaxHP and raise HP proportionally
        if (type == StatType.MaxHP)
        {
            maxHP = GetCurrentStat(StatType.MaxHP, baseMaxHP);
            if (prevMaxHP > 0f)
            {
                float ratio = currentHP / prevMaxHP;
                currentHP = maxHP * ratio;
            }
            else
            {
                currentHP = maxHP;
            }
            if (hudManager != null)
            {
                hudManager.UpdateHP(currentHP, maxHP);
            }
        }

        float total = GetStatBonus(type); // This is the raw bonus
        Debug.Log($"[UPGRADE APPLIED] Stat: {type} | Added: {value} | New Total Bonus: {total}");
    }

    /// <summary>
    /// Applies a milestone upgrade and notifies the HUD.
    /// </summary>
    private void ApplyMilestone(string message, StatType type, float value)
    {
        ApplyUpgrade(type, value);
        if (hudManager != null)
            hudManager.ShowMilestone(message);
    }

    private void Update()
    {
        // HP Regen (per second)
        float regen = GetStatBonus(StatType.HPRegen);
        if (regen > 0f && currentHP < maxHP)
        {
            Heal(regen * Time.deltaTime);
        }
        
        // Update temporary buff timers and expire finished ones
        if (temporaryBuffTimers != null)
        {
            List<StatType> expiredBuffs = new List<StatType>();
            foreach (StatType type in temporaryBuffTimers.Keys)
            {
                if (temporaryBuffTimers[type] > 0f)
                {
                    temporaryBuffTimers[type] -= Time.deltaTime;
                    if (temporaryBuffTimers[type] <= 0f)
                    {
                        expiredBuffs.Add(type);
                    }
                }
            }
            
            // Remove expired buffs
            foreach (StatType type in expiredBuffs)
            {
                temporaryBuffs[type] = 0f;
                temporaryBuffTimers[type] = 0f;
            }
        }
        
        // Update magnet timer
        if (magnetTimer > 0f)
        {
            magnetTimer -= Time.deltaTime;
            if (magnetTimer < 0f)
                magnetTimer = 0f;
        }
    }
    
    /// <summary>
    /// Applies a temporary buff that adds to the stat bonus for a duration.
    /// If a buff already exists for this stat, it will be refreshed (timer reset).
    /// </summary>
    public void ApplyTemporaryBuff(StatType type, float bonusAmount, float duration)
    {
        if (temporaryBuffs == null || temporaryBuffTimers == null) return;
        
        temporaryBuffs[type] = bonusAmount;
        temporaryBuffTimers[type] = duration;
    }
    
    /// <summary>
    /// Activates magnet mode for a duration. All XP orbs will fly to the player.
    /// </summary>
    public void ActivateMagnet(float duration)
    {
        magnetTimer = duration;
    }
    
    /// <summary>
    /// Returns true if magnet mode is currently active.
    /// </summary>
    public bool IsMagnetActive()
    {
        return magnetTimer > 0f;
    }

    /// <summary>
    /// Adds XP, factoring in the XPGain stat, and checks for level up.
    /// Calls GameManager.Instance.TriggerLevelUp() on level up.
    /// </summary>
    public void AddXP(float amount)
    {
        float xpMultiplier = 1f + GetStatBonus(StatType.XPGain);
        float totalXP = amount * xpMultiplier;
        currentXP += totalXP;

        // Tutorial Hook
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnXPGained();
        }

        if (currentXP >= requiredXP)
        {
            currentLevel++;
            ApplyClassPassives();
            CheckClassMilestones(currentLevel);

            currentXP -= requiredXP;
            requiredXP *= 1.2f;

            if (hudManager != null)
            {
                hudManager.UpdateXP(currentXP, requiredXP, currentLevel);
            }

            // THE IMPORTANT LINE:
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerLevelUp();
        }
        else
        {
            if (hudManager != null)
            {
                hudManager.UpdateXP(currentXP, requiredXP, currentLevel);
            }
        }

        // --- SOUND: XP Pickup ---
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayXPPickup();
        }
    }

    /// <summary>
    /// Increase gold, factoring in GoldGain stat. Only applies multiplier when gaining gold (amount > 0).
    /// </summary>
    public void AddGold(float amount)
    {
        // Apply Gold Gain multiplier ONLY when gaining gold, not when spending
        if (amount > 0f)
        {
            float multiplier = 1f + GetStatBonus(StatType.GoldGain);
            currentGold += amount * multiplier;
        }
        else
        {
            // Directly subtract when spending (no multiplier)
            currentGold += amount;
        }

        // Clamp gold to prevent negative values
        if (currentGold < 0f)
            currentGold = 0f;

        if (hudManager != null)
        {
            hudManager.UpdateGold(currentGold);
        }

        // --- SOUND: Gold Pickup ---
        // Only play if gaining gold (amount > 0), not spending
        if (amount > 0 && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayGoldPickup();
        }
    }

    /// <summary>
    /// Handles incoming damage, factoring in DamageReduction. Returns true if player dies.
    /// Ensures HP is clamped and updates HUD after taking damage. Triggers GameOver if dead.
    /// Accepts an optional attacker Transform to notify HUDManager.
    /// </summary>
    public bool TakeDamage(float amount, Transform attacker = null)
    {
        float reduction = Mathf.Clamp(GetStatBonus(StatType.DamageReduction), 0f, 0.8f);
        float finalAmount = amount * (1f - reduction);
        currentHP -= finalAmount;
        // Clamp HP to 0 so it never goes negative
        if (currentHP < 0f) currentHP = 0f;

        // Always call UpdateHP immediately after taking damage
        if (hudManager != null)
        {
            hudManager.UpdateHP(currentHP, maxHP);
        }

        // --- Show damage indicator pointing to attacker ---
        if (hudManager != null)
        {
            hudManager.ShowDamageIndicator(attacker);
        }
        // --------------------------------------------------

        // --- SOUND: Player Take Damage ---
        if (amount > 0 && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPlayerTakeDamage();
        }

        // If HP hits 0, call GameOver
        if (currentHP <= 0f)
        {
            // --- SOUND: Death ---
            if (SoundManager.Instance != null) SoundManager.Instance.PlayDeath();

            // Player is dead. Properly handle death using GameManager.Instance.GameOver()
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }

            // Optionally disable player movement and shooting scripts
            PlayerController playerController = GetComponent<PlayerController>();
            if (playerController != null)
                playerController.enabled = false;

            Weapon weapon = GetComponentInChildren<Weapon>();
            if (weapon != null)
                weapon.enabled = false;

            return true;
        }
        return false;
    }

    /// <summary>
    /// Heals player up to max HP.
    /// </summary>
    public void Heal(float amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);

        if (hudManager != null)
        {
            hudManager.UpdateHP(currentHP, maxHP);
        }
    }

    /// <summary>
    /// Reduces HP by a percentage (0-1). Ensures player cannot die from this (minimum 1 HP).
    /// Returns the actual damage dealt.
    /// </summary>
    public float ReduceHPByPercentage(float percentage)
    {
        float damageAmount = currentHP * Mathf.Clamp01(percentage);
        float minHP = 1f;
        float actualDamage = Mathf.Min(damageAmount, currentHP - minHP);
        
        currentHP = Mathf.Max(currentHP - actualDamage, minHP);

        if (hudManager != null)
        {
            hudManager.UpdateHP(currentHP, maxHP);
        }

        return actualDamage;
    }

    /// <summary>
    /// Forces an instant level up, bypassing XP requirements. Triggers level up screen.
    /// </summary>
    public void ForceLevelUp()
    {
        currentLevel++;
        ApplyClassPassives();
        CheckClassMilestones(currentLevel);

        // Reset XP to 0 for new level
        currentXP = 0f;
        requiredXP = CalculateRequiredXP(currentLevel);

        if (hudManager != null)
        {
            hudManager.UpdateXP(currentXP, requiredXP, currentLevel);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerLevelUp();
        }
    }

    private float CalculateRequiredXP(int level)
    {
        // 3) XP SYSTEM - Level Requirements
        return bal_baseXPLevel1 * Mathf.Pow(bal_xpLevelGrowth, level - 1);
    }

    private void ApplyClassPassives()
    {
        if (GameManager.Instance == null) return;

        int classIdx = GameManager.Instance.SelectedClassIndex;

        switch (classIdx)
        {
            case 0: // Slayer (+2% Damage)
                ApplyUpgrade(StatType.Damage, 0.02f);
                break;
            case 1: // Sentinel (+0.5% DR, cap 80%)
                if (GetStatBonus(StatType.DamageReduction) < 0.8f)
                    ApplyUpgrade(StatType.DamageReduction, 0.005f);
                break;
            case 2: // Reaver (+2.5% Fire Rate)
                ApplyUpgrade(StatType.FireRate, 0.025f);
                break;
            case 3: // Harvester (+3% XP Gain)
                ApplyUpgrade(StatType.XPGain, 0.03f);
                break;
        }
    }

    public void InitializeClassStats()
    {
        ApplyClassPassives();
    }

    /// <summary>
    /// Creates a localized milestone message in the format "Lvl X: StatName +Value"
    /// </summary>
    private string GetLocalizedMilestoneMessage(int level, string statKey, string valueText)
    {
        string lvlPrefix = "Lvl";
        string statName = statKey;
        
        if (LocalizationManager.Instance != null)
        {
            lvlPrefix = LocalizationManager.Instance.GetLocalizedString("Lvl");
            statName = LocalizationManager.Instance.GetLocalizedString(statKey);
        }
        
        // Remove "Milestone_" prefix if present
        if (statName.StartsWith("Milestone_"))
        {
            statName = statName.Substring("Milestone_".Length);
        }
        
        return $"{lvlPrefix} {level}: {statName} {valueText}";
    }

    private void CheckClassMilestones(int level)
    {
        if (GameManager.Instance == null) return;
        int classIdx = GameManager.Instance.SelectedClassIndex;

        switch (classIdx)
        {
            case 0: // Slayer
                if (level == 5) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_CritChance", "+10%"), StatType.CritChance, 0.10f);
                if (level == 15) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_Damage", "+30%"), StatType.Damage, 0.30f);
                if (level == 30) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_CritDamage", "+60%"), StatType.CritDamage, 0.60f);
                if (level == 50) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_Damage", "+100%"), StatType.Damage, 1.00f);
                if (level == 100) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_Damage", "+300%"), StatType.Damage, 3.00f);
                break;

            case 1: // Sentinel
                if (level == 5) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_MaxHP", "+50"), StatType.MaxHP, 50f);
                if (level == 15) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_DamageReduction", "+10%"), StatType.DamageReduction, 0.10f);
                if (level == 30) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_HPRegen", "+2/s"), StatType.HPRegen, 2f);
                if (level == 50) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_MaxHP", "+200"), StatType.MaxHP, 200f);
                if (level == 100) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_DamageReduction", "+30%"), StatType.DamageReduction, 0.30f);
                break;

            case 2: // Reaver
                if (level == 5) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_Ricochet", "+2"), StatType.RicochetBounces, 2f);
                if (level == 15) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_Projectile", "+1"), StatType.ProjectileCount, 1f);
                if (level == 30) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_FireRate", "+40%"), StatType.FireRate, 0.40f);
                if (level == 50) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_Projectile", "+2"), StatType.ProjectileCount, 2f);
                if (level == 100) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_FireRate", "+100%"), StatType.FireRate, 1.00f);
                break;

            case 3: // Harvester
                if (level == 5) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_PickupRange", "+60%"), StatType.PickupRange, 0.6f);
                if (level == 15) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_GoldGain", "+20%"), StatType.GoldGain, 0.20f);
                if (level == 30) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_XPGain", "+50%"), StatType.XPGain, 0.50f);
                if (level == 50) ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_GoldGain", "+50%"), StatType.GoldGain, 0.50f);
                if (level == 100)
                {
                    ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_XPGain", "+200%"), StatType.XPGain, 2.00f);
                    ApplyMilestone(GetLocalizedMilestoneMessage(level, "Milestone_GoldGain", "+100%"), StatType.GoldGain, 1.00f);
                }
                break;
        }
    }
}
