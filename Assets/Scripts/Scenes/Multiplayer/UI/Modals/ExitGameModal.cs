using System.Linq;
using System.Threading.Tasks;
using Mono.CSharp;
using UnityEngine.UIElements;

namespace Multiplayer.UI {

    public delegate Task OnDisconnectClient();
    public class ExitGameModal : Modal {

        public event OnCloseModal CloseModal;
        public event OnDisconnectClient DisconnectClient;
        
        private Button _confirmExitBtn;
        private Button _cancelExitBtn;
        
        public ExitGameModal(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();    
            ParentContainer = parentContainer;
            InitializeElements();
        }
        
        private void InitializeElements() {
            _confirmExitBtn = Template.Q<Button>("confirm-exit-btn");
            _cancelExitBtn = Template.Q<Button>("cancel-exit-btn");
            
            _confirmExitBtn.RegisterCallback<ClickEvent>(evt => OnClickExitGameBtn());
            _cancelExitBtn.RegisterCallback<ClickEvent>(evt => OnClickCancelExitGameBtn());
        }

        private async void OnClickExitGameBtn() {
            await DisconnectClient?.Invoke();
        }
        
        private void OnClickCancelExitGameBtn() {
            CloseModal?.Invoke(this); 
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


        