using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace Multiplayer {
    public class LobbyPlayerData {
        
        public string Id { get; set; }
        public string Name { get; set; }
        public string ClientId { get; set; }
        public bool IsConnected { get; set; }
        public bool IsReady { get; set; }
        public bool IsAlive { get; set; }
        
        
        public void Initialize(string id, string name, string clientId, bool isConnected = false, bool isReady = false, bool isAlive = true) {
            Id = id;
            Name = name;
            ClientId = clientId;
            IsConnected = isConnected;
            IsReady = isReady;
            IsAlive = isAlive;
        }

        public void Initialize(Dictionary<string, PlayerDataObject> playerData) {
            UpdateState(playerData);   
        }
        
        private void UpdateState(Dictionary<string, PlayerDataObject> playerData) {
            if (playerData.ContainsKey("Id")) {
                Id = playerData["Id"].Value;
            }
            
            if (playerData.ContainsKey("Name")) {
                Name = playerData["Name"].Value;
            }
            
            if (playerData.ContainsKey("ClientId")) {
                ClientId = playerData["ClientId"].Value;
            }
            
            if (playerData.ContainsKey("IsConnected")) {
                IsConnected= bool.Parse(playerData["IsConnected"].Value);
            }
            
            if (playerData.ContainsKey("IsReady")) {
                IsReady = bool.Parse(playerData["IsReady"].Value);
            }
            
            if (playerData.ContainsKey("IsAlive")) {
                IsAlive = bool.Parse(playerData["IsAlive"].Value);
            }
        }
        
        public Dictionary<string, string> Serialize() {
            return new Dictionary<string, string>() {
                { "Id", Id },
                { "Name", Name },
                { "ClientId", ClientId },
                { "IsConnected", IsConnected.ToString()},
                { "IsReady", IsReady.ToString()},
                { "IsAlive", IsAlive.ToString()},
            };
        }

        public Dictionary<string, PlayerDataObject> SerializeUpdate() {
            return new Dictionary<string, PlayerDataObject>() {
                {
                    "Id", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: Id)
                }, 
                {
                    "Name", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: Name)
                },
                {
                    "ClientId", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: ClientId)
                },
                {
                    "IsConnected", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: IsConnected.ToString())
                },
                {
                    "IsReady", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: IsReady.ToString())
                },
                {
                    "IsAlive", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: IsAlive.ToString())
                }
            };
        }
        
    }
}