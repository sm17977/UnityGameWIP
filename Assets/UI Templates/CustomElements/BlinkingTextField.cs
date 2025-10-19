using System;
using Scenes.Multiplayer.GameChat;
using UnityEngine;
using UnityEngine.UIElements;


namespace CustomElements {
    [UxmlElement]
    public partial class BlinkingTextField : TextField {
        
        private readonly IVisualElementScheduledItem _blink;
        
        private Color _caretColor = Color.white;
        private StyleSheet _defaultStyle;
        
        private long _blinkInterval = 500;
        private bool _isBlinkEnabled = true;
        private string _blinkStyle = "cursor-transparent";
        private readonly string _defaultText = "Press Enter to Chat";

        [UxmlAttribute("caret-color")]
        public Color CaretColor {
            get => _caretColor;
            set {
                _caretColor = value;
                UpdateCaretColor();
            }
        }


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
        
        protected override void ExecuteDefaultAction(EventBase evt) {
            if ((evt is KeyDownEvent keyEvent && keyEvent.keyCode == KeyCode.Return)
                || (evt is KeyUpEvent keyUpEvent && keyUpEvent.keyCode == KeyCode.Return)) {
                return;
            }
        
            base.ExecuteDefaultAction(evt);
        }


        private void OnFocus(FocusEvent evt) {

            parent.MarkDirtyRepaint();
            
            if (!_isBlinkEnabled)
                return;
            
            textEdition.placeholder = "";
            _blink.Resume();
        }
        
        private void OnInputEnded(BlurEvent evt) {
            parent.MarkDirtyRepaint();
            textEdition.placeholder = _defaultText;
            _blink.Pause();
        }

        private void UpdateCaretColor() {
            textSelection.cursorColor = _caretColor;
        }
    }
}