using System.Linq;
using CustomElements;
using Global.Game_Modes;
using UnityEngine;
using UnityEngine.UIElements;

public delegate void StartDuelCountdown();

namespace Multiplayer.UI {
    public class GameView : View {

        private CountdownTimerElement _countdownTimerElement;
        private VisualElement _healthBarContainer;
        private LuxPlayerController _player;
        private float _healthBarYOffset = -50f;
        public static event StartDuelCountdown OnStartGameModeCountdown; 
        
        public GameView(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            BindUIElements();

        }
        
        private void BindUIElements() {
            _countdownTimerElement = Template.Q<CountdownTimerElement>("countdown-timer");
            _healthBarContainer = Template.Q<VisualElement>("health-bar-container");
        }   

        public override async void Show() {
            base.Show();
            BindUIElements();
            GlobalState.GameModeManager.CurrentGameMode.UpdateCountdownText += _countdownTimerElement.UpdateCountdown;
            GlobalState.GameModeManager.CurrentGameMode.HideCountdown += _countdownTimerElement.HideCountdown;
            GlobalState.GameModeManager.CurrentGameMode.ShowCountdown += _countdownTimerElement.ShowCountdown;
            OnStartGameModeCountdown?.Invoke();
            SetHealthBarPosition();
            Show(_healthBarContainer);
        }

        public override void Hide() {
            base.Hide();
            Hide(_healthBarContainer);
        }
        
        public override void Update() {
            SetHealthBarPosition();
        }
        
        public override void RePaint() {

        }

        public void SetPlayer(GameObject playerGameObject) {
            _player = playerGameObject.GetComponent<LuxPlayerController>();
        }

        private void SetHealthBarPosition() {
            
            if(_player == null) return;
            
            Vector2 newPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                _healthBarContainer.panel, _player.healthBarAnchor.transform.position, _player.mainCamera);

            newPosition.x += -(Screen.width / 2);
            newPosition.y += -(Screen.height) + _healthBarYOffset;
        
            _healthBarContainer.transform.position = newPosition;
        }
    }
}