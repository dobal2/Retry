using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

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
    
    [Header("References")]
    public GrassComputeScript grassCompute;
    
    [Header("Debug")]
    public bool showDebugInfo = true;

    // 청크별 풀 데이터 저장
    private Dictionary<Vector2Int, List<GrassData>> chunkGrassData = new Dictionary<Vector2Int, List<GrassData>>();
    private List<GrassData> allGrassData = new List<GrassData>();
    
    /// <summary>
    /// 특정 청크에 풀 생성
    /// </summary>
    public void GenerateGrassForChunk(Vector2Int chunkCoord, ChunkData chunkData, Transform chunkTransform)
    {
        if (chunkData == null)
        {
            Debug.LogError($"ChunkData is null for chunk {chunkCoord}");
            return;
        }

        List<GrassData> grassList = new List<GrassData>();
        
        // 청크 크기 기반으로 풀 개수 계산
        int chunkSize = chunkData.chunkSize;
        float chunkArea = chunkSize * chunkSize;
        int numGrass = Mathf.FloorToInt(chunkArea * generationDensity);
        
        if (showDebugInfo)
        {
            Debug.Log($"Generating {numGrass} grass instances for chunk {chunkCoord}");
        }

        // 풀 생성
        for (int i = 0; i < numGrass; i++)
        {
            // 청크 내 랜덤 위치
            float localX = Random.Range(0f, chunkSize);
            float localZ = Random.Range(0f, chunkSize);
            
            // 높이와 노말 가져오기
            float height = GetHeightAt(chunkData, localX, localZ);
            Vector3 normal = GetNormalAt(chunkData, localX, localZ);
            
            // 경사도 체크
            if (normal.y < (1 - normalLimit) || normal.y > (1 + normalLimit))
            {
                continue;
            }
            
            // 월드 좌표 계산
            Vector3 worldPos = chunkTransform.TransformPoint(new Vector3(localX, height, localZ));
            
            GrassData grassData = new GrassData
            {
                position = worldPos,
                color = GetRandomColor(),
                length = new Vector2(sizeWidth, sizeLength),
                normal = normal
            };
            
            grassList.Add(grassData);
        }
        
        // 청크별로 저장
        chunkGrassData[chunkCoord] = grassList;
        
        if (showDebugInfo)
        {
            Debug.Log($"Generated {grassList.Count} grass instances for chunk {chunkCoord}");
        }
    }
    
    /// <summary>
    /// 모든 청크의 풀 데이터를 GrassComputeScript에 적용
    /// </summary>
    public void ApplyAllGrass()
    {
        allGrassData.Clear();
        
        foreach (var kvp in chunkGrassData)
        {
            allGrassData.AddRange(kvp.Value);
        }
        
        if (grassCompute != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"<color=green>Applying {allGrassData.Count} grass instances to GrassComputeScript</color>");
            }
            
            grassCompute.SetGrassPaintedDataList = allGrassData;
            grassCompute.Reset();
        }
        else
        {
            Debug.LogError("GrassComputeScript reference is not set!");
        }
    }
    
    /// <summary>
    /// ChunkData에서 특정 위치의 높이 가져오기 (보간)
    /// </summary>
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
    
    /// <summary>
    /// ChunkData에서 특정 위치의 노말 계산
    /// </summary>
    private Vector3 GetNormalAt(ChunkData chunkData, float x, float z)
    {
        int x0 = Mathf.FloorToInt(x);
        int z0 = Mathf.FloorToInt(z);
        
        // 경계 체크
        x0 = Mathf.Clamp(x0, 0, chunkData.chunkSize - 1);
        z0 = Mathf.Clamp(z0, 0, chunkData.chunkSize - 1);
        
        // 주변 높이 샘플링
        float heightL = chunkData.heightMap[Mathf.Max(0, x0 - 1), z0];
        float heightR = chunkData.heightMap[Mathf.Min(chunkData.chunkSize, x0 + 1), z0];
        float heightD = chunkData.heightMap[x0, Mathf.Max(0, z0 - 1)];
        float heightU = chunkData.heightMap[x0, Mathf.Min(chunkData.chunkSize, z0 + 1)];
        
        // 노말 벡터 계산
        Vector3 normal = new Vector3(heightL - heightR, 2f, heightD - heightU);
        return normal.normalized;
    }
    
    /// <summary>
    /// 랜덤 색상 생성
    /// </summary>
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
    
    /// <summary>
    /// 특정 청크의 풀 데이터 제거
    /// </summary>
    public void RemoveChunkGrass(Vector2Int chunkCoord)
    {
        if (chunkGrassData.ContainsKey(chunkCoord))
        {
            chunkGrassData.Remove(chunkCoord);
        }
    }
    
    /// <summary>
    /// 모든 풀 데이터 초기화
    /// </summary>
    public void ClearAllGrass()
    {
        chunkGrassData.Clear();
        allGrassData.Clear();
        
        if (grassCompute != null)
        {
            grassCompute.SetGrassPaintedDataList = new List<GrassData>();
            grassCompute.Reset();
        }
    }
}