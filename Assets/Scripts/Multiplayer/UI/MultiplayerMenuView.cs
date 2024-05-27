using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public class MultiplayerMenuView : View {
        
        private UIDocument _uiDocument;
        private List<Button> _menuBtns;
        private Label _playerIdLabel;
        private VisualElement _createLobbyMenuBtnContainer;
        private VisualElement _currentLobbyMenuBtnContainer;

        public MultiplayerMenuView(VisualElement parentContainer, UIDocument uiDocument) {
            ParentContainer = parentContainer;
            _uiDocument = uiDocument;
            Root = uiDocument.rootVisualElement;
            InitializeElements();
        }

        private void InitializeElements() {
            _playerIdLabel = Root.Q<Label>("player-id");
            _menuBtns = Root.Query<Button>("btn").ToList();
            _createLobbyMenuBtnContainer = Root.Q<VisualElement>("create-lobby-btn-container");
            _currentLobbyMenuBtnContainer = Root.Q<VisualElement>("current-lobby-btn-container");
        }
        
    }
}