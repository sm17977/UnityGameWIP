using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    
    public delegate void OnCloseModal(Modal modal);
   
    public abstract class Modal : View {
        
        protected Button CloseButton;
        protected VisualElement Loader;
        private EventCallback<TransitionEndEvent> _loaderTransitionEndCallback;
        
        public virtual void ShowModal() {
            ParentContainer.Add(Template);

        }
        public virtual void HideModal() {
            ParentContainer.Remove(Template);
        }

        /// <summary>
        /// Change the Modal's UXML current template
        /// </summary>
        public virtual void ChangeTemplateOfOpenModal(VisualTreeAsset vta) { }

        protected void ShowLoader() {
            if (Loader != null) {
                if (_loaderTransitionEndCallback == null) {
                    _loaderTransitionEndCallback = (evt => {
                        Loader.ToggleInClassList("loader-transition");

                        Loader.style.rotate =
                            new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle(0, AngleUnit.Degree)));

                        Loader.schedule.Execute(() => Loader.ToggleInClassList("loader-transition")).StartingIn(1);

                        Loader.schedule.Execute(() => {
                            Loader.style.rotate =
                                new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle(360, AngleUnit.Degree)));
                        }).StartingIn(30);
                    });

                    Show(Loader);
                    Loader.ToggleInClassList("loader-transition");
                    Loader.ToggleInClassList("loader-rotation");
                }
                
                Loader.RegisterCallback(_loaderTransitionEndCallback);
            }
        }
        
        protected void HideLoader() {
            if (Loader != null) {
                if (_loaderTransitionEndCallback != null) {
                    Loader.UnregisterCallback(_loaderTransitionEndCallback);
                    _loaderTransitionEndCallback = null; 
                }
                Hide(Loader);
            }
        }
        
        public void ChangeTemplate(VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
        }
    }
}

