using UnityEngine;

public class WeaponStats : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] private WeaponDefinition definition;

    // All stat backing fields (private, single source of truth)
    private float damage = 10f;
    private float fireRate = 1f;
    private float reloadSpeed = 2f;
    private int magSize = 8;
    private int projectileCount = 1;
    private float knockback = 0f;
    private float aoeRadius = 0f;
    private float projectileSpeed = 20f;
    private int pierceCount = 0;
    private int ricochetBounces = 0;
    private float critChance = 0f;
    private float critDamage = 1.5f;
    private float spreadAngle = 2f;
    private float range = 50f;

    private PlayerStats _cachedPlayerStats;
    private PlayerStats Stats
    {
        get
        {
            if (_cachedPlayerStats == null)
                _cachedPlayerStats = FindFirstObjectByType<PlayerStats>();
            return _cachedPlayerStats;
        }
    }

    private void Awake()
    {
        // Load ALL values from definition if assigned, or use field defaults
        if (definition != null)
        {
            damage = definition.damage > 0f ? definition.damage : 10f;
            fireRate = definition.fireRate > 0f ? definition.fireRate : 1f;
            reloadSpeed = definition.reloadTime > 0f ? definition.reloadTime : 2f;
            magSize = definition.magazineSize > 0 ? definition.magazineSize : 8;
            projectileCount = definition.projectileCount > 0 ? definition.projectileCount : 1;
            knockback = definition.knockback;
            aoeRadius = definition.aoeRadius;
            projectileSpeed = definition.projectileSpeed > 0f ? definition.projectileSpeed : 20f;
            pierceCount = definition.pierceCount;
            ricochetBounces = definition.ricochetBounces;
            critChance = definition.critChance;
            critDamage = definition.critDamage > 0f ? definition.critDamage : 1.5f;
            spreadAngle = definition.spreadAngle;
            range = definition.range;
        }
        // Find PlayerStats reference
        _cachedPlayerStats = FindFirstObjectByType<PlayerStats>();
        if (_cachedPlayerStats != null)
        {
            Debug.Log("[WeaponStats] PlayerStats found in Awake.");
        }
        else
        {
            Debug.LogWarning("[WeaponStats] PlayerStats not found in Awake.");
        }
    }

    // Single-source private base value lookups (if values got changed elsewhere - keep these synced)
    private float GetBaseOrOne(float val, float fallback = 1f) => val > 0f ? val : fallback;
    private int GetBaseOrOne(int val, int fallback = 1) => val > 0 ? val : fallback;

    public float GetDamage()
    {
        float baseVal = GetBaseOrOne(damage, 10f);
        float bonus = Stats != null ? Stats.GetStatBonus(StatType.Damage) : 0f;
        float final = baseVal * (1f + bonus);
        return final != 0f ? final : 1f;
    }

    public float GetFireRate()
    {
        // FireRate is stored as "seconds between shots" (cooldown time)
        // Increasing fire rate should DECREASE the cooldown, so we divide instead of multiply
        float baseVal = GetBaseOrOne(fireRate, 1f);
        float bonus = Stats != null ? Stats.GetStatBonus(StatType.FireRate) : 0f;
        // Divide by (1 + bonus) to decrease cooldown when fire rate increases
        float final = baseVal / (1f + bonus);
        return final > 0f ? final : 0.01f; // Prevent division by zero and return minimum cooldown
    }

    public float GetReloadSpeed()
    {
        float baseVal = GetBaseOrOne(reloadSpeed, 2f);
        float bonus = Stats != null ? Stats.GetStatBonus(StatType.ReloadSpeed) : 0f;
        float final = baseVal * (1f + bonus);
        return final != 0f ? final : 1f;
    }

    public int GetProjectileCount()
    {
        int baseVal = GetBaseOrOne(projectileCount, 1);
        int bonus = Stats != null ? Mathf.FloorToInt(Stats.GetStatBonus(StatType.ProjectileCount)) : 0;
        int total = baseVal + bonus;
        return total > 0 ? total : 1;
    }

    public int GetPierce()
    {
        int baseVal = GetBaseOrOne(pierceCount, 0);
        int bonus = Stats != null ? Mathf.FloorToInt(Stats.GetStatBonus(StatType.ProjectilePierce)) : 0;
        int total = baseVal + bonus;
        return total >= 0 ? total : 0;
    }

    public int GetRicochetBounces()
    {
        int baseVal = GetBaseOrOne(ricochetBounces, 0);
        int bonus = Stats != null ? Mathf.FloorToInt(Stats.GetStatBonus(StatType.RicochetBounces)) : 0;
        int total = baseVal + bonus;
        return total >= 0 ? total : 0;
    }

    public float GetKnockback()
    {
        float baseVal = GetBaseOrOne(knockback, 0f);
        float bonus = Stats != null ? Stats.GetStatBonus(StatType.Knockback) : 0f;
        float final = baseVal * (1f + bonus);
        return final != 0f ? final : 0f;
    }

    public float GetAoERadius()
    {
        float baseVal = GetBaseOrOne(aoeRadius, 0f);
        float bonus = Stats != null ? Stats.GetStatBonus(StatType.AoERadius) : 0f;
        float final = baseVal * (1f + bonus);
        return final != 0f ? final : 0f;
    }

    public float GetProjectileSpeed()
    {
        float baseVal = GetBaseOrOne(projectileSpeed, 20f);
        float bonus = Stats != null ? Stats.GetStatBonus(StatType.ProjectileSpeed) : 0f;
        float final = baseVal * (1f + bonus);
        // Cap projectile speed at 50 to prevent hit registration issues
        final = Mathf.Clamp(final, 1f, 50f);
        return final != 0f ? final : 1f;
    }

    public int GetMagSize()
    {
        // Safe integer cast for magazine size
        float bonus = Stats != null ? Stats.GetStatBonus(StatType.MagazineSize) : 0f;
        float calc = magSize * (1f + bonus);
        int mag = Mathf.RoundToInt(calc);
        return mag > 0 ? mag : 1;
    }

    public float GetCritChance()
    {
        float baseVal = Mathf.Max(critChance, 0f);
        float bonus = Stats != null ? Stats.GetStatBonus(StatType.CritChance) : 0f;
        float final = baseVal + bonus;
        return final > 0f ? final : 0f;
    }

    public float GetCritDamage()
    {
        float baseVal = GetBaseOrOne(critDamage, 1.5f);
        float bonus = Stats != null ? Stats.GetStatBonus(StatType.CritDamage) : 0f;
        float final = baseVal + bonus;
        return final > 0f ? final : 1f;
    }

    public float GetSpreadAngle()
    {
        // No player stat for spread currently, return base
        return spreadAngle;
    }

    public float GetRange()
    {
        // No player stat for range currently, return base
        return range;
    }

    // --- BASE STATS (without player bonuses, for stats panel display) ---
    public float GetBaseDamage() => damage;
    public float GetBaseFireRate() => fireRate;
    public float GetBaseReloadSpeed() => reloadSpeed;
    public int GetBaseMagSize() => magSize;
    public int GetBaseProjectileCount() => projectileCount;
    public float GetBaseKnockback() => knockback;
    public float GetBaseAoERadius() => aoeRadius;
    public float GetBaseProjectileSpeed() => projectileSpeed;
    public int GetBasePierce() => pierceCount;
    public int GetBaseRicochetBounces() => ricochetBounces;
    public float GetBaseCritChance() => critChance;
    public float GetBaseCritDamage() => critDamage;
}

