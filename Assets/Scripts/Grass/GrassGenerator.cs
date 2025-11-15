using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GrassGenerator : MonoBehaviour
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
    
    [Header("References")]
    public GrassComputeScript grassCompute;
    
    [Header("Batch Update Settings")]
    [SerializeField] private int batchSizeBeforeUpdate = 10; // ★ N개 청크 모이면 한 번에 업데이트
    
    [Header("Debug")]
    public bool showDebugInfo = true;

    private Dictionary<Vector2Int, List<GrassData>> chunkGrassData = new Dictionary<Vector2Int, List<GrassData>>();
    private List<GrassData> allGrassData = new List<GrassData>();
    private HashSet<Vector2Int> appliedChunks = new HashSet<Vector2Int>(); // ★ 이미 적용된 청크 추적
    private List<Vector2Int> pendingChunks = new List<Vector2Int>(); // ★ 대기 중인 청크
    private bool isApplied = false;

    public void GenerateGrassForChunkOptimized(OptimizedTerrainGenerator.GrassChunkInfo info)
    {
        List<GrassData> grassList = new List<GrassData>();
        
        int chunkSize = info.data.chunkSize;
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
            
            float height = GetHeightAt(info.data, localX, localZ);
            Vector3 normal = GetNormalAt(info.data, localX, localZ);
            
            if (height <= waterLevel) continue;
            if (normal.y < (1 - normalLimit)) continue;
            
            Vector3 localPos = new Vector3(localX, height, localZ);
            Vector3 worldPos = info.position + localPos;
            
            GrassData grassData = new GrassData
            {
                position = worldPos,
                color = GetRandomColor(),
                length = new Vector2(sizeWidth, sizeLength),
                normal = normal
            };
            
            grassList.Add(grassData);
            successCount++;
        }
        
        chunkGrassData[info.coord] = grassList;
    }

    // ✅ 초기 풀 적용
    public async Task ApplyInitialGrass()
    {
        if (isApplied)
        {
            Debug.LogWarning("Grass already applied!");
            return;
        }
        
        if (grassCompute == null)
        {
            Debug.LogError("GrassComputeScript reference is not set!");
            return;
        }
        
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        allGrassData.Clear();
        appliedChunks.Clear();
        
        foreach (var kvp in chunkGrassData)
        {
            allGrassData.AddRange(kvp.Value);
            appliedChunks.Add(kvp.Key); // ★ 적용된 청크로 표시
        }
        
        if (allGrassData.Count == 0)
        {
            Debug.LogWarning("No grass data to apply!");
            return;
        }
        
        Debug.Log($"<color=yellow>Applying {allGrassData.Count} initial grass...</color>");
        
        grassCompute.SetGrassPaintedDataList = allGrassData;
        grassCompute.Reset();
        
        await Task.Yield();
        
        isApplied = true;
        stopwatch.Stop();
        
        Debug.Log($"<color=green>✓ Initial grass applied in {stopwatch.ElapsedMilliseconds}ms!</color>");
    }
    
    // ✅ 추가 풀 적용 (배치 방식 - 안전)
    public async Task ApplyAdditionalGrass()
    {
        if (grassCompute == null)
        {
            Debug.LogError("GrassComputeScript reference is not set!");
            return;
        }
        
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // ★ 새로 생성된 청크만 수집
        List<Vector2Int> newChunks = new List<Vector2Int>();
        
        foreach (var kvp in chunkGrassData)
        {
            if (!appliedChunks.Contains(kvp.Key))
            {
                newChunks.Add(kvp.Key);
            }
        }
        
        if (newChunks.Count == 0)
        {
            return;
        }
        
        // ★ 대기 리스트에 추가
        pendingChunks.AddRange(newChunks);
        
        Debug.Log($"<color=cyan>New chunks: {newChunks.Count}, Pending: {pendingChunks.Count}/{batchSizeBeforeUpdate}</color>");
        
        // ★ 배치 크기 도달 시에만 업데이트
        if (pendingChunks.Count >= batchSizeBeforeUpdate)
        {
            Debug.Log($"<color=yellow>Batch size reached! Updating {pendingChunks.Count} chunks...</color>");
            
            // 대기 중인 청크들의 풀 데이터 추가
            foreach (var chunkCoord in pendingChunks)
            {
                if (chunkGrassData.TryGetValue(chunkCoord, out List<GrassData> grassList))
                {
                    allGrassData.AddRange(grassList);
                    appliedChunks.Add(chunkCoord); // ★ 적용 완료 표시
                }
            }
            
            // GPU 업데이트
            grassCompute.SetGrassPaintedDataList = allGrassData;
            grassCompute.Reset(); // ★ Reset 필수! (배치로 최소화)
            
            // 대기 리스트 초기화
            int appliedCount = pendingChunks.Count;
            pendingChunks.Clear();
            
            Debug.Log($"<color=green>✓ Batch update complete! Applied {appliedCount} chunks ({allGrassData.Count} total grass)</color>");
        }
        else
        {
            Debug.Log($"<color=gray>Waiting for batch... ({pendingChunks.Count}/{batchSizeBeforeUpdate})</color>");
        }
        
        await Task.Yield();
        stopwatch.Stop();
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
        int x0 = Mathf.FloorToInt(x);
        int z0 = Mathf.FloorToInt(z);
        
        x0 = Mathf.Clamp(x0, 0, chunkData.chunkSize - 1);
        z0 = Mathf.Clamp(z0, 0, chunkData.chunkSize - 1);
        
        float heightL = chunkData.heightMap[Mathf.Max(0, x0 - 1), z0];
        float heightR = chunkData.heightMap[Mathf.Min(chunkData.chunkSize, x0 + 1), z0];
        float heightD = chunkData.heightMap[x0, Mathf.Max(0, z0 - 1)];
        float heightU = chunkData.heightMap[x0, Mathf.Min(chunkData.chunkSize, z0 + 1)];
        
        Vector3 normal = new Vector3(heightL - heightR, 2f, heightD - heightU);
        return normal.normalized;
    }
    
    private Vector3 GetRandomColor()
    {
        Color newRandomCol = new Color(
            baseColor.r + (Random.Range(0, 1.0f) * rangeR),
            baseColor.g + (Random.Range(0, 1.0f) * rangeG),
            baseColor.b + (Random.Range(0, 1.0f) * rangeB),
            1
        );
        return new Vector3(newRandomCol.r, newRandomCol.g, newRandomCol.b);
    }
    
    public void RemoveChunkGrass(Vector2Int chunkCoord)
    {
        if (chunkGrassData.ContainsKey(chunkCoord))
        {
            chunkGrassData.Remove(chunkCoord);
        }
    }
    
    public void ClearAllGrass()
    {
        chunkGrassData.Clear();
        allGrassData.Clear();
        appliedChunks.Clear();
        pendingChunks.Clear();
        isApplied = false;
        
        if (grassCompute != null)
        {
            grassCompute.SetGrassPaintedDataList = new List<GrassData>();
            grassCompute.Reset();
        }
    }
    
    public void SetWaterLevel(float level)
    {
        waterLevel = level;
    }
    
    public bool IsGrassApplied()
    {
        return isApplied;
    }
}