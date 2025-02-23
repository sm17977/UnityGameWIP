using System.Collections.Generic;
using System.Linq;
using CustomElements;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

namespace Scenes.Multiplayer.GameChat {
    public class ChatServer : NetworkBehaviour {

        private List<ChatMessage> _messages;
        private ChatBoxElement _chatUI;

        private void Awake() {
            _messages = new List<ChatMessage>();
        }

        public void AddMessage(ChatMessage message, NetworkObjectReference networkObjectRef) {
            _messages.Add(message);
            ChatMessageListWrapper wrapper = new ChatMessageListWrapper { messages = _messages.ToArray() };
            string jsonMessages = JsonConvert.SerializeObject(wrapper);
            SendChatMessagesClientRpc(jsonMessages, networkObjectRef);
        }

        public void SetUI(ChatBoxElement chatBoxElement) {
            _chatUI = chatBoxElement;
        }

        [Rpc(SendTo.NotOwner)]
        private void SendChatMessagesClientRpc(string jsonMessages, NetworkObjectReference networkObjectRef) {
            if (networkObjectRef.TryGet(out NetworkObject obj)) {
                ChatMessageListWrapper wrapper = JsonConvert.DeserializeObject<ChatMessageListWrapper>(jsonMessages);
                _chatUI.UpdateMessages(wrapper.messages.ToList());
            }
        }

    }
}