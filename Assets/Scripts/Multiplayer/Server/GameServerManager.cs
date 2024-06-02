using System;
using System.Threading.Tasks;
using Unity.Services.Multiplay;
using UnityEngine;

namespace Multiplayer {
    public class GameServerManager : MonoBehaviour {
      
        private readonly ServerManager _serverManager = ServerManager.Instance;
        private void Awake() {
#if DEDICATED_SERVER
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
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