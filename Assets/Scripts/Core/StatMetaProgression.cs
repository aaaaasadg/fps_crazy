using System.Collections.Generic;

public static class StatMetaProgression
{
    private enum ValueFormat
    {
        Percent,
        Flat,
        FlatWithUnit
    }

    private class MetaConfig
    {
        public float perLevel;
        public ValueFormat format;
        public string unit;

        public MetaConfig(float perLevel, ValueFormat format, string unit = "")
        {
            this.perLevel = perLevel;
            this.format = format;
            this.unit = unit;
        }
    }

    private static readonly MetaConfig defaultPercentConfig = new MetaConfig(0.01f, ValueFormat.Percent);

    private static readonly Dictionary<StatType, MetaConfig> configByStat = new Dictionary<StatType, MetaConfig>
    {
        { StatType.MaxHP, new MetaConfig(5f, ValueFormat.FlatWithUnit, " HP") },
        { StatType.HPRegen, new MetaConfig(0.25f, ValueFormat.FlatWithUnit, " HP/s") },
        { StatType.ProjectileCount, new MetaConfig(1f, ValueFormat.Flat, "") },
        { StatType.ProjectilePierce, new MetaConfig(1f, ValueFormat.Flat, "") },
        { StatType.RicochetBounces, new MetaConfig(1f, ValueFormat.Flat, "") },
        { StatType.Damage, new MetaConfig(0.01f, ValueFormat.Percent) },
        { StatType.MoveSpeed, new MetaConfig(0.01f, ValueFormat.Percent) },
        { StatType.FireRate, new MetaConfig(0.01f, ValueFormat.Percent) },
        { StatType.ReloadSpeed, new MetaConfig(0.01f, ValueFormat.Percent) },
        { StatType.Knockback, new MetaConfig(0.01f, ValueFormat.Percent) },
        { StatType.AoERadius, new MetaConfig(0.01f, ValueFormat.Percent) },
        { StatType.XPGain, new MetaConfig(0.01f, ValueFormat.Percent) },
        { StatType.GoldGain, new MetaConfig(0.01f, ValueFormat.Percent) },
        { StatType.DamageReduction, new MetaConfig(0.01f, ValueFormat.Percent) },
        { StatType.Luck, new MetaConfig(0.01f, ValueFormat.Percent) },
        { StatType.PickupRange, new MetaConfig(0.01f, ValueFormat.Percent) },
        { StatType.CritChance, new MetaConfig(0.005f, ValueFormat.Percent) },
        { StatType.CritDamage, new MetaConfig(0.02f, ValueFormat.Percent) },
        { StatType.MagazineSize, new MetaConfig(0.02f, ValueFormat.Percent) },
        { StatType.ProjectileSpeed, new MetaConfig(0.02f, ValueFormat.Percent) }
    };

    private static MetaConfig GetConfig(StatType type)
    {
        return configByStat.TryGetValue(type, out MetaConfig config) ? config : defaultPercentConfig;
    }

    public static float GetMetaBonus(StatType type, int level)
    {
        if (level <= 0)
            return 0f;

        MetaConfig config = GetConfig(type);
        return level * config.perLevel;
    }

    public static string FormatBonusText(StatType type, float bonus)
    {
        MetaConfig config = GetConfig(type);

        switch (config.format)
        {
            case ValueFormat.FlatWithUnit:
                string unit = config.unit;
                if (LocalizationManager.Instance != null)
                {
                    unit = LocalizationManager.Instance.GetLocalizedString(unit);
                }
                return $"+{bonus:0.#}{unit}";
            case ValueFormat.Flat:
                return $"+{bonus:0.#}";
            default:
                return $"+{bonus * 100f:0.#}%";
        }
    }

    public static bool IsPercentStat(StatType type)
    {
        return GetConfig(type).format == ValueFormat.Percent;
    }
}

