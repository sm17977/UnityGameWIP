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
        private List<GameObject> _playerGameObjects;
        public static event StartDuelCountdown OnStartGameModeCountdown; 
        private HealthBarManager _healthBarManager;

        public GameView(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            BindUIElements();
            _healthBarManager = new HealthBarManager(Template); 
        }
        
        private void BindUIElements() {
            _countdownTimerElement = Template.Q<CountdownTimerElement>("countdown-timer");
        }   

        public override void Show() {
            _healthBarManager.GenerateHealthBars(_playerGameObjects); 
            base.Show();
            BindUIElements();
            GlobalState.GameModeManager.CurrentGameMode.UpdateCountdownText += _countdownTimerElement.UpdateCountdown;
            GlobalState.GameModeManager.CurrentGameMode.HideCountdown += _countdownTimerElement.HideCountdown;
            GlobalState.GameModeManager.CurrentGameMode.ShowCountdown += _countdownTimerElement.ShowCountdown;
            OnStartGameModeCountdown?.Invoke();
            _healthBarManager.SetHealthBarPosition(); 
        }

        public override void Update() {
            _healthBarManager.SetHealthBarPosition();
        }
        
        public override void RePaint() { }

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
    }
}