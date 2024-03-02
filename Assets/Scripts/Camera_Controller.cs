using UnityEngine;
using UnityEngine.InputSystem;

public class Camera_Controller : MonoBehaviour
{
    public Transform playerTransform;
    public float heightOffset = 0f; 
    public float depthOffset = 0f;
    public Camera cam;
    private Vector3 dragOrigin;
    private Controls controls;
    private bool isDragging = false;
    


    void Start(){
        controls = new Controls();
        controls.Player.Enable();
        controls.Player.MiddleBtn.performed += OnMiddleBtnDown;
        controls.Player.MiddleBtn.canceled += OnMiddleBtnReleased;
        controls.Player.Space.performed += OnSpacebarDown;
    }

    public void OnMiddleBtnDown (InputAction.CallbackContext context){
        dragOrigin = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        isDragging = true;
    }

    public void OnMiddleBtnReleased (InputAction.CallbackContext context){
       isDragging = false;
    }

    public void OnSpacebarDown (InputAction.CallbackContext context){
        Vector3 cameraPosition = playerTransform.position;
        cameraPosition -= Vector3.forward * depthOffset;
        cameraPosition += Vector3.up * heightOffset;
        transform.position = cameraPosition;
    }

    // Update is called once per frame
    void LateUpdate(){
        if(isDragging){
            Vector3 dragMovement = dragOrigin - cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            cam.transform.position += dragMovement;
        }
    }
}
