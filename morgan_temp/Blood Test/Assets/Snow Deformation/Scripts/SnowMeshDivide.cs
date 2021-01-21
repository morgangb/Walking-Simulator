using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowMeshDivide : MonoBehaviour
{
    [Range(0, 5)]
    public int divisions = 1;
    public bool convexMeshColliders = false;
    public PhysicMaterial physicMat;
    List<Mesh> meshes;
    List<Mesh> newMeshes;

    Mesh mesh;
    Vector3[] vertices;
    Vector2[] uv;
    int vertexCount;
    float vertDistX;
    float vertDistZ;

    Vector3 scale;

    private List<Vector3> Append(List<Vector3> original, List<Vector3> append)
    {
        for (int i = 0; i < append.Count; i++)
        {
            original.Add(append[i]);
        }
        return original;
    }

    private List<Vector2> Append(List<Vector2> original, List<Vector2> append)
    {
        for (int i = 0; i < append.Count; i++)
        {
            original.Add(append[i]);
        }
        return original;
    }

    private void InitializeMesh()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        vertexCount = mesh.vertexCount;
        uv = mesh.uv;
    }

    private void OrganizeMesh()
    {
        // Vertices
        int meshSide = (int)Mathf.Sqrt(mesh.vertexCount);
        List<float> rowPos = new List<float>();
        List<Vector3>[] rowVectors = new List<Vector3>[meshSide];
        List<Vector3> organizedVerts = new List<Vector3>();

        // UVs
        List<Vector2>[] rowUVs = new List<Vector2>[meshSide];
        List<Vector2> organizedUVs = new List<Vector2>();

        for (int i = 0; i < vertexCount; i++)
        {
            if (!rowPos.Contains(vertices[i].z))
            {
                rowPos.Add(vertices[i].z);
            }
        }

        rowPos.Sort();

        for (int i = 0; i < meshSide; i++)
        {
            rowVectors[i] = new List<Vector3>();
            rowUVs[i] = new List<Vector2>();
            for (int j = 0; j < mesh.vertexCount; j++)
            {
                if (rowPos[i] == vertices[j].z)
                {
                    rowVectors[i].Add(new Vector3(vertices[j].x * scale.x, vertices[j].y * scale.y, vertices[j].z * scale.z));
                    rowUVs[i].Add(uv[j]);
                }
            }
            rowVectors[i].Sort((x, y) => x.x.CompareTo(y.x));
            organizedVerts = Append(organizedVerts, rowVectors[i]);
            rowUVs[i].Sort((x, y) => y.x.CompareTo(x.x));
            organizedUVs = Append(organizedUVs, rowUVs[i]);
        }

        mesh.vertices = organizedVerts.ToArray();
        vertices = mesh.vertices;
        mesh.uv = organizedUVs.ToArray();
        uv = mesh.uv;
        mesh.RecalculateBounds();

        vertDistX = Mathf.Abs(vertices[0].x - vertices[1].x);
        vertDistZ = Mathf.Abs(vertices[0].z - vertices[meshSide].z);
    }

    void QuadrantLoop(float maxX, float minX, float maxZ, float minZ,
                      ref List<Vector3> vertList, ref List<Vector2> uvList)
    {

        for (int i = 0; i < vertexCount; i++)
        {
            if (vertices[i].x <= maxX &&
               vertices[i].x >= minX &&
               vertices[i].z <= maxZ &&
               vertices[i].z >= minZ)
            {
                vertList.Add(vertices[i]);
                uvList.Add(uv[i]);
            }
        }
    }

    void Divide(Mesh mesh)
    {
        Bounds bounds = mesh.bounds;
        List<Vector3> vertList = new List<Vector3>();
        List<Vector2> uvList = new List<Vector2>();

        // North East
        Mesh meshQuadrant1 = new Mesh();

        float maxX = bounds.center.x + bounds.extents.x;
        float minX = bounds.center.x;
        float maxZ = bounds.center.z + bounds.extents.z;
        float minZ = bounds.center.z;

        QuadrantLoop(maxX, minX, maxZ, minZ, ref vertList, ref uvList);

        meshQuadrant1.vertices = vertList.ToArray();
        meshQuadrant1.uv = uvList.ToArray();
        meshQuadrant1.RecalculateBounds();
        newMeshes.Add(meshQuadrant1);

        vertList.Clear();
        uvList.Clear();

        // North West
        Mesh meshQuadrant2 = new Mesh();

        maxX = bounds.center.x;
        minX = bounds.center.x - bounds.extents.x;
        maxZ = bounds.center.z + bounds.extents.z;
        minZ = bounds.center.z;

        QuadrantLoop(maxX, minX, maxZ, minZ, ref vertList, ref uvList);

        meshQuadrant2.vertices = vertList.ToArray();
        meshQuadrant2.uv = uvList.ToArray();
        meshQuadrant2.RecalculateBounds();
        newMeshes.Add(meshQuadrant2);

        vertList.Clear();
        uvList.Clear();

        // South West
        Mesh meshQuadrant3 = new Mesh();

        maxX = bounds.center.x;
        minX = bounds.center.x - bounds.extents.x;
        maxZ = bounds.center.z;
        minZ = bounds.center.z - bounds.extents.z;

        QuadrantLoop(maxX, minX, maxZ, minZ, ref vertList, ref uvList);

        meshQuadrant3.vertices = vertList.ToArray();
        meshQuadrant3.uv = uvList.ToArray();
        meshQuadrant3.RecalculateBounds();
        newMeshes.Add(meshQuadrant3);

        vertList.Clear();
        uvList.Clear();

        // South East
        Mesh meshQuadrant4 = new Mesh();

        maxX = bounds.center.x + bounds.extents.x;
        minX = bounds.center.x;
        maxZ = bounds.center.z;
        minZ = bounds.center.z - bounds.extents.z;

        QuadrantLoop(maxX, minX, maxZ, minZ, ref vertList, ref uvList);

        meshQuadrant4.vertices = vertList.ToArray();
        meshQuadrant4.uv = uvList.ToArray();
        meshQuadrant4.RecalculateBounds();
        newMeshes.Add(meshQuadrant4);
    }

    Mesh InitializeTrianglesAndUVs(Mesh mesh)
    {
        float row = mesh.vertices[0].z;
        List<Vector3> row0 = new List<Vector3>();
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            if(mesh.vertices[i].z == row)
            {
                row0.Add(mesh.vertices[i]);
            }
        }
        int meshSideX = row0.Count;
        int meshSideZ = mesh.vertexCount / meshSideX;

        // Set up mesh triangles
        int[] triangles = new int[(meshSideX - 1) * (meshSideZ - 1) * 6];
        for (int ti = 0, vi = 0, y = 0; y < meshSideZ - 1; y++, vi++)
        {
            for (int x = 0; x < meshSideX - 1; x++, ti += 6, vi++)
            {
                triangles[ti] = triangles[ti + 3] = vi;
                triangles[ti + 1] = triangles[ti + 5] = vi + meshSideX + 1;
                triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = vi + meshSideX;
            }
        }
        mesh.triangles = triangles;

        // Set up displacement UVs
        List<Vector4> baseUVs = new List<Vector4>();
        mesh.GetUVs(0, baseUVs);
        for (int i = 0, y = 0; y < meshSideZ; y++)
        {
            for (int x = 0; x < meshSideX; x++, i++)
            {
                baseUVs[i] = new Vector4(baseUVs[i].x,
                                         baseUVs[i].y,
                                         (float)x / (meshSideX - 1),
                                         1 - (float)y / (meshSideZ - 1));
            }
        }
        mesh.SetUVs(0, baseUVs);

        mesh.RecalculateTangents();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void Awake()
    {
        scale = transform.localScale;
        InitializeMesh();
        OrganizeMesh();

        meshes = new List<Mesh>();
        meshes.Add(mesh);
        newMeshes = new List<Mesh>();

        while (divisions > 0)
        {
            newMeshes.Clear();
            int meshCount = meshes.Count;
            for (int i = 0; i < meshCount; i++)
            {
                Divide(meshes[i]);
            }
            divisions--;
            meshes = new List<Mesh>(newMeshes);
        }

        GameObject tiles = new GameObject("Snow Tiles");

        for (int i = 0; i < meshes.Count; i++)
        {
            Mesh currentMesh = meshes[i];
            Vector3 center = currentMesh.bounds.center;
            Vector3 extents = currentMesh.bounds.extents;
            List<Vector3> vertList = new List<Vector3>();
            List<Vector2> uvList = new List<Vector2>();

            float maxX = center.x + extents.x + vertDistX * 1.5f;
            float minX = center.x - extents.x - vertDistX * 1.5f;
            float maxZ = center.z + extents.z + vertDistZ * 1.5f;
            float minZ = center.z - extents.z - vertDistZ * 1.5f;

            for (int j = 0; j < vertexCount; j++)
            {
                if (vertices[j].x <= maxX &&
                    vertices[j].x >= minX &&
                    vertices[j].z <= maxZ &&
                    vertices[j].z >= minZ)
                {
                    vertList.Add(vertices[j]);
                    uvList.Add(uv[j]);
                }
            }

            currentMesh.vertices = vertList.ToArray();
            currentMesh.uv = uvList.ToArray();

            GameObject tile = new GameObject("Tile");
            tile.transform.parent = tiles.transform;
            tile.transform.position = transform.position;
            tile.transform.rotation = transform.rotation;
            tile.tag = "Tile";
            tile.AddComponent<MeshFilter>().mesh = InitializeTrianglesAndUVs(currentMesh);
            tile.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = tile.AddComponent<MeshCollider>();
            meshCollider.material = physicMat;
            if(convexMeshColliders)
            {
                meshCollider.convex = true;
            }
        }

        Destroy(gameObject);
    }
}