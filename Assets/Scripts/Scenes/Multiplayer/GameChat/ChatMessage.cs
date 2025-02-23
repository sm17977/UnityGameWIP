using System;

namespace Scenes.Multiplayer.GameChat {
    public class ChatMessage {
        
        public int ClientId;
        public string Timestamp;
        public string Content;
        public string PlayerName;

        public ChatMessage(int clientId, string content, string playerName) {
            ClientId = clientId;
            Content = content;
            PlayerName = playerName;
            Timestamp = Convert.ToString((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
        }
    }
}