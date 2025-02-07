using CustomElements;
using Multiplayer.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Multiplayer_Camera : NetworkBehaviour
{
    public Transform playerTransform;
    public float heightOffset = 0f; 
    public float depthOffset = 0f;
    public Camera cam;
    private Vector3 dragOrigin; // for middle mouse drag
    private Controls controls;
    private bool isMiddleMouseDragging = false;
    private GlobalState globalState;
    
    // Reference to the minimap element (set externally via SetMinimap)
    private MinimapElement _minimap;
    
    // For delta-based minimap dragging.
    private bool isMinimapDragging = false;
    private Vector2 minimapDragOrigin = Vector2.zero;      // pointer position at drag start (minimap space)
    private Vector3 cameraPosAtMinimapDragStart = Vector3.zero; // camera position at drag start
    
    private Vector3 _cameraVelocity = Vector3.zero;
    public float minimapSmoothTime = 0.2f;
    
    void Start(){
        globalState = GameObject.Find("Global State").GetComponent<GlobalState>();
    }
    
    // Called externally (e.g., by GameView) to assign the minimap.
    public void SetMinimap(MinimapElement minimapElement) {
        _minimap = minimapElement;
        _minimap.OnMinimapTargetSet += OnMinimapTargetSetHandler;
    }
    
    public void SetTarget(GameObject player) {
        playerTransform = player.transform;
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
        if(_minimap != null)
            _minimap.OnMinimapTargetSet -= OnMinimapTargetSetHandler;
    }
    
    public void OnMiddleBtnDown (InputAction.CallbackContext context){
        if(GlobalState.GameModeManager.CurrentGameMode.CountdownActive || GlobalState.Paused) return;
        dragOrigin = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        isMiddleMouseDragging = true;
    }
    
    public void OnMiddleBtnReleased (InputAction.CallbackContext context){
        if(GlobalState.GameModeManager.CurrentGameMode.CountdownActive || GlobalState.Paused) return;
        isMiddleMouseDragging = false;
    }
    
    public void OnSpacebarDown (InputAction.CallbackContext context){
        if(GlobalState.GameModeManager.CurrentGameMode.CountdownActive || GlobalState.Paused) return;
        Vector3 cameraPosition = playerTransform.position;
        cameraPosition -= Vector3.forward * depthOffset;
        cameraPosition += Vector3.up * heightOffset;
        transform.position = cameraPosition;
        if(_minimap != null)
            _minimap.UpdateViewPortIndicatorFromCamera(cam.transform.position);
    }
    
    // We ignore the OnMinimapTargetSet event for delta-based dragging.
    private void OnMinimapTargetSetHandler(Vector3 target) {
        // (If you want to support clicks that jump the camera, you could use this event.)
        // For this delta-based approach, we let the drag session handle camera movement.
    }
    
    void LateUpdate(){
        // Middle mouse drag.
        if(isMiddleMouseDragging){
            Vector3 dragMovement = dragOrigin - cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            cam.transform.position += dragMovement;
            if(_minimap != null)
                _minimap.UpdateViewPortIndicatorFromCamera(cam.transform.position);
            // Cancel minimap dragging if middle mouse is active.
            isMinimapDragging = false;
        }
        // Minimap drag (left mouse).
        else if(_minimap != null && _minimap.IsDragging)
        {
            // On drag start, record the starting values.
            if(!isMinimapDragging) {
                isMinimapDragging = true;
                minimapDragOrigin = _minimap.ViewPortCenter;
                cameraPosAtMinimapDragStart = cam.transform.position;
            }
            // Compute the delta in minimap space.
            Vector2 currentMinimapPos = _minimap.ViewPortCenter;
            Vector2 delta = currentMinimapPos - minimapDragOrigin;
            Vector3 worldDelta = ConvertMinimapDeltaToWorldDelta(delta, _minimap.contentRect);
            Vector3 desiredCameraPos = cameraPosAtMinimapDragStart + worldDelta;
            cam.transform.position = Vector3.SmoothDamp(cam.transform.position, desiredCameraPos, ref _cameraVelocity, minimapSmoothTime);
            // Update viewport indicator based on camera position.
            //_minimap.UpdateViewPortIndicatorFromCamera(cam.transform.position);
        }
        else {
            isMinimapDragging = false;
        }
    }
    
    /// <summary>
    /// Converts a minimap delta (in local minimap pixels) to a world-space delta.
    /// Adjust the Y inversion so that dragging up moves the camera up.
    /// </summary>
    private Vector3 ConvertMinimapDeltaToWorldDelta(Vector2 minimapDelta, Rect minimapRect)
    {
        float normX = minimapDelta.x / minimapRect.width;
        // Invert the Y delta if necessary. Experiment with sign.
        float normY = -minimapDelta.y / minimapRect.height;
        // World extents.
        float worldWidth = _minimap != null ? (_minimap.WorldMax.x - _minimap.WorldMin.x) : 120f;
        float worldHeight = _minimap != null ? (_minimap.WorldMax.y - _minimap.WorldMin.y) : 120f;
        Vector3 worldDeltaUnrotated = new Vector3(normX * worldWidth, 0, normY * worldHeight);
        Vector3 worldDelta = Quaternion.Euler(0, cam.transform.eulerAngles.y, 0) * worldDeltaUnrotated;
        return worldDelta;
    }
}
