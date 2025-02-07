using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform playerTransform;
    public float heightOffset = 0f; 
    public float depthOffset = 0f;
    public Camera cam;
    private Vector3 dragOrigin;
    private Controls controls;
    private bool isDragging = false;
    
    // For minimap smoothing (if desired)
    private Vector3 _cameraVelocity = Vector3.zero;
    public float minimapSmoothTime = 0.2f;
    
    // Reference to the minimap element (assign in inspector or find at runtime)
    public CustomElements.MinimapElement minimap;
    

    void OnEnable(){
        controls = new Controls();
        controls.Player.Enable();
        controls.Player.MiddleBtn.performed += OnMiddleBtnDown;
        controls.Player.MiddleBtn.canceled += OnMiddleBtnReleased;
        controls.Player.Space.performed += OnSpacebarDown;
        
        // Subscribe to minimap events if needed.
        if(minimap != null)
            minimap.OnMinimapTargetSet += OnMinimapTargetSetHandler;
    }

    void OnDisable(){
        controls.Player.MiddleBtn.performed -= OnMiddleBtnDown;
        controls.Player.MiddleBtn.canceled -= OnMiddleBtnReleased;
        controls.Player.Space.performed -= OnSpacebarDown;
        controls.Player.Disable();
        
        if(minimap != null)
            minimap.OnMinimapTargetSet -= OnMinimapTargetSetHandler;
    }

    public void OnMiddleBtnDown (InputAction.CallbackContext context){
        if( GlobalState.GameModeManager.CurrentGameMode.CountdownActive || GlobalState.Paused) return;
        dragOrigin = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        isDragging = true;
    }

    public void OnMiddleBtnReleased (InputAction.CallbackContext context){
        if( GlobalState.GameModeManager.CurrentGameMode.CountdownActive || GlobalState.Paused) return;
        isDragging = false;
    }

    public void OnSpacebarDown (InputAction.CallbackContext context){
        if( GlobalState.GameModeManager.CurrentGameMode.CountdownActive || GlobalState.Paused) return;
        Vector3 cameraPosition = playerTransform.position;
        cameraPosition -= Vector3.forward * depthOffset;
        cameraPosition += Vector3.up * heightOffset;
        transform.position = cameraPosition;
    }

    // Called when the minimap (left click) sets a target.
    private void OnMinimapTargetSetHandler(Vector3 target) {
        // When not dragging with middle mouse, update the target.
        if (!isDragging)
            MoveCameraToTarget(target);
    }

    // Moves the camera to the given target instantly or with smoothing.
    private void MoveCameraToTarget(Vector3 target) {
        // You can choose either a smooth approach or immediate:
        // For smoothing, you might store a target and then use SmoothDamp in LateUpdate.
        // For immediate movement, simply set:
        Vector3 currentCamPos = cam.transform.position;
        // For smooth movement, for example:
        cam.transform.position = Vector3.SmoothDamp(currentCamPos, target, ref _cameraVelocity, minimapSmoothTime);
    }

    void LateUpdate(){
        // If middle mouse is dragging, update the camera position directly.
        if(isDragging){
            Vector3 dragMovement = dragOrigin - cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            cam.transform.position += dragMovement;
            // Also update the minimap viewport indicator based on the new camera position.
            if(minimap != null) {
                minimap.UpdateViewPortIndicatorFromCamera(cam.transform.position);
            }
        }
        // Else, let the camera move toward the target set by the minimap (handled in OnMinimapTargetSetHandler).
    }
}
