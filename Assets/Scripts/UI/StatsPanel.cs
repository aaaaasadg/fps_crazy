using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Simple stats panel that displays all player stats and weapon base stats.
/// Assign text fields in the inspector for each stat line.
/// </summary>
public class StatsPanel : MonoBehaviour
{
    [Header("Weapon Images (0=Sniper, 1=Shotgun, 2=AR, 3=Grenade Launcher)")]
    [SerializeField] private Image weaponImageDisplay;
    [SerializeField] private Sprite[] weaponImages = new Sprite[4];

    [Header("Player Stat Text Fields")]
    [SerializeField] private TextMeshProUGUI statText_MaxHP;
    [SerializeField] private TextMeshProUGUI statText_HPRegen;
    [SerializeField] private TextMeshProUGUI statText_MoveSpeed;
    [SerializeField] private TextMeshProUGUI statText_Damage;
    [SerializeField] private TextMeshProUGUI statText_FireRate;
    [SerializeField] private TextMeshProUGUI statText_ReloadSpeed;
    [SerializeField] private TextMeshProUGUI statText_ProjectileCount;
    [SerializeField] private TextMeshProUGUI statText_ProjectilePierce;
    [SerializeField] private TextMeshProUGUI statText_RicochetBounces;
    [SerializeField] private TextMeshProUGUI statText_Knockback;
    [SerializeField] private TextMeshProUGUI statText_AoERadius;
    [SerializeField] private TextMeshProUGUI statText_XPGain;
    [SerializeField] private TextMeshProUGUI statText_GoldGain;
    [SerializeField] private TextMeshProUGUI statText_DamageReduction;
    [SerializeField] private TextMeshProUGUI statText_Luck;
    [SerializeField] private TextMeshProUGUI statText_PickupRange;
    [SerializeField] private TextMeshProUGUI statText_CritChance;
    [SerializeField] private TextMeshProUGUI statText_CritDamage;
    [SerializeField] private TextMeshProUGUI statText_MagazineSize;
    [SerializeField] private TextMeshProUGUI statText_ProjectileSpeed;

    [Header("Base Weapon Stat Text Fields")]
    [SerializeField] private TextMeshProUGUI baseText_Damage;
    [SerializeField] private TextMeshProUGUI baseText_FireRate;
    [SerializeField] private TextMeshProUGUI baseText_MagazineSize;
    [SerializeField] private TextMeshProUGUI baseText_ProjectileSpeed;
    [SerializeField] private TextMeshProUGUI baseText_ReloadTime;
    [SerializeField] private TextMeshProUGUI baseText_Knockback;
    [SerializeField] private TextMeshProUGUI baseText_AoERadius;
    [SerializeField] private TextMeshProUGUI baseText_ProjectileCount;
    [SerializeField] private TextMeshProUGUI baseText_Pierce;
    [SerializeField] private TextMeshProUGUI baseText_Ricochet;
    [SerializeField] private TextMeshProUGUI baseText_CritChance;
    [SerializeField] private TextMeshProUGUI baseText_CritDamage;

    private PlayerStats playerStats;
    private WeaponStats weaponStats;
    
    // Fallback English names if localization isn't ready
    private static readonly Dictionary<string, string> fallbackNames = new Dictionary<string, string>
    {
        // Player stats
        { "Stats_MaxHP", "Max HP" },
        { "Stats_HPRegen", "HP Regen" },
        { "Stats_MoveSpeed", "Move Speed" },
        { "Stats_Damage", "Damage" },
        { "Stats_FireRate", "Fire Rate" },
        { "Stats_ReloadSpeed", "Reload Speed" },
        { "Stats_Projectiles", "Projectiles" },
        { "Stats_Pierce", "Pierce" },
        { "Stats_Ricochet", "Ricochet" },
        { "Stats_Knockback", "Knockback" },
        { "Stats_AoERadius", "AoE Radius" },
        { "Stats_XPGain", "XP Gain" },
        { "Stats_GoldGain", "Gold Gain" },
        { "Stats_DamageReduction", "Damage Reduction" },
        { "Stats_Luck", "Luck" },
        { "Stats_PickupRange", "Pickup Range" },
        { "Stats_CritChance", "Crit Chance" },
        { "Stats_CritDamage", "Crit Damage" },
        { "Stats_MagazineSize", "Magazine Size" },
        { "Stats_ProjectileSpeed", "Projectile Speed" },
        { "Stats_Faster", "faster" },
        { "Stats_Taken", "taken" },
        // Base weapon stats
        { "Base_Damage", "Base Damage" },
        { "Base_FireRate", "Base Fire Rate" },
        { "Base_MagazineSize", "Base Magazine Size" },
        { "Base_ProjectileSpeed", "Base Projectile Speed" },
        { "Base_ReloadTime", "Base Reload Time" },
        { "Base_Knockback", "Base Knockback" },
        { "Base_AoERadius", "Base AoE Radius" },
        { "Base_ProjectileCount", "Base Projectile Count" },
        { "Base_Pierce", "Base Pierce" },
        { "Base_Ricochet", "Base Ricochet" },
        { "Base_CritChance", "Base Crit Chance" },
        { "Base_CritDamage", "Base Crit Damage" }
    };

    private void OnEnable()
    {
        // Subscribe to language change events
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from language change events
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    private void OnLanguageChanged()
    {
        // Refresh stats when language changes (if panel is visible)
        if (gameObject.activeInHierarchy)
        {
            RefreshStats();
        }
    }

    /// <summary>
    /// Refreshes all stat displays with current values from PlayerStats and WeaponStats.
    /// Call this when showing the panel.
    /// </summary>
    public void RefreshStats()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        // Find current weapon stats
        if (weaponStats == null)
        {
            weaponStats = FindFirstObjectByType<WeaponStats>();
        }

        // Update weapon image based on selected weapon index
        UpdateWeaponImage();

        // Update base weapon stats
        RefreshBaseWeaponStats();

        if (playerStats == null) return;

        // MaxHP - show total HP value
        SetStatText(statText_MaxHP, GetLocalized("Stats_MaxHP"), playerStats.MaxHP.ToString("F0"));

        // HPRegen - flat value per second
        SetStatText(statText_HPRegen, GetLocalized("Stats_HPRegen"), playerStats.GetStatBonus(StatType.HPRegen).ToString("F1") + "/s");

        // MoveSpeed - percentage
        SetStatText(statText_MoveSpeed, GetLocalized("Stats_MoveSpeed"), FormatPercent(playerStats.GetStatBonus(StatType.MoveSpeed)));

        // Damage - percentage
        SetStatText(statText_Damage, GetLocalized("Stats_Damage"), FormatPercent(playerStats.GetStatBonus(StatType.Damage)));

        // FireRate - percentage
        SetStatText(statText_FireRate, GetLocalized("Stats_FireRate"), FormatPercent(playerStats.GetStatBonus(StatType.FireRate)));

        // ReloadSpeed - percentage (negative means faster reload)
        float reloadBonus = playerStats.GetStatBonus(StatType.ReloadSpeed);
        string fasterText = GetLocalized("Stats_Faster");
        string reloadStr = reloadBonus <= 0 ? 
            (Mathf.Abs(reloadBonus) * 100f).ToString("F0") + "% " + fasterText : 
            FormatPercent(reloadBonus);
        SetStatText(statText_ReloadSpeed, GetLocalized("Stats_ReloadSpeed"), reloadStr);

        // ProjectileCount - flat
        SetStatText(statText_ProjectileCount, GetLocalized("Stats_Projectiles"), "+" + playerStats.GetStatBonus(StatType.ProjectileCount).ToString("F0"));

        // ProjectilePierce - flat
        SetStatText(statText_ProjectilePierce, GetLocalized("Stats_Pierce"), "+" + playerStats.GetStatBonus(StatType.ProjectilePierce).ToString("F0"));

        // RicochetBounces - flat
        SetStatText(statText_RicochetBounces, GetLocalized("Stats_Ricochet"), "+" + playerStats.GetStatBonus(StatType.RicochetBounces).ToString("F0"));

        // Knockback - percentage
        SetStatText(statText_Knockback, GetLocalized("Stats_Knockback"), FormatPercent(playerStats.GetStatBonus(StatType.Knockback)));

        // AoERadius - percentage
        SetStatText(statText_AoERadius, GetLocalized("Stats_AoERadius"), FormatPercent(playerStats.GetStatBonus(StatType.AoERadius)));

        // XPGain - percentage
        SetStatText(statText_XPGain, GetLocalized("Stats_XPGain"), FormatPercent(playerStats.GetStatBonus(StatType.XPGain)));

        // GoldGain - percentage
        SetStatText(statText_GoldGain, GetLocalized("Stats_GoldGain"), FormatPercent(playerStats.GetStatBonus(StatType.GoldGain)));

        // DamageReduction - show as damage taken percentage
        float dr = playerStats.GetStatBonus(StatType.DamageReduction);
        string takenText = GetLocalized("Stats_Taken");
        string drStr = ((1f - dr) * 100f).ToString("F0") + "% " + takenText;
        SetStatText(statText_DamageReduction, GetLocalized("Stats_DamageReduction"), drStr);

        // Luck - percentage
        SetStatText(statText_Luck, GetLocalized("Stats_Luck"), FormatPercent(playerStats.GetStatBonus(StatType.Luck)));

        // PickupRange - percentage
        SetStatText(statText_PickupRange, GetLocalized("Stats_PickupRange"), FormatPercent(playerStats.GetStatBonus(StatType.PickupRange)));

        // CritChance - additive percentage
        float critChance = playerStats.GetStatBonus(StatType.CritChance);
        SetStatText(statText_CritChance, GetLocalized("Stats_CritChance"), (critChance * 100f).ToString("F0") + "%");

        // CritDamage - percentage
        SetStatText(statText_CritDamage, GetLocalized("Stats_CritDamage"), FormatPercent(playerStats.GetStatBonus(StatType.CritDamage)));

        // MagazineSize - percentage
        SetStatText(statText_MagazineSize, GetLocalized("Stats_MagazineSize"), FormatPercent(playerStats.GetStatBonus(StatType.MagazineSize)));

        // ProjectileSpeed - percentage
        SetStatText(statText_ProjectileSpeed, GetLocalized("Stats_ProjectileSpeed"), FormatPercent(playerStats.GetStatBonus(StatType.ProjectileSpeed)));
    }

    private void UpdateWeaponImage()
    {
        if (weaponImageDisplay == null || weaponImages == null) return;

        int weaponIndex = 0;
        if (GameManager.Instance != null)
        {
            weaponIndex = GameManager.Instance.SelectedWeaponIndex;
        }

        // Clamp to valid range
        weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponImages.Length - 1);

        if (weaponImages.Length > weaponIndex && weaponImages[weaponIndex] != null)
        {
            weaponImageDisplay.sprite = weaponImages[weaponIndex];
            weaponImageDisplay.enabled = true;
        }
        else
        {
            weaponImageDisplay.enabled = false;
        }
    }

    private void RefreshBaseWeaponStats()
    {
        if (weaponStats == null)
        {
            weaponStats = FindFirstObjectByType<WeaponStats>();
        }

        if (weaponStats == null) return;

        // Base Damage
        SetStatText(baseText_Damage, GetLocalized("Base_Damage"), weaponStats.GetBaseDamage().ToString("F0"));

        // Base Fire Rate (shots per second = 1 / cooldown)
        float baseFireRate = weaponStats.GetBaseFireRate();
        string fireRateStr = baseFireRate > 0 ? (1f / baseFireRate).ToString("F1") + "/s" : "0/s";
        SetStatText(baseText_FireRate, GetLocalized("Base_FireRate"), fireRateStr);

        // Base Magazine Size
        SetStatText(baseText_MagazineSize, GetLocalized("Base_MagazineSize"), weaponStats.GetBaseMagSize().ToString());

        // Base Projectile Speed
        SetStatText(baseText_ProjectileSpeed, GetLocalized("Base_ProjectileSpeed"), weaponStats.GetBaseProjectileSpeed().ToString("F0"));

        // Base Reload Time
        SetStatText(baseText_ReloadTime, GetLocalized("Base_ReloadTime"), weaponStats.GetBaseReloadSpeed().ToString("F1") + "s");

        // Base Knockback
        SetStatText(baseText_Knockback, GetLocalized("Base_Knockback"), weaponStats.GetBaseKnockback().ToString("F1"));

        // Base AoE Radius
        SetStatText(baseText_AoERadius, GetLocalized("Base_AoERadius"), weaponStats.GetBaseAoERadius().ToString("F1"));

        // Base Projectile Count
        SetStatText(baseText_ProjectileCount, GetLocalized("Base_ProjectileCount"), weaponStats.GetBaseProjectileCount().ToString());

        // Base Pierce
        SetStatText(baseText_Pierce, GetLocalized("Base_Pierce"), weaponStats.GetBasePierce().ToString());

        // Base Ricochet
        SetStatText(baseText_Ricochet, GetLocalized("Base_Ricochet"), weaponStats.GetBaseRicochetBounces().ToString());

        // Base Crit Chance
        SetStatText(baseText_CritChance, GetLocalized("Base_CritChance"), (weaponStats.GetBaseCritChance() * 100f).ToString("F0") + "%");

        // Base Crit Damage
        SetStatText(baseText_CritDamage, GetLocalized("Base_CritDamage"), (weaponStats.GetBaseCritDamage() * 100f).ToString("F0") + "%");
    }

    private string GetLocalized(string key)
    {
        if (LocalizationManager.Instance != null)
        {
            string result = LocalizationManager.Instance.GetLocalizedString(key);
            // If localization returns the key itself, use fallback
            if (result == key && fallbackNames.ContainsKey(key))
            {
                return fallbackNames[key];
            }
            return result;
        }
        
        // Fallback if LocalizationManager not available
        if (fallbackNames.ContainsKey(key))
        {
            return fallbackNames[key];
        }
        return key;
    }

    private void SetStatText(TextMeshProUGUI textField, string statName, string value)
    {
        if (textField != null)
        {
            textField.text = $"{statName}: {value}";
        }
    }

    private string FormatPercent(float bonus)
    {
        float percent = (1f + bonus) * 100f;
        return percent.ToString("F0") + "%";
    }
}

