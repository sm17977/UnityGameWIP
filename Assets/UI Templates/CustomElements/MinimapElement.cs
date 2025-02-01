using System.Collections.Generic;
using Multiplayer;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomElements {
    [UxmlElement]
    public partial class MinimapElement : VisualElement {
        public static readonly string ussClassName = "minimap-element";
  
        // Dictionary to hold player marker elements.
        private Dictionary<string, VisualElement> _playerMarkers = new Dictionary<string, VisualElement>();

        // World boundaries (could be set externally if needed)
        private Vector2 worldMin = new Vector2(-60f, -60f);
        private Vector2 worldMax = new Vector2(60f, 60f);
        
        private List<GameObject> _players;
        
        // Scheduled update for markers.
        private IVisualElementScheduledItem _updateMarkersSchedule;

        public MinimapElement() {
            // Add USS class for styling (positioning, size, etc.)
            AddToClassList(ussClassName);
            
            // Assign custom drawing callback.
            generateVisualContent += GenerateVisualContent;
        }
        
        private void GenerateVisualContent(MeshGenerationContext mgc) {
            var painter = mgc.painter2D;
            // Use the element's contentRect, which reflects the USS-defined dimensions.
            Rect rect = contentRect;
    
            // If you already have the background defined in USS, you might not need to draw anything here.
            // But if you want to draw additional graphics, you can use the rect.
            //
            // For example, if you want to draw a subtle border or other overlay, you might do so here.
            // Otherwise, you can leave this empty.
        }

        
        // Converts a world position (using x and z) into a minimap coordinate.
        // Converts a world position (using x and z) into a minimap coordinate.
// This version applies a correction rotation to align with the minimap's orientation.
        private Vector2 WorldToMinimapPosition(Vector3 worldPos, Vector2 worldMin, Vector2 worldMax, Rect minimapRect)
        {
            // Optional: Define a world center if needed (here, the center between worldMin and worldMax).
            float centerX = (worldMin.x + worldMax.x) * 0.5f;
            float centerZ = (worldMin.y + worldMax.y) * 0.5f;
            Vector3 worldCenter = new Vector3(centerX, 0f, centerZ);

            // Get the player's position relative to the center of the world.
            Vector3 relativePos = worldPos - worldCenter;

            // Determine the correction rotation. 
            // For example, if your camera's y rotation is -45 degrees, then the correction is +45 degrees.
            // You could also read the camera's rotation dynamically, e.g.:
            // float correctionAngle = -Camera.main.transform.eulerAngles.y;
            // For now, we'll assume it's fixed at +45.
            float correctionAngle = 45f;
            Quaternion correctionRotation = Quaternion.Euler(0f, correctionAngle, 0f);

            // Apply the rotation to the relative position.
            Vector3 rotatedPos = correctionRotation * relativePos;

            // Now, add back the world center if you want to map based on a rotated world.
            Vector3 correctedWorldPos = rotatedPos + worldCenter;

            // Now normalize the corrected position based on the world boundaries.
            float normalizedX = (correctedWorldPos.x - worldMin.x) / (worldMax.x - worldMin.x);
            float normalizedY = (correctedWorldPos.z - worldMin.y) / (worldMax.y - worldMin.y);

            float minimapX = normalizedX * minimapRect.width;
            // Invert Y if you want "up" on the minimap to correspond to increasing world Z.
            float minimapY = (1 - normalizedY) * minimapRect.height;

            return new Vector2(minimapX, minimapY);
        }

        
        // Adds a player marker to the minimap.
        public void AddPlayerMarker(string playerId, Color markerColor) {
            Debug.Log("Adding new player maker");
            VisualElement marker = new VisualElement();
            marker.style.width = 10;
            marker.style.height = 10;
            marker.style.backgroundColor = markerColor;
            marker.AddToClassList("minimap-player-marker");
            // Center the marker by offsetting its pivot if desired.
            marker.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            // Add marker as a child of the minimap.
            Add(marker);
            _playerMarkers[playerId] = marker;
        }
        
        // Updates the marker's position based on the player's world position.
        public void UpdatePlayerMarkersPosition(List<GameObject> players) {
            if (players == null) return;
    
            foreach (var player in players) {
                if (player == null) return;
                
                var playerScript = player.GetComponent<LuxPlayerController>();
                var playerId = player.name;
                if (_playerMarkers.TryGetValue(playerId, out VisualElement marker)) {
                    // Use the current contentRect (which reflects your USS dimensions)
                    Rect currentRect = this.contentRect;
                    Vector2 minimapPos = WorldToMinimapPosition(playerScript.transform.position, worldMin, worldMax, currentRect);
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
    }
}
