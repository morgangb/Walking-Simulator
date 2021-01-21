using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowDeformationManager : MonoBehaviour
{
    [Header("Setup")]
    public GameObject snowDefomationModule;
    [Range(0,31)]
    public int objectLayer = 9;
    [Range(0,31)]
    public int groundLayer = 10;

    [Header("Deformation Settings")]
    public Texture2D displacementMap;
    [Range(0,1)]
    public float displacement = 0.2f;
    [Range(5,64)]
    public float tessellationEdgeLength = 16f;
    public float accuracyInMeters = 0.01953125f;
    public SnowRenderTexturePipeline.DownSampleMode downSampleMode;
    [Range(0, 8)]
    public int blurIterations = 1;

    [Header("Snowfall")]
    public bool snowfallEnabled;
    [Range(0, 0.1f)]
    public float flakeAmount;
    [Range(0, 0.1f)]
    public float flakeStrength;

    [Header("Shaders")]
    public Shader snowShader;
    public Shader blurShader;
    public Shader normalizeShader;
    public Shader snowfallShader;

    [Header("Texture")]
    public Texture snow;
    public Color snowColor = Color.white;
    public Texture ground;
    public Color groundColor = Color.white;
    public Texture2D normalMap;
    public float bumpScale;
    [Range(0, 1)]
    public float metallic = 0f;
    [Range(0, 1)]
    public float smoothness = 0.5f;
    public Texture2D detail;
    public Texture2D detailNormals;
    public float detailBumpScale;

    private GameObject[] tiles;
    private GameObject SDM;
    private SnowRenderTexturePipeline SRTP;

    private void Start()
    {
        tiles = GameObject.FindGameObjectsWithTag("Tile");

        if (tiles.Length == 0)
        {
            Debug.LogWarning("No tiles found in scene");
            return;
        }

        if (groundLayer == objectLayer)
        {
            Debug.LogWarning("WARNING: Object Layer and Ground Layer are the same. Snow deformation will not work as intended");
        }

        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].layer = groundLayer;

            Bounds tileLocalBounds = tiles[i].GetComponent<MeshFilter>().sharedMesh.bounds;

            SDM = Instantiate(snowDefomationModule, tiles[i].transform);
            SDMControl moduleControl = SDM.GetComponent<SDMControl>();
            Vector3 tilePos = tiles[i].transform.position;
            Vector3 meshCenter = tileLocalBounds.center;
            SDM.transform.localPosition = new Vector3(meshCenter.x, 0, meshCenter.z);
            float meshSize = tileLocalBounds.extents.z;
            float near = tileLocalBounds.min.y;
            float far = displacement + tileLocalBounds.max.y;
            Camera objectCam = SDM.transform.GetChild(0).GetComponent<Camera>();
            objectCam.orthographicSize = meshSize;
            objectCam.nearClipPlane = near;
            objectCam.farClipPlane = far;
            objectCam.cullingMask = 1 << objectLayer;
            Camera groundCam = SDM.transform.GetChild(1).GetComponent<Camera>();
            groundCam.orthographicSize = meshSize;
            groundCam.nearClipPlane = 0;
            groundCam.farClipPlane = far - near;
            groundCam.cullingMask = 1 << groundLayer;
            SDM.transform.GetChild(1).localPosition = new Vector3(0, far, 0);
            moduleControl.SetObjectLayer(objectLayer);

            SRTP = SDM.transform.GetChild(0).GetComponent<SnowRenderTexturePipeline>();
            SRTP.displacementMap = displacementMap;
            SRTP.displacement = displacement;
            SRTP.tessellationEdgeLength = tessellationEdgeLength;
            float tileLengthX = tileLocalBounds.extents.x * 2;
            float tileLengthZ = tileLocalBounds.extents.z * 2;
            SRTP.resolutionX = (int)(tileLengthX / accuracyInMeters);
            SRTP.resolutionY = (int)(tileLengthZ / accuracyInMeters);
            SRTP.downSampleMode = downSampleMode;
            SRTP.blurIterations = blurIterations;
            SRTP.snowfallEnabled = snowfallEnabled;
            SRTP.flakeAmount = flakeAmount;
            SRTP.flakeStrength = flakeStrength;
            SRTP.snowShader = snowShader;
            SRTP.blurShader = blurShader;
            SRTP.normalizeShader = normalizeShader;
            SRTP.snowfallShader = snowfallShader;
            SRTP.snow = snow;
            SRTP.snowColor = snowColor;
            SRTP.ground = ground;
            SRTP.groundColor = groundColor;
            SRTP.normalMap = normalMap;
            SRTP.bumpScale = bumpScale;
            SRTP.metallic = metallic;
            SRTP.smoothness = smoothness;
            SRTP.detail = detail;
            SRTP.detailNormals = detailNormals;
            SRTP.detailBumpScale = detailBumpScale;
        }
    }
}
