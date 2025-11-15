using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GrassGeneratorCoroutine : MonoBehaviour
{
    [Header("Grass Settings")]
    [Range(0.01f, 10f)]
    public float generationDensity = 0.5f;
    
    [Header("Grass Size")]
    [Range(0.01f, 2f)]
    public float sizeWidth = 0.5f;
    [Range(0.01f, 2f)]
    public float sizeLength = 1f;
    
    [Header("Color Settings")]
    public Color baseColor = Color.green;
    [Range(0f, 1f)]
    public float rangeR = 0.1f;
    [Range(0f, 1f)]
    public float rangeG = 0.1f;
    [Range(0f, 1f)]
    public float rangeB = 0.1f;
    
    [Header("Normal Limit")]
    [Range(0f, 1f)]
    public float normalLimit = 0.5f;
    
    [Header("Height Constraints")]
    [SerializeField] private float waterLevel = -3f;
    [SerializeField] private AnimationCurve grassDensityByHeight = AnimationCurve.Linear(0, 1, 1, 1);
    
    [Header("References")]
    public GrassComputeScript grassCompute;
    
    [Header("Optimization - Coroutine")]
    [SerializeField] private bool batchGrassInstances = true;
    [SerializeField] private int maxGrassPerBatch = 5000;
    [SerializeField] private int chunksPerFrame = 10; // ★ 프레임당 처리할 청크 수
    [SerializeField] private int batchesPerFrame = 1; // ★ 프레임당 적용할 배치 수
    [SerializeField] private bool useTimeSlicing = true; // ★ 시간 분할 사용
    [SerializeField] private float maxFrameTime = 0.016f; // ★ 16ms = 60fps 유지
    
    [Header("Debug")]
    public bool showDebugInfo = true;

    private Dictionary<Vector2Int, List<GrassData>> chunkGrassData = new Dictionary<Vector2Int, List<GrassData>>();
    private List<GrassData> allGrassData = new List<GrassData>();
    private bool isApplied = false;
    private Coroutine applyCoroutine;
    
    public void GenerateGrassForChunk(Vector2Int chunkCoord, ChunkData chunkData, Transform chunkTransform)
    {
        if (chunkData == null)
        {
            Debug.LogError($"ChunkData is null for chunk {chunkCoord}");
            return;
        }

        List<GrassData> grassList = new List<GrassData>();
        
        int chunkSize = chunkData.chunkSize;
        float chunkArea = chunkSize * chunkSize;
        int numGrass = Mathf.FloorToInt(chunkArea * generationDensity);

        int attemptCount = 0;
        int successCount = 0;
        int maxAttempts = numGrass * 2;

        while (successCount < numGrass && attemptCount < maxAttempts)
        {
            attemptCount++;
            
            float localX = Random.Range(0f, chunkSize);
            float localZ = Random.Range(0f, chunkSize);
            
            float height = GetHeightAt(chunkData, localX, localZ);
            Vector3 normal = GetNormalAt(chunkData, localX, localZ);
            
            if (height <= waterLevel || normal.y < (1 - normalLimit))
                continue;
            
            Vector3 worldPos = chunkTransform.TransformPoint(new Vector3(localX, height, localZ));
            
            grassList.Add(new GrassData
            {
                position = worldPos,
                color = GetRandomColor(),
                length = new Vector2(sizeWidth, sizeLength),
                normal = normal
            });
            
            successCount++;
        }
        
        chunkGrassData[chunkCoord] = grassList;
    }
    
    // ★ Coroutine 버전 - Unity 전통 방식
    public void ApplyAllGrassCoroutine()
    {
        if (isApplied)
        {
            Debug.LogWarning("Grass already applied!");
            return;
        }
        
        if (applyCoroutine != null)
        {
            StopCoroutine(applyCoroutine);
        }
        
        applyCoroutine = StartCoroutine(ApplyGrassCoroutineInternal());
    }
    
    private IEnumerator ApplyGrassCoroutineInternal()
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        allGrassData.Clear();
        
        // ★ Step 1: 데이터 수집 (시간 분할)
        if (showDebugInfo)
            Debug.Log("Collecting grass data from chunks...");
        
        int chunksProcessed = 0;
        float startTime = Time.realtimeSinceStartup;
        
        foreach (var kvp in chunkGrassData)
        {
            allGrassData.AddRange(kvp.Value);
            chunksProcessed++;
            
            // ★ 시간 분할: 일정 시간 초과 시 프레임 양보
            if (useTimeSlicing)
            {
                if (Time.realtimeSinceStartup - startTime > maxFrameTime)
                {
                    yield return null;
                    startTime = Time.realtimeSinceStartup;
                }
            }
            else if (chunksProcessed % chunksPerFrame == 0)
            {
                yield return null;
            }
        }
        
        if (allGrassData.Count == 0)
        {
            Debug.LogWarning("No grass data to apply!");
            yield break;
        }
        
        if (showDebugInfo)
            Debug.Log($"<color=green>Collected {allGrassData.Count} grass instances</color>");
        
        // ★ Step 2: 배치 적용
        if (batchGrassInstances && allGrassData.Count > maxGrassPerBatch)
        {
            yield return StartCoroutine(ApplyGrassInBatchesCoroutine());
        }
        else
        {
            grassCompute.SetGrassPaintedDataList = allGrassData;
            grassCompute.Reset();
            yield return null;
        }
        
        isApplied = true;
        stopwatch.Stop();
        
        if (showDebugInfo)
            Debug.Log($"<color=cyan>Grass application complete in {stopwatch.ElapsedMilliseconds}ms</color>");
    }
    
    private IEnumerator ApplyGrassInBatchesCoroutine()
    {
        int batchCount = Mathf.CeilToInt((float)allGrassData.Count / maxGrassPerBatch);
        Debug.Log($"<color=yellow>Applying grass in {batchCount} batches</color>");
        
        List<GrassData> accumulatedData = new List<GrassData>(allGrassData.Count);
        int batchesApplied = 0;
        float startTime = Time.realtimeSinceStartup;
        
        for (int i = 0; i < batchCount; i++)
        {
            int startIndex = i * maxGrassPerBatch;
            int count = Mathf.Min(maxGrassPerBatch, allGrassData.Count - startIndex);
            
            // 배치 데이터 추가
            for (int j = 0; j < count; j++)
            {
                accumulatedData.Add(allGrassData[startIndex + j]);
            }
            
            // 적용
            grassCompute.SetGrassPaintedDataList = new List<GrassData>(accumulatedData);
            grassCompute.Reset();
            
            batchesApplied++;
            
            if (showDebugInfo && i % 5 == 0)
            {
                float progress = (i + 1) / (float)batchCount * 100f;
                Debug.Log($"Batch {i + 1}/{batchCount} - {accumulatedData.Count} instances ({progress:F1}%)");
            }
            
            // ★ 프레임 양보 조건
            if (useTimeSlicing)
            {
                // 시간 기반: 16ms 초과 시 양보
                if (Time.realtimeSinceStartup - startTime > maxFrameTime)
                {
                    yield return null;
                    startTime = Time.realtimeSinceStartup;
                }
            }
            else
            {
                // 배치 기반: N개 배치마다 양보
                if (batchesApplied >= batchesPerFrame)
                {
                    yield return null;
                    batchesApplied = 0;
                }
            }
        }
        
        Debug.Log($"<color=green>✓ Batch application complete: {accumulatedData.Count} instances</color>");
    }
    
    // ★ Task 버전도 제공 (호환성)
    public async Task ApplyAllGrassAsync()
    {
        if (isApplied)
        {
            Debug.LogWarning("Grass already applied!");
            return;
        }
        
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        allGrassData.Clear();
        
        int chunksProcessed = 0;
        foreach (var kvp in chunkGrassData)
        {
            allGrassData.AddRange(kvp.Value);
            chunksProcessed++;
            
            if (chunksProcessed % chunksPerFrame == 0)
                await Task.Yield();
        }
        
        if (allGrassData.Count == 0)
        {
            Debug.LogWarning("No grass data to apply!");
            return;
        }
        
        if (grassCompute != null)
        {
            if (batchGrassInstances && allGrassData.Count > maxGrassPerBatch)
            {
                await ApplyGrassInBatchesAsync();
            }
            else
            {
                grassCompute.SetGrassPaintedDataList = allGrassData;
                grassCompute.Reset();
                await Task.Yield();
            }
            
            isApplied = true;
        }
        
        stopwatch.Stop();
        if (showDebugInfo)
            Debug.Log($"<color=cyan>Async grass complete in {stopwatch.ElapsedMilliseconds}ms</color>");
    }
    
    private async Task ApplyGrassInBatchesAsync()
    {
        int batchCount = Mathf.CeilToInt((float)allGrassData.Count / maxGrassPerBatch);
        List<GrassData> accumulatedData = new List<GrassData>(allGrassData.Count);
        
        for (int i = 0; i < batchCount; i++)
        {
            int startIndex = i * maxGrassPerBatch;
            int count = Mathf.Min(maxGrassPerBatch, allGrassData.Count - startIndex);
            
            for (int j = 0; j < count; j++)
                accumulatedData.Add(allGrassData[startIndex + j]);
            
            grassCompute.SetGrassPaintedDataList = new List<GrassData>(accumulatedData);
            grassCompute.Reset();
            
            await Task.Yield();
            
            if ((i + 1) % 5 == 0)
                await Task.Delay(50);
        }
    }
    
    private float GetHeightAt(ChunkData chunkData, float x, float z)
    {
        int x0 = Mathf.FloorToInt(x);
        int z0 = Mathf.FloorToInt(z);
        int x1 = Mathf.Min(x0 + 1, chunkData.chunkSize);
        int z1 = Mathf.Min(z0 + 1, chunkData.chunkSize);
        
        float fx = x - x0;
        float fz = z - z0;
        
        float h00 = chunkData.heightMap[x0, z0];
        float h10 = chunkData.heightMap[x1, z0];
        float h01 = chunkData.heightMap[x0, z1];
        float h11 = chunkData.heightMap[x1, z1];
        
        float h0 = Mathf.Lerp(h00, h10, fx);
        float h1 = Mathf.Lerp(h01, h11, fx);
        
        return Mathf.Lerp(h0, h1, fz);
    }
    
    private Vector3 GetNormalAt(ChunkData chunkData, float x, float z)
    {
        int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, chunkData.chunkSize - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(z), 0, chunkData.chunkSize - 1);
        
        float heightL = chunkData.heightMap[Mathf.Max(0, x0 - 1), z0];
        float heightR = chunkData.heightMap[Mathf.Min(chunkData.chunkSize, x0 + 1), z0];
        float heightD = chunkData.heightMap[x0, Mathf.Max(0, z0 - 1)];
        float heightU = chunkData.heightMap[x0, Mathf.Min(chunkData.chunkSize, z0 + 1)];
        
        return new Vector3(heightL - heightR, 2f, heightD - heightU).normalized;
    }
    
    private Vector3 GetRandomColor()
    {
        return new Vector3(
            baseColor.r + Random.Range(0, 1.0f) * rangeR,
            baseColor.g + Random.Range(0, 1.0f) * rangeG,
            baseColor.b + Random.Range(0, 1.0f) * rangeB
        );
    }
    
    public void ClearAllGrass()
    {
        if (applyCoroutine != null)
        {
            StopCoroutine(applyCoroutine);
            applyCoroutine = null;
        }
        
        chunkGrassData.Clear();
        allGrassData.Clear();
        isApplied = false;
        
        if (grassCompute != null)
        {
            grassCompute.SetGrassPaintedDataList = new List<GrassData>();
            grassCompute.Reset();
        }
    }
    
    public bool IsGrassApplied() => isApplied;
}