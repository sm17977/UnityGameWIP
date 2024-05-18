using System;
using System.Threading.Tasks;
using Unity.Services.Multiplay;
using UnityEngine;

namespace Multiplayer {
    public class GameServerManager : MonoBehaviour {
      
        private readonly ServerManager _serverManager = ServerManager.Instance;
        private void Awake() {
            Debug.Log("Awake");
        }

        private async void Start() {
            await _serverManager.InitializeUnityAuth();
        }

        private void Update() {
            _serverManager.UpdateServer();
        }

        private void ConnectClient() {
            
        }

    }
}