using System;
using CustomElements;
using Mono.CSharp;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MultiplayerCamera : NetworkBehaviour {
    
    public Transform playerTransform;
    public GameObject terrain;
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
    private readonly float _minCameraSize = 2f;
    private float _currentCameraSize;

    public float boundsPadding = 0f;
    private Vector2 _minCamPos;
    private Vector2 _maxCamPos;
    
    
    private float _recenterHeight;

    public void Start() {
        cam.orthographicSize = _defaultCameraSize;
        _currentCameraSize = _defaultCameraSize;
        _recenterHeight = transform.position.y;
        RecalculateBounds();
    }

    private void RecalculateBounds() {
        var groundPlane = new Plane(Vector3.up, Vector3.zero);
        var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (groundPlane.Raycast(ray, out var enter)) {
            var hitPoint = ray.GetPoint(enter);
            var offset = transform.position - hitPoint;
            var centerCamPos = Vector3.zero + offset;

            float horizExtent = cam.orthographicSize * cam.aspect;

            var topRay = cam.ViewportPointToRay(new Vector3(0.5f, 1f, 0));
            var bottomRay = cam.ViewportPointToRay(new Vector3(0.5f, 0f, 0));
            
            groundPlane.Raycast(topRay, out var topDist);
            groundPlane.Raycast(bottomRay, out var bottomDist);

            Vector3 topHit = topRay.GetPoint(topDist);
            Vector3 bottomHit = bottomRay.GetPoint(bottomDist);

            float verticalExtentOnGround = (topHit.z - bottomHit.z) / 2f;

            var halfWidth = (terrain.transform.lossyScale.x / 2f) - horizExtent + boundsPadding;
            var halfDepth = (terrain.transform.lossyScale.z / 2f) - verticalExtentOnGround + boundsPadding;

            halfWidth = Mathf.Max(0, halfWidth);
            halfDepth = Mathf.Max(0, halfDepth);

            _minCamPos = new Vector2(centerCamPos.x - halfWidth, centerCamPos.z - halfDepth);
            _maxCamPos = new Vector2(centerCamPos.x + halfWidth, centerCamPos.z + halfDepth);

            Debug.Log("Min vam pos:  " + _minCamPos + "m Max vam pos:  " + _maxCamPos);
        }
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
        
        if(_currentCameraSize <= _minCameraSize && scrollDelta > 0) return; 
        if(_currentCameraSize >= _defaultCameraSize && scrollDelta < 0) return;
        
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

        Vector3 forward = transform.forward;
        
        float t = (playerTransform.position.y - _recenterHeight) / forward.y;
        Vector3 cameraPosition = playerTransform.position - forward * t;
        
        cameraPosition -= forward * depthOffset;     
        cameraPosition += Vector3.up * heightOffset; 
        cameraPosition = GetClampedPosition(cameraPosition);

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
            
            cam.transform.position = GetClampedPosition(cam.transform.position + dragMovement);

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
            var smoothPos = Vector3.SmoothDamp(cam.transform.position, desiredCameraPos, ref _cameraVelocity, minimapSmoothTime);
            cam.transform.position = GetClampedPosition(smoothPos);
        }
        else {
            _isMinimapDragging = false;
        }
    }

    private Vector3 GetClampedPosition(Vector3 targetPosition) {
        float x = Mathf.Clamp(targetPosition.x, _minCamPos.x, _maxCamPos.x);
        float z = Mathf.Clamp(targetPosition.z, _minCamPos.y, _maxCamPos.y); 
        return new Vector3(x, targetPosition.y, z);
    }

    /// <summary>
    /// Converts a minimap delta (in local minimap pixels) to a world-space delta
    /// Adjust the Y inversion so that dragging up moves the camera up
    /// </summary>
    private Vector3 ConvertMinimapDeltaToWorldDelta(Vector2 minimapDelta, Rect minimapRect) {
        var normX = minimapDelta.x / minimapRect.width;
        var normY = -minimapDelta.y / minimapRect.height;
        var worldWidth = _minimap.WorldMax.x - _minimap.WorldMin.x;
        var worldHeight = _minimap.WorldMax.y - _minimap.WorldMin.y;
        var worldDeltaUnrotated = new Vector3(normX * worldWidth, 0, normY * worldHeight);
        var worldDelta = Quaternion.Euler(0, cam.transform.eulerAngles.y, 0) * worldDeltaUnrotated;
        return worldDelta;
    }
}