using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public class GameView : View {
        
        private MultiplayerUIController _uiController;
        
        private VisualElement _mainContainer;
        private VisualElement _backBtnContainer;
        
        public GameView(VisualElement parentContainer, MultiplayerUIController uiController) {
            _uiController = uiController;
            var uiDocument = uiController.uiDocument;
            Root = uiDocument.rootVisualElement;
            InitializeElements();
        }

        private void InitializeElements() {
            _backBtnContainer = Root.Q<VisualElement>("back-btn-container");
            _mainContainer = Root.Q<VisualElement>("main-container");
        }

        public override async void Show() {
            base.Show();
            Hide(_backBtnContainer);
            Hide(_mainContainer);
        }

        public override void Hide() {
            base.Hide();
            Show(_backBtnContainer);
            Show(_mainContainer);
        }
        
        public override void Update() {

        }
        
        public override void RePaint() {

        }
    }
}