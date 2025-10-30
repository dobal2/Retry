using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeGrassGenerator : MonoBehaviour
{
    [Header("Target Object")]
    [Tooltip("풀을 생성할 메시 또는 Terrain 오브젝트")]
    public GameObject targetObject;

    [Header("Generation Settings")]
    public int grassAmountToGenerate = 10000;
    [Range(0.01f, 10f)]
    public float generationDensity = 0.1f;
    
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
    
    [Header("Blocking")]
    [Tooltip("충돌 체크 활성화 (끄면 훨씬 빠름)")]
    public bool useCollisionCheck = false;
    public LayerMask paintBlockMask;
    
    [Header("Performance Settings")]
    [Tooltip("한 프레임에 생성할 풀 개수 (낮을수록 부드럽지만 느림)")]
    [Range(10, 5000)]
    public int batchSize = 500;
    
    [Tooltip("배치 사이 대기 프레임 수 (0=매 프레임, 1=한 프레임 건너뛰기)")]
    [Range(0, 10)]
    public int skipFrames = 0;
    
    [Header("Delay Settings")]
    [Tooltip("게임 시작 후 생성까지의 지연 시간 (초)")]
    public float delayTime = 2f;
    
    [Header("References")]
    public GrassComputeScript grassCompute;
    
    [Header("UI (Optional)")]
    [Tooltip("진행률 표시 UI Text 또는 Slider")]
    public TextMeshProUGUI progressText;
    public Slider progressSlider;
    public GameObject loadingPanel;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private List<GrassData> grassData = new List<GrassData>();
    
    private NativeArray<float> sizes;
    private NativeArray<float> cumulativeSizes;
    private NativeArray<float> total;
    
    private bool isGenerating = false;

    void Start()
    {
        StartCoroutine(GenerateGrassAfterDelay());
    }
    
    IEnumerator GenerateGrassAfterDelay()
    {
        yield return new WaitForSeconds(delayTime);
        
        if (targetObject != null)
        {
            yield return StartCoroutine(GenerateGrassOnTargetAsync());
        }
        else
        {
            Debug.LogWarning("Target Object가 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 비동기 풀 생성 (여러 프레임에 분산)
    /// </summary>
    public IEnumerator GenerateGrassOnTargetAsync()
    {
        if (isGenerating)
        {
            Debug.LogWarning("이미 풀을 생성 중입니다!");
            yield break;
        }
        
        if (targetObject == null)
        {
            Debug.LogError("Target Object가 null입니다!");
            yield break;
        }
        
        isGenerating = true;
        
        // UI 활성화
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
        
        // 기존 데이터 초기화
        grassData.Clear();
        
        float startTime = Time.realtimeSinceStartup;
        
        // MeshFilter가 있는 경우
        if (targetObject.TryGetComponent(out MeshFilter sourceMesh))
        {
            yield return StartCoroutine(GenerateOnMeshAsync(sourceMesh));
        }
        // Terrain인 경우
        else if (targetObject.TryGetComponent(out Terrain terrain))
        {
            yield return StartCoroutine(GenerateOnTerrainAsync(terrain));
        }
        else
        {
            Debug.LogError("Target Object에 MeshFilter나 Terrain 컴포넌트가 없습니다!");
            isGenerating = false;
            yield break;
        }
        
        float elapsedTime = Time.realtimeSinceStartup - startTime;
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=green>★ 풀 생성 완료! ★</color>");
            Debug.Log($"생성 개수: {grassData.Count}개");
            Debug.Log($"소요 시간: {elapsedTime:F2}초");
        }
        
        // UI 비활성화
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        
        isGenerating = false;
    }
    
    IEnumerator GenerateOnMeshAsync(MeshFilter sourceMesh)
    {
        CalcAreas(sourceMesh.sharedMesh);
        Matrix4x4 localToWorld = sourceMesh.transform.localToWorldMatrix;
        
        var oTriangles = sourceMesh.sharedMesh.triangles;
        var oVertices = sourceMesh.sharedMesh.vertices;
        var oColors = sourceMesh.sharedMesh.colors;
        var oNormals = sourceMesh.sharedMesh.normals;
        
        var meshTriangles = new NativeArray<int>(oTriangles.Length, Allocator.Persistent);
        var meshVertices = new NativeArray<Vector4>(oVertices.Length, Allocator.Persistent);
        var meshColors = new NativeArray<Color>(oVertices.Length, Allocator.Persistent);
        var meshNormals = new NativeArray<Vector3>(oNormals.Length, Allocator.Persistent);
        
        for (int i = 0; i < meshTriangles.Length; i++)
        {
            meshTriangles[i] = oTriangles[i];
        }
        
        for (int i = 0; i < meshVertices.Length; i++)
        {
            meshVertices[i] = oVertices[i];
            meshNormals[i] = oNormals[i];
            meshColors[i] = oColors.Length == 0 ? Color.black : oColors[i];
        }
        
        Bounds bounds = sourceMesh.sharedMesh.bounds;
        Vector3 meshSize = new Vector3(
            bounds.size.x * sourceMesh.transform.lossyScale.x,
            bounds.size.y * sourceMesh.transform.lossyScale.y,
            bounds.size.z * sourceMesh.transform.lossyScale.z
        );
        meshSize += Vector3.one;
        
        float meshVolume = meshSize.x * meshSize.y * meshSize.z;
        int numPoints = Mathf.Min(Mathf.FloorToInt(meshVolume * generationDensity), grassAmountToGenerate);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>메시에 {numPoints}개의 풀을 생성합니다...</color>");
            Debug.Log($"배치 크기: {batchSize}, 예상 배치 수: {Mathf.CeilToInt((float)numPoints / batchSize)}");
        }
        
        int processedPoints = 0;
        int batchCount = 0;
        
        // 배치 단위로 생성
        while (processedPoints < numPoints)
        {
            int currentBatchSize = Mathf.Min(batchSize, numPoints - processedPoints);
            
            // 배치 생성
            for (int j = 0; j < currentBatchSize; j++)
            {
                var point = new NativeArray<Vector3>(1, Allocator.TempJob);
                var normals = new NativeArray<Vector3>(1, Allocator.TempJob);
                var lengthWidth = new NativeArray<float>(1, Allocator.TempJob);
                
                var job = new GrassGenerationJob
                {
                    CumulativeSizes = cumulativeSizes,
                    Sizes = sizes,
                    Total = total,
                    MeshColors = meshColors,
                    MeshVertices = meshVertices,
                    MeshNormals = meshNormals,
                    MeshTriangles = meshTriangles,
                    Point = point,
                    LengthWidth = lengthWidth,
                    Normals = normals
                };
                
                job.Run();
                
                if (point[0] != Vector3.zero)
                {
                    GrassData newData = new GrassData();
                    Vector3 worldPoint = localToWorld.MultiplyPoint3x4(point[0]);
                    newData.position = worldPoint;
                    
                    // 충돌 체크 (옵션)
                    if (useCollisionCheck)
                    {
                        Collider[] cols = Physics.OverlapBox(newData.position, Vector3.one * 0.2f, Quaternion.identity, paintBlockMask);
                        if (cols.Length > 0)
                        {
                            point.Dispose();
                            normals.Dispose();
                            lengthWidth.Dispose();
                            continue;
                        }
                    }
                    
                    Vector3 worldNormal = localToWorld.MultiplyVector(normals[0]).normalized;
                    
                    if (worldNormal.y <= (1 + normalLimit) && worldNormal.y >= (1 - normalLimit))
                    {
                        newData.color = GetRandomColor();
                        newData.length = new Vector2(sizeWidth, sizeLength);
                        newData.normal = worldNormal;
                        grassData.Add(newData);
                    }
                }
                
                point.Dispose();
                normals.Dispose();
                lengthWidth.Dispose();
            }
            
            processedPoints += currentBatchSize;
            batchCount++;
            
            // 진행률 업데이트
            float progress = (float)processedPoints / numPoints;
            UpdateProgress(progress, processedPoints, numPoints);
            
            // 프레임 건너뛰기
            for (int i = 0; i <= skipFrames; i++)
            {
                yield return null;
            }
        }
        
        // 메모리 정리
        sizes.Dispose();
        cumulativeSizes.Dispose();
        total.Dispose();
        meshColors.Dispose();
        meshTriangles.Dispose();
        meshVertices.Dispose();
        meshNormals.Dispose();
        
        ApplyToGrassCompute();
    }
    
    IEnumerator GenerateOnTerrainAsync(Terrain terrain)
    {
        float meshVolume = terrain.terrainData.size.x * terrain.terrainData.size.y * terrain.terrainData.size.z;
        int numPoints = Mathf.Min(Mathf.FloorToInt(meshVolume * generationDensity), grassAmountToGenerate);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>Terrain에 {numPoints}개의 풀을 생성합니다...</color>");
            Debug.Log($"배치 크기: {batchSize}, 예상 배치 수: {Mathf.CeilToInt((float)numPoints / batchSize)}");
        }
        
        int processedPoints = 0;
        
        // 배치 단위로 생성
        while (processedPoints < numPoints)
        {
            int currentBatchSize = Mathf.Min(batchSize, numPoints - processedPoints);
            
            // 배치 생성
            for (int j = 0; j < currentBatchSize; j++)
            {
                GrassData newData = new GrassData();
                Vector3 newPoint = Vector3.zero;
                Vector3 newNormal = Vector3.zero;
                
                GetRandomPointOnTerrain(terrain, terrain.terrainData.size, ref newPoint, ref newNormal);
                newData.position = newPoint;
                
                // 충돌 체크 (옵션)
                if (useCollisionCheck)
                {
                    Collider[] cols = Physics.OverlapBox(newData.position, Vector3.one * 0.2f, Quaternion.identity, paintBlockMask);
                    if (cols.Length > 0)
                    {
                        continue;
                    }
                }
                
                if (newNormal.y <= (1 + normalLimit) && newNormal.y >= (1 - normalLimit))
                {
                    newData.color = GetRandomColor();
                    newData.length = new Vector2(sizeWidth, sizeLength);
                    newData.normal = newNormal;
                    if (newPoint != Vector3.zero)
                    {
                        grassData.Add(newData);
                    }
                }
            }
            
            processedPoints += currentBatchSize;
            
            // 진행률 업데이트
            float progress = (float)processedPoints / numPoints;
            UpdateProgress(progress, processedPoints, numPoints);
            
            // 프레임 건너뛰기
            for (int i = 0; i <= skipFrames; i++)
            {
                yield return null;
            }
        }
        
        ApplyToGrassCompute();
    }
    
    void UpdateProgress(float progress, int current, int total)
    {
        if (progressSlider != null)
            progressSlider.value = progress;
        
        if (progressText != null)
            progressText.text = $"Generating Grass... {current}/{total} ({progress * 100:F0}%)";
    }
    
    void ApplyToGrassCompute()
    {
        if (grassCompute != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"<color=green>총 {grassData.Count}개의 풀이 생성되었습니다</color>");
            }
            grassCompute.SetGrassPaintedDataList = grassData;
            grassCompute.Reset();
        }
        else
        {
            Debug.LogError("GrassComputeScript 참조가 설정되지 않았습니다!");
        }
    }
    
    Vector3 GetRandomColor()
    {
        Color newRandomCol = new Color(
            baseColor.r + (Random.Range(0, 1.0f) * rangeR),
            baseColor.g + (Random.Range(0, 1.0f) * rangeG),
            baseColor.b + (Random.Range(0, 1.0f) * rangeB),
            1
        );
        Vector3 color = new Vector3(newRandomCol.r, newRandomCol.g, newRandomCol.b);
        return color;
    }
    
    void GetRandomPointOnTerrain(Terrain terrain, Vector3 size, ref Vector3 point, ref Vector3 normal)
    {
        point = new Vector3(Random.Range(0, size.x), 0, Random.Range(0, size.z));
        
        float pointSizeX = (point.x / size.x);
        float pointSizeZ = (point.z / size.z);
        
        normal = terrain.terrainData.GetInterpolatedNormal(pointSizeX, pointSizeZ);
        point = terrain.transform.TransformPoint(point);
        point.y = terrain.SampleHeight(point) + terrain.GetPosition().y;
    }
    
    public void CalcAreas(Mesh mesh)
    {
        sizes = GetTriSizes(mesh.triangles, mesh.vertices);
        cumulativeSizes = new NativeArray<float>(sizes.Length, Allocator.Persistent);
        total = new NativeArray<float>(1, Allocator.Persistent);
        
        for (int i = 0; i < sizes.Length; i++)
        {
            total[0] += sizes[i];
            cumulativeSizes[i] = total[0];
        }
    }
    
    public NativeArray<float> GetTriSizes(int[] tris, Vector3[] verts)
    {
        int triCount = tris.Length / 3;
        var sizes = new NativeArray<float>(triCount, Allocator.Persistent);
        for (int i = 0; i < triCount; i++)
        {
            sizes[i] = .5f * Vector3.Cross(
                verts[tris[i * 3 + 1]] - verts[tris[i * 3]],
                verts[tris[i * 3 + 2]] - verts[tris[i * 3]]).magnitude;
        }
        return sizes;
    }
    
    // Burst Job 구조체
    [BurstCompile(CompileSynchronously = true)]
    private struct GrassGenerationJob : IJob
    {
        [ReadOnly] public NativeArray<float> Sizes;
        [ReadOnly] public NativeArray<float> Total;
        [ReadOnly] public NativeArray<float> CumulativeSizes;
        [ReadOnly] public NativeArray<Color> MeshColors;
        [ReadOnly] public NativeArray<Vector4> MeshVertices;
        [ReadOnly] public NativeArray<Vector3> MeshNormals;
        [ReadOnly] public NativeArray<int> MeshTriangles;
        [WriteOnly] public NativeArray<Vector3> Point;
        [WriteOnly] public NativeArray<float> LengthWidth;
        [WriteOnly] public NativeArray<Vector3> Normals;
        
        public void Execute()
        {
            float randomsample = Random.value * Total[0];
            int triIndex = -1;
            
            for (int i = 0; i < Sizes.Length; i++)
            {
                if (randomsample <= CumulativeSizes[i])
                {
                    triIndex = i;
                    break;
                }
            }
            
            if (triIndex == -1)
            {
                Point[0] = Vector3.zero;
                return;
            }
            
            LengthWidth[0] = 1.0f;
            
            Vector3 a = MeshVertices[MeshTriangles[triIndex * 3]];
            Vector3 b = MeshVertices[MeshTriangles[triIndex * 3 + 1]];
            Vector3 c = MeshVertices[MeshTriangles[triIndex * 3 + 2]];
            
            // 랜덤 무게중심 좌표 생성
            float r = Random.value;
            float s = Random.value;
            
            if (r + s >= 1)
            {
                r = 1 - r;
                s = 1 - s;
            }
            
            Normals[0] = MeshNormals[MeshTriangles[triIndex * 3 + 1]];
            
            // 메시 위의 점으로 변환
            Vector3 pointOnMesh = a + r * (b - a) + s * (c - a);
            Point[0] = pointOnMesh;
        }
    }
}