using System.Collections.Generic;
using UnityEngine;

public class UpgradeSystem : MonoBehaviour
{
    // Reload speed cap: -0.20 means max 20% reload time reduction
    private const float RELOAD_SPEED_CAP = -0.20f;
    // Crit chance cap: 1.0 means 100%
    private const float CRIT_CHANCE_CAP = 1.0f;
    
    private string LocalizeUpgradeName(string originalName)
    {
        if (LocalizationManager.Instance != null)
        {
            string localized = LocalizationManager.Instance.GetLocalizedString(originalName);
            return localized != originalName ? localized : originalName;
        }
        return originalName;
    }

    private string LocalizeUpgradeDescription(string originalDesc, string fullDescription)
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(originalDesc))
        {
            string localized = LocalizationManager.Instance.GetLocalizedString(originalDesc);
            if (localized != originalDesc && fullDescription.Contains(originalDesc))
            {
                // Only replace the description text, not the numbers or color tags
                // Split by newline to find the description part (first line) vs numbers (later lines)
                string[] lines = fullDescription.Split('\n');
                if (lines.Length > 0)
                {
                    // Replace description only in the first line(s) that contain text, preserve number lines
                    for (int i = 0; i < lines.Length; i++)
                    {
                        // Skip lines that are just whitespace or contain color tags (number lines)
                        if (lines[i].Trim().Length > 0 && !lines[i].Contains("<color="))
                        {
                            // This is likely the description line - replace it
                            if (lines[i].Contains(originalDesc))
                            {
                                lines[i] = lines[i].Replace(originalDesc, localized);
                                break; // Only replace once
                            }
                        }
                    }
                    return string.Join("\n", lines);
                }
                else
                {
                    // Fallback to simple replace if structure is unexpected
                    return fullDescription.Replace(originalDesc, localized);
                }
            }
        }
        return fullDescription;
    }

    [SerializeField] private List<UpgradeDefinition> allUpgradeDefinitions;
    [SerializeField] private List<UpgradeDefinition> itemDefinitions; // Item pool for Chests/Items

    private PlayerStats playerStats;

    [System.Serializable]
    public class UpgradeChoice
    {
        public string Name;      // Item name
        public string Desc;      // Full: Description + newline + Current -> New (rich text)
        public Sprite Icon;      // Icon of the item
        public float Value;      // Upgrade value
        public StatType Type;
        public Rarity Rarity;
        // RarityString/ValueString intentionally omitted; Desc contains everything needed
    }

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        }

        if (allUpgradeDefinitions == null)
        {
            allUpgradeDefinitions = new List<UpgradeDefinition>();
        }

        if (itemDefinitions == null)
        {
            itemDefinitions = new List<UpgradeDefinition>();
        }
    }

    public List<UpgradeChoice> GetRandomUpgrades(int count = 3)
    {
        List<UpgradeChoice> choices = new List<UpgradeChoice>(count);

        if (allUpgradeDefinitions == null || allUpgradeDefinitions.Count == 0) 
            return choices;

        // Build list of valid upgrade indices (excluding capped stats)
        List<int> validIndices = new List<int>();
        for (int i = 0; i < allUpgradeDefinitions.Count; i++)
        {
            UpgradeDefinition def = allUpgradeDefinitions[i];
            
            // Check if ReloadSpeed is capped
            if (def.statType == StatType.ReloadSpeed)
            {
                float currentBonus = playerStats != null ? playerStats.GetStatBonus(StatType.ReloadSpeed) : 0f;
                // ReloadSpeed uses negative values, so check if already at or past cap
                if (currentBonus <= RELOAD_SPEED_CAP)
                {
                    continue; // Skip this upgrade, it's capped
                }
            }
            
            // Check if CritChance is capped at 100%
            if (def.statType == StatType.CritChance)
            {
                float currentBonus = playerStats != null ? playerStats.GetStatBonus(StatType.CritChance) : 0f;
                if (currentBonus >= CRIT_CHANCE_CAP)
                {
                    continue; // Skip this upgrade, it's capped
                }
            }
            
            validIndices.Add(i);
        }
        
        if (validIndices.Count == 0) return choices;

        // Select random unique indices from valid pool
        List<int> selectedIndices = new List<int>();
        int attempts = 0;
        int maxAttempts = validIndices.Count * 3;
        
        while (selectedIndices.Count < count && selectedIndices.Count < validIndices.Count && attempts < maxAttempts)
        {
            int randomIndex = UnityEngine.Random.Range(0, validIndices.Count);
            int actualIdx = validIndices[randomIndex];
            
            if (!selectedIndices.Contains(actualIdx))
                selectedIndices.Add(actualIdx);
            
            attempts++;
        }

        foreach (int idx in selectedIndices)
        {
            UpgradeDefinition def = allUpgradeDefinitions[idx];
            Rarity rarity = CalculateRarity();
            float val = def.GetValueForRarity(rarity);

            string fullDescription = FormatDescription(def, val);

            UpgradeChoice choice = new UpgradeChoice
            {
                Name = LocalizeUpgradeName(def.upgradeName),
                Desc = fullDescription, // Already localized in FormatDescription
                Icon = def.icon,
                Value = val,
                Type = def.statType,
                Rarity = rarity
            };
            choices.Add(choice);
        }
        return choices;
    }

    // Provide a random item from the itemDefinitions list (for chests/etc.)
    public UpgradeChoice GetRandomItem()
    {
        return GetRandomItem(false); // Default: use normal rarity calculation
    }

    /// <summary>
    /// Gets a random item. If forceLegendary is true, always returns a Legendary item.
    /// </summary>
    public UpgradeChoice GetRandomItem(bool forceLegendary)
    {
        if (itemDefinitions == null || itemDefinitions.Count == 0)
            return null;
        int idx = UnityEngine.Random.Range(0, itemDefinitions.Count);
        UpgradeDefinition def = itemDefinitions[idx];
        Rarity rarity = forceLegendary ? Rarity.Legendary : CalculateRarity();
        float val = def.GetValueForRarity(rarity);

        string fullDescription = FormatDescription(def, val);

        return new UpgradeChoice
        {
            Name = LocalizeUpgradeName(def.upgradeName),
            Desc = fullDescription, // Already localized in FormatDescription
            Icon = def.icon,
            Value = val,
            Type = def.statType,
            Rarity = rarity
        };
    }

    /// <summary>
    /// Returns a "description\nCurrent -> New" formatted string according to stat type:
    /// For Percentage:      "desc\n<color=white>100%</color> -> <color=green>115%</color>"
    /// For Flat:            "desc\n<color=white>2</color> -> <color=green>3</color>"
    /// For Magazine Size:   "desc\n<color=white>8</color> -> <color=green>12</color>"
    /// </summary>
    private string FormatDescription(UpgradeDefinition def, float value)
    {
        if (def == null)
            return "";

        // 1. Clean Description: Remove {value}
        string desc = def.description ?? "";
        while (desc.Contains("{value}"))
        {
            desc = desc.Replace("{value}", "");
        }
        desc = desc.Trim();

        // 2. Localize the description BEFORE formatting
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(desc))
        {
            string localized = LocalizationManager.Instance.GetLocalizedString(desc);
            if (localized != desc)
            {
                desc = localized;
            }
        }

        // Get current stat bonus (default 0)
        float currentBonus = 0f;
        if (playerStats != null)
            currentBonus = playerStats.GetStatBonus(def.statType);

        string currentStr = "";
        string newStr = "";

        switch (def.statType)
        {
            // MAXHP: Show total HP values (Base + Bonus, additive now)
            case StatType.MaxHP:
            {
                float currentHP = playerStats != null ? playerStats.MaxHP : 100f;
                float newHP = currentHP + value; // Simple addition (additive model)
                currentStr = currentHP.ToString("F0");
                newStr = newHP.ToString("F0");
                break;
            }
            // FLAT GROUP (raw number, "0.#")
            case StatType.HPRegen:
            case StatType.ProjectileCount:
            case StatType.ProjectilePierce:
            case StatType.RicochetBounces:
                currentStr = currentBonus.ToString("0.#");
                newStr = (currentBonus + value).ToString("0.#");
                break;

            // LUCK: Display as Percentage (moved to percentage group)
            case StatType.Luck:
                currentStr = ((1f + currentBonus) * 100f).ToString("F0") + "%";
                newStr = ((1f + currentBonus + value) * 100f).ToString("F0") + "%";
                break;

            // MAGAZINE SIZE (integer bullets, uses WeaponStats)
            case StatType.MagazineSize:
            {
                WeaponStats ws = UnityEngine.Object.FindFirstObjectByType<WeaponStats>();
                if (ws != null)
                {
                    int currentCount = ws.GetMagSize();
                    // Avoid double-counting bonuses:
                    // baseMag = currentCount / (1f + currentBonus)
                    float baseMag = currentCount / (1f + currentBonus);
                    int newCount = Mathf.RoundToInt(baseMag * (1f + currentBonus + value));
                    currentStr = currentCount.ToString();
                    newStr = newCount.ToString();
                }
                else
                {
                    currentStr = ((1f + currentBonus) * 100f).ToString("F0") + "%";
                    newStr = ((1f + currentBonus + value) * 100f).ToString("F0") + "%";
                }
                break;
            }

            // DAMAGE REDUCTION (inverted percent, lower is better)
            case StatType.DamageReduction:
                currentStr = ((1f - currentBonus) * 100f).ToString("F0") + "%";
                newStr = ((1f - (currentBonus + value)) * 100f).ToString("F0") + "%";
                break;

            // CRIT CHANCE (additive percent)
            case StatType.CritChance:
                currentStr = (currentBonus * 100f).ToString("F0") + "%";
                newStr = ((currentBonus + value) * 100f).ToString("F0") + "%";
                break;

            // DEFAULT: Show as Multiplier Percent
            default:
                currentStr = ((1f + currentBonus) * 100f).ToString("F0") + "%";
                newStr = ((1f + currentBonus + value) * 100f).ToString("F0") + "%";
                break;
        }

        return $"{desc}\n\n<color=white>{currentStr}</color> -> <color=green>{newStr}</color>";
    }

    private Rarity CalculateRarity()
    {
        // Luck is stored as 0.2, 0.4, 0.6, 0.8 (representing 20%, 40%, 60%, 80%)
        float luck = playerStats != null ? playerStats.GetStatBonus(StatType.Luck) : 0f;
        luck = Mathf.Max(0f, luck);

        // Base probabilities (percentages)
        float common = 60f;
        float rare = 25f;
        float epic = 10f;
        float legendary = 5f;

        // Each 0.1 (10%) of luck shifts weights:
        // -5 common, +2.5 rare, +1.5 epic, +1 legendary
        float luckFactor = luck / 0.1f; // Convert to "how many 10% increments"
        
        common -= 5f * luckFactor;
        rare += 2.5f * luckFactor;
        epic += 1.5f * luckFactor;
        legendary += 1f * luckFactor;

        // Clamp to reasonable ranges
        common = Mathf.Clamp(common, 5f, 60f);
        rare = Mathf.Clamp(rare, 10f, 50f);
        epic = Mathf.Clamp(epic, 5f, 40f);
        legendary = Mathf.Clamp(legendary, 3f, 30f);

        float total = common + rare + epic + legendary;
        float roll = UnityEngine.Random.Range(0f, total);

        if (roll < common) return Rarity.Common;
        if (roll < common + rare) return Rarity.Rare;
        if (roll < common + rare + epic) return Rarity.Epic;
        return Rarity.Legendary;
    }

    public void ApplyUpgrade(UpgradeChoice choice)
    {
        Debug.Log($"Attempting to apply upgrade: {choice?.Name} Type: {choice?.Type} Value: {choice?.Value}");

        if (playerStats == null)
        {
            playerStats = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        }

        if (playerStats != null && choice != null)
        {
            playerStats.ApplyUpgrade(choice.Type, choice.Value);
        }
        else if (playerStats == null)
        {
            Debug.LogError("PlayerStats missing in UpgradeSystem!");
        }
    }

    public UpgradeChoice GenerateRandomStatItemDrop()
    {
        if (allUpgradeDefinitions == null || allUpgradeDefinitions.Count == 0)
            return null;

        int idx = UnityEngine.Random.Range(0, allUpgradeDefinitions.Count);
        UpgradeDefinition def = allUpgradeDefinitions[idx];

        float val = def.GetValueForRarity(Rarity.Common);

        string fullDesc = FormatDescription(def, val);
        var choice = new UpgradeChoice
        {
            Name = LocalizeUpgradeName(def.upgradeName),
            Icon = def.icon,
            Value = val,
            Type = def.statType,
            Rarity = Rarity.Common,
            Desc = fullDesc // Already localized in FormatDescription
        };

        return choice;
    }
}
