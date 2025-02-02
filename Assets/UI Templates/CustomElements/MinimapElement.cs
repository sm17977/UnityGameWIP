using System.Collections.Generic;
using Multiplayer;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomElements {
    [UxmlElement]
    public partial class MinimapElement : VisualElement {
        public static readonly string ussClassName = "minimap-element";
        
        private Dictionary<string, VisualElement> _playerMarkers = new Dictionary<string, VisualElement>();
        
        private Vector2 worldMin = new Vector2(-60f, -60f);
        private Vector2 worldMax = new Vector2(60f, 60f);
        
        private List<GameObject> _players;
        private IVisualElementScheduledItem _updateMarkersSchedule;
        private bool _showViewPort;

        public MinimapElement() {
            AddToClassList(ussClassName);
            generateVisualContent += GenerateVisualContent;
            RegisterCallback<PointerDownEvent>(OnPointerDown);
        }
        
        private void GenerateVisualContent(MeshGenerationContext mgc) {
            var painter = mgc.painter2D;
            Rect rect = contentRect;
            if (_showViewPort) DrawViewPort();

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
            Debug.Log("Adding new player maker");
            VisualElement marker = new VisualElement();
            marker.style.width = 10;
            marker.style.height = 10;
            marker.style.backgroundColor = markerColor;
            marker.AddToClassList("minimap-player-marker");
            marker.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(marker);
            _playerMarkers[playerId] = marker;
        }
        
        public void UpdatePlayerMarkersPosition(List<GameObject> players) {
            if (players == null) return;
    
            foreach (var player in players) {
                if (player == null) return;
                
                var playerScript = player.GetComponent<LuxPlayerController>();
                var playerId = player.name;
                if (_playerMarkers.TryGetValue(playerId, out VisualElement marker)) {
                    Rect currentRect = this.contentRect;
                    Vector2 minimapPos = WorldToMinimapPosition(playerScript.transform.position, worldMin, worldMax, currentRect);
                    float markerWidth = 10f;  
                    float markerHeight = 10f;
                    marker.style.left = minimapPos.x;
                    marker.style.top = minimapPos.y;
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
        
        /// <summary>
        /// Callback for pointer down events on the minimap.
        /// </summary>
        /// <param name="evt">The pointer event data.</param>
        private void OnPointerDown(PointerDownEvent evt) {
            
            Vector2 clickPos = evt.localPosition;
            Debug.Log("Minimap clicked at: " + clickPos);

    

            // You can now perform further actions based on where the minimap was clicked,
            // such as moving the camera, highlighting an area, etc.
        }

        private void DrawViewPort() {
            
            // Draw outline of view port rectangle centered on the click
            
        }
        
        
    }
}
