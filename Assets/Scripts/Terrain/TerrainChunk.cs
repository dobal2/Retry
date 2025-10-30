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
        meshFilter.sharedMesh = mesh;
        
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
        
        // 버텍스와 삼각형 설정
        mesh.vertices = data.vertices;
        mesh.triangles = data.triangles;
        
        // 노말과 바운드 재계산
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // 콜라이더 업데이트
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = mesh;
        }
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    private void OnDestroy()
    {
        if (mesh != null)
        {
            Destroy(mesh);
        }
    }
}