using System;
using Scenes.Multiplayer.GameChat;
using UnityEngine;
using UnityEngine.UIElements;


namespace CustomElements {
    [UxmlElement]
    public partial class BlinkingTextField : TextField {
        
        private readonly IVisualElementScheduledItem _blink;

        private long _blinkInterval = 500;
        private bool _isBlinkEnabled = true;
        private string _blinkStyle = "cursor-transparent";
        private readonly string _defaultText = "Press Enter to Chat";
        
        /// <summary>
        /// Caret blink interval in ms.
        /// </summary>
        public long BlinkInterval {
            get => _blinkInterval;
            set {
                _blinkInterval = value;
                _blink?.Every(_blinkInterval);
            }
        }

        /// <summary>
        /// Caret uss style applied on blink.
        /// </summary>
        public string BlinkStyle {
            get => _blinkStyle;
            set => _blinkStyle = value;
        }

        /// <summary>
        /// If true, caret blinks.
        /// </summary>
        public bool BlinkEnable {
            get => _isBlinkEnabled;
            set {
                if (_isBlinkEnabled == value)
                    return;

                _isBlinkEnabled = value;

                if (!_isBlinkEnabled) {
                    if (IsFocused)
                        _blink?.Pause();

                    if (ClassListContains(_blinkStyle))
                        RemoveFromClassList(_blinkStyle);
                }
                else if (IsFocused) {
                    _blink?.Resume();
                }
            }
        }

        /// <summary>
        /// Returns true if active input.
        /// </summary>
        public bool IsFocused => focusController?.focusedElement == this;

        public BlinkingTextField() {
            textEdition.placeholder = _defaultText;
            RegisterCallback<FocusEvent>(OnFocus);
            RegisterCallback<BlurEvent>(OnInputEnded);

            _blink = schedule.Execute(() => {
                if (ClassListContains(_blinkStyle))
                    RemoveFromClassList(_blinkStyle);
                else
                    AddToClassList(_blinkStyle);
            }).Every(_blinkInterval);

            _blink.Pause();
        }

        private void OnFocus(FocusEvent evt) {
            if (!_isBlinkEnabled)
                return;

            Debug.Log("Text Field is focussed: " + evt.target);
            textEdition.placeholder = "";
            _blink.Resume();
        }
        
        private void OnInputEnded(BlurEvent evt) {
            Debug.Log("Text Field is un-focussed");
            textEdition.placeholder = _defaultText;
            _blink.Pause();
        }
    }
}