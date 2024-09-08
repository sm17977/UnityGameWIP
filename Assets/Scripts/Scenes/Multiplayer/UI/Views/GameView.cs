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
        private Dictionary<LuxPlayerController, VisualElement> _playerHealthBars;
        private List<GameObject> _playerGameObjects;
        public static event StartDuelCountdown OnStartGameModeCountdown; 
        
        public GameView(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            BindUIElements();
            _playerHealthBars = new Dictionary<LuxPlayerController, VisualElement>();
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
        }

        public override void Hide() {
            base.Hide();
        }
        
        public override void Update() {
            SetHealthBarPosition();
        }
        
        public override void RePaint() {

        }

        /// <summary>
        /// Set the player game objects list
        /// </summary>
        /// <param name="players"></param>
        public void SetPlayers(List<GameObject> players) {
            _playerGameObjects = players;
        }

        /// <summary>
        /// Generate new health bars if new players join (will this work if a player leaves?)
        /// </summary>
        /// <param name="players"></param>
        public void UpdatePlayers(List<GameObject> players) {
            _playerGameObjects = players;
            GenerateHealthBars();
            base.Show();
        }

        /// <summary>
        /// Generate the health bar visual elements for each connected client
        /// Store mapping of player and their health bar visual element
        /// </summary>
        private void GenerateHealthBars() {
            foreach (var player in _playerGameObjects) {
                
                var healthBarContainer = new VisualElement();
                var healthBar = new VisualElement();
                
                healthBarContainer.AddToClassList("health-bar-container");
                healthBar.AddToClassList("health-bar");
                
                healthBarContainer.Add(healthBar);
                var playerScript = player.GetComponent<LuxPlayerController>();
                
                if (_playerHealthBars.ContainsKey(playerScript)) {
                    continue; // Avoid creating duplicate health bars
                }
                
                Template.Add(healthBarContainer);
                _playerHealthBars[playerScript] = healthBarContainer;
            }
        }
        
        /// <summary>
        /// Set the position of the health bar above the player model for each client
        /// </summary>
        private void SetHealthBarPosition() {
            
            foreach (var (player, healthBar) in _playerHealthBars) {
                if (player == null) {
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