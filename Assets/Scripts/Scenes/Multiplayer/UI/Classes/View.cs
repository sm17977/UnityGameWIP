using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    
    public abstract class View {
        
        // Top level container of the view
        protected VisualElement ParentContainer;
        protected VisualElement Template;
        
        /// <summary>
        /// Adds a view template to the parent container of the UI
        /// </summary>
        public virtual void Show() {
            ParentContainer.Add(Template);
        }

        /// <summary>
        /// Removes a view template from the parent container of the UI
        /// </summary>
        public virtual void Hide() {
            ParentContainer.Remove(Template);
        }

        public abstract void Update();

        public virtual void FixedUpdate(){}
        public abstract void RePaint();
        
        /// <summary>
        /// Sets the USS display value of the element to flex
        /// </summary>
        /// <param name="ele"></param>
        protected void Show(VisualElement ele) {
            if (ele != null) {
                ele.style.display = DisplayStyle.Flex;
            }
            else {
                Debug.LogError("Cannot show null visual element!");
            }
        }

        /// <summary>
        /// Sets the USS display value of the element to none
        /// </summary>
        /// <param name="ele"></param>
        protected void Hide(VisualElement ele) {
            if (ele != null) {
                ele.style.display = DisplayStyle.None;
            }
            else {
                Debug.LogError("Cannot hide null visual element!");
            }
        }

        protected void OnReturnToMultiplayerMenu() {
            MultiplayerUIController.ReturnToMultiplayerMenu();
        }
    }
}