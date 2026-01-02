using UnityEngine;
using System.Collections.Generic;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private int poolSize = 30;

    // --- Accumulation dictionary ---
    private Dictionary<int, DamagePopup> activePopups = new Dictionary<int, DamagePopup>();

    private void Awake()
    {
        Instance = this;
        // (Retain pool instantiation as legacy, but not used in refactor for debugging simplicity)
        for (int i = 0; i < poolSize; i++)
        {
            GameObject popupObj = Instantiate(damagePopupPrefab, transform);
            DamagePopup popup = popupObj.GetComponent<DamagePopup>();
            if (popup == null)
            {
                Debug.LogError("DamageTextManager: damagePopupPrefab does not have a DamagePopup component!");
                Destroy(popupObj);
                continue;
            }
            popupObj.SetActive(false);
        }
    }

    /// <summary>
    /// Show damage popup at the given position.
    /// Reuses popup for target, accumulating if applicable.
    /// </summary>
    public DamagePopup ShowDamage(float amount, Vector3 pos, bool isCrit, GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning("DamageTextManager: ShowDamage called with null target!");
            return null;
        }

        int id = target.GetInstanceID();

        // Clean dictionary lazily: if entry exists but obj is inactive/null, remove
        if (activePopups.TryGetValue(id, out DamagePopup popup))
        {
            if (popup != null && popup.gameObject.activeSelf)
            {
                // Accumulate on existing popup: add damage, update position, update crit/highlight
                popup.Setup(amount, isCrit);
                popup.transform.position = pos;
                return popup;
            }
            else
            {
                // Cleanup stale
                activePopups.Remove(id);
            }
        }

        // Spawn new popup
        GameObject obj = ObjectPool.Instance.SpawnFromPool("DamagePopup", pos, Quaternion.identity);

        if (obj == null)
        {
            Debug.LogError("DamageTextManager: ObjectPool returned NULL for 'DamagePopup'. Check pool name!");
            return null;
        }

        DamagePopup newPopup = obj.GetComponent<DamagePopup>();
        if (newPopup != null)
        {
            newPopup.Setup(amount, isCrit);
            activePopups[id] = newPopup;
            return newPopup;
        }
        else
        {
            Debug.LogError("DamageTextManager: Spawned object has no DamagePopup component!");
            return null;
        }
    }
}
