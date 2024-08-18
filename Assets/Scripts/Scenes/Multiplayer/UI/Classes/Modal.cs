using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    
    public delegate void OnCloseModal(Modal modal);
   
    public abstract class Modal : View {
        
        protected Button CloseButton;
        protected VisualElement Loader;
        
        public virtual void ShowModal() {
            ParentContainer.Add(Template);

        }
        public virtual void HideModal() {
            ParentContainer.Remove(Template);
        }

        protected void ShowLoader() {
            if (Loader != null) {
                
                // Handles the looping of the loader rotation transition
                Loader.RegisterCallback<TransitionEndEvent>(evt => {
                    Loader.ToggleInClassList("loader-transition");

                    Loader.style.rotate =
                        new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle(0, AngleUnit.Degree)));
                    
                    Loader.schedule.Execute(() => Loader.ToggleInClassList("loader-transition")).StartingIn(1);
                    
                    Loader.schedule.Execute(() => {
                        Loader.style.rotate =
                            new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle(360, AngleUnit.Degree)));
                    }).StartingIn(20);
                });
                
                Show(Loader);
                Loader.ToggleInClassList("loader-transition");
                Loader.ToggleInClassList("loader-rotation");
            }
        }

        protected void HideLoader() {
            if(Loader != null) Hide(Loader);
        }
    }
}

