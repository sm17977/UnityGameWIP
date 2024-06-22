using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    
    public abstract class View {
        
        // Top level container of the view
        protected VisualElement ParentContainer;
        protected VisualElement Template;
        
        // Root document element
        protected VisualElement Root;
        
        public virtual void Show() {
            ParentContainer.Add(Template);
        }

        public virtual void Hide() {
            ParentContainer.Remove(Template);
        }

       public abstract void Update();
       public abstract void RePaint();
        
        public void Show(VisualElement ele) {
            ele.style.display = DisplayStyle.Flex;
        }

        public void Hide(VisualElement ele) {
            ele.style.display = DisplayStyle.None;
        }
    }
}