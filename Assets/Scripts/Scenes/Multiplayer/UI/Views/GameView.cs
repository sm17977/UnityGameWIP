
using System.Linq;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public class GameView : View {
        
        public GameView(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            InitializeElements();
        }

        private void InitializeElements() {
        }

        public override async void Show() {
            base.Show();
        }

        public override void Hide() {
            base.Hide();
        }
        
        public override void Update() {

        }
        
        public override void RePaint() {

        }
    }
}