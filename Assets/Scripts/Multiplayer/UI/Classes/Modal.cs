using UnityEngine.UIElements;

namespace Multiplayer.UI {
   
    public abstract class Modal : View {
        
        protected Button CloseButton;

        // protected override void OnInitialize() {
        //     closeButton = _parentContainer.Q<Button>("close-button");
        //     closeButton.RegisterCallback<ClickEvent>(evt => Hide());
        // }

        public void ShowModal() {
            Show();
        }
        public void HideModal() {
            Hide();
        }

        public abstract void ShowLoader();
        public abstract void HideLoader();
        
    }
    
}