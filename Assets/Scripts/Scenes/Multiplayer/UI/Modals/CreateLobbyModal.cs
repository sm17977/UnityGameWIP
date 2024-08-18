using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    
    public delegate Task OnCreateLobby(string lobbyName, uint maxPlayers, string gameMode);
    
    public class CreateLobbyModal : Modal {
        public event OnCreateLobby CreateLobby;
        public event OnCloseModal CloseModal;
        
        // Create Lobby Modal
        private Button _createLobbyBtn;
        private Button _cancelLobbyBtn;
        private VisualElement _contentContainer;
        private OuterGlow _containerShadow;
        
        // User Input Fields
        private TextField _lobbyNameInput;
        private UnsignedIntegerField _lobbyMaxPlayersInput;
        private DropdownField _gameModeDropdown;
        
        public CreateLobbyModal(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            InitializeElements();
        }
        
        private void InitializeElements() {
            _lobbyNameInput = Template.Q<TextField>("lobby-name-input");
            _lobbyMaxPlayersInput = Template.Q<UnsignedIntegerField>("lobby-max-players-input");
            _gameModeDropdown = Template.Q<DropdownField>("lobby-gamemode-input");
            
            PopulateGameModeDropdown(GlobalState.MultiplayerGameModes.Select(gm => gm.Name).ToList());
            
            _gameModeDropdown.RegisterValueChangedCallback(evt => {
                if (evt.newValue == "Duel") {
                    _lobbyMaxPlayersInput.value = 2;
                    _lobbyMaxPlayersInput.SetEnabled(false);
                }
                else {
                    _lobbyMaxPlayersInput.SetEnabled(true);
                }
            });
            
            if (_gameModeDropdown.value == "Duel") {
                _lobbyMaxPlayersInput.value = 2;
                _lobbyMaxPlayersInput.SetEnabled(false);
            }
            
            _createLobbyBtn = Template.Q<Button>("create-lobby-btn");
            _cancelLobbyBtn = Template.Q<Button>("cancel-lobby-btn");
            
            _contentContainer = Template.Q("lobby-modal-content");
            _containerShadow = Template.Q<OuterGlow>("container-shadow");
            
            _createLobbyBtn.RegisterCallback<ClickEvent>(evt => OnClickCreateLobbyBtn());
            _cancelLobbyBtn.RegisterCallback<ClickEvent>(evt => OnClickCancelBtn());
            
            Loader = Template.Q<VisualElement>("lobby-loader");
           
        }

        public override async void ShowModal() {
            _contentContainer.AddToClassList("lobby-modal-show-transition");
            base.ShowModal();
            await Task.Delay(200);
            _contentContainer.AddToClassList("lobby-modal-active");
            _containerShadow.AddToClassList("shadow-active");
        }

        public override async void HideModal() {
            _contentContainer.AddToClassList("lobby-modal-hide-transition");
            _containerShadow.RemoveFromClassList("shadow-active");
            _contentContainer.RemoveFromClassList("lobby-modal-active");
            await Task.Delay(200);
            base.HideModal();
            _contentContainer.RemoveFromClassList("lobby-modal-hide-transition");
        }

        private async void OnClickCreateLobbyBtn() {
            ShowLoader();
            _createLobbyBtn.SetEnabled(false);
            var lobbyNameInput = _lobbyNameInput.text;
            var maxPlayersInput = _lobbyMaxPlayersInput.value;
            var gameModeInput = _gameModeDropdown.value;
          
            await CreateLobby?.Invoke(lobbyNameInput, maxPlayersInput, gameModeInput);
            _createLobbyBtn.SetEnabled(true);
            HideLoader();
            ClearFormInput();
        }

        private void OnClickCancelBtn() {
           CloseModal?.Invoke(this); 
        }
        
        private void ClearFormInput() {
            _lobbyNameInput.SetValueWithoutNotify("");
            InitializeElements();
        }

        private void ValidateFormInput() {
            if (_gameModeDropdown.value == "Duel") {
                _lobbyMaxPlayersInput.value = 2;
                _lobbyMaxPlayersInput.SetEnabled(false);
            }
        }
        
        private void PopulateGameModeDropdown(List<string> gameModes) {
            _gameModeDropdown.choices = gameModes;
            _gameModeDropdown.index = 0;
        }
        
        public override void Update() {
   
        }
        
        public override void RePaint() {
            
        }
    }
}