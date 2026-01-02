using UnityEngine;
using UnityEditor;
using System.IO;

public class ItemGenerator
{
    [MenuItem("Tools/Generate Items")]
    public static void Generate()
    {
        // Create folder if not exists
        string path = "Assets/Data/Items";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        // 1. Max HP (Flat)
        CreateItem("Item_MaxHP", "Healthy Heart", "Increases Max HP.", StatType.MaxHP, 20f, path);

        // 2. HP Regen (Flat)
        CreateItem("Item_HPRegen", "Troll Blood", "Regenerates HP over time.", StatType.HPRegen, 1f, path);

        // 3. Move Speed (%)
        CreateItem("Item_MoveSpeed", "Wind Boots", "Increases movement speed.", StatType.MoveSpeed, 0.05f, path); // 5%

        // 4. Damage (%)
        CreateItem("Item_Damage", "Whetstone", "Increases damage.", StatType.Damage, 0.1f, path); // 10%

        // 5. Fire Rate (%)
        CreateItem("Item_FireRate", "Rapid Trigger", "Increases fire rate.", StatType.FireRate, 0.1f, path); // 10%

        // 6. Reload Speed (%) - Negative reduces time
        CreateItem("Item_Reload", "Oiled Mag", "Reduces reload time.", StatType.ReloadSpeed, -0.05f, path); // -5%

        // 7. Projectile Count (Flat)
        CreateItem("Item_ProjCount", "Split Shot", "Adds an extra projectile.", StatType.ProjectileCount, 1f, path);

        // 8. Pierce (Flat)
        CreateItem("Item_Pierce", "Drill Tip", "Projectiles pierce enemies.", StatType.ProjectilePierce, 1f, path);

        // 9. Ricochet (Flat)
        CreateItem("Item_Ricochet", "Bouncy Ball", "Projectiles bounce off walls.", StatType.RicochetBounces, 1f, path);

        // 10. Knockback (%)
        CreateItem("Item_Knockback", "Heavy Hammer", "Increases knockback force.", StatType.Knockback, 0.15f, path); // 15%

        // 11. AoE Radius (%)
        CreateItem("Item_AoE", "Explosive Powder", "Increases explosion radius.", StatType.AoERadius, 0.1f, path); // 10%

        // 12. XP Gain (%)
        CreateItem("Item_XP", "Knowledge Tome", "Increases XP gain.", StatType.XPGain, 0.1f, path); // 10%

        // 13. Gold Gain (%)
        CreateItem("Item_Gold", "Gold Coin", "Increases Gold gain.", StatType.GoldGain, 0.1f, path); // 10%

        // 14. Damage Reduction (%) - Positive value reduces damage taken in formula (1 - val)
        CreateItem("Item_Armor", "Iron Plate", "Reduces damage taken.", StatType.DamageReduction, 0.05f, path); // 5%

        // 15. Luck (Flat/Points)
        CreateItem("Item_Luck", "Lucky Clover", "Increases luck.", StatType.Luck, 2f, path); // 2 luck

        // 16. Pickup Range (%)
        CreateItem("Item_Pickup", "Magnet", "Increases pickup range.", StatType.PickupRange, 0.15f, path); // 15%

        // 17. Crit Chance (Additive %)
        CreateItem("Item_CritChance", "Scope", "Increases critical chance.", StatType.CritChance, 0.05f, path); // 5%

        // 18. Crit Damage (%)
        CreateItem("Item_CritDmg", "Assassin Dagger", "Increases critical damage.", StatType.CritDamage, 0.15f, path); // 15%

        // 19. Magazine Size (Multiplier for Int)
        CreateItem("Item_MagSize", "Extended Mag", "Increases magazine size.", StatType.MagazineSize, 0.1f, path); // 10%

        // 20. Projectile Speed (%)
        CreateItem("Item_ProjSpeed", "Aerodynamics", "Increases projectile speed.", StatType.ProjectileSpeed, 0.1f, path); // 10%

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generated 20 Items in " + path);
    }

    private static void CreateItem(string fileName, string name, string desc, StatType type, float baseVal, string path)
    {
        UpgradeDefinition asset = ScriptableObject.CreateInstance<UpgradeDefinition>();
        asset.upgradeName = name;
        asset.description = desc;
        asset.statType = type;

        float[] rarityValues = new float[4];
        for (int i = 0; i < 4; i++)
        {
            float factor = 1f;
            switch (i)
            {
                case 0: factor = 1f; break;         // Common
                case 1: factor = 1.5f; break;       // Rare
                case 2: factor = 2.0f; break;       // Epic
                case 3: factor = 3.0f; break;       // Legendary
            }

            float val = baseVal * factor;

            // Rounding for display/logic clarity
            switch (type)
            {
                case StatType.MaxHP:
                case StatType.ProjectileCount:
                case StatType.ProjectilePierce:
                case StatType.RicochetBounces:
                case StatType.Luck:
                    val = Mathf.Round(val);    // Flat stats get rounded to nearest int
                    break;

                default:
                    val = Mathf.Round(val * 100f) / 100f; // Percentage stats: keep 2 decimal places
                    break;
            }
            rarityValues[i] = val;
        }
        asset.rarityValues = rarityValues;

        string assetPath = path + "/" + fileName + ".asset";
        AssetDatabase.CreateAsset(asset, assetPath);
    }
}