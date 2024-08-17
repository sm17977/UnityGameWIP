using System.Linq;
using CustomElements;
using Global.Game_Modes;
using UnityEngine;
using UnityEngine.UIElements;

public delegate void StartDuelCountdown();

namespace Multiplayer.UI {
    public class GameView : View {

        private CountdownTimerElement _countdownTimerElement;
        public static event StartDuelCountdown OnStartGameModeCountdown; 
        
        public GameView(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            InitializeElements();

        }
        
        private void InitializeElements() {
            _countdownTimerElement = Template.Q<CountdownTimerElement>("countdown-timer");
        }   

        public override async void Show() {
            base.Show();
            InitializeElements();
            GlobalState.GameModeManager.CurrentGameMode.UpdateCountdownText += _countdownTimerElement.UpdateCountdown;
            GlobalState.GameModeManager.CurrentGameMode.HideCountdown += _countdownTimerElement.HideCountdown;
            GlobalState.GameModeManager.CurrentGameMode.ShowCountdown += _countdownTimerElement.ShowCountdown;
            OnStartGameModeCountdown?.Invoke();
        }

        public override void Hide() {
            base.Hide();
        }
        
        public override void Update() {

        }
        
        public override void RePaint() {

        }
        
        // private void OnUpdateCountdownText(int timer) {
        //     _countdownText.text = timer > 0 ? timer.ToString() : "Duel!";
        // }
    }
}