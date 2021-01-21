using UnityEngine;

public class SnowRenderTexturePipeline : MonoBehaviour {

    public enum DownSampleMode { Off, Half, Quarter }

    public Texture displacementMap;
    [Range(0, 1)]
    public float displacement = 0.2f;
    [Range(5, 64)]
    public float tessellationEdgeLength = 16f;
    public int resolutionX = 512;
    public int resolutionY = 512;
    public DownSampleMode downSampleMode = DownSampleMode.Off;
    [Range(0, 8)]
    public int blurIterations = 1;
    public bool snowfallEnabled;
    [Range(0, 0.1f)]
    public float flakeAmount;
    [Range(0, 0.1f)]
    public float flakeStrength;
    public Shader snowShader;
    public Shader blurShader;
    public Shader normalizeShader;
    public Shader snowfallShader;
    public Texture snow;
    public Color snowColor = Color.white;
    public Texture ground;
    public Color groundColor = Color.white;
    public Texture2D normalMap;
    public float bumpScale;
    [Range(0,1)]
    public float metallic = 0f;
    [Range(0,1)]
    public float smoothness = 0.5f;
    public Texture2D detail;
    public Texture2D detailNormals;
    public float detailBumpScale;

    Material snowMat;
    Material blurMat;
    Material normalizeMat;
    Material snowfallMat;

    RenderTexture objectDepth;
    RenderTexture groundDepth;
    RenderTexture depthTexture;
    RenderTexture blurredDepthTexture;
    RenderTexture offObjectDepth;
    
    Camera objectCam;
    public Camera groundCam;

    bool active = true;

    public void SetActive(bool flag)
    {
        active = flag;
    }

    private void Start()
    {
        snowMat = new Material(snowShader);
        snowMat.hideFlags = HideFlags.HideInHierarchy;
        snowMat.SetTexture("_DispMap", displacementMap);
        snowMat.SetFloat("_Displacement", displacement);
        snowMat.SetFloat("_TessellationEdgeLength", tessellationEdgeLength);
        snowMat.SetTexture("_SnowTex", snow);
        snowMat.SetColor("_SnowColor", snowColor);
        snowMat.SetTexture("_GroundTex", ground);
        snowMat.SetColor("_GroundColor", groundColor);
        snowMat.SetTexture("_NormalMap", normalMap);
        snowMat.SetFloat("_BumpScale", bumpScale);
        snowMat.SetFloat("_Metallic", metallic);
        snowMat.SetFloat("_Smoothness", smoothness);
        snowMat.SetTexture("_DetailTex", detail);
        snowMat.SetTexture("_DetailNormalMap", detailNormals);
        snowMat.SetFloat("_DetailBumpScale", detailBumpScale);
        transform.parent.transform.parent.GetComponent<Renderer>().material = snowMat;

        snowfallMat = new Material(snowfallShader);
        snowfallMat.SetFloat("_FlakeAmount", flakeAmount);
        snowfallMat.SetFloat("_FlakeStrength", flakeStrength);

        // If LODs 1 - 3 are attached to LOD 0...
        if (transform.parent.parent.childCount > 1)
        {
            Transform tileTransform = transform.parent.parent;
            tileTransform.GetChild(0).GetComponent<MeshRenderer>().material = snowMat;
            tileTransform.GetChild(1).GetComponent<MeshRenderer>().material = snowMat;
            tileTransform.GetChild(2).GetComponent<MeshRenderer>().material = snowMat;
        }

        blurMat = new Material(blurShader);
        blurMat.hideFlags = HideFlags.HideInHierarchy;
        normalizeMat = new Material(normalizeShader);
        normalizeMat.hideFlags = HideFlags.HideInHierarchy;

        depthTexture = new RenderTexture(resolutionX, resolutionY, 24, RenderTextureFormat.Depth);
        objectDepth = new RenderTexture(depthTexture.width, depthTexture.height, 24, depthTexture.format);
        groundDepth = new RenderTexture(depthTexture.width, depthTexture.height, 24, depthTexture.format);
        blurredDepthTexture = new RenderTexture(depthTexture.width, depthTexture.height, 24, depthTexture.format);
        offObjectDepth = new RenderTexture(depthTexture.width, depthTexture.height, 24, depthTexture.format);

        objectCam = GetComponent<Camera>();
        objectCam.targetTexture = objectDepth;
        groundCam.targetTexture = groundDepth;

        normalizeMat.SetTexture("_ObjectTex", objectDepth);
        normalizeMat.SetTexture("_GroundTex", groundDepth);
        normalizeMat.SetFloat("_Displacement", displacement);
        normalizeMat.SetFloat("_NearClip", objectCam.nearClipPlane);
        normalizeMat.SetFloat("_FarClip", objectCam.farClipPlane);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if(!active)
        {
            return;
        }

        RenderTexture rt1, rt2;

        if(snowfallEnabled)
        {
            RenderTexture tempRT = RenderTexture.GetTemporary(depthTexture.width, depthTexture.height, 24, depthTexture.format);

            snowfallMat.SetTexture("_MyDepthTex", objectDepth);
            Graphics.Blit(objectDepth, tempRT, snowfallMat, 0);
            snowfallMat.SetTexture("_MyDepthTex", tempRT);
            Graphics.Blit(tempRT, objectDepth, snowfallMat, 1);

            RenderTexture.ReleaseTemporary(tempRT);
        }

        Graphics.Blit(groundDepth, depthTexture, normalizeMat);
        blurMat.SetTexture("_MyDepthTex", depthTexture);

        if (downSampleMode == DownSampleMode.Half)
        {
            rt1 = RenderTexture.GetTemporary(depthTexture.width / 2, depthTexture.height / 2, 24, depthTexture.format);
            rt2 = RenderTexture.GetTemporary(depthTexture.width / 2, depthTexture.height / 2, 24, depthTexture.format);
            Graphics.Blit(depthTexture, rt1, blurMat, 0);
        }
        else if (downSampleMode == DownSampleMode.Quarter)
        {
            rt1 = RenderTexture.GetTemporary(depthTexture.width / 4, depthTexture.height / 4, 24, depthTexture.format);
            rt2 = RenderTexture.GetTemporary(depthTexture.width / 4, depthTexture.height / 4, 24, depthTexture.format);
            Graphics.Blit(depthTexture, rt1, blurMat, 1);
        }
        else
        {
            rt1 = RenderTexture.GetTemporary(depthTexture.width, depthTexture.height, 24, depthTexture.format);
            rt2 = RenderTexture.GetTemporary(depthTexture.width, depthTexture.height, 24, depthTexture.format);
            Graphics.Blit(depthTexture, rt1, blurMat, 0);
        }

        for (var i = 0; i < blurIterations; i++)
        {
            blurMat.SetTexture("_MyDepthTex", rt1);
            Graphics.Blit(rt1, rt2, blurMat, 2);
            blurMat.SetTexture("_MyDepthTex", rt2);
            Graphics.Blit(rt2, rt1, blurMat, 3);
        }

        blurMat.SetTexture("_MyDepthTex", rt1);
        Graphics.Blit(rt1, blurredDepthTexture, blurMat, 0);

        snowMat.SetTexture("_DispTex", blurredDepthTexture);

        RenderTexture.ReleaseTemporary(rt1);
        RenderTexture.ReleaseTemporary(rt2);
    }
}
