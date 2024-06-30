using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Multiplayer.UI {

    public delegate bool OnIsThisPlayerInLobby();
    public class MultiplayerMenuView : View {
        public event OnIsThisPlayerInLobby IsThisPlayerInLobby;
        
        private List<Button> _buttonsList;
        private Label _playerIdLabel;
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
            if (IsThisPlayerInLobby?.Invoke() == true) {
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
        }

        private void Placeholder() {
            
        }
        public void DisplayPlayerId(string clientId, Label label) {
            label.text = "Player ID: " + clientId;
        }
    }
}