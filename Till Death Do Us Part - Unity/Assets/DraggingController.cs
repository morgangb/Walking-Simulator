using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

// Require other components needed for this behaviour to work
[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(ConfigurableJoint))]
public class DraggingController : MonoBehaviour
{
    // References to other components on this GameObject
    private FirstPersonController myFPSController;
    private ConfigurableJoint myJoint;

    // Parameters for the FPSController to affect grabbing
    [SerializeField] private float minimumX;
    [SerializeField] private float draggingLookSpeed;
    [SerializeField] private float draggingMoveSpeed;

    void Start()
    {
        // Get components on this GameObject
        myFPSController = GetComponentInParent<FirstPersonController>();
        myJoint = GetComponent<ConfigurableJoint>();
    }

    void FixedUpdate()
    {
        // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = 1 << 8;

        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        layerMask = ~layerMask;

        RaycastHit hit;

        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, layerMask))
        {
            if (hit.transform.tag == "Corpse")
            {
                if (Input.GetButton("Fire1") && !myJoint.connectedBody)
                {
                    myJoint.connectedBody = hit.transform.gameObject.GetComponent<Rigidbody>();

                    ToggleDragging(true);
                }
            }
        }

        if (Input.GetButton("Fire2") && myJoint.connectedBody)
        {
            myJoint.connectedBody = null;

            ToggleDragging(false);
        }
    }

    private void ToggleDragging(bool toggle)
    {
        float lookSpeed = 0f;
        float moveSpeed = 0f;

        // Set lookSpeed (what we are multiplying sensitivity by) and moveSpeed (what we are multiplying movement by)
        if (toggle)
        {
            lookSpeed = draggingLookSpeed;
            moveSpeed = draggingMoveSpeed;

            myFPSController.m_MouseLook.MinimumX = minimumX;
        }
        else
        {
            lookSpeed = 1 / draggingLookSpeed;
            moveSpeed = 1 / draggingMoveSpeed;

            myFPSController.m_MouseLook.MinimumX = -90;
        }

        // Multiply the FPSController values by the lookSpeed and moveSpeed
        myFPSController.m_MouseLook.XSensitivity *= lookSpeed;
        myFPSController.m_MouseLook.YSensitivity *= lookSpeed;

        myFPSController.m_WalkSpeed *= moveSpeed;
        myFPSController.m_RunSpeed *= moveSpeed;
    }
}
