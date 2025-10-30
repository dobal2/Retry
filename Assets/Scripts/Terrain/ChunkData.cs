using UnityEngine;

/// <summary>
/// 지형 청크의 메시 데이터를 저장하는 클래스
/// </summary>
public class ChunkData
{
    public Vector2Int chunkCoord;
    public int chunkSize;
    public Vector3[] vertices;
    public int[] triangles;
    public float[,] heightMap;
    public Bounds bounds;

    public ChunkData(Vector2Int coord, int size)
    {
        chunkCoord = coord;
        chunkSize = size;
        
        // 버텍스 배열 초기화 (chunkSize + 1 x chunkSize + 1 그리드)
        int vertexCount = (size + 1) * (size + 1);
        vertices = new Vector3[vertexCount];
        
        // 삼각형 배열 초기화 (각 쿼드는 2개의 삼각형 = 6개의 인덱스)
        int triangleCount = size * size * 6;
        triangles = new int[triangleCount];
        
        // 높이맵 초기화
        heightMap = new float[size + 1, size + 1];
    }
}