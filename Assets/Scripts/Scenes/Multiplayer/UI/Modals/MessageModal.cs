using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public class MessageModal : Modal {

        private Label _headerLabel;
        private Label _bodyLabel;
        private Button _modalBtn;
        private VisualElement _contentContainer;
        private OuterGlow _containerShadow;
        private ModalType _messageModalType;
        
        public enum ModalType {
            SignInFailed,
            SignInConnecting,
        }

        public MessageModal(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            InitializeElements();
        }

        private void InitializeElements() {

            _messageModalType = GetModalType(Template.name);
            
            _contentContainer = Template.Q("message-modal-content");
            _containerShadow = Template.Q<OuterGlow>("container-shadow");
            _headerLabel = Template.Q<Label>("message-modal-header-label");
            _bodyLabel = Template.Q<Label>("message-modal-body-label");

            if (_messageModalType == ModalType.SignInConnecting) {
                Loader = Template.Q<VisualElement>("loader");
            }

            if (_messageModalType == ModalType.SignInFailed) {
                _modalBtn = Template.Q<Button>("message-modal-btn");
                _modalBtn.RegisterCallback<ClickEvent>( async (evt) => {
                    HideModal();
                    await Task.Delay(200);
                    SceneManager.LoadScene("Main Menu");
                });
            }
        }

        private ModalType GetModalType(string name) {
            return name switch {
                "message-modal-signin-connecting" => ModalType.SignInConnecting,
                "message-modal-signin-failed" => ModalType.SignInFailed,
                _ => throw new ArgumentException($"Unknown modal type: {name}")
            };
        }

        public override async void ShowModal() {
            _contentContainer.AddToClassList("message-modal-show-transition");
            base.ShowModal();
            await Task.Delay(200);
            _contentContainer.AddToClassList("message-modal-active");
            _containerShadow.AddToClassList("shadow-active");
            ShowLoader();
        }

        public override async void HideModal() {
            _contentContainer.AddToClassList("message-modal-hide-transition");
            _containerShadow.RemoveFromClassList("shadow-active");
            _contentContainer.RemoveFromClassList("message-modal-active");
            await Task.Delay(200);
            base.HideModal();
            _contentContainer.RemoveFromClassList("message-modal-hide-transition");
            HideLoader();
        }
        
        public override async void ChangeTemplate(VisualTreeAsset vta) {
            HideModal();
            await Task.Delay(200);
            Template = vta.Instantiate().Children().FirstOrDefault();
            InitializeElements();
            await Task.Delay(200);
            ShowModal();
        }
        
        public override void Update() {
            throw new System.NotImplementedException();
        }

        public override void RePaint() {
            throw new System.NotImplementedException();
        }
    }
}