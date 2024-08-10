using System.Linq;
using Global.Game_Modes;
using UnityEngine;
using UnityEngine.UIElements;


namespace Multiplayer.UI {
    public class GameView : View {

        private Label _countdownText;
        private GameMode _gameMode;
        private Duel _duelMode;
        
        public GameView(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            InitializeElements();
        }
        
        private void InitializeElements() {
            _countdownText = Template.Q<Label>("countdown-text");
            Debug.Log("GameMode: " + GlobalState.GameModeManager.CurrentGameMode);
        }   

        public override async void Show() {
            base.Show();
            InitializeElements();
            _duelMode = GlobalState.GameModeManager.CurrentGameMode as Duel;
            if (_duelMode != null) {
                _duelMode.UpdateCountdownText += OnUpdateCountdownText;
            }
        }

        public override void Hide() {
            base.Hide();
        }
        
        public override void Update() {

        }
        
        public override void RePaint() {

        }
        
        private void OnUpdateCountdownText(int timer) {
            _countdownText.text = timer > 0 ? timer.ToString() : "Duel!";
        }
    }
}