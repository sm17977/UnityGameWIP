using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[assembly: UxmlNamespacePrefix("CustomElements", "custom")]

namespace CustomElements {
    
    [UxmlElement]
    public partial class CountdownTimerElement : VisualElement {

        private readonly Label _countdownLabel;
        private VisualElement _countdownLabelContainer;
        
        public CountdownTimerElement() {

            Debug.Log("CountdownTimerElement Constructor");
            
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

        public void ShowCountdown() {
            AddToClassList("countdown-container");
            _countdownLabelContainer.style.visibility = Visibility.Visible;
        }

        public void HideCountdown() {
            _countdownLabel.text = string.Empty;
            RemoveFromClassList("countdown-container");
            _countdownLabelContainer.style.visibility = Visibility.Hidden;
        }
        
        
        [UxmlAttribute]
        public string myString { get; set; } = "default_value";

        [UxmlAttribute]
        public int myInt { get; set; } = 2;
    
    }
}
   

