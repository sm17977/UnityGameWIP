using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CustomElements;
using Global.Game_Modes;
using Scenes.Multiplayer.GameChat;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public delegate void StartDuelCountdown();

namespace Multiplayer.UI {
    public class GameView : View {
        
        public ChatBoxElement Chat;
        public static event StartDuelCountdown OnStartGameModeCountdown;
        public MinimapElement Minimap;
        
        private readonly float _pingUpdateInterval = 2f;
        private readonly FloatingUIManager _floatingUIManager;
        private CountdownTimerElement _countdownTimerElement;
        private Label _gameTimerLabel;
        private Label _pingLabel;
        private List<GameObject> _playerGameObjects;
        private PanelSettings _panelSettings;
        private BlinkingTextField _chatInput;
        private float _timeSinceLastPingUpdate;
        
        private VisualElement _centerHealthBar;
        private Label _centerHealthBarLabel;
   
        
        public GameView(VisualElement parentContainer, VisualTreeAsset vta, PanelSettings panelSettings) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            BindUIElements();
            _panelSettings = panelSettings;
            _floatingUIManager = new FloatingUIManager(Template);
        }

        private void BindUIElements() {
            Minimap = Template.Q<MinimapElement>("minimap");
            Chat = Template.Q<ChatBoxElement>("chat");
            _countdownTimerElement = Template.Q<CountdownTimerElement>("countdown-timer");
            _gameTimerLabel = Template.Q<Label>("game-timer-label");
            _pingLabel = Template.Q<Label>("ping-label");
            _chatInput = Chat.ChatInputField;
            
            var centerHealthBarContainer = Template.Q<VisualElement>("center-health-container");
            _centerHealthBar = centerHealthBarContainer.Q<VisualElement>("center-health-bar");
            _centerHealthBarLabel = centerHealthBarContainer.Q<Label>("center-health-label");

            Health.OnHealthChanged += UpdateCenterHealthBar;

        }

        public override void Show() {
            _panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _floatingUIManager.GenerateUIComponents(_playerGameObjects);
            BindUIElements();
            GenerateMinimap();
            base.Show();
            GlobalState.GameModeManager.CurrentGameMode.UpdateCountdownText += _countdownTimerElement.UpdateCountdown;
            GlobalState.GameModeManager.CurrentGameMode.HideCountdown += _countdownTimerElement.HideCountdown;
            GlobalState.GameModeManager.CurrentGameMode.ShowCountdown += _countdownTimerElement.ShowCountdown;
            OnStartGameModeCountdown?.Invoke();
            _floatingUIManager.SetUIComponentPositions(_panelSettings);
        }
        
        public override void Hide() {
            base.Hide();
            _panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        }

        public override void Update() {
            _floatingUIManager.SetUIComponentPositions(_panelSettings); 
            _gameTimerLabel.text = GlobalState.GameModeManager.CurrentGameMode.GetGameTimer();
            Minimap.UpdatePlayerMarkersPosition(_playerGameObjects);
        }

        public override void FixedUpdate() {
            _timeSinceLastPingUpdate += Time.fixedDeltaTime;
            if (_timeSinceLastPingUpdate >= _pingUpdateInterval) {
                _timeSinceLastPingUpdate = 0f;
                UpdatePing();
            }
        }

        private void UpdatePing() {
            var ping = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(
                NetworkManager.ServerClientId);
            
            _pingLabel.text = ping + "ms";
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
            var playerScriptsToRemove = _floatingUIManager.GetPlayerScriptsToRemove(players);

            foreach (var playerScript in playerScriptsToRemove) {
                _floatingUIManager.RemoveUIComponents(playerScript);
            }

            // Update the player list
            _playerGameObjects = players;

            // Delegate generation of new health bars
            _floatingUIManager.GenerateUIComponents(_playerGameObjects);
            
            _floatingUIManager.UpdatePlayerNameLabels();
        }

        /// <summary>
        /// Handle the USS transitions whenever an ability is cast
        /// </summary>
        /// <param name="key"></param>
        /// <param name="duration"></param>
        public void ActivateAbilityAnimation(string key, float duration) { 
            var skillsContainer = Template.Q<VisualElement>("skills-box");
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

        public bool IsChatActive() {
            return _chatInput.IsFocused;
        }
        
        public string GetCurrentChatInput() {
            var text = _chatInput.value;
            _chatInput.SetValueWithoutNotify("");
            return text;
        }

        public void BlurInput() {
            if (_chatInput.IsFocused) {
                _chatInput.Blur();
                _chatInput.focusable = false;
            }
        }

        public void FocusInput() {
            _chatInput.focusable = true;
            // Have to delay this or focus won't work
            if (!_chatInput.IsFocused) {
                _chatInput.schedule.Execute(() => {
                    _chatInput.Focus();
                }).StartingIn(100);
            }
        }

        private void UpdateCenterHealthBar(LuxPlayerController controller, float currentHealth, float maxHealth, float damage) {
            if(controller == null) return;
            if(!controller.IsLocalPlayer) return;
            
            var scale = maxHealth / currentHealth;
            var newBarWidth = 100 /  scale;
            _centerHealthBar.style.width = new Length(newBarWidth, LengthUnit.Percent);
            _centerHealthBarLabel.text = currentHealth + "/" + maxHealth;
        }
    }
}