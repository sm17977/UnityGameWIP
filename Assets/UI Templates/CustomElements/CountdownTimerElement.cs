using Global.Game_Modes;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[assembly: UxmlNamespacePrefix("CustomElements", "custom")]

namespace CustomElements {
    
    [UxmlElement]
    public partial class CountdownTimerElement : VisualElement {

        private readonly Label _countdownLabel;
        private VisualElement _countdownLabelContainer;
        private Arena arena;
        
        
        public CountdownTimerElement() {
            
            
            // _countdownContainer = new VisualElement();
            // _countdownContainer.AddToClassList("countdown-container");
            
            AddToClassList("countdown-container");
            
            _countdownLabelContainer = new VisualElement();
            _countdownLabelContainer.AddToClassList("countdown-label-container");
            
            _countdownLabel = new Label();
            _countdownLabel.AddToClassList("countdown-label");
            
            Add(_countdownLabelContainer);
            _countdownLabelContainer.Add(_countdownLabel);
        }
        
        public void UpdateCountdown(int timeLeft) {
            _countdownLabel.text = timeLeft > 0 ? timeLeft.ToString() : "Go!";
        }

        public void HideCountdown() {
            _countdownLabel.text = string.Empty;
            RemoveFromClassList("countdown-container");
        }
        
        
        [UxmlAttribute]
        public string myString { get; set; } = "default_value";

        [UxmlAttribute]
        public int myInt { get; set; } = 2;
    
    }
}
   

