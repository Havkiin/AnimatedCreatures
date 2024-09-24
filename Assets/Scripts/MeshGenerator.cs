using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshGenerator : MonoBehaviour
{
    public int resolution = 10;
    public float size = 1.0f;

    void Awake()
    {
        GenerateMesh();
    }

    void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[resolution * resolution * 6];

        // Fill vertices and uvs
        for (int i = 0, y = 0; y < resolution + 1; y++)
        {
            for (int x = 0; x < resolution + 1; x++, i++)
            {
                float xPos = (float)x / resolution * size;
                float yPos = (float)y / resolution * size;
                vertices[i] = new Vector3(xPos - size / 2.0f, yPos - size / 2.0f, 0);
                uvs[i] = new Vector2((float)x / resolution, (float)y / resolution);
            }
        }

        // Generate triangles
        int tris = 0;
        for (int y = 0, vert = 0; y < resolution; y++, vert++)
        {
            for (int x = 0; x < resolution; x++, vert++, tris += 6)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + resolution + 1;
                triangles[tris + 2] = vert + resolution + 2;

                triangles[tris + 3] = vert + 0;
                triangles[tris + 4] = vert + resolution + 2;
                triangles[tris + 5] = vert + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        mesh.RecalculateBounds();
    }
}
