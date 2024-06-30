using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    
    public delegate void OnCloseModal(Modal modal);
   
    public abstract class Modal : View {
        
        protected Button CloseButton;
        protected VisualElement Loader;
        
        // Loader
        private float _rotation = 0;
        private float _timer = 0;
        
        public void ShowModal() {
            ParentContainer.Add(Template);
        }
        public void HideModal() {
            ParentContainer.Remove(Template);
        }

        public abstract void ShowLoader();
        public abstract void HideLoader();
        
        private void RotateLoader() {
            _rotation += 360;
            Loader.style.rotate =
                new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle(_rotation, AngleUnit.Degree)));
        }

        public void UpdateLoader() {
            if (Loader != null) {
                if (_timer >= 1) {
                    RotateLoader();
                    _timer = 0;
                }
                else {
                    _timer += Time.deltaTime;
                }
            }
        }
    }
}