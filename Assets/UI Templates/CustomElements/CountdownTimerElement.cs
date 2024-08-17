using UnityEditor.UIElements;
using UnityEngine.UIElements;

[assembly: UxmlNamespacePrefix("CustomElements", "custom")]

namespace CustomElements {
    
    [UxmlElement]
    public partial class CountdownTimerElement : VisualElement {

        private readonly Label _countdownLabel;
        private VisualElement _countdownContainer;

        private CountdownTimerElement() {
            _countdownContainer = new VisualElement();
            _countdownLabel = new Label();
            Add(_countdownContainer);
            Add(_countdownLabel);
            _countdownLabel.AddToClassList("countdown-timer");
        }
        
        public void UpdateCountdown(int timeLeft) {
            _countdownLabel.text = timeLeft > 0 ? timeLeft.ToString() : "Go!";
        }

        public void HideCountdown() {
            _countdownLabel.text = string.Empty;
        }
        
        
        [UxmlAttribute]
        public string myString { get; set; } = "default_value";

        [UxmlAttribute]
        public int myInt { get; set; } = 2;
    
    }
}
   

