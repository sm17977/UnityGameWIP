using System.Linq;
using System.Threading.Tasks;
using Mono.CSharp;
using UnityEngine.UIElements;

namespace Multiplayer.UI {

    public class ExitGameModal : Modal {

        private MultiplayerUIController _uiController;

        private Button _confirmExitBtn;
        private Button _cancelExitBtn;
        private VisualElement _mainContainer;

        public ExitGameModal(VisualElement parentContainer, MultiplayerUIController uiController, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();    
            ParentContainer = parentContainer;
            _uiController = uiController;
            InitializeElements();
        }
        
        private void InitializeElements() {
            _confirmExitBtn = Template.Q<Button>("confirm-exit-btn");
            _cancelExitBtn = Template.Q<Button>("cancel-exit-btn");
            _mainContainer = Template.Q<VisualElement>("main-container");
            
            _confirmExitBtn.RegisterCallback<ClickEvent>(evt => OnClickExitGameBtn());
            _cancelExitBtn.RegisterCallback<ClickEvent>(evt => OnClickCancelExitGameBtn());
        }

        private async void OnClickExitGameBtn() {
            await _uiController.DisconnectClient();
        }
        
        private void OnClickCancelExitGameBtn() {
            _uiController.OnCloseModal(this);
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


        