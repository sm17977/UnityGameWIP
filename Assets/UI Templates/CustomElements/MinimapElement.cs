using System.Collections.Generic;
using Multiplayer;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomElements {
    [UxmlElement]
    public partial class MinimapElement : VisualElement {
        private const string USSClassName = "minimap-element";
        
        private Dictionary<string, VisualElement> _playerMarkers = new Dictionary<string, VisualElement>();
        
        public Vector2 WorldMin = new Vector2(-30f, -30f);
        public Vector2 WorldMax = new Vector2(30f, 30f);
        private bool _showViewPort = true; 
        public bool IsDragging = false;
        public Vector2 ViewPortCenter; 
        private float _viewPortWidth = 50f;
        private float _viewPortHeight = 50f;
        private Vector2 _pointerDownPosition;
        private const float ClickThreshold = 5f; 
        
        public event System.Action<Vector3>OnMinimapTargetSet;

        public MinimapElement() {
            AddToClassList(USSClassName);
            pickingMode = PickingMode.Position; 
            generateVisualContent += GenerateVisualContent;
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }
        
        private void GenerateVisualContent(MeshGenerationContext mgc) {
            var painter = mgc.painter2D;
            Rect rect = contentRect;
            
            // Default the viewport center to the minimap center if not set.
            if (ViewPortCenter == Vector2.zero && rect.width > 0 && rect.height > 0) {
                ViewPortCenter = new Vector2(rect.width * 0.5f, rect.height * 0.5f);
            }
            if (_showViewPort) {
                DrawViewPort(painter, rect);
            }
        }
        
        private Vector2 WorldToMinimapPosition(Vector3 worldPos, Vector2 worldMin, Vector2 worldMax, Rect minimapRect) {
            float centerX = (worldMin.x + worldMax.x) * 0.5f;
            float centerZ = (worldMin.y + worldMax.y) * 0.5f;
            Vector3 worldCenter = new Vector3(centerX, 0f, centerZ);
            
            Vector3 relativePos = worldPos - worldCenter;
            float correctionAngle = -Camera.main.transform.eulerAngles.y;
            Quaternion correctionRotation = Quaternion.Euler(0f, correctionAngle, 0f);
            Vector3 rotatedPos = correctionRotation * relativePos;
            Vector3 correctedWorldPos = rotatedPos + worldCenter;
            
            float normalizedX = (correctedWorldPos.x - worldMin.x) / (worldMax.x - worldMin.x);
            float normalizedY = (correctedWorldPos.z - worldMin.y) / (worldMax.y - worldMin.y);
            float minimapX = normalizedX * minimapRect.width;
            float minimapY = (1 - normalizedY) * minimapRect.height;
            return new Vector2(minimapX, minimapY);
        }
        
        public void AddPlayerMarker(string playerId, Color markerColor) {
            VisualElement marker = new VisualElement();
            marker.style.width = 10;
            marker.style.height = 10;
            marker.style.backgroundColor = markerColor;
            marker.AddToClassList("minimap-player-marker");
            marker.style.unityTextAlign = TextAnchor.MiddleCenter;
            marker.style.position = Position.Absolute;
            Add(marker);
            _playerMarkers[playerId] = marker;
        }
        
        public void UpdatePlayerMarkersPosition(List<GameObject> players) {
            if (players == null) return;
            foreach (var player in players) {
                var playerScript = player.GetComponent<LuxPlayerController>();
                var playerId = player.name;
                if (_playerMarkers.TryGetValue(playerId, out VisualElement marker)) {
                    Rect currentRect = contentRect;
                    Vector2 minimapPos = WorldToMinimapPosition(playerScript.transform.position, WorldMin, WorldMax, currentRect);
                    marker.style.left = minimapPos.x - marker.resolvedStyle.width * 0.5f;
                    marker.style.top = minimapPos.y - marker.resolvedStyle.height * 0.5f;
                }
            }
        }
        
        public bool ContainsPlayerMarker(string playerId) {
            return _playerMarkers.ContainsKey(playerId);
        }
        
        public void ResetPlayerMarkers() {
            foreach (var marker in _playerMarkers) {
                Remove(marker.Value);
            }
            _playerMarkers = new Dictionary<string, VisualElement>();
        }
        
        private void OnPointerDown(PointerDownEvent evt) {
            if (evt.button != 0) return;
            if (!contentRect.Contains(evt.localPosition)) return;
            
            _pointerDownPosition = evt.localPosition;
            IsDragging = true;
            ViewPortCenter = evt.localPosition;
            _showViewPort = true;
            MarkDirtyRepaint();
            evt.StopPropagation();
            // For clicks, we can update target immediately.
            UpdateTargetCameraPosition();
        }
        
        private void OnPointerMove(PointerMoveEvent evt) {
            if (!IsDragging) return;
            ViewPortCenter = evt.localPosition;
            MarkDirtyRepaint();
            // Continuously update target during drag.
            //UpdateTargetCameraPosition();
        }
        
        private void OnPointerUp(PointerUpEvent evt) {
            if (evt.button != 0) return;
            float dist = Vector2.Distance(_pointerDownPosition, evt.localPosition);
            IsDragging = false;
            MarkDirtyRepaint();
            if (dist < ClickThreshold) {
                ViewPortCenter = evt.localPosition;
                UpdateTargetCameraPosition();
            }
        }
        
        private void OnPointerLeave(PointerLeaveEvent evt) {
            IsDragging = false;
        }
        
        private void DrawViewPort(Painter2D painter, Rect rect) {
            float halfWidth = _viewPortWidth * 0.5f;
            float halfHeight = _viewPortHeight * 0.5f;
            Vector2 topLeft = new Vector2(ViewPortCenter.x - halfWidth, ViewPortCenter.y - halfHeight);
            Vector2 topRight = new Vector2(topLeft.x + _viewPortWidth, topLeft.y);
            Vector2 bottomRight = new Vector2(topLeft.x + _viewPortWidth, topLeft.y + _viewPortHeight);
            Vector2 bottomLeft = new Vector2(topLeft.x, topLeft.y + _viewPortHeight);
            painter.strokeColor = Color.white;
            painter.lineWidth = 2f;
            painter.BeginPath();
            painter.MoveTo(topLeft);
            painter.LineTo(topRight);
            painter.LineTo(bottomRight);
            painter.LineTo(bottomLeft);
            painter.ClosePath();
            painter.Stroke();
        }
        
        private void UpdateTargetCameraPosition() {
            Rect rect = contentRect;
            if (rect.width == 0 || rect.height == 0) return;
            float normalizedX = ViewPortCenter.x / rect.width;
            float normalizedY = 1 - (ViewPortCenter.y / rect.height); 
            float worldX = Mathf.Lerp(WorldMin.x, WorldMax.x, normalizedX);
            float worldZ = Mathf.Lerp(WorldMin.y, WorldMax.y, normalizedY);
            Vector3 correctedWorldPos = new Vector3(worldX, 0f, worldZ);
            float centerX = (WorldMin.x + WorldMax.x) * 0.5f;
            float centerZ = (WorldMin.y + WorldMax.y) * 0.5f;
            Vector3 worldCenter = new Vector3(centerX, 0f, centerZ);
            Vector3 relativePos = correctedWorldPos - worldCenter;
            Quaternion inverseRotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
            Vector3 targetWorldPos = inverseRotation * relativePos + worldCenter;
            Vector3 target = new Vector3(targetWorldPos.x, Camera.main.transform.position.y, targetWorldPos.z);
            OnMinimapTargetSet?.Invoke(target);
        }
        
        public void UpdateViewPortIndicatorFromCamera(Vector3 cameraWorldPos) {
            Rect rect = contentRect;
            if (rect.width == 0 || rect.height == 0) return;
            float centerX = (WorldMin.x + WorldMax.x) * 0.5f;
            float centerZ = (WorldMin.y + WorldMax.y) * 0.5f;
            Vector3 worldCenter = new Vector3(centerX, 0f, centerZ);
            Vector3 relativePos = cameraWorldPos - worldCenter;
            float correctionAngle = -Camera.main.transform.eulerAngles.y;
            Quaternion correctionRotation = Quaternion.Euler(0f, correctionAngle, 0f);
            Vector3 rotatedPos = correctionRotation * relativePos;
            Vector3 correctedWorldPos = rotatedPos + worldCenter;
            float normalizedX = (correctedWorldPos.x - WorldMin.x) / (WorldMax.x - WorldMin.x);
            float normalizedY = (correctedWorldPos.z - WorldMin.y) / (WorldMax.y - WorldMin.y);
            float minimapX = normalizedX * rect.width;
            float minimapY = (1 - normalizedY) * rect.height;
            ViewPortCenter = new Vector2(minimapX, minimapY);
            MarkDirtyRepaint();
        }
    }
}