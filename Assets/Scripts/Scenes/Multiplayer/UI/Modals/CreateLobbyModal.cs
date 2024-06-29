using System.Linq;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public class CreateLobbyModal : Modal {
        
        private MultiplayerUIController _uiController;
        
        // Create Lobby Modal
        private Button _createLobbyBtn;
        private Button _cancelLobbyBtn;
        private TextField _lobbyNameInput;
        
        public Button CreateLobbyBtn {
            get => _createLobbyBtn;
        }

        public TextField LobbyNameInput {
            get => _lobbyNameInput;
        }
        
        public CreateLobbyModal(VisualElement parentContainer, MultiplayerUIController uiController, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            _uiController = uiController;
            InitializeElements();
        }
        
        private void InitializeElements() {
            _lobbyNameInput = Template.Q<TextField>("lobby-name-input");
          
            _createLobbyBtn = Template.Q<Button>("create-lobby-btn");
            _cancelLobbyBtn = Template.Q<Button>("cancel-lobby-btn");
            
            _createLobbyBtn.RegisterCallback<ClickEvent>(evt => OnClickCreateLobbyBtn());
            _cancelLobbyBtn.RegisterCallback<ClickEvent>(evt => OnClickCancelBtn());
            
            Loader = Template.Q<VisualElement>("lobby-loader");
        }
        
        private async void OnClickCreateLobbyBtn() {
            ShowLoader();
            _createLobbyBtn.SetEnabled(false);
            var lobbyNameInput = _lobbyNameInput.text;
            await _uiController.CreateLobby(lobbyNameInput);
            _createLobbyBtn.SetEnabled(true);
            HideLoader();
            ClearFormInput();
        }

        private async void OnClickCancelBtn() {
            _uiController.OnCloseModal(this);
        }
        
        private void ClearFormInput() {
            _lobbyNameInput.SetValueWithoutNotify("");
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