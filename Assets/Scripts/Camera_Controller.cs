using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Controller : MonoBehaviour
{
    public Transform playerTransform;
    public float heightOffset = 0f; 
    public float depthOffset = 0f;
    public Camera cam;
    private Vector3 dragOrigin;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(Input.GetKeyDown(KeyCode.Space)){
            Vector3 cameraPosition = playerTransform.position;
            cameraPosition -= Vector3.forward * depthOffset;
            cameraPosition += Vector3.up * heightOffset;
            transform.position = cameraPosition;
        }

        if(Input.GetMouseButtonDown(2)){
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if(Input.GetMouseButton(2)){
            Vector3 dragMovement = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            cam.transform.position += dragMovement;
        }


    }
}
