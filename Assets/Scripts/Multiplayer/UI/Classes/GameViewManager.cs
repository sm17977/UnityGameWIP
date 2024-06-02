using UnityEngine;

namespace Multiplayer.UI {
    public class GameViewManager : MonoBehaviour {

        private ViewManager _viewManager;
        public ViewManager ViewManager {
            get {
                if (_viewManager == null) {
                    _viewManager = ViewManager.Instance;
                }
                return _viewManager;
            }
        }

        private void Awake() {
            _viewManager = ViewManager.Instance;
        }

    }
}