using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public struct Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    [SerializeField]
    public List<Pool> pools;

    // Use List<GameObject> for pooling
    public Dictionary<string, List<GameObject>> poolDictionary;
    // Track the next index for circular recycling per pool
    private Dictionary<string, int> nextPoolIndex;
    // Store prefab default rotations to preserve them when identity is passed
    private Dictionary<string, Quaternion> prefabRotations;

    private static readonly List<ParticleSystem> particleBuffer = new List<ParticleSystem>(8);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        poolDictionary = new Dictionary<string, List<GameObject>>();
        nextPoolIndex = new Dictionary<string, int>();
        prefabRotations = new Dictionary<string, Quaternion>();

        foreach (Pool pool in pools)
        {
            List<GameObject> objectList = new List<GameObject>(pool.size);
            
            // Store prefab's default rotation
            if (pool.prefab != null)
            {
                prefabRotations[pool.tag] = pool.prefab.transform.rotation;
            }

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(this.transform); // Keep hierarchy clean
                objectList.Add(obj);
            }

            poolDictionary.Add(pool.tag, objectList);
            nextPoolIndex.Add(pool.tag, 0);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return null;
        }

        // If identity rotation is passed, use prefab's default rotation instead
        Quaternion finalRotation = rotation;
        if (rotation == Quaternion.identity && prefabRotations.ContainsKey(tag))
        {
            finalRotation = prefabRotations[tag];
        }

        List<GameObject> poolList = poolDictionary[tag];
        int poolSize = poolList.Count;

        // 1. Try to find an inactive object in pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = poolList[i];
            if (!obj.activeInHierarchy)
            {
                obj.transform.position = position;
                obj.transform.rotation = finalRotation;
                obj.SetActive(true);
                RestartParticleSystems(obj);
                return obj;
            }
        }

        // 2. If all active, recycle using nextPoolIndex as circular buffer
        int index = nextPoolIndex[tag];
        GameObject recycled = poolList[index];
        Debug.LogWarning($"Pool {tag} exhausted, recycling active object.");

        StopParticleSystems(recycled);
        recycled.SetActive(false); // Reset/disable if needed
        recycled.transform.position = position;
        recycled.transform.rotation = finalRotation;
        recycled.SetActive(true);  // Immediately activate for reuse
        RestartParticleSystems(recycled);

        // Advance circular index
        index = (index + 1) % poolSize;
        nextPoolIndex[tag] = index;

        return recycled;
    }

    private static void RestartParticleSystems(GameObject obj)
    {
        if (obj == null)
            return;

        // Ensure all child GameObjects are active (for images, sprites, etc.)
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            Transform child = obj.transform.GetChild(i);
            if (child != null && !child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);
            }
        }

        particleBuffer.Clear();
        obj.GetComponentsInChildren(true, particleBuffer);
        for (int i = 0; i < particleBuffer.Count; i++)
        {
            ParticleSystem ps = particleBuffer[i];
            if (ps == null)
                continue;
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private static void StopParticleSystems(GameObject obj)
    {
        if (obj == null)
            return;

        particleBuffer.Clear();
        obj.GetComponentsInChildren(true, particleBuffer);
        for (int i = 0; i < particleBuffer.Count; i++)
        {
            ParticleSystem ps = particleBuffer[i];
            if (ps == null)
                continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Ensure all child renderers/components are properly reset
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = true; // Reset renderer state
            }
        }
    }
}
