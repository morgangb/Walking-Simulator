using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDMControl : MonoBehaviour
{
    private int objectLayer = 9;
    private BoxCollider boxCollider;
    private int objectCount;
    private SnowRenderTexturePipeline SRTP; 
    private Camera objectCam;
    private Camera groundCam;

    public void SetObjectLayer(int layer)
    {
        objectLayer = layer;
    }

    private void Start()
    {
        SRTP = transform.GetChild(0).GetComponent<SnowRenderTexturePipeline>();

        Bounds meshLocalBounds = transform.parent.GetComponent<MeshFilter>().sharedMesh.bounds;
        Bounds meshWorldBounds = transform.parent.GetComponent<MeshRenderer>().bounds;
        float colliderHeight = SRTP.groundCam.farClipPlane;
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.center = new Vector3(0, meshLocalBounds.center.y + SRTP.displacement / 2f, 0);
        boxCollider.size = new Vector3(meshLocalBounds.extents.x * 2f, colliderHeight, meshLocalBounds.extents.z * 2f);

        SRTP.SetActive(true);

        objectCam = transform.GetChild(0).GetComponent<Camera>();
        objectCam.Render();
        groundCam = transform.GetChild(1).GetComponent<Camera>();
        groundCam.Render();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Tile") && other.gameObject.layer == objectLayer)
        {
            objectCount++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(!other.CompareTag("Tile") && other.gameObject.layer == objectLayer)
        {
            objectCount--;
        }
    }

    private void Update()
    {
        if(SRTP == null)
        {
            Debug.Log("No SnowRenderTexturePipeline found.");
        }

        if (objectCount > 0)
        {
            SRTP.SetActive(true);
            objectCam.enabled = true;
            groundCam.enabled = true;
        }
        else
        {
            SRTP.SetActive(false);
            objectCam.enabled = false;
            groundCam.enabled = false;
        }
    }
}
