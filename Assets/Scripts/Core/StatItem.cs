using UnityEngine;

public class StatItem : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private ParticleSystem pickupEffect;

    private void Awake()
    {
        // Optionally auto-assign components if not set
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (pickupEffect == null)
            pickupEffect = GetComponentInChildren<ParticleSystem>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Get UpgradeSystem reference
        UpgradeSystem upgradeSystem = FindFirstObjectByType<UpgradeSystem>();
        if (upgradeSystem == null)
        {
            Debug.LogWarning("StatItem: UpgradeSystem not found!");
            Destroy(gameObject);
            return;
        }

        // Generate a random Stat upgrade (StatItem drop, not LevelUp choice)
        UpgradeSystem.UpgradeChoice choice = upgradeSystem.GenerateRandomStatItemDrop();
        if (choice != null)
        {
            upgradeSystem.ApplyUpgrade(choice);

            // Show popup text
            string statName = choice.Name ?? "Stat";
            if (DamageTextManager.Instance != null)
            {
                Vector3 popupPos = transform.position + Vector3.up * 1.5f;
                // Fix: Proper argument order for ShowDamage(float, Vector3, bool, GameObject)
                DamageTextManager.Instance.ShowDamage(0, popupPos, false, null); // Use 0 so only text shows
                // If DamagePopup supports custom text, you'd call it here; for now, show as +[StatName]
            }
        }

        // Play pickup effect if present
        if (pickupEffect != null)
        {
            pickupEffect.Play(true);
        }

        // Hide mesh immediately
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        // Destroy object (or deactivate for pooling)
        Destroy(gameObject);
    }
}
