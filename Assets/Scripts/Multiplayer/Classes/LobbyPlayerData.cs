using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace Multiplayer {
    public class LobbyPlayerData {

        private string _id;
        private string _name;
        private string _clientId;
        private bool _isConnected;
        public string Id => _id;
        public string Name => _name;
        public string ClientId {
            get => _clientId;
            set => _clientId = value;
        }

        public bool IsConnected {
            get => _isConnected;
            set => _isConnected = value;
        }
        
        public void Initialize(string id, string name, string clientId, bool isConnected = false) {
            _id = id;
            _name = name;
            _clientId = clientId;
            _isConnected = isConnected;
        }

        public void Initialize(Dictionary<string, PlayerDataObject> playerData) {
            UpdateState(playerData);   
        }
        
        private void UpdateState(Dictionary<string, PlayerDataObject> playerData) {
            if (playerData.ContainsKey("Id")) {
                _id = playerData["Id"].Value;
            }
            
            if (playerData.ContainsKey("Name")) {
                _name = playerData["Name"].Value;
            }
            
            if (playerData.ContainsKey("ClientId")) {
                _clientId = playerData["ClientId"].Value;
            }
            
            if (playerData.ContainsKey("IsConnected")) {
                _clientId = playerData["IsConnected"].Value;
            }
        }

        public Dictionary<string, string> Serialize() {
            return new Dictionary<string, string>() {
                { "Id", _id },
                { "Name", _name },
                { "ClientId", _name },
                { "IsConnected", _isConnected.ToString()}
            };
        }

        public Dictionary<string, PlayerDataObject> SerializeUpdate() {
            return new Dictionary<string, PlayerDataObject>() {
                {
                    "Id", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Member,
                        value: _id)
                }, 
                {
                    "Name", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Member,
                        value: _name)
                },
                {
                    "ClientId", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Member,
                        value: _clientId)
                },
                {
                    "IsConnected", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Member,
                        value: _isConnected.ToString())
                }
            };
        }
        
    }
}