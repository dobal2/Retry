using System;
using UnityEngine;

//[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] private int xSize = 10;
    [SerializeField] private int zSize = 10;

    [SerializeField] private int xOffset;
    [SerializeField] private int zOffset;

    [SerializeField] private float noiseScale = 0.03f;
    [SerializeField] private float heightMultiplier = 7;

    [SerializeField] private int octavesCount = 1;
    [SerializeField] private float lacunarity = 2;
    [SerializeField] private float persistance = 0.5f;

    //[SerializeField] private Layer terrainLayer;

    [SerializeField] private Gradient terrainGradient;
    [SerializeField] private Material mat;

    [SerializeField] private float testUpdateCoolTime;
    private float curTime;

    private Mesh mesh;
    private Texture2D gradientTexture;
    
    private Vector3[] vertices;

    private void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        
        GenerateTerrain();
    }

    private void Update()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
        
        float minTerrainHeight = mesh.bounds.min.y + transform.position.y;
        float maxTerrainHeight = mesh.bounds.max.y + transform.position.y;
        
        // mat.SetTexture("_TerrainGradient",gradientTexture);
        //
        // mat.SetFloat("_MinTerrainHeight",minTerrainHeight);
        // mat.SetFloat("_MaxTerrainHeight",maxTerrainHeight);
        
        // if (testUpdateCoolTime <= curTime)
        // {
        //     curTime = 0;
        //
        //     float minTerrainHeight = mesh.bounds.min.y + transform.position.y;
        //     float maxTerrainHeight = mesh.bounds.max.y + transform.position.y;
        //
        //     mat.SetTexture("_TerrainGradient",gradientTexture);
        //
        //     mat.SetFloat("_MinTerrainHeight",minTerrainHeight);
        //     mat.SetFloat("_MaxTerrainHeight",maxTerrainHeight);
        // }
        //
        // curTime += Time.deltaTime;

    }

    public void GenerateTerrain()
    {
        GradientToTexture();
        CreateMesh();
    }

    private void GradientToTexture()
    {
        gradientTexture = new Texture2D(1, 100);
        Color[] pixelColor = new Color[100];

        for (int i = 0; i < 100; i++)
        {
            pixelColor[i] = terrainGradient.Evaluate((float)i / 100);
        }
        
        gradientTexture.SetPixels(pixelColor);
        gradientTexture.Apply();
    }

    private void CreateMesh()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        int i = 0;
        for (int z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float yPos = 0;

                for (int o = 0; o < octavesCount; o++)
                {
                    float frequency = Mathf.Pow(lacunarity, o);
                    float amplitude = Mathf.Pow(persistance, o);
                    
                    
                    yPos = Mathf.PerlinNoise((x + xOffset) * noiseScale * frequency, (z + zOffset) * noiseScale * frequency) * amplitude;

                    yPos *= heightMultiplier;
                }
                vertices[i] = new Vector3(x, yPos, z);
                i++;
            }
        }

        int[] triangles = new int[xSize * zSize * 6];

        int vertex = 0;
        int triangleIndex = 0;
        
        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[triangleIndex + 0] = vertex + 0;
                triangles[triangleIndex + 1] = vertex + xSize + 1;
                triangles[triangleIndex + 2] = vertex + 1;
                
                triangles[triangleIndex + 3] = vertex + 1;
                triangles[triangleIndex + 4] = vertex + xSize + 1;
                triangles[triangleIndex + 5] = vertex + xSize + 2;

                vertex++;
                triangleIndex += 6;
                
            }

            vertex++;
        }
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    class Layer
    {
        public Texture texture;
        [Range(0, 1)] public float startHeight;
    }
    

    // private void OnDrawGizmos()
    // {
    //     foreach (Vector3 pos in vertices)
    //     {
    //         Gizmos.DrawSphere(pos,0.2f);
    //     }
    // }
}
