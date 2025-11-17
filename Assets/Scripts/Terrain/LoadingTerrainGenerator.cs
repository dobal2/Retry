using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class LoadingTerrainGenerator : MonoBehaviour
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
    
    [Header("Falloff Settings")]
    [SerializeField] private bool useFalloff = true;
    [SerializeField] private float falloffStrength = 3f;
    [SerializeField] private float centerFalloffReduction = 0.7f;
    
    [Header("Visual Settings")]
    [SerializeField] private Gradient terrainGradient;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private Texture2D baseTexture;

    [Header("Grass")] 
    [SerializeField] private bool GenerateGrass = true;
    [SerializeField] private float grassRadius = 100f;
    [SerializeField] private GrassComputeScript grassComputeScript; // ★ 직접 참조
    
    [Header("Water Settings")]
    [SerializeField] private bool enableWater = true;
    [SerializeField] private Material waterMaterial;
    [SerializeField] private float waterLevel = 0f;
    
    [Header("Camera (No Player)")]
    [SerializeField] private Camera loadingCamera; // ★ 로딩용 카메라
    
    [Header("Optimization")]
    [SerializeField] private bool useMultithreading = true;

    private Dictionary<Vector2Int, TerrainChunk> activeChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private Dictionary<Vector2Int, ChunkData> chunkDataCache = new Dictionary<Vector2Int, ChunkData>();
    private Queue<TerrainChunk> chunkPool = new Queue<TerrainChunk>();
    
    private Texture2D gradientTexture;
    private GrassGenerator grassGenerator;
    private GameObject waterPlane;
    
    [Header("Seed Settings")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private string seedString = "";
    private int numericSeed;
    
    private float[,] falloffMap;
    private HashSet<Vector2Int> processedGrassChunks = new HashSet<Vector2Int>();

    private void Start()
    {
        if (useRandomSeed || string.IsNullOrEmpty(seedString))
        {
            seedString = GenerateRandomSeedString(16);
            Debug.Log($"🌍 Loading World Seed: {seedString}");
        }

        numericSeed = SeedStringToInt(seedString);
        grassGenerator = GetComponent<GrassGenerator>();
        
        // ★ 카메라 자동 찾기 (할당 안 됐으면)
        if (loadingCamera == null)
        {
            loadingCamera = Camera.main;
        }
        
        // ★ GrassComputeScript에 카메라 설정
        if (GenerateGrass && grassComputeScript != null && loadingCamera != null)
        {
            // GrassComputeScript의 카메라 필드 설정 (public이면 직접, 아니면 메서드로)
            SetGrassCamera();
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
    
    // ★ GrassComputeScript에 카메라 설정
    private void SetGrassCamera()
    {
        if (grassComputeScript == null || loadingCamera == null) return;
        
        // GrassComputeScript에 mainCamera 필드가 있다고 가정
        // 리플렉션 또는 직접 접근
        var cameraField = grassComputeScript.GetType().GetField("mainCamera", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (cameraField != null)
        {
            cameraField.SetValue(grassComputeScript, loadingCamera);
            Debug.Log($"<color=green>✓ Grass camera set to: {loadingCamera.name}</color>");
        }
        else
        {
            // 다른 방법: cam 또는 _camera 등 다른 이름일 수 있음
            var camField = grassComputeScript.GetType().GetField("cam", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (camField != null)
            {
                camField.SetValue(grassComputeScript, loadingCamera);
                Debug.Log($"<color=green>✓ Grass camera set to: {loadingCamera.name}</color>");
            }
            else
            {
                Debug.LogWarning("Could not find camera field in GrassComputeScript. Add public SetCamera method or check field name.");
            }
        }
    }

    public void SetCamera(Camera cam)
    {
        loadingCamera = cam;
        Debug.Log($"[LoadingTerrainGenerator] Camera set: {cam.name}");
        
        // ★ 카메라 변경 시 GrassComputeScript도 업데이트
        SetGrassCamera();
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
        waterPlane.name = "LoadingWater";
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
    
        Collider col = waterPlane.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
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
        Debug.Log($"Loading terrain complete in {stopwatch.ElapsedMilliseconds}ms");
    }

    private async Task GenerateWorldMultithreaded()
    {
        List<Task<ChunkData>> tasks = new List<Task<ChunkData>>();
        int halfSize = worldSizeInChunks / 2;

        for (int x = -halfSize; x < halfSize; x++)
        {
            for (int z = -halfSize; z < halfSize; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                tasks.Add(Task.Run(() => GenerateChunkData(coord)));
            }
        }

        ChunkData[] results = await Task.WhenAll(tasks);

        int processedChunks = 0;
        foreach (ChunkData data in results)
        {
            CreateChunkFromData(data);
            processedChunks++;
            
            if (processedChunks % 50 == 0)
            {
                await Task.Yield();
            }
        }

        UpdateMaterialHeightBounds();

        if (GenerateGrass && grassGenerator != null)
        {
            await GenerateGrassForLoading();
            await grassGenerator.ApplyInitialGrass();
        }
    }

    private async Task GenerateGrassForLoading()
    {
        int processedChunks = 0;
        
        foreach (var kvp in chunkDataCache)
        {
            if (activeChunks.TryGetValue(kvp.Key, out TerrainChunk chunk))
            {
                float distanceFromCenter = Vector3.Distance(chunk.transform.position, Vector3.zero);
                
                if (distanceFromCenter <= grassRadius)
                {
                    OptimizedTerrainGenerator.GrassChunkInfo info = new OptimizedTerrainGenerator.GrassChunkInfo
                    {
                        coord = kvp.Key,
                        data = kvp.Value,
                        position = chunk.transform.position,
                        rotation = chunk.transform.rotation,
                        scale = chunk.transform.localScale
                    };
                    
                    grassGenerator.GenerateGrassForChunkOptimized(info);
                    processedGrassChunks.Add(kvp.Key);
                    processedChunks++;
                }
            }
            
            if (processedChunks % 10 == 0)
            {
                await Task.Yield();
            }
        }

        Debug.Log($"<color=cyan>Loading grass generated: {processedChunks} chunks</color>");
    }

    private async Task GenerateWorldSingleThreaded()
    {
        int halfSize = worldSizeInChunks / 2;
        int currentChunk = 0;

        for (int x = -halfSize; x < halfSize; x++)
        {
            for (int z = -halfSize; z < halfSize; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                ChunkData data = GenerateChunkData(coord);
                CreateChunkFromData(data);

                currentChunk++;
                
                if (currentChunk % 50 == 0)
                {
                    await Task.Yield();
                }
            }
        }

        UpdateMaterialHeightBounds();

        if (GenerateGrass && grassGenerator != null)
        {
            await GenerateGrassForLoading();
            await grassGenerator.ApplyInitialGrass();
        }
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
    
    private void CreateChunkFromData(ChunkData data)
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
        if (groundLayer != -1)
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

        GameObject chunkObject = new GameObject("LoadingChunk");
        chunkObject.transform.parent = transform;
        return chunkObject.AddComponent<TerrainChunk>();
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
        if (enableWater)
        {
            Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireCube(new Vector3(0, waterLevel, 0), new Vector3(mapSize, 0.1f, mapSize));
        }
        
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(Vector3.zero, grassRadius);
    }
}