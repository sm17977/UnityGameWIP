using System.Collections.Generic;
using CustomElements;
using Unity.Netcode;
using UnityEngine;

namespace Scenes.Multiplayer.GameChat {
    public class ChatServer : NetworkBehaviour {

        private List<ChatMessage> _messages;
        private ChatBoxElement _chatUI;

        private void Start() {
            _messages = new List<ChatMessage>();
        }

        public void AddMessage(ChatMessage message, NetworkObjectReference networkObjectRef) {
            _messages.Add(message);
            var jsonMessages = JsonUtility.ToJson(_messages);
            SendChatMessagesClientRpc(jsonMessages, networkObjectRef);
        }

        public void SetUI(ChatBoxElement chatBoxElement) {
            _chatUI = chatBoxElement;
        }

        [Rpc(SendTo.NotOwner)]
        private void SendChatMessagesClientRpc(string jsonMessages, NetworkObjectReference networkObjectRef) {
            if (networkObjectRef.TryGet(out NetworkObject obj)) {
                var player = obj.gameObject;
                var messages = JsonUtility.FromJson<List<ChatMessage>>(jsonMessages);
                _chatUI.UpdateMessages(messages);
            }
        }
    }
}