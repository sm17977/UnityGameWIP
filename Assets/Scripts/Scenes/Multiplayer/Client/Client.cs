using UnityEngine;

namespace Multiplayer {
    public sealed class Client {
        
        private static Client _instance = null;
        private static readonly object Padlock = new object();
        
        private string _id;
        private string _name;
        private string _lobbyId;
        private bool _isLobbyHost;
        private bool _isConnectedToServer;
        public bool ServerStarted;
        
        private string _serverIp;
        private string _port;
        private string _serverStatus = "INACTIVE";

        public string ID {
            get => _id;
            set => _id = value;
        }

        public bool IsLobbyHost {
            get => _isLobbyHost;
            set => _isLobbyHost = value;
        }

        public string ServerIP {
            get => _serverIp;
            set => _serverIp = value;
        }

        public string Port {
            get => _port;
            set => _port = value;
        }

        public string ServerStatus {
            get => _serverStatus;
            set => _serverStatus = value;
        }

        public bool IsConnectedToServer {
            get => _isConnectedToServer;
            set => _isConnectedToServer = value;
        }
        
        public static Client Instance {
            get {
                lock (Padlock) {
                    Debug.Log("Getting client instance");
                    var tmp = _instance == null;
                    Debug.Log("Is client null? " + tmp);
                    _instance ??= new Client();
                    return _instance;
                }
            }
        }
        /// <summary>
        /// Make the client null to ensure next access creates a new client
        /// </summary>
        public void Delete() {
            Debug.Log("instance = null");
            _instance = null;
        }
    }
}