using UnityEngine.UIElements;

namespace Multiplayer.UI {
    
    public abstract class View {
        
        // Top level container of the view
        protected VisualElement ParentContainer;
        protected VisualElement Template;
        
        public virtual void Show() {
            ParentContainer.Add(Template);
        }

        public virtual void Hide() {
            ParentContainer.Remove(Template);
        }

        public abstract void Update();
        public abstract void RePaint();
        
        protected void Show(VisualElement ele) {
            ele.style.display = DisplayStyle.Flex;
        }

        protected void Hide(VisualElement ele) {
            ele.style.display = DisplayStyle.None;
        }

        protected void OnReturnToMultiplayerMenu() {
            MultiplayerUIController.ReturnToMultiplayerMenu();
        }
    }
}