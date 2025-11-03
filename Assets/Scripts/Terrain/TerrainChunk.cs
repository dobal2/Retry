using UnityEngine;

/// <summary>
/// 개별 지형 청크를 관리하는 컴포넌트
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainChunk : MonoBehaviour
{
    public Vector2Int chunkCoord { get; private set; }
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh mesh;

    [Header("Debug")]
    [SerializeField] private bool showNormals = false;
    [SerializeField] private float normalLength = 0.5f;

    public void Initialize(Vector2Int coord, Material material)
    {
        chunkCoord = coord;
        
        // 컴포넌트 가져오기
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        
        // 메시 생성
        mesh = new Mesh();
        mesh.name = $"TerrainChunk_{coord.x}_{coord.y}";
        
        // 머티리얼 설정
        meshRenderer.sharedMaterial = material;
        
        // 게임 오브젝트 이름 설정
        gameObject.name = $"Chunk_{coord.x}_{coord.y}";
    }

    public void ApplyMeshData(ChunkData data)
    {
        if (mesh == null)
        {
            Debug.LogError("Mesh is null! Call Initialize first.");
            return;
        }

        mesh.Clear();
        
        // ★★★ 중요: 메시 데이터 설정 ★★★
        mesh.vertices = data.vertices;
        mesh.triangles = data.triangles;
        mesh.uv = data.uvs; // UV 설정
        
        // ★★★ 노말 재계산 (굴곡 표현의 핵심!) ★★★
        mesh.RecalculateNormals();
        
        // 탄젠트 재계산 (노말맵 사용시 필요)
        mesh.RecalculateTangents();
        
        // 바운드 재계산
        mesh.RecalculateBounds();
        
        // 메시 최적화
        mesh.Optimize();
        
        // 메시 적용
        meshFilter.sharedMesh = mesh;
        
        // 콜라이더 업데이트
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = mesh;
        }

        // 디버그 로그
        Debug.Log($"Chunk {chunkCoord}: Verts={mesh.vertices.Length}, Tris={mesh.triangles.Length/3}, Normals={mesh.normals.Length}");
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    // Scene 뷰에서 노말 벡터 시각화
    private void OnDrawGizmosSelected()
    {
        if (!showNormals) return;
        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        Mesh m = meshFilter.sharedMesh;
        Vector3[] vertices = m.vertices;
        Vector3[] normals = m.normals;

        if (normals == null || normals.Length == 0)
        {
            Debug.LogWarning($"Chunk {chunkCoord}: No normals found!");
            return;
        }

        Gizmos.color = Color.cyan;

        // 성능을 위해 일부만 표시
        for (int i = 0; i < vertices.Length; i += 5)
        {
            Vector3 worldPos = transform.TransformPoint(vertices[i]);
            Vector3 worldNormal = transform.TransformDirection(normals[i]).normalized;
            
            Gizmos.DrawLine(worldPos, worldPos + worldNormal * normalLength);
        }
    }

    private void OnDestroy()
    {
        if (mesh != null)
        {
            Destroy(mesh);
        }
    }
}