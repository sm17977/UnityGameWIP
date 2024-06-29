using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public sealed class ViewManager : MonoBehaviour{
        
        public static ViewManager Instance = null;
        private static readonly object Padlock = new object();
        
        private static Dictionary<Type, View> _views = new Dictionary<Type, View>();
        private static View _currentView;
        
        private static Dictionary<Type, Modal> _modals = new Dictionary<Type, Modal>();
        private static Modal _currentModal;

        public View CurrentView {
            get => _currentView;
        }
        
        public Modal CurrentModal {
            get => _currentModal;
        }

        private void Awake() {
            if(Instance == null){
                Instance = this;
            }
            else if(Instance != this){
                Destroy(this);
            }
        }
        
        /// <summary>
        /// Initialize all views and modals
        /// </summary>
        /// <param name="uiController">The multiplayer UI controller</param>
        public void Initialize(MultiplayerUIController uiController) {
            
            var root = uiController.uiDocument.rootVisualElement;
            var viewContainer = root.Q<VisualElement>("view-container");
            
            _views.Add(typeof(MultiplayerMenuView), new MultiplayerMenuView(viewContainer, uiController, uiController.multiplayerMenuViewTmpl));
            _views.Add(typeof(LobbiesView), new LobbiesView(viewContainer, uiController, uiController.lobbiesViewTmpl));
            _views.Add(typeof(LobbyView), new LobbyView(viewContainer, uiController, uiController.lobbyViewTmpl));
            _views.Add(typeof(GameView), new GameView(root, uiController, uiController.gameViewTmpl));
            
            _modals.Add(typeof(CreateLobbyModal), new CreateLobbyModal(root, uiController, uiController.createLobbyModalTmpl));
            _modals.Add(typeof(ExitGameModal), new ExitGameModal(root, uiController, uiController.exitGameModalTmpl));

            ChangeView(typeof(MultiplayerMenuView));
            _currentModal = null;
        }
        
        /// <summary>
        /// Change the current view
        /// </summary>
        /// <param name="viewType">The type of view to change to</param>
        public void ChangeView(Type viewType) {
            if (_views.ContainsKey(viewType)) {
                _currentView?.Hide();
                _currentView = _views[viewType];
                _currentView?.Show();
            } else {
               Debug.LogError("View not found.");
            }
        }

        /// <summary>
        /// Update the current view
        /// </summary>
        /// <param name="viewType">The type of view to update</param>
        public void UpdateView(Type viewType) {
            if (_views.ContainsKey(viewType)) {
                _views[viewType].Update();
            } else {
                Debug.LogError("View not found.");
            }
        }
        
        /// <summary>
        /// Re-paint the current view
        /// </summary>
        /// <param name="viewType">The type of view to re-paint</param>
        public void RePaintView(Type viewType) {
            if (_views.ContainsKey(viewType)) {
                _views[viewType].RePaint();
            } else {
                Debug.LogError("View not found.");
            }
        }

        /// <summary>
        /// Open a modal
        /// </summary>
        /// <param name="modalType">The type of modal to open</param>
        public void OpenModal(Type modalType) {
            if (_modals.ContainsKey(modalType)) {
                _currentModal?.HideModal();
                _currentModal = _modals[modalType];
                _currentModal?.ShowModal();
            } else {
                Debug.LogError("Modal not found.");
            }
        }

        /// <summary>
        /// Close a modal
        /// </summary>
        /// <param name="modalType">The type of modal to close</param>
        public void CloseModal(Type modalType) {
            if (_modals.ContainsKey(modalType)) {
                _currentModal = _modals[modalType];
                _currentModal?.HideModal();
                _currentModal = null;
            } else {
                Debug.LogError("Modal not found.");
            }
        }
        
        /// <summary>
        /// Log the modal and view dictionaries
        /// </summary>
        private void LogDictionaries() {
            Debug.Log("Logging Views Dictionary:");
            foreach (var kvp in _views) {
                Debug.Log($"View Type: {kvp.Key}, View Instance: {kvp.Value}");
            }

            Debug.Log("Logging Modals Dictionary:");
            foreach (var kvp in _modals) {
                Debug.Log($"Modal Type: {kvp.Key}, Modal Instance: {kvp.Value}");
            }
        }
    }
}
