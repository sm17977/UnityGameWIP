using System.Linq;
using Global.Game_Modes;
using UnityEngine;
using UnityEngine.UIElements;


namespace Multiplayer.UI {
    public class GameView : View {

        private Label _countdownText;
        private GameMode _gameMode;
        
        public GameView(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            InitializeElements();
        }
        
        private void InitializeElements() {
            _countdownText = Template.Q<Label>("countdown-text");
            Debug.Log("GameMode: " + GlobalState.GameModeManager.CurrentGameMode);

            if (GlobalState.GameModeManager.CurrentGameMode is Duel duel) {
                Debug.Log("Subsicribed to countdown events");
                duel.UpdateCountdownText += timer => _countdownText.text = timer.ToString();
            }
        }   

        public override async void Show() {
            base.Show();
            InitializeElements();
        }

        public override void Hide() {
            base.Hide();
        }
        
        public override void Update() {

        }
        
        public override void RePaint() {

        }
    }
}