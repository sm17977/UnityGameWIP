using System.Linq;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    
    public delegate void OnConfirmPlayerName(string playerName);
    public class SetPlayerNameModal : Modal {
        
        public event OnConfirmPlayerName ConfirmPlayerName;
        public event OnCloseModal CloseModal;
        
        private Button _confirmNameBtn;
        private Button _cancelNameBtn;
        private VisualElement _contentContainer;
        private OuterGlow _containerShadow;
        
        private TextField _playerNameInput;

        public SetPlayerNameModal(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            BindUIElements();
        }

        private void BindUIElements() {
            _playerNameInput = Template.Q<TextField>("player-name-input");
            
            _confirmNameBtn = Template.Q<Button>("confirm-name-btn");
            _cancelNameBtn = Template.Q<Button>("cancel-name-btn");
            
            _contentContainer = Template.Q("setname-modal-content");
            _containerShadow = Template.Q<OuterGlow>("container-shadow");
            
            _confirmNameBtn.RegisterCallback<ClickEvent>(evt => OnClickConfirmPlayerNameBtn());
            _cancelNameBtn.RegisterCallback<ClickEvent>(evt => OnClickCancelPlayerNameBtn());
        }
        
        public override async void ShowModal() {
            _contentContainer.AddToClassList("setname-modal-show-transition");
            base.ShowModal();
            await Task.Delay(200);
            _contentContainer.AddToClassList("setname-modal-active");
            _containerShadow.AddToClassList("shadow-active");
        }

        public override async void HideModal() {
            _contentContainer.AddToClassList("setname-modal-hide-transition");
            _containerShadow.RemoveFromClassList("shadow-active");
            _contentContainer.RemoveFromClassList("setname-modal-active");
            await Task.Delay(200);
            base.HideModal();
            _contentContainer.RemoveFromClassList("setname-modal-hide-transition");
        }

        private void OnClickConfirmPlayerNameBtn() {
            var playerNameInput = _playerNameInput.text;
            ConfirmPlayerName?.Invoke(playerNameInput);
            CloseModal?.Invoke(this); 
            ClearFormInput();
        }

        private void OnClickCancelPlayerNameBtn() {
            CloseModal?.Invoke(this); 
        }
        
        public override void Update() {
            throw new System.NotImplementedException();
        }

        public override void RePaint() {
            throw new System.NotImplementedException();
        }
        
        private void ClearFormInput() {
            _playerNameInput.SetValueWithoutNotify("");
            BindUIElements();
        }
    }
}