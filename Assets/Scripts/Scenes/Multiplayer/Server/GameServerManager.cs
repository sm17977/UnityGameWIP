using Unity.Netcode;
using UnityEngine;

namespace Multiplayer {
    public class GameServerManager : MonoBehaviour {

        private static GameServerManager _instance;
        private ServerManager _serverManager;
        private GameObject _spawnPoints;
        private void Awake() {
            // This code is critical, if we don't set the target frame rate it will introduce sync issues which
            // are very noticeable when players cast projectiles
            #if DEDICATED_SERVER
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;
           
                if(_instance == null){
                    _instance = this;
                }
                else if(_instance != this){
                    Destroy(this);
                }
                
                _serverManager = ServerManager.Instance;
                _spawnPoints = GameObject.Find("Spawn Points");
                if (_spawnPoints == null) {
                    Debug.Log("_spawnPoints is null!");
                }
                else {
                    _serverManager.Initialize(_spawnPoints); 
                }
             
            #else
                gameObject.SetActive(false);
                return;
            #endif
        }

        private async void Start() {
            await _serverManager.InitializeUnityAuth();
        }

        private void Update() {
            _serverManager.UpdateServer();
        }
    }
}