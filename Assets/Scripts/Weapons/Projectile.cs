using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    // --- Core Stats ---
    private float speed;
    private float damage;
    private float range;
    private int pierceCount;
    private int ricochetCount;
    private float critChance;
    private float critMult;
    private float knockback;
    private float aoeRadius;

    // --- Runtime State ---
    private int currentPierce = 0;
    private List<GameObject> hitHistory = new List<GameObject>();
    private Vector3 startPos;
    private Rigidbody rb;
    private static readonly Collider[] initHitBuffer = new Collider[8];
    private static readonly Collider[] aoeHitBuffer = new Collider[32];
    private static readonly Collider[] ricochetHitBuffer = new Collider[32];

    /// <summary>
    /// Initializes the projectile with all relevant stats.
    /// </summary>
    /// <param name="dmg">Damage value.</param>
    /// <param name="spd">Projectile speed (if 0 or less, defaults to 50).</param>
    /// <param name="rng">Projectile range.</param>
    /// <param name="direction">Forward direction to launch the projectile.</param>
    /// <param name="critChance">Crit chance (0-1).</param>
    /// <param name="critMultiplier">Crit multiplier.</param>
    /// <param name="pierceCount">Number of pierces.</param>
    /// <param name="ricochetBounces">Number of ricochets.</param>
    /// <param name="knockbackForce">Knockback force.</param>
    /// <param name="aoeRad">AOE radius.</param>
    public void Initialize(
        float dmg, float spd, float rng, Vector3 direction,
        float critChance, float critMultiplier,
        int pierceCount, int ricochetBounces,
        float knockbackForce,
        float aoeRad
    )
    {
        damage = dmg;
        speed = (spd > 0f) ? spd : 50f; // Ensure speed is never zero or negative
        // Cap projectile speed at 50 to prevent hit registration issues
        speed = Mathf.Clamp(speed, 1f, 50f);
        range = rng;
        this.critChance = Mathf.Clamp01(critChance);
        critMult = critMultiplier;
        this.pierceCount = pierceCount;
        this.ricochetCount = ricochetBounces;
        knockback = knockbackForce;
        aoeRadius = aoeRad;
        currentPierce = 0;

        // Direction
        transform.forward = direction.normalized;

        gameObject.SetActive(true);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void OnEnable()
    {
        // Always cancel any scheduled disables to avoid old timers affecting this reuse
        CancelInvoke();

        startPos = transform.position;
        currentPierce = 0;
        hitHistory.Clear();

        // Immediate "inside enemy" check - use smaller radius to prevent multiple hits on same enemy
        int initHitCount = Physics.OverlapSphereNonAlloc(transform.position, 0.2f, initHitBuffer);
        for (int i = 0; i < initHitCount; i++)
        {
            // Only process each collider once
            if (initHitBuffer[i] != null)
            {
                OnTriggerEnter(initHitBuffer[i]);
            }
        }

        // Safety auto-disable
        Invoke("Disable", 5f);
    }

    private void OnDisable()
    {
        // Cancel any running disable timer and always clear hit history to prevent leaks between uses
        CancelInvoke();
        hitHistory.Clear();
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += transform.forward * step;
        if (Vector3.Distance(startPos, transform.position) > range)
        {
            Disable();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (hitHistory.Contains(other.gameObject)) return;
        if (other.CompareTag("Player") || other.gameObject.layer == LayerMask.NameToLayer("Player")) return;

        hitHistory.Add(other.gameObject);

        // ENEMY HIT
        if (other.TryGetComponent(out Enemy enemy))
        {
            bool isCrit = Random.value < critChance;
            float finalDmg = damage * (isCrit ? critMult : 1f);

            // --- SOUND: Crit Hit ---
            if (isCrit && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayCritHit();
            }

            // Pass aoeRadius to TakeDamage for the primary (direct) target (for proper VFX)
            enemy.TakeDamage(finalDmg, isCrit, aoeRadius);

            // KNOCKBACK LOGIC
            if (knockback > 0f)
            {
                // Calculate direction away from projectile
                Vector3 knockDir = transform.forward;
                enemy.TakeKnockback(knockDir, knockback);
            }

            // AOE - always deals 20% of base damage, radius can scale but damage is fixed
            if (aoeRadius > 0f)
            {
                float aoeDamage = damage * 0.2f; // Fixed at 20% of original damage
                int aoeHitCount = Physics.OverlapSphereNonAlloc(transform.position, aoeRadius, aoeHitBuffer);
                for (int i = 0; i < aoeHitCount; i++)
                {
                    Collider h = aoeHitBuffer[i];
                    if (h == null || h.gameObject == other.gameObject) continue;
                    if (h.TryGetComponent(out Enemy nearEnemy))
                    {
                        // Pass 0f as aoeRadius so only the *main* (direct) target triggers the AoE VFX
                        nearEnemy.TakeDamage(aoeDamage, false, 0f);
                    }
                }
            }

            // --- RICOCHET LOGIC ---
            if (ricochetCount > 0)
            {
                Enemy nextTarget = FindRicochetTarget(transform.position, 15f, other.gameObject);
                if (nextTarget != null)
                {
                    ricochetCount--;
                    // Aim at new target
                    transform.LookAt(nextTarget.transform.position + Vector3.up * 1.5f);
                    // Clear history to allow hitting the new target
                    hitHistory.Clear();
                    // IMPORTANT: Add the OLD target to history so we don't immediately bounce back to it
                    hitHistory.Add(other.gameObject);
                    return; // Keep flying!
                }
            }

            // If no ricochet occurred, check Pierce
            currentPierce++;
            if (currentPierce > pierceCount)
            {
                Disable();
            }
            return;
        }

        // WALL/GROUND HIT
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.CompareTag("Ground"))
        {
            // --- SOUND: Ricochet (if applicable) or just Wall Hit ---
            // PlayRicochet() call removed as requested.

            // Chain ricochet logic
            if (ricochetCount > 0)
            {
                Enemy nextTarget = FindRicochetTarget(transform.position, 15f, null);
                if (nextTarget != null)
                {
                    ricochetCount--;
                    // Aim at new target
                    transform.LookAt(nextTarget.transform.position + Vector3.up * 1.5f);
                    hitHistory.Clear();
                    return;
                }
            }
            Disable();
        }
    }

    // Helper method for ricochet targeting
    private Enemy FindRicochetTarget(Vector3 pos, float range, GameObject ignoreObj)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(pos, range, ricochetHitBuffer);
        float bestDist = range * range;
        Enemy best = null;
        for (int i = 0; i < hitCount; i++)
        {
            Collider h = ricochetHitBuffer[i];
            if (h == null || h.gameObject == ignoreObj) continue;
            Enemy e = h.GetComponent<Enemy>();
            if (e != null)
            {
                float d = (h.transform.position - pos).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = e;
                }
            }
        }
        return best;
    }

    private void Disable()
    {
        CancelInvoke("Disable");
        gameObject.SetActive(false);
    }
}
