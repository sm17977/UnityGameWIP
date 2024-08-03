using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    
    public delegate Task OnCreateLobby(string lobbyName);
    
    public delegate Task OnSetLobbyGameMode(string gamemode);
    public class CreateLobbyModal : Modal {
        public event OnCreateLobby CreateLobby;
        public event OnCloseModal CloseModal;
        public event OnSetLobbyGameMode SetLobbyGameMode;
        
        // Create Lobby Modal
        private Button _createLobbyBtn;
        private Button _cancelLobbyBtn;
        private TextField _lobbyNameInput;
        private VisualElement _contentContainer;
        private OuterGlow _containerShadow;
        private DropdownField _gameModeDropdown;
        
        public CreateLobbyModal(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            InitializeElements();
        }
        
        private void InitializeElements() {
            _lobbyNameInput = Template.Q<TextField>("lobby-name-input");
          
            _createLobbyBtn = Template.Q<Button>("create-lobby-btn");
            _cancelLobbyBtn = Template.Q<Button>("cancel-lobby-btn");
            
            _contentContainer = Template.Q("lobby-modal-content");
            _containerShadow = Template.Q<OuterGlow>("container-shadow");
            
            _gameModeDropdown = Template.Q<DropdownField>("lobby-gamemode-dropdown");
            PopulateGameModeDropdown(GlobalState.MultiplayerGameModes);
            
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
            await CreateLobby?.Invoke(lobbyNameInput);
            await SetLobbyGameMode?.Invoke(_gameModeDropdown.value);
            _createLobbyBtn.SetEnabled(true);
            HideLoader();
            ClearFormInput();
        }

        private void OnClickCancelBtn() {
           CloseModal?.Invoke(this); 
        }
        
        private void ClearFormInput() {
            _lobbyNameInput.SetValueWithoutNotify("");
        }
        
        private void PopulateGameModeDropdown(List<string> gamemodes) {
            _gameModeDropdown.choices = gamemodes;
            _gameModeDropdown.index = 0;
        }
        
        public override void ShowLoader() {
            Show(Loader);
        }
        
        public override void HideLoader() {
            Hide(Loader);
        }

        public override void Update() {
   
        }
        
        public override void RePaint() {
            
        }
    }
}