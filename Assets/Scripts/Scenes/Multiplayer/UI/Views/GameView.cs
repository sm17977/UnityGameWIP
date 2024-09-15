using System.Collections.Generic;
using System.Linq;
using CustomElements;
using Global.Game_Modes;
using UnityEngine;
using UnityEngine.UIElements;

public delegate void StartDuelCountdown();

namespace Multiplayer.UI {
    public class GameView : View {

        private CountdownTimerElement _countdownTimerElement;
        private float _healthBarYOffset = -50f;
        private Dictionary<LuxPlayerController, VisualElement> _playerHealthBarMappings;
        private List<GameObject> _playerGameObjects;
        public static event StartDuelCountdown OnStartGameModeCountdown; 
        
        public GameView(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            BindUIElements();
            _playerHealthBarMappings = new Dictionary<LuxPlayerController, VisualElement>();
        }
        
        private void BindUIElements() {
            _countdownTimerElement = Template.Q<CountdownTimerElement>("countdown-timer");
        }   

        public override async void Show() {
            GenerateHealthBars(); // This function updates the template which means it must run before base.Show
            base.Show();
            BindUIElements();
            GlobalState.GameModeManager.CurrentGameMode.UpdateCountdownText += _countdownTimerElement.UpdateCountdown;
            GlobalState.GameModeManager.CurrentGameMode.HideCountdown += _countdownTimerElement.HideCountdown;
            GlobalState.GameModeManager.CurrentGameMode.ShowCountdown += _countdownTimerElement.ShowCountdown;
            OnStartGameModeCountdown?.Invoke();
            SetHealthBarPosition();
            foreach (var (playerScript, healthBar) in _playerHealthBarMappings) {
                Show(healthBar);
            }
        }

        public override void Hide() {
            
            // Hide and remove health bars
            foreach (var (playerScript, healthBar) in _playerHealthBarMappings.ToList()) {
                _playerHealthBarMappings[playerScript].RemoveFromHierarchy();
            }
            
            // Hide needs to be called before the health bars are removed from the hierarchy
            base.Hide();
            
            // Empty the player game objects list
            _playerGameObjects.Clear();
        }
        
        public override void Update() {
            SetHealthBarPosition();
        }
        
        public override void RePaint() {

        }

        /// <summary>
        /// Set the initial player game objects list
        /// </summary>
        /// <param name="players"></param>
        public void SetPlayers(List<GameObject> players) {
            _playerGameObjects = players;
        }

        /// <summary>
        /// This is called from the lobby events in the UI controller when a lobby player either joins + connects, or
        /// the player leaves the lobby (in which case they will disconnect soon after)
        /// </summary>
        /// <param name="players"></param>
        public void UpdatePlayerHealthBars(List<GameObject> players) {
            // Get player scripts of players that have left from their (now destroyed) game object
            var playerScriptsToRemove = _playerHealthBarMappings.Keys
                .Where(playerScript => playerScript == null || playerScript.gameObject == null)
                .ToList();
            
            foreach (var playerScript in playerScriptsToRemove) {
                // Remove health bar from the UI and dictionary
                if (_playerHealthBarMappings.ContainsKey(playerScript)) {
                    _playerHealthBarMappings[playerScript].RemoveFromHierarchy();
                    _playerHealthBarMappings.Remove(playerScript);
                }
            }

            // Update the player game objects list, removing any players that have left
            _playerGameObjects = players.Where(playerObject => playerObject != null).ToList();
            
            // Generate health bars for new players
            GenerateHealthBars();

            // Call show to update the template as we've changed the health bar markup
            base.Show();
        }

        
        /// <summary>
        /// Generate the health bar visual elements for each connected client
        /// Store mapping of player and their health bar visual element
        /// </summary>
        private void GenerateHealthBars() {
            foreach (var player in _playerGameObjects) {
                
                var playerScript = player.GetComponent<LuxPlayerController>();
                if (_playerHealthBarMappings.ContainsKey(playerScript)) {
                    continue; // Avoid creating duplicate health bars
                }
                
                var healthBarContainer = new VisualElement();
                var healthBar = new VisualElement();
                
                healthBarContainer.AddToClassList("health-bar-container");
                healthBar.AddToClassList("health-bar");
                
                healthBarContainer.Add(healthBar);
                
                Template.Add(healthBarContainer);
                _playerHealthBarMappings[playerScript] = healthBarContainer;
            }
        }
        
        /// <summary>
        /// Set the position of the health bar above the player model for each client
        /// </summary>
        private void SetHealthBarPosition() {
            
            foreach (var (player, healthBar) in _playerHealthBarMappings) {
                if (player == null || healthBar == null) {
                    continue;
                }
                
                Vector2 newPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                    healthBar.panel, player.healthBarAnchor.transform.position, player.mainCamera);
            
                newPosition.x += -(Screen.width / 2);
                newPosition.y += -(Screen.height / 2) + _healthBarYOffset;
            
                healthBar.transform.position = newPosition;
            }
        }
    }
}