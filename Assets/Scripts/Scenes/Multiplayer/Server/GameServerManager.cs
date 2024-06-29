using UnityEngine;

namespace Multiplayer {
    public class GameServerManager : MonoBehaviour {

        private static GameServerManager _instance;
        private ServerManager _serverManager;
        private void Awake() {
            #if DEDICATED_SERVER
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;
            #endif
            
            if(_instance == null){
                _instance = this;
            }
            else if(_instance != this){
                Destroy(this);
            }
            
            _serverManager = ServerManager.Instance;
        }

        private async void Start() {
            await _serverManager.InitializeUnityAuth();
        }

        private void Update() {
            _serverManager.UpdateServer();
        }
    }
}