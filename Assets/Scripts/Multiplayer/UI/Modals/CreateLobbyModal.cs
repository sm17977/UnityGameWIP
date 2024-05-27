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
        
        // Loader
        VisualElement _loader;
        private float _rotation = 0;
        private float _timer = 0;
        
        public CreateLobbyModal(VisualElement parentContainer, MultiplayerUIController uiController) {
            ParentContainer = parentContainer;
            _uiController = uiController;
            var uiDocument = uiController.uiDocument;
            Root = uiDocument.rootVisualElement;
            InitializeElements();
        }
        
        private void InitializeElements() {
            _lobbyNameInput = Root.Q<TextField>("lobby-name-input");
          
            _createLobbyBtn = Root.Q<Button>("create-lobby-btn");
            _cancelLobbyBtn = Root.Q<Button>("cancel-lobby-btn");
            
            _createLobbyBtn.RegisterCallback<ClickEvent>(evt => OnClickCreateLobbyBtn());
            _cancelLobbyBtn.RegisterCallback<ClickEvent>(evt => OnClickCancelBtn());
            
            _loader = Root.Q<VisualElement>("lobby-loader");
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
            _uiController.CloseModal(typeof(CreateLobbyModal));
        }
        
        private void ClearFormInput() {
            _lobbyNameInput.SetValueWithoutNotify("");
        }
        
        public override void ShowLoader() {
            //Show(_lobbyStatusLabel);
            //Show(_lobbyLoader);
        }
        
        public override void HideLoader() {
            //Show(_lobbyStatusLabel);
            //Show(_lobbyLoader);
        }

        public override void Update() {
            
        }
        
        public override void RePaint() {
            
        }
    }
}