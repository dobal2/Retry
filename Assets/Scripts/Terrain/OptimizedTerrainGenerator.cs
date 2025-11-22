using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class OptimizedTerrainGenerator : MonoBehaviour
{
    [Header("World Settings")]
    [SerializeField] private int worldSizeInChunks = 10;
    public int chunkSize = 32;
    [SerializeField] private float mapSize = 320f;
    
    [Header("Noise Settings")]
    [SerializeField] private float noiseScale = 0.03f;
    [SerializeField] private float heightMultiplier = 7;
    [SerializeField] private int octavesCount = 4;
    [SerializeField] private float lacunarity = 2;
    [SerializeField] private float persistance = 0.5f;
    [SerializeField] private int seed = 0;
    
    [Header("Falloff Settings")]
    [SerializeField] private bool useFalloff = true;
    [SerializeField] private float falloffStrength = 3f;
    [SerializeField] private float falloffShift = 2.2f;
    [SerializeField] private float centerFalloffReduction = 0.7f;
    
    [Header("Visual Settings")]
    [SerializeField] private Gradient terrainGradient;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private Texture2D baseTexture;

    [Header("Grass")] 
    [SerializeField] private bool GenerateGrass;
    [SerializeField] private float initialGrassRadius = 150f; // ★ 처음 생성 반경
    [SerializeField] private float dynamicGrassDistance = 120f; // ★ 플레이어 근처 생성 거리
    [SerializeField] private float grassCheckInterval = 2f; // ★ 체크 주기
    [SerializeField] private int maxDynamicChunksPerUpdate = 3; // ★ 한 번에 최대 N개 청크만 생성
    [SerializeField] private bool sortByDistance = true;
    
    [Header("Water Settings")]
    [SerializeField] private bool enableWater = true;
    [SerializeField] private Material waterMaterial;
    [SerializeField] private float waterLevel = 0f;
    
    [Header("Optimization")]
    [SerializeField] private bool useMultithreading = true;
    [SerializeField] private float viewDistance = 200f;
    [SerializeField] private float updateInterval = 0.2f;

    private Dictionary<Vector2Int, TerrainChunk> activeChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private Dictionary<Vector2Int, ChunkData> chunkDataCache = new Dictionary<Vector2Int, ChunkData>();
    private Queue<TerrainChunk> chunkPool = new Queue<TerrainChunk>();
    [SerializeField] private Transform viewer;
    private Texture2D gradientTexture;
    private float lastUpdateTime;
    private Vector2Int lastViewerChunk;
    private GrassGenerator grassGenerator;
    private GameObject waterPlane;
    
    [Header("Object Placement")]
    [SerializeField] private GameObject energyCore;
    [SerializeField] private GameObject terrainScanner;
    [SerializeField] private GameObject boxPrefab;
    
    [SerializeField] private float boxDensity = 0.005f;
    [SerializeField] private float minBoxHeight = 0.5f;
    [SerializeField] private float centerExclusionRadius = 30f;
    [SerializeField] private float minBoxSpacing = 8f;
    [SerializeField] private int maxConsecutiveFailures = 1000;

    private List<Vector3> spawnedBoxPositions = new List<Vector3>();
    
    [Header("Seed Settings")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private string seedString = "";
    private int numericSeed;
    
    private float[,] falloffMap;
    
    // ★ 동적 풀 생성용
    private float lastGrassCheckTime;
    private HashSet<Vector2Int> processedGrassChunks = new HashSet<Vector2Int>();
    private bool isInitialGrassComplete = false;

    public struct GrassChunkInfo
    {
        public Vector2Int coord;
        public ChunkData data;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    private void Start()
    {
        if (useRandomSeed || string.IsNullOrEmpty(seedString))
        {
            seedString = GenerateRandomSeedString(16);
            Debug.Log($"🌍 World Seed: {seedString}");
        }

        numericSeed = SeedStringToInt(seedString);
        Debug.Log($"Numeric seed: {numericSeed}");
    
        grassGenerator = GetComponent<GrassGenerator>();
    
        if (grassGenerator == null)
        {
            Debug.LogWarning("GrassGenerator component not found! Grass will not be generated.");
        }
    
        mapSize = worldSizeInChunks * chunkSize;
    
        if (useFalloff)
        {
            GenerateFalloffMap();
        }
    
        CreateGradientTexture();
        SetupMaterial();
    
        GenerateWorld();
    
        if (enableWater && waterMaterial != null)
        {
            CreateWaterPlane();
        }
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateVisibleChunks();
            lastUpdateTime = Time.time;
        }
        
        if (isInitialGrassComplete && GenerateGrass && grassGenerator != null && 
            Time.time - lastGrassCheckTime > grassCheckInterval)
        {
            CheckAndGenerateNearbyGrass();
            lastGrassCheckTime = Time.time;
        }
    }
    
    public void SetPlayer(Transform player)
    {
        viewer = player;
        Debug.Log($"[TerrainGenerator] Player set: {player.name}");
    }
    
    private async void CheckAndGenerateNearbyGrass()
{
    if (viewer == null) return;
    
    List<GrassChunkInfo> chunksToGenerate = new List<GrassChunkInfo>();
    
    foreach (var kvp in chunkDataCache)
    {
        Vector2Int coord = kvp.Key;
        
        if (processedGrassChunks.Contains(coord)) continue;
        
        if (activeChunks.TryGetValue(coord, out TerrainChunk chunk))
        {
            float distanceFromPlayer = Vector3.Distance(chunk.transform.position, viewer.position);
            
            if (distanceFromPlayer <= dynamicGrassDistance)
            {
                chunksToGenerate.Add(new GrassChunkInfo
                {
                    coord = coord,
                    data = kvp.Value,
                    position = chunk.transform.position,
                    rotation = chunk.transform.rotation,
                    scale = chunk.transform.localScale
                });
            }
        }
    }
    
    if (chunksToGenerate.Count == 0) return;
    
    if (sortByDistance)
    {
        chunksToGenerate.Sort((a, b) =>
        {
            float distA = Vector3.Distance(a.position, viewer.position);
            float distB = Vector3.Distance(b.position, viewer.position);
            return distA.CompareTo(distB);
        });
    }
    
    int processCount = Mathf.Min(maxDynamicChunksPerUpdate, chunksToGenerate.Count);
    
    Debug.Log($"<color=cyan>[Dynamic Grass] Processing {processCount}/{chunksToGenerate.Count} chunks near player...</color>");
    
    for (int i = 0; i < processCount; i++)
    {
        GrassChunkInfo info = chunksToGenerate[i];
        
        // 생성
        grassGenerator.GenerateGrassForChunkOptimized(info);
        processedGrassChunks.Add(info.coord);
        
        await Task.Yield();
    }
    
    await grassGenerator.ApplyAdditionalGrass();
    
    int remainingChunks = chunksToGenerate.Count - processCount;
    if (remainingChunks > 0)
    {
        Debug.Log($"<color=yellow>[Dynamic Grass] {remainingChunks} chunks deferred to next update</color>");
    }
}
    private string GenerateRandomSeedString(int length)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder(length);
        System.Random random = new System.Random();
    
        for (int i = 0; i < length; i++)
        {
            result.Append(random.Next(0, 10));
        }
    
        return result.ToString();
    }

    private int SeedStringToInt(string seed)
    {
        if (string.IsNullOrEmpty(seed))
            return 0;
    
        const uint FNV_prime = 16777619;
        uint hash = 2166136261;
    
        foreach (char c in seed)
        {
            hash ^= c;
            hash *= FNV_prime;
        }
    
        return (int)hash;
    }
    
    private void PlaceEnergyCoreObject()
    {
        Vector3 rayStart = new Vector3(0, 100f, 0);
    
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1000f, LayerMask.GetMask("Ground")))
        {
            GameObject obj = Instantiate(energyCore, hit.point + new Vector3(0, 3, 0), Quaternion.identity);
            obj.transform.up = hit.normal;
            Debug.Log($"Center object placed at height: {hit.point.y}");
        }
    }
    
    private void PlaceTerrainScanner()
    {
        Vector3 rayStart = new Vector3(0, 100f, 0);
    
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1000f, LayerMask.GetMask("Ground")))
        {
            GameObject obj = Instantiate(terrainScanner, hit.point + new Vector3(0, 1, 0), Quaternion.identity);
            obj.transform.up = hit.normal;
        }
    }

    private void GenerateFalloffMap()
    {
        int totalSize = worldSizeInChunks * chunkSize + 1;
        falloffMap = new float[totalSize, totalSize];

        for (int y = 0; y < totalSize; y++)
        {
            for (int x = 0; x < totalSize; x++)
            {
                float xNorm = x / (float)(totalSize - 1);
                float yNorm = y / (float)(totalSize - 1);
            
                float distX = Mathf.Abs(xNorm * 2 - 1);
                float distY = Mathf.Abs(yNorm * 2 - 1);
            
                float value = Mathf.Max(distX, distY);
                
                float centerDist = Mathf.Sqrt(
                    Mathf.Pow(xNorm - 0.5f, 2) + 
                    Mathf.Pow(yNorm - 0.5f, 2)
                ) * 2f;
                
                float centerProtection = Mathf.Lerp(centerFalloffReduction, 1f, centerDist);
                
                falloffMap[x, y] = Evaluate(value) * centerProtection;
            }
        }

        Debug.Log($"Falloff map complete with center protection.");
    }

    private float Evaluate(float value)
    {
        return Mathf.Pow(value, falloffStrength);
    }

    private void CreateWaterPlane()
    {
        if (waterPlane != null)
        {
            Destroy(waterPlane);
        }

        waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "Water";
        waterPlane.transform.parent = transform;
        waterPlane.layer = LayerMask.NameToLayer("Water");
    
        waterPlane.transform.position = new Vector3(0, waterLevel, 0);
    
        float scale = mapSize * 10f;
        waterPlane.transform.localScale = new Vector3(scale, 1, scale);
    
        MeshRenderer renderer = waterPlane.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = waterMaterial;
        }
    
        MeshCollider meshCollider = waterPlane.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            Destroy(meshCollider);
        }
    
        BoxCollider boxCollider = waterPlane.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3(10f, 0.1f, 10f);
    }

    private void CreateGradientTexture()
    {
        gradientTexture = new Texture2D(1, 256);
        Color[] pixels = new Color[256];

        for (int i = 0; i < 256; i++)
        {
            pixels[i] = terrainGradient.Evaluate(i / 255f);
        }

        gradientTexture.SetPixels(pixels);
        gradientTexture.wrapMode = TextureWrapMode.Clamp;
        gradientTexture.filterMode = FilterMode.Bilinear;
        gradientTexture.Apply();
    }

    private void SetupMaterial()
    {
        if (terrainMaterial != null)
        {
            terrainMaterial.SetTexture("_TerrainGradient", gradientTexture);
            if (baseTexture != null)
            {
                terrainMaterial.SetTexture("_MainTex", baseTexture);
            }
        }
    }

    private async void GenerateWorld()
    {
        LoadingUI.Instance?.ShowLoading("Generating Terrain...");
        await Task.Yield();

        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (useMultithreading)
        {
            await GenerateWorldMultithreaded();
        }
        else
        {
            await GenerateWorldSingleThreaded();
        }

        stopwatch.Stop();

        LoadingUI.Instance?.UpdateProgress(1f, "Complete!");
        await Task.Delay(300);
    
        if (energyCore != null)
        {
            PlaceEnergyCoreObject();
        }

        // if (terrainScanner != null)
        // {
        //     PlaceTerrainScanner();
        // }
    
        if (boxPrefab != null)
        {
            LoadingUI.Instance?.UpdateProgress(1f, "Placing objects...");
            await Task.Yield();
            await Task.Delay(100);
            PlaceBoxes();
        }
    
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTerrainGenerationComplete();
        }
    
        await Task.Delay(200);
        LoadingUI.Instance?.HideLoading();
    
        Debug.Log($"Terrain generation complete in {stopwatch.ElapsedMilliseconds}ms");
    }

    private async Task GenerateWorldMultithreaded()
    {
        List<Task<ChunkData>> tasks = new List<Task<ChunkData>>();

        int halfSize = worldSizeInChunks / 2;
        int totalChunks = worldSizeInChunks * worldSizeInChunks;

        LoadingUI.Instance?.UpdateProgress(0.05f, "Generating chunk data...");
        await Task.Yield();

        for (int x = -halfSize; x < halfSize; x++)
        {
            for (int z = -halfSize; z < halfSize; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                tasks.Add(Task.Run(() => GenerateChunkData(coord)));
            }
        }

        ChunkData[] results = await Task.WhenAll(tasks);

        LoadingUI.Instance?.UpdateProgress(0.25f, "Creating terrain meshes...");

        int processedChunks = 0;
        foreach (ChunkData data in results)
        {
            CreateChunkFromData(data, false);
            processedChunks++;
            
            if (processedChunks % 50 == 0)
            {
                float meshProgress = 0.25f + (processedChunks / (float)totalChunks) * 0.45f;
                LoadingUI.Instance?.UpdateProgress(meshProgress, $"Creating terrain... {processedChunks}/{totalChunks}");
                await Task.Yield();
            }
        }

        UpdateMaterialHeightBounds();

        LoadingUI.Instance?.UpdateProgress(0.7f, "Terrain ready!");
        Debug.Log("=== TERRAIN READY ===");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTerrainReady();
        }

        // ✅ 풀 생성 - 중앙만!
        if (GenerateGrass && grassGenerator != null)
        {
            LoadingUI.Instance?.UpdateProgress(0.75f, "Generating grass (center)...");
            
            await GenerateInitialGrassOnly();
            
            LoadingUI.Instance?.UpdateProgress(0.85f, "Applying grass...");
            Debug.Log("=== Applying initial grass ===");
            
            await grassGenerator.ApplyInitialGrass();
            
            isInitialGrassComplete = true; // ★ 초기 풀 완료
            
            LoadingUI.Instance?.UpdateProgress(0.95f, "Almost ready...");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAlmostReady();
            }
            
            await Task.Delay(200);
        }
        else
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAlmostReady();
            }
        }
        
        LoadingUI.Instance?.UpdateProgress(0.99f, "Complete!");
    }

    // ✅ 초기 생성 (중앙만)
    private async Task GenerateInitialGrassOnly()
    {
        int totalChunks = chunkDataCache.Count;
        int processedChunks = 0;
        int skippedChunks = 0;
        
        Debug.Log($"Generating grass for center area (radius: {initialGrassRadius}m)...");

        foreach (var kvp in chunkDataCache)
        {
            if (activeChunks.TryGetValue(kvp.Key, out TerrainChunk chunk))
            {
                float distanceFromCenter = Vector3.Distance(chunk.transform.position, Vector3.zero);
                
                if (distanceFromCenter <= initialGrassRadius)
                {
                    GrassChunkInfo info = new GrassChunkInfo
                    {
                        coord = kvp.Key,
                        data = kvp.Value,
                        position = chunk.transform.position,
                        rotation = chunk.transform.rotation,
                        scale = chunk.transform.localScale
                    };
                    
                    grassGenerator.GenerateGrassForChunkOptimized(info);
                    processedGrassChunks.Add(kvp.Key); // ★ 완료 표시
                    processedChunks++;
                }
                else
                {
                    skippedChunks++;
                }
            }
            
            if ((processedChunks + skippedChunks) % 10 == 0)
            {
                float progress = 0.75f + ((processedChunks + skippedChunks) / (float)totalChunks) * 0.1f;
                LoadingUI.Instance?.UpdateProgress(progress, $"Grass... {processedChunks}/{totalChunks}");
                await Task.Yield();
            }
        }

        Debug.Log($"<color=green>✓ Initial grass: {processedChunks} generated, {skippedChunks} deferred</color>");
    }

    private async Task GenerateWorldSingleThreaded()
    {
        int halfSize = worldSizeInChunks / 2;
        int totalChunks = worldSizeInChunks * worldSizeInChunks;
        int currentChunk = 0;

        LoadingUI.Instance?.UpdateProgress(0.1f, "Generating terrain...");

        for (int x = -halfSize; x < halfSize; x++)
        {
            for (int z = -halfSize; z < halfSize; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                ChunkData data = GenerateChunkData(coord);
                CreateChunkFromData(data, false);

                currentChunk++;
                
                if (currentChunk % 50 == 0)
                {
                    float progress = 0.1f + (currentChunk / (float)totalChunks) * 0.6f;
                    LoadingUI.Instance?.UpdateProgress(progress, $"Creating terrain... {currentChunk}/{totalChunks}");
                    await Task.Yield();
                }
            }
        }

        UpdateMaterialHeightBounds();

        LoadingUI.Instance?.UpdateProgress(0.7f, "Terrain ready!");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTerrainReady();
        }

        if (GenerateGrass && grassGenerator != null)
        {
            LoadingUI.Instance?.UpdateProgress(0.75f, "Generating grass...");
            
            await GenerateInitialGrassOnly();
            
            LoadingUI.Instance?.UpdateProgress(0.9f, "Applying grass...");
            await grassGenerator.ApplyInitialGrass();
            
            isInitialGrassComplete = true;
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAlmostReady();
            }
        }
        else
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAlmostReady();
            }
        }

        LoadingUI.Instance?.UpdateProgress(1f, "Complete!");
    }

    private void PlaceBoxes()
    {
        float totalArea = mapSize * mapSize;
        int targetBoxCount = Mathf.RoundToInt(totalArea * boxDensity);
        
        spawnedBoxPositions.Clear();
        System.Random random = new System.Random(numericSeed);
        
        int successfulPlacements = 0;
        int totalAttempts = 0;
        int consecutiveFailures = 0;
        
        float halfMapSize = mapSize * 0.5f;
        int groundLayer = LayerMask.GetMask("Ground");
        
        if (groundLayer == 0)
        {
            Debug.LogError("Ground layer not found!");
            return;
        }
        
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        while (successfulPlacements < targetBoxCount)
        {
            totalAttempts++;
            
            if (consecutiveFailures >= maxConsecutiveFailures || stopwatch.ElapsedMilliseconds > 10000)
            {
                break;
            }
            
            float randomX = (float)(random.NextDouble() * mapSize - halfMapSize);
            float randomZ = (float)(random.NextDouble() * mapSize - halfMapSize);
            
            if (Vector3.Distance(new Vector3(randomX, 0, randomZ), Vector3.zero) < centerExclusionRadius)
            {
                consecutiveFailures++;
                continue;
            }
            
            bool tooClose = false;
            foreach (Vector3 existingPos in spawnedBoxPositions)
            {
                if (Vector2.Distance(new Vector2(randomX, randomZ), new Vector2(existingPos.x, existingPos.z)) < minBoxSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (tooClose)
            {
                consecutiveFailures++;
                continue;
            }
            
            Vector3 rayStart = new Vector3(randomX, 500f, randomZ);
            
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1000f, groundLayer))
            {
                if (hit.point.y < minBoxHeight)
                {
                    consecutiveFailures++;
                    continue;
                }
                
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > 45f)
                {
                    consecutiveFailures++;
                    continue;
                }
                
                Vector3 spawnPosition = hit.point + Vector3.up * 0.5f;
                GameObject box = Instantiate(boxPrefab, spawnPosition, Quaternion.identity);
                
                if (slopeAngle < 30f)
                {
                    box.transform.up = hit.normal;
                }
                else
                {
                    box.transform.up = Vector3.Slerp(Vector3.up, hit.normal, 0.5f);
                }
                
                box.name = $"Box_{successfulPlacements + 1}";
                
                spawnedBoxPositions.Add(hit.point);
                successfulPlacements++;
                consecutiveFailures = 0;
            }
            else
            {
                consecutiveFailures++;
            }
        }
        
        stopwatch.Stop();
        Debug.Log($"<color=green>Successfully placed: {successfulPlacements}/{targetBoxCount} boxes in {stopwatch.ElapsedMilliseconds}ms</color>");
    }

    private ChunkData GenerateChunkData(Vector2Int chunkCoord)
    {
        ChunkData data = new ChunkData(chunkCoord, chunkSize);
        
        Vector2 offset = new Vector2(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize);
        System.Random prng = new System.Random(numericSeed);
        Vector2[] octaveOffsets = new Vector2[octavesCount];

        for (int i = 0; i < octavesCount; i++)
        {
            octaveOffsets[i] = new Vector2(
                prng.Next(-100000, 100000),
                prng.Next(-100000, 100000)
            );
        }

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        int halfWorldSize = (worldSizeInChunks * chunkSize) / 2;
        
        int vertexIndex = 0;
        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                float height = CalculateHeight(x + offset.x, z + offset.y, octaveOffsets);
                
                if (useFalloff && falloffMap != null)
                {
                    float worldX = x + offset.x;
                    float worldZ = z + offset.y;
                    
                    int falloffX = Mathf.RoundToInt(worldX + halfWorldSize);
                    int falloffZ = Mathf.RoundToInt(worldZ + halfWorldSize);
                    
                    int totalSize = worldSizeInChunks * chunkSize + 1;
                    
                    if (falloffX >= 0 && falloffX < totalSize && falloffZ >= 0 && falloffZ < totalSize)
                    {
                        float falloffValue = falloffMap[falloffX, falloffZ];
                        height = Mathf.Lerp(height, -heightMultiplier * 1.5f, falloffValue);
                    }
                    else
                    {
                        height = -heightMultiplier;
                    }
                }
                
                data.vertices[vertexIndex] = new Vector3(x, height, z);
                data.heightMap[x, z] = height;
                data.uvs[vertexIndex] = new Vector2((float)x / chunkSize, (float)z / chunkSize);

                if (height < minHeight) minHeight = height;
                if (height > maxHeight) maxHeight = height;

                vertexIndex++;
            }
        }

        int triangleIndex = 0;
        int vertex = 0;

        for (int z = 0; z < chunkSize; z++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                data.triangles[triangleIndex + 0] = vertex + 0;
                data.triangles[triangleIndex + 1] = vertex + chunkSize + 1;
                data.triangles[triangleIndex + 2] = vertex + 1;

                data.triangles[triangleIndex + 3] = vertex + 1;
                data.triangles[triangleIndex + 4] = vertex + chunkSize + 1;
                data.triangles[triangleIndex + 5] = vertex + chunkSize + 2;

                vertex++;
                triangleIndex += 6;
            }
            vertex++;
        }

        data.bounds = new Bounds(
            new Vector3(chunkSize / 2f, (minHeight + maxHeight) / 2f, chunkSize / 2f),
            new Vector3(chunkSize, maxHeight - minHeight, chunkSize)
        );

        return data;
    }

    private float CalculateHeight(float x, float z, Vector2[] octaveOffsets)
    {
        float height = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octavesCount; i++)
        {
            float sampleX = (x + octaveOffsets[i].x) * noiseScale * frequency;
            float sampleZ = (z + octaveOffsets[i].y) * noiseScale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1;
            height += perlinValue * amplitude;

            amplitude *= persistance;
            frequency *= lacunarity;
        }

        return height * heightMultiplier;
    }
    
    private void CreateChunkFromData(ChunkData data, bool generateGrass = false)
    {
        TerrainChunk chunk = GetChunkFromPool();

        Vector3 position = new Vector3(
            data.chunkCoord.x * chunkSize,
            0,
            data.chunkCoord.y * chunkSize
        );

        chunk.transform.position = position;
        chunk.Initialize(data.chunkCoord, terrainMaterial);
        chunk.ApplyMeshData(data);
    
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer == -1)
        {
            Debug.LogError("Ground layer does not exist!");
        }
        else
        {
            chunk.gameObject.layer = groundLayer;
        }

        activeChunks[data.chunkCoord] = chunk;
        chunkDataCache[data.chunkCoord] = data;
    }

    private TerrainChunk GetChunkFromPool()
    {
        if (chunkPool.Count > 0)
        {
            TerrainChunk chunk = chunkPool.Dequeue();
            chunk.SetActive(true);
            return chunk;
        }

        GameObject chunkObject = new GameObject();
        chunkObject.transform.parent = transform;
        return chunkObject.AddComponent<TerrainChunk>();
    }

    private void UpdateVisibleChunks()
    {
        if (viewer == null) return;

        Vector2Int currentViewerChunk = new Vector2Int(
            Mathf.RoundToInt(viewer.position.x / chunkSize),
            Mathf.RoundToInt(viewer.position.z / chunkSize)
        );

        if (currentViewerChunk == lastViewerChunk) return;
        lastViewerChunk = currentViewerChunk;

        int chunksVisibleInViewDst = Mathf.RoundToInt(viewDistance / chunkSize);

        foreach (var chunk in activeChunks.Values)
        {
            float distance = Vector2.Distance(
                new Vector2(chunk.chunkCoord.x, chunk.chunkCoord.y),
                new Vector2(currentViewerChunk.x, currentViewerChunk.y)
            );

            chunk.SetActive(distance <= chunksVisibleInViewDst);
        }
    }

    private void UpdateMaterialHeightBounds()
    {
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        foreach (var chunk in activeChunks.Values)
        {
            Mesh mesh = chunk.GetComponent<MeshFilter>().sharedMesh;
            if (mesh != null)
            {
                float chunkMin = mesh.bounds.min.y + chunk.transform.position.y;
                float chunkMax = mesh.bounds.max.y + chunk.transform.position.y;

                if (chunkMin < minHeight) minHeight = chunkMin;
                if (chunkMax > maxHeight) maxHeight = chunkMax;
            }
        }

        if (terrainMaterial != null)
        {
            terrainMaterial.SetFloat("_MinTerrainHeight", minHeight);
            terrainMaterial.SetFloat("_MaxTerrainHeight", maxHeight);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (viewer == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(viewer.position, viewDistance);
        
        if (enableWater)
        {
            Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireCube(new Vector3(0, waterLevel, 0), new Vector3(mapSize, 0.1f, mapSize));
        }
        
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(Vector3.zero, centerExclusionRadius);
        
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(Vector3.zero, initialGrassRadius);
        
        if (viewer != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawWireSphere(viewer.position, dynamicGrassDistance);
        }
        
        Gizmos.color = Color.green;
        foreach (Vector3 pos in spawnedBoxPositions)
        {
            Gizmos.DrawWireSphere(pos, 1f);
        }
    }
}