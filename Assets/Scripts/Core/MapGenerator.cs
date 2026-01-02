using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-50)]
public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; }

    [Header("Map Settings")]
    [SerializeField] private int width = 100;
    [SerializeField] private int depth = 100;
    [SerializeField] private float scale = 20f;
    [SerializeField] private float heightMultiplier = 5f;

    [Header("Low Poly Settings")]
    [Tooltip("Distance between vertices. Higher = Fewer triangles (Low Poly).")]
    [SerializeField, Range(1, 20)] private int resolution = 5;

    [Header("Props")]
    [SerializeField] private GameObject[] propPrefabs;
    [SerializeField] private int propCount = 100;

    [Header("Water Settings")]
    [SerializeField] private Material waterMaterial;

    [Header("Floor Material")]
    [SerializeField] private Material floorMaterial;
    [SerializeField] private float floorTextureTiling = 10f; // How many times the texture repeats across the map

    private float offsetX;
    private float offsetZ;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private void Awake()
    {
        Instance = this;
        offsetX = UnityEngine.Random.Range(0f, 9999f);
        offsetZ = UnityEngine.Random.Range(0f, 9999f);

        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.convex = false;

        GenerateMap();
        CreateBoundaries();
        SpawnProps();
    }

    /// <summary>
    /// Returns the playable radius of the map (inside the boundary walls).
    /// </summary>
    public float GetPlayableRadius()
    {
        // Boundary walls are at width * 0.4f, so playable area is slightly inside
        return width * 0.38f;
    }

    /// <summary>
    /// Checks if a position is within the playable bounds of the map.
    /// </summary>
    public bool IsWithinPlayableBounds(Vector3 pos)
    {
        float dist = new Vector2(pos.x, pos.z).magnitude;
        return dist <= GetPlayableRadius();
    }

    /// <summary>
    /// Clamps a position to be within playable bounds.
    /// Returns the clamped position.
    /// </summary>
    public Vector3 ClampToPlayableBounds(Vector3 pos)
    {
        float dist = new Vector2(pos.x, pos.z).magnitude;
        float maxRadius = GetPlayableRadius();
        
        if (dist > maxRadius && dist > 0.001f)
        {
            // Clamp to playable radius
            Vector2 dir = new Vector2(pos.x, pos.z).normalized;
            pos.x = dir.x * maxRadius;
            pos.z = dir.y * maxRadius;
        }
        
        return pos;
    }

    /// <summary>
    /// Returns height shaped by Perlin within circular island, hard cliff after edge.
    /// </summary>
    public float GetHeight(Vector3 pos)
    {
        // Calculate distance from center (0,0)
        float dist = new Vector2(pos.x, pos.z).magnitude;
        float islandRadius = width * 0.45f;
        float normDist = dist / islandRadius;

        if (normDist > 1.0f)
        {
            // Deep drop-off outside the playable island
            return -50f;
        }

        // Perlin noise, using the same mapping as before
        float xCoord = ((pos.x + width / 2f) / width) * scale + offsetX;
        float zCoord = ((pos.z + depth / 2f) / depth) * scale + offsetZ;
        float perlinVal = Mathf.PerlinNoise(xCoord, zCoord);

        return perlinVal * heightMultiplier;
    }

    private void GenerateMap()
    {
        int vertsX = (width / resolution) + 1;
        int vertsZ = (depth / resolution) + 1;
        Vector3[] vertices = new Vector3[vertsX * vertsZ];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[(vertsX - 1) * (vertsZ - 1) * 6];

        // Vertices and UV (tiled based on world position)
        for (int z = 0; z < vertsZ; z++)
        {
            for (int x = 0; x < vertsX; x++)
            {
                float worldX = (x * resolution) - (width / 2f);
                float worldZ = (z * resolution) - (depth / 2f);
                float y = GetHeight(new Vector3(worldX, 0, worldZ));
                int i = z * vertsX + x;
                vertices[i] = new Vector3(worldX, y, worldZ);
                // Tile UV based on world position for proper texture tiling
                uv[i] = new Vector2(worldX / floorTextureTiling, worldZ / floorTextureTiling);
            }
        }

        // Triangles
        int ti = 0;
        for (int z = 0; z < vertsZ - 1; z++)
        {
            for (int x = 0; x < vertsX - 1; x++)
            {
                int start = z * vertsX + x;
                triangles[ti++] = start;
                triangles[ti++] = start + vertsX;
                triangles[ti++] = start + 1;

                triangles[ti++] = start + 1;
                triangles[ti++] = start + vertsX;
                triangles[ti++] = start + vertsX + 1;
            }
        }

        // Apply to mesh
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;

        // Apply floor material if assigned
        if (floorMaterial != null && meshRenderer != null)
        {
            meshRenderer.sharedMaterial = floorMaterial;
        }

        // Create Water after terrain/mesh is ready
        CreateWater();
    }

    /// <summary>
    /// Place 8 boundary walls in a circular/octagonal fashion around the island.
    /// </summary>
    private void CreateBoundaries()
    {
        GameObject boundariesParent = new GameObject("Boundaries");
        boundariesParent.transform.SetParent(transform, false);

        float thickness = 5f;
        // float wallHeight = 100f; // Removed unused variable
        float wallYBottom = -20f;
        float wallYTop = 80f;
        float yMid = (wallYBottom + wallYTop) * 0.5f;
        float wallVerticalSize = wallYTop - wallYBottom;

        float octagonRadius = width * 0.4f; // Keep just inside the island

        for (int i = 0; i < 8; i++)
        {
            float angleRad = Mathf.Deg2Rad * (i * 45f);
            Vector3 dir = new Vector3(Mathf.Cos(angleRad), 0, Mathf.Sin(angleRad));
            Vector3 pos = dir * octagonRadius;
            pos.y = yMid;

            // Rotation: Wall faces inward.
            Quaternion rot = Quaternion.LookRotation(-dir, Vector3.up);

            Vector3 scale = new Vector3(octagonRadius, wallVerticalSize, thickness);

            GameObject wall = new GameObject("BoundaryWall_" + i);
            wall.transform.SetParent(boundariesParent.transform, false);
            wall.transform.localPosition = pos;
            wall.transform.localRotation = rot;
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = scale;
            // Intentionally invisible (no MeshRenderer)
        }

        // Ceiling at Y=80
        float ceilingY = wallYTop;
        Vector3 ceilingPos = new Vector3(0f, ceilingY, 0f);
        Vector3 ceilingScale = new Vector3(octagonRadius * 2f, 1f, octagonRadius * 2f);
        CreateBoundary("Ceiling", ceilingPos, ceilingScale, boundariesParent.transform);
    }

    private void CreateBoundary(string name, Vector3 pos, Vector3 scale, Transform parent)
    {
        GameObject boundary = new GameObject(name);
        boundary.transform.SetParent(parent, false);
        boundary.transform.localPosition = pos;
        boundary.transform.localRotation = Quaternion.identity;
        BoxCollider collider = boundary.AddComponent<BoxCollider>();
        collider.size = scale;
        // Ensure boundaries are invisible (no MeshRenderer)
    }

    /// <summary>
    /// Creates a plane extremely large to cover all visual space below the player and block out the default skybox.
    /// </summary>
    private void CreateWater()
    {
        GameObject water = new GameObject("Water");
        water.transform.SetParent(transform, false);

        MeshFilter mf = water.AddComponent<MeshFilter>();
        MeshRenderer mr = water.AddComponent<MeshRenderer>();

        // Water plane should be huge to fully block all skybox at visible horizon for any height.
        // Use a VERY large factor.
        float planeSize = Mathf.Max(width, depth) * 25f; // Increased from 1.5x to 25x
        float halfSize = planeSize * 0.5f;

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-halfSize, 0f, -halfSize),
            new Vector3(-halfSize, 0f, halfSize),
            new Vector3(halfSize, 0f, halfSize),
            new Vector3(halfSize, 0f, -halfSize)
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0,0),
            new Vector2(0,1),
            new Vector2(1,1),
            new Vector2(1,0)
        };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;

        // Use assigned water material if present, else create a blue material
        if (waterMaterial != null)
        {
            mr.sharedMaterial = waterMaterial;
        }
        else
        {
            Shader shader = Shader.Find("Unlit/Color");
            Material tempMat;
            if (shader != null)
            {
                tempMat = new Material(shader);
                tempMat.color = Color.blue;
            }
            else
            {
                tempMat = new Material(Shader.Find("Standard"));
                tempMat.color = Color.blue;
            }
            mr.sharedMaterial = tempMat;
        }

        water.transform.localPosition = new Vector3(0f, -2f, 0f);
        // The plane should not interfere with gameplay so do not add collider.
    }

    private void SpawnProps()
    {
        if (propPrefabs == null || propPrefabs.Length == 0) return;

        const float pylonMinDistance = 10f; // Minimum distance between pylons in meters
        const float propMinDistance = 4f; // Minimum distance between regular props in meters
        const float propToPylonMinDistance = 6f; // Minimum distance between props and pylons
        const float playerSpawnSafeRadius = 8f; // No props within this distance of center (player spawn point)
        
        List<Vector3> spawnedPylonPositions = new List<Vector3>();
        List<Vector3> spawnedPropPositions = new List<Vector3>(); // Track all prop positions
        
        const int maxPylonSpawnAttempts = 50; // Max attempts to find valid pylon position
        const int maxPropSpawnAttempts = 20; // Max attempts to find valid prop position within bounds
        
        float playableRadius = GetPlayableRadius();

        for (int i = 0; i < propCount; i++)
        {
            GameObject prefab = propPrefabs[UnityEngine.Random.Range(0, propPrefabs.Length)];
            
            // Check if this prefab is a Pylon
            bool isPylon = prefab.GetComponent<Pylon>() != null;
            
            Vector3 pos = Vector3.zero;
            bool validPosition = false;
            int attempts = 0;
            
            // For pylons, ensure minimum distance from other pylons AND props AND within playable bounds
            if (isPylon)
            {
                while (!validPosition && attempts < maxPylonSpawnAttempts)
                {
                    // Spawn within circular playable area using polar coordinates
                    float angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
                    float radius = UnityEngine.Random.Range(playerSpawnSafeRadius, playableRadius * 0.9f); // Start from safe radius
                    float randX = Mathf.Cos(angle) * radius;
                    float randZ = Mathf.Sin(angle) * radius;
                    float y = GetHeight(new Vector3(randX, 0, randZ));
                    pos = new Vector3(randX, y, randZ);
                    
                    // Skip positions that are underwater (below 0)
                    if (y < 0f)
                    {
                        attempts++;
                        continue;
                    }
                    
                    validPosition = true;
                    
                    // Check distance to all previously spawned pylons
                    foreach (Vector3 pylonPos in spawnedPylonPositions)
                    {
                        float distance = Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(pylonPos.x, 0, pylonPos.z));
                        if (distance < pylonMinDistance)
                        {
                            validPosition = false;
                            break;
                        }
                    }
                    
                    // Check distance to all previously spawned props
                    if (validPosition)
                    {
                        foreach (Vector3 propPos in spawnedPropPositions)
                        {
                            float distance = Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(propPos.x, 0, propPos.z));
                            if (distance < propToPylonMinDistance)
                            {
                                validPosition = false;
                                break;
                            }
                        }
                    }
                    
                    attempts++;
                }
                
                // If we couldn't find a valid position after max attempts, skip this pylon
                if (!validPosition)
                {
                    continue;
                }
                
                // Add this pylon position to the list
                spawnedPylonPositions.Add(pos);
            }
            else
            {
                // Regular props: spawn within circular playable area, but outside player spawn safe zone
                // AND check distance from other props and pylons
                attempts = 0;
                while (!validPosition && attempts < maxPropSpawnAttempts)
                {
                    // Use polar coordinates to spawn within circular area, starting from safe radius
                    float angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
                    float radius = UnityEngine.Random.Range(playerSpawnSafeRadius, playableRadius * 0.9f); // Start from safe radius
                    float randX = Mathf.Cos(angle) * radius;
                    float randZ = Mathf.Sin(angle) * radius;
                    float y = GetHeight(new Vector3(randX, 0, randZ));
                    pos = new Vector3(randX, y, randZ);
                    
                    // Skip positions that are underwater (below 0)
                    if (y < 0f)
                    {
                        attempts++;
                        continue;
                    }
                    
                    validPosition = true;
                    
                    // Check distance to all previously spawned props
                    foreach (Vector3 propPos in spawnedPropPositions)
                    {
                        float distance = Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(propPos.x, 0, propPos.z));
                        if (distance < propMinDistance)
                        {
                            validPosition = false;
                            break;
                        }
                    }
                    
                    // Check distance to all pylons
                    if (validPosition)
                    {
                        foreach (Vector3 pylonPos in spawnedPylonPositions)
                        {
                            float distance = Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(pylonPos.x, 0, pylonPos.z));
                            if (distance < propToPylonMinDistance)
                            {
                                validPosition = false;
                                break;
                            }
                        }
                    }
                    
                    attempts++;
                }
                
                // If we couldn't find a valid position, skip this prop
                if (!validPosition)
                {
                    continue;
                }
                
                // Add this prop position to the list
                spawnedPropPositions.Add(pos);
            }

            GameObject prop = Instantiate(prefab, pos, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0), transform);
        }
    }
}
