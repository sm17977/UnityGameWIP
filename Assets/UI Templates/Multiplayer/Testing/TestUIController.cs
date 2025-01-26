using System.Collections;
using CustomElements;
using UnityEngine;
using UnityEngine.UIElements;

public class TestUIController : MonoBehaviour {
    
    [SerializeField] private RenderTexture blurredRenderTexture;
    
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
    
        // Get the BlurPanel from the UXML
        var radialCooldownElement = root.Q<RadialCooldownElement>("radial-cooldown");
        

        // Start a 10-second cooldown
        radialCooldownElement.StartCooldown(5f);
    }
    
}
