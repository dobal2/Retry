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
    [SerializeField] private float falloffStrength = 3f; // 가장자리 감소 강도
    [SerializeField] private float falloffShift = 2.2f; // 감소 시작 지점
    
    [Header("Visual Settings")]
    [SerializeField] private Gradient terrainGradient;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private Texture2D baseTexture;

    [Header("Grass")] 
    [SerializeField] private bool GenerateGrass;
    
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
    [SerializeField]private Transform viewer;
    private Texture2D gradientTexture;
    private float lastUpdateTime;
    private Vector2Int lastViewerChunk;
    private GrassGenerator grassGenerator;
    private GameObject waterPlane;
    

    
    // Falloff 맵 캐시
    private float[,] falloffMap;

    private void Start()
    {
        grassGenerator = GetComponent<GrassGenerator>();
        
        if (grassGenerator == null)
        {
            Debug.LogWarning("GrassGenerator component not found! Grass will not be generated.");
        }
        
        mapSize = worldSizeInChunks * chunkSize;
        
        // Falloff 맵 생성
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
    }

    // ★ Falloff 맵 생성
    // ★ Falloff 맵 생성 (더 강력하게)
    // ★ Falloff 맵 생성 (완전히 새로 작성)
    private void GenerateFalloffMap()
    {
        int totalSize = worldSizeInChunks * chunkSize + 1;
        falloffMap = new float[totalSize, totalSize];
    
        Debug.Log($"Generating falloff map: {totalSize}x{totalSize}");
    
        for (int y = 0; y < totalSize; y++)
        {
            for (int x = 0; x < totalSize; x++)
            {
                // 0~1 범위로 정규화 (중심이 0.5, 가장자리가 0 또는 1)
                float xNorm = x / (float)(totalSize - 1);
                float yNorm = y / (float)(totalSize - 1);
            
                // 중심으로부터의 거리 (0~1 범위, 중심이 0, 가장자리가 1)
                float distX = Mathf.Abs(xNorm * 2 - 1); // 0~1
                float distY = Mathf.Abs(yNorm * 2 - 1); // 0~1
            
                // 사각형 falloff (더 예측 가능)
                float value = Mathf.Max(distX, distY);
            
                falloffMap[x, y] = Evaluate(value);
            }
        }
    
        // 디버그 출력
        Debug.Log($"Falloff map complete.");
        Debug.Log($"Center (should be ~0): {falloffMap[totalSize/2, totalSize/2]:F3}");
        Debug.Log($"Edge (should be ~1): {falloffMap[0, 0]:F3}");
        Debug.Log($"Mid (should be ~0.5): {falloffMap[totalSize/4, totalSize/2]:F3}");
    }

// ★ Falloff 곡선 (더 간단하고 강력하게)
    private float Evaluate(float value)
    {
        // value는 0~1 (중심에서 가장자리로)
    
        // 방법 1: 간단한 제곱 (추천)
        return Mathf.Pow(value, falloffStrength);
    
        // 방법 2: Smoothstep (더 부드러움)
        // float t = Mathf.SmoothStep(0, 1, value);
        // return Mathf.Pow(t, falloffStrength);
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
        
        waterPlane.transform.position = new Vector3(0, waterLevel, 0);
        
        float scale = mapSize * 10f;
        waterPlane.transform.localScale = new Vector3(scale, 1, scale);
        
        MeshRenderer renderer = waterPlane.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = waterMaterial;
        }
        
        Collider collider = waterPlane.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        
        Debug.Log($"Water plane created at height {waterLevel} with size {mapSize}x{mapSize}");
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
        
        Debug.Log("Starting terrain generation...");
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (useMultithreading)
        {
            await GenerateWorldMultithreaded();
        }
        else
        {
            GenerateWorldSingleThreaded();
        }

        stopwatch.Stop();
        Debug.Log($"Terrain generation completed in {stopwatch.ElapsedMilliseconds}ms");
        
        LoadingUI.Instance?.HideLoading();
    }

    private async Task GenerateWorldMultithreaded()
    {
        List<Task<ChunkData>> tasks = new List<Task<ChunkData>>();

        int halfSize = worldSizeInChunks / 2;
        int totalChunks = worldSizeInChunks * worldSizeInChunks;

        LoadingUI.Instance?.UpdateProgress(0.1f, "Generating chunk data...");

        for (int x = -halfSize; x < halfSize; x++)
        {
            for (int z = -halfSize; z < halfSize; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                tasks.Add(Task.Run(() => GenerateChunkData(coord)));
            }
        }

        ChunkData[] results = await Task.WhenAll(tasks);

        LoadingUI.Instance?.UpdateProgress(0.3f, "Creating terrain meshes...");

        int processedChunks = 0;
        foreach (ChunkData data in results)
        {
            CreateChunkFromData(data, false);
            
            processedChunks++;
            float meshProgress = 0.3f + (processedChunks / (float)totalChunks) * 0.4f;
            LoadingUI.Instance?.UpdateProgress(meshProgress, $"Creating terrain... {processedChunks}/{totalChunks}");
            
            if (processedChunks % 10 == 0)
            {
                await Task.Yield();
            }
        }

        UpdateMaterialHeightBounds();

        if (GenerateGrass)
        {
            LoadingUI.Instance?.UpdateProgress(0.7f, "Generating grass...");
        
            if (grassGenerator != null)
            {
                await GenerateAllGrass();
                grassGenerator.ApplyAllGrass();
            }

            LoadingUI.Instance?.UpdateProgress(1f, "Complete!");
            await Task.Delay(300);    
        }
    }

    private void GenerateWorldSingleThreaded()
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
                float progress = 0.1f + (currentChunk / (float)totalChunks) * 0.6f;
                LoadingUI.Instance?.UpdateProgress(progress, $"Creating terrain... {currentChunk}/{totalChunks}");
            }
        }

        UpdateMaterialHeightBounds();

        LoadingUI.Instance?.UpdateProgress(0.7f, "Generating grass...");
        
        if (grassGenerator != null)
        {
            GenerateAllGrassSync();
            grassGenerator.ApplyAllGrass();
        }

        LoadingUI.Instance?.UpdateProgress(1f, "Complete!");
    }

    private async Task GenerateAllGrass()
    {
        int totalChunks = chunkDataCache.Count;
        int processedChunks = 0;

        Debug.Log($"Starting grass generation for {totalChunks} chunks...");

        foreach (var kvp in chunkDataCache)
        {
            Vector2Int coord = kvp.Key;
            ChunkData data = kvp.Value;

            if (activeChunks.TryGetValue(coord, out TerrainChunk chunk))
            {
                grassGenerator.GenerateGrassForChunk(coord, data, chunk.transform);
                
                processedChunks++;
                float progress = 0.7f + (processedChunks / (float)totalChunks) * 0.3f;
                LoadingUI.Instance?.UpdateProgress(progress, $"Generating grass... {processedChunks}/{totalChunks}");
                
                if (processedChunks % 10 == 0)
                {
                    await Task.Yield();
                }
            }
        }

        Debug.Log($"Grass generation complete! Total chunks: {processedChunks}");
    }

    private void GenerateAllGrassSync()
    {
        int totalChunks = chunkDataCache.Count;
        int processedChunks = 0;

        Debug.Log($"Starting grass generation for {totalChunks} chunks...");

        foreach (var kvp in chunkDataCache)
        {
            Vector2Int coord = kvp.Key;
            ChunkData data = kvp.Value;

            if (activeChunks.TryGetValue(coord, out TerrainChunk chunk))
            {
                grassGenerator.GenerateGrassForChunk(coord, data, chunk.transform);
                
                processedChunks++;
                float progress = 0.7f + (processedChunks / (float)totalChunks) * 0.3f;
                LoadingUI.Instance?.UpdateProgress(progress, $"Generating grass... {processedChunks}/{totalChunks}");
            }
        }

        Debug.Log($"Grass generation complete! Total chunks: {processedChunks}");
    }

    private ChunkData GenerateChunkData(Vector2Int chunkCoord)
    {
        ChunkData data = new ChunkData(chunkCoord, chunkSize);
        
        Vector2 offset = new Vector2(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize);
        System.Random prng = new System.Random(seed);
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

        // 월드 중심 계산
        int halfWorldSize = (worldSizeInChunks * chunkSize) / 2;
        
        int vertexIndex = 0;
        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                float height = CalculateHeight(x + offset.x, z + offset.y, octaveOffsets);
                
                // ★ Falloff 적용
                if (useFalloff && falloffMap != null)
                {
                    // 로컬 좌표를 월드 좌표로 변환 (맵 중심이 0,0)
                    float worldX = x + offset.x;
                    float worldZ = z + offset.y;
                    
                    // Falloff 맵 인덱스로 변환 (0 ~ totalSize)
                    int falloffX = Mathf.RoundToInt(worldX + halfWorldSize);
                    int falloffZ = Mathf.RoundToInt(worldZ + halfWorldSize);
                    
                    int totalSize = worldSizeInChunks * chunkSize + 1;
                    
                    // 범위 체크
                    if (falloffX >= 0 && falloffX < totalSize && falloffZ >= 0 && falloffZ < totalSize)
                    {
                        float falloffValue = falloffMap[falloffX, falloffZ];
                        
                        height = Mathf.Lerp(height, -heightMultiplier * 1.5f, falloffValue);
                        
                    }
                    else
                    {
                        // 범위 밖이면 강제로 아래로
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

        // 삼각형 생성
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
    }
}