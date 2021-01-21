using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LODQuality
{
    [Range(1, 0)]
    public float LOD1 = 0.5f;
    [Range(1, 0)]
    public float LOD2 = 0.25f;
    [Range(1, 0)]
    public float LOD3 = 0.125f;
}

public class SnowMeshGenerator : MonoBehaviour
{
    public enum DownSampleMode { Off, Half, Quarter }

    [Header("Height Map Settings")]
    public Texture2D heightMap;
    public Shader blurShader;
    [Range(0, 32)]
    public int blurIterations = 8;
    public DownSampleMode downSampleMode = DownSampleMode.Off;
    public bool invertMap = false;

    [Header("Mesh Generation")]
    public Material basicMaterial;
    public float heightScale = 10;
    public int meshWidth = 100; // X
    public int meshLength = 100; // Z
    public float vertexDistance = 0.5f;

    [Header("Level of Detail")]
    public bool createTileLOD = true;
    public float LODBias = 1.5f;
    public LODQuality LODQuality = new LODQuality();

    [Header("Mesh Divide")]
    [Range(0, 5)]
    public int divisions = 1;
    public bool convexMeshColliders = false;
    public PhysicMaterial physicMat;

    private Texture2D internalHeightMap;
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector2[] UVs;
    private int[] triangles;
    private List<Mesh> meshes;
    private List<Mesh> newMeshes;
    private GameObject tiles;

    private LOD LODdetail0;
    private LOD LODdetail1;
    private LOD LODdetail2;
    private LOD LODdetail3;
    MeshRenderer[] detail0Renderers;
    MeshRenderer[] detail1Renderers;
    MeshRenderer[] detail2Renderers;
    MeshRenderer[] detail3Renderers;

    private void GenerateMesh()
    {
        mesh = new Mesh();

        vertices = new Vector3[(meshWidth + 1) * (meshLength + 1)];
        UVs = new Vector2[vertices.Length];
        int xCount = 0;
        int zCount = 0;
        int i = 0;
        for (float z = 0; zCount < meshLength + 1; z += vertexDistance, zCount++)
        {
            xCount = 0;
            for (float x = 0; xCount < meshWidth + 1; x += vertexDistance, i++, xCount++)
            {
                vertices[i] = new Vector3(x - meshWidth / 2 * vertexDistance,
                                          0,
                                          z - meshLength / 2 * vertexDistance);
                UVs[i] = new Vector2((float)xCount / meshWidth, (float)zCount / meshLength);
            }
        }
        mesh.vertices = vertices;
        mesh.uv = UVs;

        int[] triangles = new int[meshWidth * meshLength * 6];
        for (int ti = 0, vi = 0, y = 0; y < meshLength; y++, vi++)
        {
            for (int x = 0; x < meshWidth; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + meshWidth + 1;
                triangles[ti + 5] = vi + meshWidth + 2;
            }
        }
    }

    private void BlurHeightMap()
    {
        if (blurShader == null)
        {
            internalHeightMap = heightMap;
            return;
        }

        Material blurMat = new Material(blurShader);
        blurMat.hideFlags = HideFlags.HideAndDontSave;

        RenderTexture rt1, rt2;

        if (downSampleMode == DownSampleMode.Half)
        {
            rt1 = RenderTexture.GetTemporary(heightMap.width / 2, heightMap.height / 2);
            rt2 = RenderTexture.GetTemporary(heightMap.width / 2, heightMap.height / 2);
            Graphics.Blit(heightMap, rt1);
        }
        else if (downSampleMode == DownSampleMode.Quarter)
        {
            rt1 = RenderTexture.GetTemporary(heightMap.width / 4, heightMap.height / 4);
            rt2 = RenderTexture.GetTemporary(heightMap.width / 4, heightMap.height / 4);
            Graphics.Blit(heightMap, rt1, blurMat, 0);
        }
        else
        {
            rt1 = RenderTexture.GetTemporary(heightMap.width, heightMap.height);
            rt2 = RenderTexture.GetTemporary(heightMap.width, heightMap.height);
            Graphics.Blit(heightMap, rt1);
        }

        for (var i = 0; i < blurIterations; i++)
        {
            Graphics.Blit(rt1, rt2, blurMat, 1);
            Graphics.Blit(rt2, rt1, blurMat, 2);
        }

        RenderTexture.active = rt1;

        internalHeightMap = new Texture2D(rt1.width, rt1.height);
        internalHeightMap.ReadPixels(new Rect(0, 0, rt1.width, rt1.height), 0, 0);
        internalHeightMap.Apply();

        RenderTexture.active = null;

        RenderTexture.ReleaseTemporary(rt1);
        RenderTexture.ReleaseTemporary(rt2);
    }

    private void SampleHeightMap()
    {
        if (internalHeightMap == null)
        {
            return;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            int x = Mathf.FloorToInt(UVs[i].x * internalHeightMap.width);
            int y = Mathf.FloorToInt(UVs[i].y * internalHeightMap.height);
            float height;
            if (invertMap)
            {
                height = 1 - (internalHeightMap.GetPixel(x, y).r - 0.5f) * heightScale;
            }
            else
            {
                height = (internalHeightMap.GetPixel(x, y).r - 0.5f) * heightScale;
            }

            vertices[i] = new Vector3(vertices[i].x, height, vertices[i].z);
        }
        mesh.vertices = vertices;
    }

    void QuadrantLoop(float maxX, float minX, float maxZ, float minZ,
                      ref List<Vector3> vertList, ref List<Vector2> uvList)
    {

        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].x <= maxX &&
               vertices[i].x >= minX &&
               vertices[i].z <= maxZ &&
               vertices[i].z >= minZ)
            {
                vertList.Add(vertices[i]);
                uvList.Add(UVs[i]);
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
            if (mesh.vertices[i].z == row)
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

        mesh.RecalculateBounds();

        return mesh;
    }

    private void DivideMesh()
    {
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

        tiles = new GameObject("Snow Tiles");
        tiles.transform.position = transform.position;

        for (int i = 0; i < meshes.Count; i++)
        {
            Mesh currentMesh = meshes[i];
            Vector3 center = currentMesh.bounds.center;
            Vector3 extents = currentMesh.bounds.extents;
            List<Vector3> vertList = new List<Vector3>();
            List<Vector2> uvList = new List<Vector2>();

            float maxX = center.x + extents.x + vertexDistance * 1.5f;
            float minX = center.x - extents.x - vertexDistance * 1.5f;
            float maxZ = center.z + extents.z + vertexDistance * 1.5f;
            float minZ = center.z - extents.z - vertexDistance * 1.5f;

            for (int j = 0; j < vertices.Length; j++)
            {
                if (vertices[j].x <= maxX &&
                    vertices[j].x >= minX &&
                    vertices[j].z <= maxZ &&
                    vertices[j].z >= minZ)
                {
                    vertList.Add(vertices[j]);
                    uvList.Add(UVs[j]);
                }
            }

            currentMesh.vertices = vertList.ToArray();
            currentMesh.uv = uvList.ToArray();

            GameObject tile = new GameObject("Tile");
            tile.transform.parent = tiles.transform;
            tile.transform.position = transform.position;
            tile.transform.rotation = transform.rotation;
            tile.tag = "Tile";
            MeshFilter meshFilter = tile.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = InitializeTrianglesAndUVs(currentMesh);
            meshFilter.sharedMesh.RecalculateNormals();
            meshFilter.sharedMesh.RecalculateTangents();

            tile.AddComponent<MeshRenderer>().sharedMaterial = basicMaterial;
            MeshCollider meshCollider = tile.AddComponent<MeshCollider>();
            meshCollider.sharedMaterial = physicMat;

            if (convexMeshColliders)
            {
                meshCollider.convex = true;
            }
        }
    }

    private Mesh[] GetLODs(Mesh mesh)
    {
        UnityMeshSimplifier.MeshSimplifier meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

        List<Vector4> UV4D = new List<Vector4>();
        mesh.GetUVs(0, UV4D);

        Mesh[] meshLODs = new Mesh[4];

        // LOD 0
        meshLODs[0] = mesh;

        // LOD 1
        Mesh LOD1 = new Mesh();
        meshSimplifier.Initialize(mesh);
        meshSimplifier.SetUVs(0, UV4D);
        meshSimplifier.SimplifyMesh(LODQuality.LOD1);
        LOD1 = meshSimplifier.ToMesh();
        meshLODs[1] = LOD1;

        // LOD 2
        Mesh LOD2 = new Mesh();
        meshSimplifier.Initialize(mesh);
        meshSimplifier.SetUVs(0, UV4D);
        meshSimplifier.SimplifyMesh(LODQuality.LOD2);
        LOD2 = meshSimplifier.ToMesh();
        meshLODs[2] = LOD2;

        // LOD 3
        Mesh LOD3 = new Mesh();
        meshSimplifier.Initialize(mesh);
        meshSimplifier.SetUVs(0, UV4D);
        meshSimplifier.SimplifyMesh(LODQuality.LOD3);
        LOD3 = meshSimplifier.ToMesh();
        meshLODs[3] = LOD3;

        return meshLODs;
    }

    private void CreateLODs()
    {
        if (!createTileLOD)
        {
            return;
        }

        LODGroup detailGroup = tiles.AddComponent<LODGroup>();

        QualitySettings.lodBias = LODBias;

        List<MeshRenderer> detail0Renderers = new List<MeshRenderer>();
        List<MeshRenderer> detail1Renderers = new List<MeshRenderer>();
        List<MeshRenderer> detail2Renderers = new List<MeshRenderer>();
        List<MeshRenderer> detail3Renderers = new List<MeshRenderer>();

        for (int i = 0; i < tiles.transform.childCount; i++)
        {
            GameObject currentTile = tiles.transform.GetChild(i).gameObject;

            Mesh currentMesh = currentTile.GetComponent<MeshFilter>().sharedMesh;
            Mesh[] LODs = GetLODs(currentMesh);

            MeshFilter meshFilter0 = currentTile.GetComponent<MeshFilter>();
            meshFilter0.sharedMesh = LODs[0];
            MeshRenderer renderer0 = currentTile.GetComponent<MeshRenderer>();
            detail0Renderers.Add(renderer0);

            GameObject LOD1 = new GameObject("Tile_LOD1");
            LOD1.transform.parent = currentTile.transform;
            LOD1.transform.position = currentTile.transform.position;
            MeshFilter meshFilter1 = LOD1.AddComponent<MeshFilter>();
            meshFilter1.sharedMesh = LODs[1];
            MeshRenderer renderer1 = LOD1.AddComponent<MeshRenderer>();
            renderer1.sharedMaterial = basicMaterial;
            detail1Renderers.Add(renderer1);

            GameObject LOD2 = new GameObject("Tile_LOD2");
            LOD2.transform.parent = currentTile.transform;
            LOD2.transform.position = currentTile.transform.position;
            MeshFilter meshFilter2 = LOD2.AddComponent<MeshFilter>();
            meshFilter2.sharedMesh = LODs[2];
            MeshRenderer renderer2 = LOD2.AddComponent<MeshRenderer>();
            renderer2.sharedMaterial = basicMaterial;
            detail2Renderers.Add(renderer2);

            GameObject LOD3 = new GameObject("Tile_LOD3");
            LOD3.transform.parent = currentTile.transform;
            LOD3.transform.position = currentTile.transform.position;
            MeshFilter meshFilter3 = LOD3.AddComponent<MeshFilter>();
            meshFilter3.sharedMesh = LODs[3];
            MeshRenderer renderer3 = LOD3.AddComponent<MeshRenderer>();
            renderer3.sharedMaterial = basicMaterial;
            detail3Renderers.Add(renderer3);
        }

        LODdetail0 = new LOD(1.0F / (0 + 1), detail0Renderers.ToArray());
        LODdetail1 = new LOD(1.0F / (1 + 1), detail1Renderers.ToArray());
        LODdetail2 = new LOD(1.0F / (2 + 1), detail2Renderers.ToArray());
        LODdetail3 = new LOD(1.0F / (3 + 1), detail3Renderers.ToArray());

        LOD[] details = new LOD[4];

        details[0] = LODdetail0;
        details[1] = LODdetail1;
        details[2] = LODdetail2;
        details[3] = LODdetail3;

        detailGroup.SetLODs(details);
    }

    public void Generate()
    {
        GenerateMesh();
        BlurHeightMap();
        SampleHeightMap();
        DivideMesh();
        CreateLODs();
    }
}
