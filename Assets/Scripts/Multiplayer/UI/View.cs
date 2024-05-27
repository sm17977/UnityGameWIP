using UnityEngine.UIElements;

namespace Multiplayer.UI {
    
    public abstract class View {
        
        // Top level container of the view
        protected VisualElement ParentContainer;
        
        // Root document element
        protected VisualElement Root;
        
        
        public void Show() {
            ParentContainer.style.display = DisplayStyle.Flex;
        }

        public void Hide() {
            ParentContainer.style.display = DisplayStyle.None;
        }
    }
}