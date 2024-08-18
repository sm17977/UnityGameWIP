using System.Linq;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public class MessageModal : Modal {
        
        
        private VisualElement _contentContainer;
        private OuterGlow _containerShadow;

        public MessageModal(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            InitializeElements();
        }

        private void InitializeElements() {
            _contentContainer = Template.Q("message-modal-content");
            _containerShadow = Template.Q<OuterGlow>("container-shadow");
            Loader = Template.Q<VisualElement>("loader");
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
        
        
        public override void Update() {
            throw new System.NotImplementedException();
        }

        public override void RePaint() {
            throw new System.NotImplementedException();
        }
    }
}