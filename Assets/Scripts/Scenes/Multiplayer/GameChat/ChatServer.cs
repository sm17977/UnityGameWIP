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
            Debug.Log("_messages length: " + _messages.Count);
            ChatMessageListWrapper wrapper = new ChatMessageListWrapper { messages = _messages.ToArray() };
            Debug.Log("wrapper.messages length: " + wrapper.messages.Length);
            string jsonMessages = JsonConvert.SerializeObject(wrapper);
            Debug.Log("jsonMessages (server RPC): " +  jsonMessages);
            SendChatMessagesClientRpc(jsonMessages, networkObjectRef);
        }

        public void SetUI(ChatBoxElement chatBoxElement) {
            _chatUI = chatBoxElement;
        }

        [Rpc(SendTo.NotOwner)]
        private void SendChatMessagesClientRpc(string jsonMessages, NetworkObjectReference networkObjectRef) {
            Debug.Log("jsonMessages (client RPC): " + jsonMessages);
            if (networkObjectRef.TryGet(out NetworkObject obj)) {
                ChatMessageListWrapper wrapper = JsonConvert.DeserializeObject<ChatMessageListWrapper>(jsonMessages);
                _chatUI.UpdateMessages(wrapper.messages.ToList());
            }
        }

    }
}