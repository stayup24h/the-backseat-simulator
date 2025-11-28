using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    [Header("지형 설정")]
    public int xSize = 20; // 가로 격자 수
    public int zSize = 20; // 세로 격자 수
    public float cellSize = 1f; // 격자 크기
    public float noiseScale = 0.1f; // 노이즈 크기 (작을수록 완만한 언덕)
    public float heightMultiplier = 5f; // 높이 배율 (높을수록 가파른 언덕)

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;

    // 지형을 생성하는 함수 (offsetZ: 노이즈의 연속성을 위한 Z축 오프셋)
    public void GenerateMesh(float offsetZ)
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Terrain";

        CreateVertices(offsetZ);
        CreateTriangles();
        UpdateMesh();
    }

    void CreateVertices(float offsetZ)
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        uvs = new Vector2[vertices.Length];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                // 펄린 노이즈 계산 (Z축은 계속 이동하므로 offsetZ를 더해줌)
                float y = Mathf.PerlinNoise(x * noiseScale, (z + offsetZ) * noiseScale) * heightMultiplier;

                // 도로(가운데) 평탄화 로직 (선택사항: 차가 다니는 길은 평평하게)
                // if (x > 5 && x < 15) y *= 0.1f; 

                vertices[i] = new Vector3(x * cellSize, y, z * cellSize);
                uvs[i] = new Vector2((float)x / xSize, (float)z / zSize);
                i++;
            }
        }
    }

    void CreateTriangles()
    {
        triangles = new int[xSize * zSize * 6];
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals(); // 조명 반사를 위해 법선 계산
    }
}