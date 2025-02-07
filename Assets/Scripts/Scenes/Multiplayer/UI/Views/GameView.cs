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
        private Label _gameTimerLabel;
        private List<GameObject> _playerGameObjects;
        private readonly HealthBarManager _healthBarManager;
        private PanelSettings _panelSettings;
        public MinimapElement Minimap;
        public static event StartDuelCountdown OnStartGameModeCountdown;

        public GameView(VisualElement parentContainer, VisualTreeAsset vta, PanelSettings panelSettings) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            BindUIElements();
            _panelSettings = panelSettings;
            _healthBarManager = new HealthBarManager(Template);
        }

        private void BindUIElements() {
            _countdownTimerElement = Template.Q<CountdownTimerElement>("countdown-timer");
            _gameTimerLabel = Template.Q<Label>("game-timer-label");
            Minimap = Template.Q<MinimapElement>("minimap");
        }

        public override void Show() {
            _panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            _healthBarManager.GenerateHealthBars(_playerGameObjects);
            BindUIElements();
            GenerateMinimap();
            base.Show();
            GlobalState.GameModeManager.CurrentGameMode.UpdateCountdownText += _countdownTimerElement.UpdateCountdown;
            GlobalState.GameModeManager.CurrentGameMode.HideCountdown += _countdownTimerElement.HideCountdown;
            GlobalState.GameModeManager.CurrentGameMode.ShowCountdown += _countdownTimerElement.ShowCountdown;
            OnStartGameModeCountdown?.Invoke();
            _healthBarManager.SetHealthBarPosition();
        }

        public override void Hide() {
            base.Hide();
            _panelSettings.scaleMode = PanelScaleMode.ConstantPhysicalSize;
        }

        public override void Update() {
            _healthBarManager.SetHealthBarPosition();
            Minimap.UpdatePlayerMarkersPosition(_playerGameObjects);
            _gameTimerLabel.text = GlobalState.GameModeManager.CurrentGameMode.GetGameTimer();
        }

        public override void RePaint() {
        }
        
        private void GenerateMinimap() {
            Debug.Log("GenerateMinimap");
            foreach(var player in _playerGameObjects) {
                var color = Color.red;
                var playerId = player.name;
                if (playerId == "Local Player") {
                    color = Color.black;
                } 
                if(!Minimap.ContainsPlayerMarker(playerId)) Minimap.AddPlayerMarker(playerId, color);
            }
        }

        public void UpdateMinimap() {
            Minimap.ResetPlayerMarkers();
            GenerateMinimap();
        }
        
        public void SetPlayers(List<GameObject> players) {
            _playerGameObjects = players;
        }

        public void UpdatePlayerHealthBars(List<GameObject> players) {
            // Clean up health bars for players that have left
            var playerScriptsToRemove = _healthBarManager.GetPlayerScriptsToRemove(players);

            foreach (var playerScript in playerScriptsToRemove) {
                _healthBarManager.RemoveHealthBar(playerScript);
            }

            // Update the player list
            _playerGameObjects = players;

            // Delegate generation of new health bars
            _healthBarManager.GenerateHealthBars(_playerGameObjects);
        }

        /// <summary>
        /// Handle the USS transitions whenever an ability is cast
        /// </summary>
        /// <param name="key"></param>
        /// <param name="duration"></param>
        public void ActivateAbilityAnimation(string key, float duration) { 
            var skillsContainer = Template.Q<VisualElement>("skills-container");
            var abilityBox = FindRadialCooldownElementByKey(skillsContainer, key);
            if(abilityBox != null) abilityBox.StartCooldown(duration);
        }
        
        private RadialCooldownElement FindRadialCooldownElementByKey(VisualElement container, string key) {
            foreach (var child in container.Children()) {
                var innerChild = child.Children().FirstOrDefault();
                if (innerChild is RadialCooldownElement radialElement && radialElement.Key == key) {
                    return radialElement;
                }
            }
            return null;
        }
    }
}