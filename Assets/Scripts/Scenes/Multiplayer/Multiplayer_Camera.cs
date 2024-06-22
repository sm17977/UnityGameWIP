using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Multiplayer_Camera : NetworkBehaviour
{
    public Transform playerTransform;
    public float heightOffset = 0f; 
    public float depthOffset = 0f;
    public Camera cam;
    private Vector3 dragOrigin;
    private Controls controls;
    private bool isDragging = false;
    private GlobalState globalState;
    
    void Start(){
        globalState = GameObject.Find("Global State").GetComponent<GlobalState>();
    }


    void Update(){

        if(!IsClient || !IsLocalPlayer){
            return;
        }

        if(playerTransform == null){
            playerTransform = GameObject.Find("Lux_Player(Clone)").GetComponent<Transform>();
        }
    }


    void OnEnable(){
        controls = new Controls();
        controls.Player.Enable();
        controls.Player.MiddleBtn.performed += OnMiddleBtnDown;
        controls.Player.MiddleBtn.canceled += OnMiddleBtnReleased;
        controls.Player.Space.performed += OnSpacebarDown;
    }

    void OnDisable(){
        controls.Player.MiddleBtn.performed -= OnMiddleBtnDown;
        controls.Player.MiddleBtn.canceled -= OnMiddleBtnReleased;
        controls.Player.Space.performed -= OnSpacebarDown;
        controls.Player.Disable();
    }


    public void OnMiddleBtnDown (InputAction.CallbackContext context){
        if(globalState.Arena.CountdownActive || GlobalState.Paused) return;
        dragOrigin = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        isDragging = true;
    }

    public void OnMiddleBtnReleased (InputAction.CallbackContext context){
        if(globalState.Arena.CountdownActive || GlobalState.Paused) return;
        isDragging = false;
    }

    public void OnSpacebarDown (InputAction.CallbackContext context){
        if(globalState.Arena.CountdownActive || GlobalState.Paused) return;
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
