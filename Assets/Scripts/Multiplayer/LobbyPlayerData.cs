using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace Multiplayer {
    public class LobbyPlayerData {

        private string _id;
        private string _name;

        public string Id => _id;
        public string Name => _name;
        
        public void Initialize(string id, string name) {
            _id = id;
            _name = name;
        }

        public void Initialize(Dictionary<string, PlayerDataObject> playerData) {
            UpdateState(playerData);   
        }
        
        public void UpdateState(Dictionary<string, PlayerDataObject> playerData) {
            if (playerData.ContainsKey("Id")) {
                _id = playerData["Id"].Value;
            }
            
            if (playerData.ContainsKey("name")) {
                _name = playerData["name"].Value;
            }
        }

        public Dictionary<string, string> Serialize() {
            return new Dictionary<string, string>() {
                { "Id", _id },
                { "name", _name }
            };
        }
    }
}