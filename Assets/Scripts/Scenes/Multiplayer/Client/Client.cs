using UnityEngine;

namespace Multiplayer {
    public sealed class Client {
        
        private static Client _instance = null;
        private static readonly object Padlock = new object();

        private string _name;
        private string _lobbyId;

        public string ID { get; set; }

        public bool IsLobbyHost { get; set; }

        public string ServerIP { get; set; }

        public string Port { get; set; }

        public string ServerStatus { get; set; } = "INACTIVE";

        public bool IsConnectedToServer { get; set; }

        public string RequestId { get; set; }

        public static Client Instance {
            get {
                lock (Padlock) {
                    var tmp = _instance == null;
                    _instance ??= new Client();
                    return _instance;
                }
            }
        }
        /// <summary>
        /// Make the client null to ensure next access creates a new client
        /// </summary>
        public void ClearConnectionData() {
            ServerIP = "";
            Port = "";
            ServerStatus = "INACTIVE";
            IsConnectedToServer = false;
        }
    }
}