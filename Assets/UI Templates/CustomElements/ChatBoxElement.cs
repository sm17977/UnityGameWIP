using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Scenes.Multiplayer.GameChat;
using UI_Templates.Helpers;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CustomElements {
    [UxmlElement]
    public partial class ChatBoxElement : VisualElement {
        
        public Action<ChatMessage> OnEnterMessage;
        public Action<ChatMessage> OnSendMessage;
        
        private float _width;
        private float _height;
        private Color _startColor;
        private Color _endColor;
        public BlinkingTextField ChatInputField;
        
        static readonly CustomStyleProperty<Color> StartColor = new CustomStyleProperty<Color>("--start-color");
        static readonly CustomStyleProperty<Color> EndColor = new CustomStyleProperty<Color>("--end-color");
        private ScrollView _messagesScrollView;
        private List<ChatMessage> _messages;
        private LuxPlayerController _player;

        [PublicAPI]
        public ChatBoxElement() {
            generateVisualContent += GenerateVisualContent;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnStylesResolved);
            _messages = new List<ChatMessage>();
         
            _messagesScrollView = new ScrollView();
            _messagesScrollView.AddToClassList("message-container");
            
            
            ChatInputField = new BlinkingTextField();
            ChatInputField.AddToClassList("chat-input");
            
            Add(_messagesScrollView);
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            AddMessage(new ChatMessage(1, "Hello, this is a test message!", "Sean"));
            Add(ChatInputField);
        }
        
        private void GenerateVisualContent(MeshGenerationContext mgc) {
            var painter2d = mgc.painter2D;
            DrawHelper.init(painter2d);
            DrawHelper.DrawGradientRect((int)_width, (int)_height, _startColor, _endColor, DrawHelper.GradientDirection.Vertical);
        }

        private void OnStylesResolved(CustomStyleResolvedEvent evt) {
            if (evt.customStyle.TryGetValue(StartColor, out var start)
                && evt.customStyle.TryGetValue(EndColor, out var end)) {
                _startColor = start;
                _endColor = end;
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) {
            _width = resolvedStyle.width;
            _height = resolvedStyle.height;
            ChatInputField.style.width = _width;
        }

        private void AddMessage(ChatMessage message) {
            _messages.Add(message);
            AddMessageToUI(message);
        }

        private void AddMessageToUI(ChatMessage message) {
            var messageElement = new VisualElement();
            var messageLabel = new Label($"{message.PlayerName}: {message.Content}");
            messageLabel.AddToClassList("chat-message-label");
            messageElement.Add(messageLabel);
            if(!_messagesScrollView.Contains(messageLabel))_messagesScrollView.Add(messageElement);
            UIHelper.ScrollToImmediate(_messagesScrollView, _messagesScrollView.Children().Last());
        }

        public void UpdateMessages(List<ChatMessage> messages) {
            _messages = messages;
            _messages.Sort((a, b) => int.Parse(a.Timestamp).CompareTo(int.Parse(b.Timestamp)));
            _messagesScrollView.Clear();
            foreach (var message in _messages) {
                AddMessageToUI(message);
            }
        }

        private void LayoutCallback(GeometryChangedEvent evt) {
            _messagesScrollView.UnregisterCallback<GeometryChangedEvent>(LayoutCallback);
            _messagesScrollView.ScrollTo(_messagesScrollView.Children().Last());
        }
    }
}