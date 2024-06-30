using System.Linq;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public delegate Task OnCreateLobby(string lobbyName);
    public class CreateLobbyModal : Modal {
        public event OnCreateLobby CreateLobby;
        
        public event OnCloseModal CloseModal;
        
        // Create Lobby Modal
        private Button _createLobbyBtn;
        private Button _cancelLobbyBtn;
        private TextField _lobbyNameInput;
        
        public CreateLobbyModal(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
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
            await CreateLobby?.Invoke(lobbyNameInput);
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