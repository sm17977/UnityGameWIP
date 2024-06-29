using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public class MultiplayerMenuView : View {

        private MultiplayerUIController _uiController;
        
        private List<Button> _buttonsList;
        private Label _playerIdLabel;
        
        public MultiplayerMenuView(VisualElement parentContainer, MultiplayerUIController uiController, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            _uiController = uiController;
            InitializeElements();
        }

        private void InitializeElements() {
            _playerIdLabel = _uiController.uiDocument.rootVisualElement.Q<Label>("player-id");
            _buttonsList = Template.Query<Button>("btn").ToList();
            
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
            DisplayPlayerId();
        }

        public void RunLobbyCheck() {
            if (_uiController.IsPlayerInLobby()) {
                var currentLobbyBtnContainer = Template.Q<VisualElement>("lobby-btn-container");
                var createLobbyBtnContainer = Template.Q<VisualElement>("create-lobby-btn-container");
                Show(currentLobbyBtnContainer);
                Hide(createLobbyBtnContainer);
            }
            else {
                var currentLobbyBtnContainer = Template.Q<VisualElement>("lobby-btn-container");
                var createLobbyBtnContainer = Template.Q<VisualElement>("create-lobby-btn-container");
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
            DisplayPlayerId();
        }

        private void Placeholder() {
            
        }

        private void DisplayPlayerId() {
            if (_uiController.Client != null) {
                _playerIdLabel.text = "Player ID: " + _uiController.Client.ID;
            }
        }
        
    }
}