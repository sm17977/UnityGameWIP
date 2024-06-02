using System.Threading.Tasks;
using Mono.CSharp;
using UnityEngine.UIElements;

namespace Multiplayer.UI {

    public class ExitGameModal : Modal {

        private MultiplayerUIController _uiController;

        private Button _confirmExitBtn;
        private Button _cancelExitBtn;
        private VisualElement _mainContainer;

        public ExitGameModal(VisualElement parentContainer, MultiplayerUIController uiController) {
            _uiController = uiController;
            ParentContainer = parentContainer;
            var uiDocument = uiController.uiDocument;
            Root = uiDocument.rootVisualElement;
            InitializeElements();
        }
        
        private void InitializeElements() {
            _confirmExitBtn = Root.Q<Button>("confirm-exit-btn");
            _cancelExitBtn = Root.Q<Button>("cancel-exit-btn");
            _mainContainer = Root.Q<VisualElement>("main-container");
            
            _confirmExitBtn.RegisterCallback<ClickEvent>(evt => OnClickExitGameBtn());
            _cancelExitBtn.RegisterCallback<ClickEvent>(evt => OnClickCancelExitGameBtn());
        }

        private async void OnClickExitGameBtn() {
            await _uiController.DisconnectClient();
        }
        
        private void OnClickCancelExitGameBtn() {
            _uiController.CloseModal(typeof(ExitGameModal));
        }
        
        public override void ShowLoader() {

        }
        
        public override void HideLoader() {
      
        }
        
        public override void Update() {
            
        }
        
        public override void RePaint() {
            
        }
    }
}


        