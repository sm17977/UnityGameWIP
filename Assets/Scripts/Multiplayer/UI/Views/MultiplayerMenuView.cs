using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public class MultiplayerMenuView : View {

        private MultiplayerUIController _uiController;
        
        private List<Button> _buttonsList;
        private Label _playerIdLabel;
        private VisualElement _createLobbyMenuBtnContainer;
        private VisualElement _currentLobbyMenuBtnContainer;

        public MultiplayerMenuView(VisualElement parentContainer, MultiplayerUIController uiController) {
            _uiController = uiController;
            ParentContainer = parentContainer;
            var uiDocument = uiController.uiDocument;
            Root = uiDocument.rootVisualElement;
            InitializeElements();
        }

        private void InitializeElements() {
            _playerIdLabel = Root.Q<Label>("player-id");
            _buttonsList = Root.Query<Button>("btn").ToList();
            _createLobbyMenuBtnContainer = Root.Q<VisualElement>("create-lobby-btn-container");
            _currentLobbyMenuBtnContainer = Root.Q<VisualElement>("current-lobby-btn-container");
            
            // Multiplayer Menu Buttons
            foreach (var button in _buttonsList){

                switch (button.text){

                    case "Create new lobby":
                        button.RegisterCallback<ClickEvent>(evt => _uiController.OnClickMultiplayerMenuBtn(typeof(CreateLobbyModal)));
                        break;
                
                    case "Current lobby":
                        button.RegisterCallback<ClickEvent>(evt => _uiController.OnClickMultiplayerMenuBtn(typeof(LobbyView)));
                        break;

                    case "List lobbies":
                        button.RegisterCallback<ClickEvent>(evt => _uiController.OnClickMultiplayerMenuBtn(typeof(LobbiesView)));
                        break;

                    case "Leaderboards":
                        button.RegisterCallback<ClickEvent>(evt => Placeholder());
                        break;

                    case "Main Menu":
                        button.RegisterCallback<ClickEvent>(evt => _uiController.OnClickMainMenuBtn());
                        break;
                }
            }
        }

        public void RunLobbyCheck() {
            if (_uiController.IsPlayerInLobby()) {
                Debug.Log("Player is in lobby!");
                var currentLobbyBtnContainer = Root.Q<VisualElement>("lobby-btn-container");
                var createLobbyBtnContainer = Root.Q<VisualElement>("create-lobby-btn-container");
                Show(currentLobbyBtnContainer);
                Hide(createLobbyBtnContainer);
            }
            else {
                var currentLobbyBtnContainer = Root.Q<VisualElement>("lobby-btn-container");
                var createLobbyBtnContainer = Root.Q<VisualElement>("create-lobby-btn-container");
                Show(createLobbyBtnContainer);
                Hide(currentLobbyBtnContainer);
            }
        }
        
        public override async void Show() {
            base.Show();
            RunLobbyCheck();
        }
        
        public override void Update() {
            RunLobbyCheck();
        }
        
        public override void RePaint() {
            
        }

        private void Placeholder() {
            
        }
        
    }
}