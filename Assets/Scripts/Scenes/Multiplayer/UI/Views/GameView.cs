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
        private readonly HealthBarManager _healthBarManager;
        private PanelSettings _panelSettings;
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
        }

        public override void Show() {
            _panelSettings.scaleMode = PanelScaleMode.ConstantPhysicalSize;
            _healthBarManager.GenerateHealthBars(_playerGameObjects);
            base.Show();
            BindUIElements();
            GlobalState.GameModeManager.CurrentGameMode.UpdateCountdownText += _countdownTimerElement.UpdateCountdown;
            GlobalState.GameModeManager.CurrentGameMode.HideCountdown += _countdownTimerElement.HideCountdown;
            GlobalState.GameModeManager.CurrentGameMode.ShowCountdown += _countdownTimerElement.ShowCountdown;
            OnStartGameModeCountdown?.Invoke();
            _healthBarManager.SetHealthBarPosition();
        }

        public override void Hide() {
            base.Hide();
            _panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        }

        public override void Update() {
            _healthBarManager.SetHealthBarPosition();
        }

        public override void RePaint() {
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
            string overlayElementName = key.ToLower() + "-overlay";
            var abilityBox = Template.Q<VisualElement>(overlayElementName);

            if (abilityBox != null) {
                // Reset height and transition
                ResetAbilityBox(abilityBox);

                abilityBox.schedule.Execute(() => {
                    StartTransition(abilityBox, duration); 
                }).StartingIn(50); 
            }
        }
        
        private void ResetAbilityBox(VisualElement abilityBox) {
            abilityBox.style.transitionDuration = new List<TimeValue>(0); 
            abilityBox.style.height = new StyleLength(new Length(0, LengthUnit.Percent));
            abilityBox.style.visibility = Visibility.Visible;
        }
        
        private void StartTransition(VisualElement abilityBox, float durationInSeconds) {
            abilityBox.style.transitionDuration = new List<TimeValue>
                { new TimeValue(durationInSeconds, TimeUnit.Second) };
            abilityBox.style.height = new StyleLength(new Length(100, LengthUnit.Percent)); 
            abilityBox.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
        }
        
        private void OnTransitionEnd(TransitionEndEvent evt) {
            var abilityBox = evt.target as VisualElement;

            if (abilityBox != null) {
                ResetAbilityBox(abilityBox); 
                abilityBox.style.visibility = Visibility.Hidden;
                abilityBox.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
            }
        }
    }
}