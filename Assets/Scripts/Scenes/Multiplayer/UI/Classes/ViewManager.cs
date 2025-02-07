using System.Collections.Generic;
using Mono.CSharp;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public sealed class ViewManager : MonoBehaviour{
        
        public static ViewManager Instance = null;
        
        private static View _currentView;
        private static Modal _currentModal;

        private static List<View> _viewsList;
        private static List<Modal> _modalsList;

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
        /// <param name="views">List of views</param>
        /// <param name="modals">List of modals</param>
        /// <param name="defaultView">The default view to display</param>
        public void Initialize(List<View> views, List<Modal> modals, View defaultView) {
            _viewsList = views;
            _modalsList = modals;
            
            ChangeView(_viewsList[_viewsList.IndexOf(defaultView)]);
            _currentModal = null;
        }
        
        /// <summary>
        /// Change the current view
        /// </summary>
        /// <param name="view">The view to change to</param>
        public void ChangeView(View view) {
            if (_viewsList.Contains(view)) {
                _currentView?.Hide();
                if (_currentView != null) {
                    Debug.Log("Current View: " + _currentView.ToString());
                }
               
                var index = _viewsList.IndexOf(view);
                _currentView = _viewsList[index];
                _currentView?.Show();
            } else {
               Debug.LogError("View not found.");
            }
        }

        /// <summary>
        /// Update the current view
        /// </summary>
        /// <param name="view">The view to update</param>
        public void UpdateView(View view) {
            if (_viewsList.Contains(view)) {
                var index = _viewsList.IndexOf(view);
                _viewsList[index].Update();
            } else {
                Debug.LogError("View not found.");
            }
        }
        
        /// <summary>
        /// Re-paint the current view
        /// </summary>
        /// <param name="view">The view to re-paint</param>
        public void RePaintView(View view) {
            if (_viewsList.Contains(view)) {
                var index = _viewsList.IndexOf(view);
                _viewsList[index].RePaint();
            } else {
                Debug.LogError("View not found.");
            }
        }

        /// <summary>
        /// Open a modal
        /// </summary>
        /// <param name="modal">The modal to open</param>
        public void OpenModal(Modal modal) {
            if (_modalsList.Contains(modal)) {
                _currentModal?.HideModal();
                var index = _modalsList.IndexOf(modal);
                _currentModal = _modalsList[index];
                _currentModal?.ShowModal();
            } else {
                Debug.LogError("Modal not found.");
            }
        }
        
        /// <summary>
        /// Open a modal with a template
        /// </summary>
        /// <param name="modal">The modal to open</param>
        /// <param name="vta">The template to use</param>
        public void OpenModal(Modal modal, VisualTreeAsset vta) {
            if (_modalsList.Contains(modal)) {
                _currentModal?.HideModal();
                var index = _modalsList.IndexOf(modal);
                _currentModal = _modalsList[index];
                _currentModal?.ChangeTemplate(vta);
                _currentModal?.ShowModal();
            } else {
                Debug.LogError("Modal not found.");
            }
        }
        
        /// <summary>
        /// Close a modal
        /// </summary>
        /// <param name="modal">The modal to close</param>
        public void CloseModal(Modal modal) {
            if (_modalsList.Contains(modal) && _currentModal != null) {
                var index = _modalsList.IndexOf(modal);
                _currentModal = _modalsList[index];
                _currentModal?.HideModal();
                _currentModal = null;
            } else {
                Debug.LogError("Modal not found.");
            }
        }
        
        /// <summary>
        /// Close the current open modal
        /// </summary>
        /// <param name="modal">The modal to close</param>
        public void CloseModal() {
            if (CurrentModal != null) {
                CurrentModal?.HideModal();
                _currentModal = null;
            } else {
                Debug.LogError("Modal not found.");
            }
        }

        /// <summary>
        /// Change a Modal's UXML template
        /// </summary>
        /// <param name="vta">A VisualTreeAsset template</param>
        public void ChangeOpenModalTemplate(VisualTreeAsset vta) {
            if (vta != null && CurrentModal != null) {
                CurrentModal.ChangeTemplateOfOpenModal(vta);
            }
        }
        
        public void UpdateGameView() {
            if (CurrentView is GameView gameView) {
                gameView.Update();
            }
        }
        
    }
}
