using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class OptimizedTerrainGenerator : MonoBehaviour
{
    [Header("World Settings")]
    [SerializeField] private int worldSizeInChunks = 10;
    public int chunkSize = 32;
    
    [Header("Noise Settings")]
    [SerializeField] private float noiseScale = 0.03f;
    [SerializeField] private float heightMultiplier = 7;
    [SerializeField] private int octavesCount = 4;
    [SerializeField] private float lacunarity = 2;
    [SerializeField] private float persistance = 0.5f;
    [SerializeField] private int seed = 0;
    
    [Header("Visual Settings")]
    [SerializeField] private Gradient terrainGradient;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private Texture2D baseTexture;
    
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

    private void Start()
    {
        //viewer = Camera.main.transform;
        grassGenerator = GetComponent<GrassGenerator>();
        
        if (grassGenerator == null)
        {
            Debug.LogWarning("GrassGenerator component not found! Grass will not be generated.");
        }
        
        CreateGradientTexture();
        SetupMaterial();
        
        GenerateWorld();
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateVisibleChunks();
            lastUpdateTime = Time.time;
        }
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

        // 1단계: 청크 데이터 생성 (멀티스레드)
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

        // 2단계: 메인 스레드에서 지형 메시 생성
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

        // 3단계: 모든 지형이 완성된 후 풀 생성
        LoadingUI.Instance?.UpdateProgress(0.7f, "Generating grass...");
        
        if (grassGenerator != null)
        {
            await GenerateAllGrass();
            
            // ★★★ 중요: 모든 풀 데이터를 GrassComputeScript에 적용 ★★★
            grassGenerator.ApplyAllGrass();
        }

        LoadingUI.Instance?.UpdateProgress(1f, "Complete!");
        await Task.Delay(300);
    }

    private void GenerateWorldSingleThreaded()
    {
        int halfSize = worldSizeInChunks / 2;
        int totalChunks = worldSizeInChunks * worldSizeInChunks;
        int currentChunk = 0;

        LoadingUI.Instance?.UpdateProgress(0.1f, "Generating terrain...");

        // 1단계: 지형 생성
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

        // 2단계: 풀 생성
        LoadingUI.Instance?.UpdateProgress(0.7f, "Generating grass...");
        
        if (grassGenerator != null)
        {
            GenerateAllGrassSync();
            
            // ★★★ 중요: 모든 풀 데이터를 GrassComputeScript에 적용 ★★★
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

        // 버텍스 생성
        int vertexIndex = 0;
        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                float height = CalculateHeight(x + offset.x, z + offset.y, octaveOffsets);
                
                data.vertices[vertexIndex] = new Vector3(x, height, z);
                data.heightMap[x, z] = height;

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
    
        // 개별 청크 풀 생성은 GenerateAllGrass에서 일괄 처리
        // generateGrass 파라미터는 더 이상 사용하지 않음
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
    }
}