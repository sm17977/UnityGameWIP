using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Multiplayer.UI {

    public delegate bool OnIsThisPlayerInLobby();
    public delegate bool OnIsPlayerSignedIn();
    public class MultiplayerMenuView : View {
        
        private List<Button> _buttonsList;
        private Label _playerIdLabel;
        public event OnIsThisPlayerInLobby IsPlayerInLobby;
        public event OnIsPlayerSignedIn IsPlayerSignedIn;
        public event Action OpenCreateLobbyModal;
        public event Action ShowLobbyView;
        public event Action ShowLobbiesView;
        public event Action LoadMainMenuScene;
        
        
        public MultiplayerMenuView(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            InitializeElements();
        }

        private void InitializeElements() {
            _buttonsList = Template.Query<Button>("btn").ToList();
            _playerIdLabel = Template.Query<Label>("player-id");
            
            // Multiplayer Menu Buttons
            foreach (var button in _buttonsList){

                switch (button.text){

                    case "Create new lobby":
                        button.RegisterCallback<ClickEvent>(evt => OpenCreateLobbyModal?.Invoke());
                        break;
                
                    case "Current lobby":
                        button.RegisterCallback<ClickEvent>(evt => ShowLobbyView?.Invoke());
                        break;

                    case "List lobbies":
                        button.RegisterCallback<ClickEvent>(evt => ShowLobbiesView?.Invoke());
                        break;

                    case "Leaderboards":
                        button.RegisterCallback<ClickEvent>(evt => Placeholder());
                        break;

                    case "Main Menu":
                        button.RegisterCallback<ClickEvent>(evt => LoadMainMenuScene?.Invoke());
                        break;
                }
            }
        }

        private void RunLobbyCheck() {
            if (IsPlayerInLobby?.Invoke() == true) {
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

        private void ValidateButtons() {

            var signedIn = IsPlayerSignedIn?.Invoke() == true;
            
            foreach (var button in _buttonsList) {
                if (button.text == "Main Menu") continue;
                button.SetEnabled(signedIn);
            }
        }
        
        public override async void Show() {
            base.Show();
            ValidateButtons();
            RunLobbyCheck();
        }

        public override void Hide() {
            HidePlayerId();
            base.Hide();
        }
        
        public override void Update() {
            ValidateButtons();
            RunLobbyCheck();
        }
        
        public override void RePaint() {
        }

        private void Placeholder() {
            
        }
        public void DisplayPlayerId(string clientId) {
            _playerIdLabel.text = "Unity Services Player ID: " + clientId;
        }

        public void HidePlayerId() {
            _playerIdLabel.text = "";
        }
    }
}