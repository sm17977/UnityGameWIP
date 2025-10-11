using System;
using CustomElements;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MultiplayerCamera : NetworkBehaviour {
    public Transform playerTransform;
    private InputProcessor _inputProcessor;
    public float heightOffset = 0f;
    public float depthOffset = 0f;
    public Camera cam;
    private MinimapElement _minimap;
    private Controls _controls;
    
    private bool _isMiddleMouseDragging;
    private Vector3 _dragOrigin;
    
    private bool _isMinimapDragging;
    private Vector2 _minimapDragOrigin = Vector2.zero;
    private Vector3 _cameraPosAtMinimapDragStart = Vector3.zero; 
    private Vector3 _cameraVelocity = Vector3.zero;
    public float minimapSmoothTime = 0.01f;

    private readonly float _defaultCameraSize = 7;
    private float _currentCameraSize;

    public void Start() {
        cam.orthographicSize = _defaultCameraSize;
        _currentCameraSize = _defaultCameraSize;
    }

    public void SetMinimap(MinimapElement minimapElement) {
        _minimap = minimapElement;
        _minimap.OnMinimapTargetSet += OnMinimapTargetSetHandler;
    }

    public void SetTarget(GameObject player) {
        playerTransform = player.transform;
        _inputProcessor = player.GetComponent<InputProcessor>();
    }

    public void SetInput() {
        _controls = new Controls();
        _controls.Player.Enable();
        _controls.Player.MiddleBtn.performed += OnMiddleBtnDown;
        _controls.Player.MiddleBtn.canceled += OnMiddleBtnReleased;
        _controls.Player.Space.performed += OnSpacebarDown;
        _controls.Player.MouseScroll.performed += OnMouseScroll;
    }

    private void OnEnable() {
 
    }

    private void OnDisable() {
        _controls.Player.MiddleBtn.performed -= OnMiddleBtnDown;
        _controls.Player.MiddleBtn.canceled -= OnMiddleBtnReleased;
        _controls.Player.Space.performed -= OnSpacebarDown;
        _controls.Player.Disable();
        if (_minimap != null)
            _minimap.OnMinimapTargetSet -= OnMinimapTargetSetHandler;
    }

    private void OnMouseScroll(InputAction.CallbackContext context) {
        if (GlobalState.GameModeManager.CurrentGameMode.CountdownActive || GlobalState.Paused) return;
        if(_inputProcessor.isTyping) return;
        var scrollDelta = context.ReadValue<float>();
        
        if(_currentCameraSize <= 1 && scrollDelta > 0) return; 
        _currentCameraSize -= scrollDelta/2;
        cam.orthographicSize = _currentCameraSize;
    }

    private void OnMiddleBtnDown(InputAction.CallbackContext context) {
        if (GlobalState.GameModeManager.CurrentGameMode.CountdownActive || GlobalState.Paused) return;
        if(_inputProcessor.isTyping) return;
        _dragOrigin = GetWorldPointAtCameraHeight(Mouse.current.position.ReadValue());
        _isMiddleMouseDragging = true;
    }

    private void OnMiddleBtnReleased(InputAction.CallbackContext context) {
        if (GlobalState.GameModeManager.CurrentGameMode.CountdownActive || GlobalState.Paused) return;
        if(_inputProcessor.isTyping) return;
        _isMiddleMouseDragging = false;
    }

    private void OnSpacebarDown(InputAction.CallbackContext context) {
        if (GlobalState.GameModeManager.CurrentGameMode.CountdownActive || GlobalState.Paused) return;
        if(_inputProcessor.isTyping) return;
        var cameraPosition = playerTransform.position;
        cameraPosition -= Vector3.forward * depthOffset;
        cameraPosition += Vector3.up * heightOffset;
        transform.position = cameraPosition;
        if (_minimap != null)
            _minimap.UpdateViewPortIndicatorFromCamera(cam.transform.position);
    }
    
    private void OnMinimapTargetSetHandler(Vector3 target) {
        cam.transform.position = target;
    }
    
    private Vector3 GetWorldPointAtCameraHeight(Vector2 screenPosition) {
        var ray = cam.ScreenPointToRay(screenPosition);
        var fixedY = cam.transform.position.y;
        var plane = new Plane(Vector3.up, new Vector3(0, fixedY, 0));
        if (plane.Raycast(ray, out var distance)) return ray.GetPoint(distance);
        return cam.transform.position;
    }
    
    private void LateUpdate() {
        if (_isMiddleMouseDragging) {
            var currentPoint = GetWorldPointAtCameraHeight(Mouse.current.position.ReadValue());
            var dragMovement = _dragOrigin - currentPoint;
            cam.transform.position += dragMovement;

            if (_minimap != null)
                _minimap.UpdateViewPortIndicatorFromCamera(cam.transform.position);
            
            _isMinimapDragging = false;
        }
        else if (_minimap != null && _minimap.IsDragging) {
            if (!_isMinimapDragging) {
                _isMinimapDragging = true;
                _minimapDragOrigin = _minimap.ViewPortCenter;
                _cameraPosAtMinimapDragStart = cam.transform.position;
            }
            
            var currentMinimapPos = _minimap.ViewPortCenter;
            var delta = currentMinimapPos - _minimapDragOrigin;
            var worldDelta = ConvertMinimapDeltaToWorldDelta(delta, _minimap.contentRect);
            var desiredCameraPos = _cameraPosAtMinimapDragStart + worldDelta;
            cam.transform.position = Vector3.SmoothDamp(cam.transform.position, desiredCameraPos, ref _cameraVelocity,
                minimapSmoothTime);
        }
        else {
            _isMinimapDragging = false;
        }
    }

    /// <summary>
    /// Converts a minimap delta (in local minimap pixels) to a world-space delta.
    /// Adjust the Y inversion so that dragging up moves the camera up.
    /// </summary>
    private Vector3 ConvertMinimapDeltaToWorldDelta(Vector2 minimapDelta, Rect minimapRect) {
        var normX = minimapDelta.x / minimapRect.width;
        // Invert the Y delta if necessary. Experiment with sign.
        var normY = -minimapDelta.y / minimapRect.height;
        var worldWidth = _minimap.WorldMax.x - _minimap.WorldMin.x;
        var worldHeight = _minimap.WorldMax.y - _minimap.WorldMin.y;
        var worldDeltaUnrotated = new Vector3(normX * worldWidth, 0, normY * worldHeight);
        var worldDelta = Quaternion.Euler(0, cam.transform.eulerAngles.y, 0) * worldDeltaUnrotated;
        return worldDelta;
    }
}