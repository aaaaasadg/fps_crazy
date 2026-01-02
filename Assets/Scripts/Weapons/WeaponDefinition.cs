using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "FPS/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    public string weaponName;
    public GameObject projectilePrefab;
    public Color weaponColor = Color.white;

    [Header("Base Stats")]
    public float damage = 10f;
    public float fireRate = 0.2f;          // Seconds between shots
    public int magazineSize = 30;
    public float range = 50f;              // Max distance for cleanup
    public float projectileSpeed = 20f;

    [Header("Reload & Handling")]
    public float reloadTime = 1.5f;        // Use this for reload speed/timing
    public float knockback = 0f;
    public float aoeRadius = 0f;
    public float spreadAngle = 2f;         // Shotgun/Accuracy

    [Header("Projectile/Shot Properties")]
    public int projectileCount = 1;        // Shotgun = multiple
    public int pierceCount = 0;
    public int ricochetBounces = 0;

    [Header("Critical Hit Stats")]
    public float critChance = 0f;          // 0-1 for percent
    public float critDamage = 1.5f;        // Multiplier; 1.5 = +50% dmg

    // Used by WeaponStats.cs to sync fields:
    // if (definition != null)
    // {
    //     // ... existing ...
    //     range = definition.range;
    //     spreadAngle = definition.spreadAngle;
    // }
}
