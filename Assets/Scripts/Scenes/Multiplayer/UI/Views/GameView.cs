using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public class GameView : View {
        
        private MultiplayerUIController _uiController;
        
 
        public GameView(VisualElement parentContainer, MultiplayerUIController uiController, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            _uiController = uiController;
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